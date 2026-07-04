using System.ClientModel;
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
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
    var messages = new ChatMessage[]
    {
            new SystemChatMessage(prompt.SystemPrompt),
            new UserChatMessage(prompt.UserPrompt)
    };

    var chatOptions = new ChatCompletionOptions
    {
      ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
    };

    ClientResult<ChatCompletion> completion;
    try
    {
      completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour la génération de scène ({Version})",
          prompt.TemplateVersion);
      throw;
    }

    var rawJson = completion.Value.Content[0].Text;
    return ParseScene(rawJson);
  }

  public async Task<IReadOnlyList<GeneratedPremise>> GeneratePremisesAsync(
      StoryPremisePrompt prompt,
      CancellationToken cancellationToken)
  {
    var messages = new ChatMessage[]
    {
            new SystemChatMessage(prompt.SystemPrompt),
            new UserChatMessage(prompt.UserPrompt)
    };

    var chatOptions = new ChatCompletionOptions
    {
      ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
    };

    ClientResult<ChatCompletion> completion;
    try
    {
      completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Échec de l'appel Azure OpenAI pour la génération de propositions d'axe narratif ({Version})",
          prompt.TemplateVersion);
      throw;
    }

    var rawJson = completion.Value.Content[0].Text;
    return ParsePremises(rawJson);
  }

  private GeneratedPremise[] ParsePremises(string rawJson)
  {
    RawPremisesResponse? raw;
    try
    {
      raw = JsonSerializer.Deserialize<RawPremisesResponse>(rawJson, JsonOptions);
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
      raw = JsonSerializer.Deserialize<RawGeneratedScene>(rawJson, JsonOptions);
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

    return new GeneratedScene(raw.Text, choices, raw.UpdatedSummary, raw.KeyFacts, imagePrompt);
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
