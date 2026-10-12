using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Infrastructure.Ai.Contracts;
using FableFlow.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace FableFlow.Infrastructure.Ai;

/// <summary>
/// Génère les chapitres et idées de départ via Azure OpenAI (modèle orchestrateur, ex. gpt-4o).
/// Demande une sortie JSON stricte afin de mapper de façon fiable vers <see cref="GeneratedChapter"/>.
/// Les réponses brutes ne sont jamais journalisées : elles peuvent reprendre des données saisies
/// par le parent (prénom, centres d'intérêt).
/// </summary>
public sealed class AzureOpenAIStoryGenerationService : IStoryGenerationService
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

  // Modèles de raisonnement (ex. famille gpt-5.x) : un effort par défaut non nul ajoute une
  // latence significative ("réflexion" interne avant la réponse visible), inutile pour une tâche
  // d'écriture créative structurée. ChatCompletionOptions n'expose pas encore ce paramètre dans la
  // version stable actuelle du SDK (Azure.AI.OpenAI 2.1.0) : on l'injecte via l'appel de protocole
  // bas niveau (BinaryContent). La valeur est configurable (AzureOpenAI:ReasoningEffort) car les
  // valeurs acceptées varient selon la famille de modèle, et un modèle sans raisonnement la refuse.
  // NB : ces modèles refusent aussi toute température/pénalité non-défaut (HTTP 400
  // "unsupported_value" constaté en pratique) : la variété narrative repose donc uniquement sur les
  // instructions du prompt, pas sur ces paramètres d'échantillonnage.
  private sealed record ChatTarget(ChatClient Client, string? ReasoningEffort);

  private readonly ChatTarget _chapterTarget;
  private readonly ChatTarget _premiseTarget;
  private readonly ILogger<AzureOpenAIStoryGenerationService> _logger;

  public AzureOpenAIStoryGenerationService(
      IOptions<AzureOpenAIOptions> options,
      ILogger<AzureOpenAIStoryGenerationService> logger)
  {
    _logger = logger;

    var settings = options.Value;
    var azureClient = AzureOpenAIClientFactory.Create(settings);
    var chapterEffort = NormalizeEffort(settings.ReasoningEffort);

    _chapterTarget = new ChatTarget(azureClient.GetChatClient(settings.ChatDeployment), chapterEffort);
    _premiseTarget = new ChatTarget(
        azureClient.GetChatClient(string.IsNullOrWhiteSpace(settings.PremiseDeployment)
            ? settings.ChatDeployment
            : settings.PremiseDeployment),
        settings.PremiseReasoningEffort is null ? chapterEffort : NormalizeEffort(settings.PremiseReasoningEffort));
  }

  public async Task<GeneratedChapter> GenerateChapterAsync(StoryPrompt prompt, CancellationToken cancellationToken)
  {
    string rawJson;
    try
    {
      rawJson = await CompleteChatAsync(_chapterTarget, prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour le chapitre {ChapterNumber} ({Version})",
          prompt.ChapterNumber, prompt.TemplateVersion);
      throw;
    }

    return ParseChapter(rawJson);
  }

  public async Task<IReadOnlyList<GeneratedPremise>> GeneratePremisesAsync(
      StoryPremisePrompt prompt,
      CancellationToken cancellationToken)
  {
    string rawJson;
    try
    {
      rawJson = await CompleteChatAsync(_premiseTarget, prompt.SystemPrompt, prompt.UserPrompt, cancellationToken);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour la génération d'idées de départ ({Version})",
          prompt.TemplateVersion);
      throw;
    }

    return ParsePremises(rawJson);
  }

  /// <summary>
  /// Appelle l'endpoint Chat Completions via la méthode de protocole bas niveau (corps de requête
  /// brut) afin de pouvoir positionner "reasoning_effort", non encore exposé par
  /// <see cref="ChatCompletionOptions"/> dans cette version du SDK. Retourne le contenu textuel du
  /// message de réponse (le JSON de chapitre/idées demandé au LLM).
  /// </summary>
  private async Task<string> CompleteChatAsync(
      ChatTarget target,
      string systemPrompt,
      string userPrompt,
      CancellationToken cancellationToken)
  {
    // Température et pénalités volontairement omises (null) : les modèles de raisonnement refusent
    // toute valeur non-défaut ; la variété narrative repose uniquement sur les instructions du
    // prompt (voir PromptTemplateRegistry.ChapterSystemPrompt, règle "Sois créatif").
    var requestBody = new RawChatCompletionRequest
    {
      Messages =
      [
          new RawChatCompletionRequestMessage { Role = "system", Content = systemPrompt },
          new RawChatCompletionRequestMessage { Role = "user", Content = userPrompt }
      ],
      ReasoningEffort = target.ReasoningEffort
    };

    var requestJson = JsonSerializer.Serialize(requestBody, _jsonOptions);
    var content = BinaryContent.Create(BinaryData.FromString(requestJson));
    var requestOptions = new RequestOptions { CancellationToken = cancellationToken };

    var result = await target.Client.CompleteChatAsync(content, requestOptions);
    var responseJson = result.GetRawResponse().Content.ToString();

    RawChatCompletionResponse? parsed;
    try
    {
      parsed = JsonSerializer.Deserialize<RawChatCompletionResponse>(responseJson, _jsonOptions);
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Réponse Azure OpenAI non conforme au format attendu ({Length} caractères).", responseJson.Length);
      throw new InvalidOperationException("La réponse Azure OpenAI n'est pas un JSON valide.", ex);
    }

    var messageContent = parsed?.Choices.FirstOrDefault()?.Message.Content;
    if (string.IsNullOrWhiteSpace(messageContent))
    {
      throw new InvalidOperationException("La réponse Azure OpenAI ne contient aucun contenu de message.");
    }

    return messageContent;
  }

  private static string? NormalizeEffort(string? effort) =>
      string.IsNullOrWhiteSpace(effort) ? null : effort.Trim();

  private GeneratedPremise[] ParsePremises(string rawJson)
  {
    RawPremisesResponse? raw;
    try
    {
      raw = JsonSerializer.Deserialize<RawPremisesResponse>(rawJson, _jsonOptions);
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Réponse LLM non conforme au format JSON attendu ({Length} caractères).", rawJson.Length);
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

  private GeneratedChapter ParseChapter(string rawJson)
  {
    RawGeneratedChapter? raw;
    try
    {
      raw = JsonSerializer.Deserialize<RawGeneratedChapter>(rawJson, _jsonOptions);
    }
    catch (JsonException ex)
    {
      // Le contenu brut peut reprendre des données saisies par le parent : il n'est pas journalisé.
      _logger.LogError(ex, "Réponse LLM non conforme au format JSON attendu ({Length} caractères).", rawJson.Length);
      throw new InvalidOperationException("La réponse du LLM n'est pas un JSON valide.", ex);
    }

    if (raw is null || string.IsNullOrWhiteSpace(raw.Text))
    {
      throw new InvalidOperationException("La réponse du LLM ne contient pas de texte de chapitre.");
    }

    var choices = raw.Choices
        .Where(c => c is not null)
        .Select(c => new GeneratedChoice(c.Id ?? string.Empty, c.Label ?? string.Empty))
        .ToArray();

    var imagePrompt = raw.ImagePrompt;
    if (string.IsNullOrWhiteSpace(imagePrompt))
    {
      _logger.LogWarning("Le LLM n'a pas fourni de champ 'imagePrompt' ; l'illustration du chapitre sera indisponible.");
      imagePrompt = string.Empty;
    }

    return new GeneratedChapter(
        raw.Text,
        choices,
        raw.UpdatedSummary ?? string.Empty,
        raw.KeyFacts?.Where(f => !string.IsNullOrWhiteSpace(f)).ToArray() ?? [],
        imagePrompt,
        raw.StoryOutline?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? [],
        string.IsNullOrWhiteSpace(raw.Title) ? null : raw.Title.Trim());
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
