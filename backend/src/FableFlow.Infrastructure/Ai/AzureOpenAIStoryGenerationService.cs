using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Enums;
using FableFlow.Infrastructure.Ai.Contracts;
using FableFlow.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace FableFlow.Infrastructure.Ai;

/// <summary>
/// Génère les scènes narratives via Azure OpenAI (modèle orchestrateur, ex. gpt-4o).
/// Demande une sortie JSON stricte afin de mapper de façon fiable vers <see cref="GeneratedScene"/>.
/// </summary>
public sealed class AzureOpenAIStoryGenerationService : IStoryGenerationService
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

  // Modèles de raisonnement (ex. famille gpt-5.x) : un effort par défaut non nul ajoute une
  // latence significative ("réflexion" interne avant la réponse visible), inutile pour une tâche
  // d'écriture créative structurée. ChatCompletionOptions n'expose pas encore ce paramètre dans la
  // version stable actuelle du SDK (Azure.AI.OpenAI 2.1.0) : on l'injecte via l'appel de protocole
  // bas niveau (BinaryContent) plutôt que d'attendre une prochaine version stable du SDK.
  // NB : ces modèles refusent aussi toute température/pénalité non-défaut (HTTP 400
  // "unsupported_value" constaté en pratique) : la variété narrative repose donc uniquement sur les
  // instructions du prompt, pas sur ces paramètres d'échantillonnage.
  private const string _reasoningEffort = "minimal";

  private readonly ChatClient _chatClient;
  private readonly ILogger<AzureOpenAIStoryGenerationService> _logger;

  public AzureOpenAIStoryGenerationService(
      IOptions<AzureOpenAIOptions> options,
      ILogger<AzureOpenAIStoryGenerationService> logger)
  {
    _logger = logger;

    var settings = options.Value;
    var azureClient = AzureOpenAIClientFactory.Create(settings);
    _chatClient = azureClient.GetChatClient(settings.ChatDeployment);
  }

  public async Task<GeneratedScene> GenerateSceneAsync(StoryPrompt prompt, CancellationToken cancellationToken)
  {
    string rawJson;
    try
    {
      rawJson = await CompleteChatAsync(prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour la génération de scène ({Version})",
          prompt.TemplateVersion);
      throw;
    }

    return ParseScene(rawJson);
  }

  public async Task<IReadOnlyList<GeneratedPremise>> GeneratePremisesAsync(
      StoryPremisePrompt prompt,
      CancellationToken cancellationToken)
  {
    string rawJson;
    try
    {
      rawJson = await CompleteChatAsync(prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour la génération de propositions d'axe narratif ({Version})",
          prompt.TemplateVersion);
      throw;
    }

    return ParsePremises(rawJson);
  }

  /// <summary>
  /// Appelle l'endpoint Chat Completions via la méthode de protocole bas niveau (corps de requête
  /// brut) afin de pouvoir positionner "reasoning_effort", non encore exposé par
  /// <see cref="ChatCompletionOptions"/> dans cette version du SDK. Retourne le contenu textuel du
  /// message de réponse (le JSON de scène/propositions demandé au LLM), inchangé par rapport à
  /// l'ancien appel fortement typé.
  /// </summary>
  private async Task<string> CompleteChatAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
  {
    // gpt-5.4-mini est un modèle de raisonnement : il refuse toute température/pénalité différente
    // de sa valeur par défaut (HTTP 400 "unsupported_value" constaté en pratique). Ces champs sont
    // donc volontairement omis (null) ; la variété narrative repose uniquement sur les instructions
    // du prompt (voir PromptTemplateRegistry.SceneSystemPrompt, règle "Sois créatif et inventif").
    var requestBody = new RawChatCompletionRequest
    {
      Messages =
      [
          new RawChatCompletionRequestMessage { Role = "system", Content = systemPrompt },
                new RawChatCompletionRequestMessage { Role = "user", Content = userPrompt }
      ],
      ReasoningEffort = _reasoningEffort
    };

    var requestJson = JsonSerializer.Serialize(requestBody, _jsonOptions);
    var content = BinaryContent.Create(BinaryData.FromString(requestJson));
    var requestOptions = new RequestOptions { CancellationToken = cancellationToken };

    var result = await _chatClient.CompleteChatAsync(content, requestOptions);
    var responseJson = result.GetRawResponse().Content.ToString();

    RawChatCompletionResponse? parsed;
    try
    {
      parsed = JsonSerializer.Deserialize<RawChatCompletionResponse>(responseJson, _jsonOptions);
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Réponse Azure OpenAI non conforme au format attendu : {RawJson}", responseJson);
      throw new InvalidOperationException("La réponse Azure OpenAI n'est pas un JSON valide.", ex);
    }

    var messageContent = parsed?.Choices.FirstOrDefault()?.Message.Content;
    if (string.IsNullOrWhiteSpace(messageContent))
    {
      throw new InvalidOperationException("La réponse Azure OpenAI ne contient aucun contenu de message.");
    }

    return messageContent;
  }

  private GeneratedPremise[] ParsePremises(string rawJson)
  {
    RawPremisesResponse? raw;
    try
    {
      raw = JsonSerializer.Deserialize<RawPremisesResponse>(rawJson, _jsonOptions);
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Réponse LLM non conforme au format JSON attendu : {RawJson}", rawJson);
      throw new InvalidOperationException("La réponse du LLM n'est pas un JSON valide.", ex);
    }

    if (raw is null || raw.Premises.Count == 0)
    {
      throw new InvalidOperationException("La réponse du LLM ne contient aucune proposition d'axe narratif.");
    }

    return raw.Premises
        .Where(p => !string.IsNullOrWhiteSpace(p.Title) && !string.IsNullOrWhiteSpace(p.Hook))
        .Select(p => new GeneratedPremise(p.Title, p.Hook))
        .ToArray();
  }

  private GeneratedScene ParseScene(string rawJson)
  {
    RawGeneratedScene? raw;
    try
    {
      raw = JsonSerializer.Deserialize<RawGeneratedScene>(rawJson, _jsonOptions);
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Réponse LLM non conforme au format JSON attendu : {RawJson}", rawJson);
      throw new InvalidOperationException("La réponse du LLM n'est pas un JSON valide.", ex);
    }

    if (raw is null || string.IsNullOrWhiteSpace(raw.Text))
    {
      throw new InvalidOperationException("La réponse du LLM ne contient pas de texte de scène.");
    }

    var choices = raw.Choices
        .Select(c => new GeneratedChoice(
            c.Id,
            c.Label,
            Enum.TryParse<ChoiceOutcome>(c.Outcome, ignoreCase: true, out var outcome)
                ? outcome
                : ChoiceOutcome.Neutral))
        .ToArray();

    var imagePrompt = raw.ImagePrompt;
    if (string.IsNullOrWhiteSpace(imagePrompt))
    {
      _logger.LogWarning("Le LLM n'a pas fourni de champ 'imagePrompt' ; utilisation d'une description générique de repli.");
      imagePrompt = "Illustration générique d'une scène d'aventure, sans personnage ni marque identifiable.";
    }

    return new GeneratedScene(raw.Text, choices, raw.UpdatedSummary, raw.KeyFacts, imagePrompt, raw.StoryOutline);
  }
}

/// <summary>Fabrique un <see cref="AzureOpenAIClient"/> à partir de la configuration (clé API ou Managed Identity).</summary>
internal static class AzureOpenAIClientFactory
{
  public static AzureOpenAIClient Create(AzureOpenAIOptions settings)
  {
    if (string.IsNullOrWhiteSpace(settings.Endpoint))
    {
      throw new InvalidOperationException("La configuration 'AzureOpenAI:Endpoint' est requise.");
    }

    var endpoint = new Uri(settings.Endpoint);

    if (!settings.UseManagedIdentity && !string.IsNullOrWhiteSpace(settings.ApiKey))
    {
      return new AzureOpenAIClient(endpoint, new ApiKeyCredential(settings.ApiKey));
    }

    TokenCredential credential = new DefaultAzureCredential();
    return new AzureOpenAIClient(endpoint, credential);
  }
}
