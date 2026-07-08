using System.Text.Json.Serialization;

namespace FableFlow.Infrastructure.Ai.Contracts;

/// <summary>
/// Corps de requête brut pour l'endpoint Chat Completions Azure OpenAI. Utilisé uniquement pour
/// injecter des paramètres pas encore exposés par <c>ChatCompletionOptions</c> dans la version du
/// SDK figée par ce projet (ex. "reasoning_effort", disponible côté API pour les modèles de
/// raisonnement type gpt-5.x mais absent d'Azure.AI.OpenAI 2.1.0 — voir
/// <see cref="AzureOpenAIStoryGenerationService"/>). Les noms de champs suivent la convention
/// snake_case de l'API REST, contrairement aux contrats <see cref="RawGeneratedScene"/> qui
/// décrivent le JSON que NOUS demandons au LLM de produire (camelCase).
/// </summary>
public sealed class RawChatCompletionRequest
{
  [JsonPropertyName("messages")]
  public List<RawChatCompletionRequestMessage> Messages { get; set; } = [];

  [JsonPropertyName("response_format")]
  public RawChatCompletionResponseFormat ResponseFormat { get; set; } = new();

  /// <summary>
  /// Modèles de raisonnement (ex. gpt-5.x) : refusent toute valeur de température différente de la
  /// valeur par défaut (HTTP 400 "unsupported_value" constaté en pratique). Laisser à <c>null</c>
  /// (champ omis de la requête) pour ces modèles.
  /// </summary>
  [JsonPropertyName("temperature")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public float? Temperature { get; set; }

  [JsonPropertyName("presence_penalty")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public float? PresencePenalty { get; set; }

  [JsonPropertyName("frequency_penalty")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public float? FrequencyPenalty { get; set; }

  /// <summary>
  /// Effort de raisonnement demandé au modèle (ex. "minimal" | "low" | "medium" | "high" selon
  /// l'API). Réduit la latence pour une tâche d'écriture créative qui n'a pas besoin d'un
  /// raisonnement multi-étapes approfondi. <c>null</c> pour ne pas envoyer ce champ (modèle qui ne
  /// le supporte pas).
  /// </summary>
  [JsonPropertyName("reasoning_effort")]
  public string? ReasoningEffort { get; set; }
}

public sealed class RawChatCompletionRequestMessage
{
  [JsonPropertyName("role")]
  public string Role { get; set; } = string.Empty;

  [JsonPropertyName("content")]
  public string Content { get; set; } = string.Empty;
}

public sealed class RawChatCompletionResponseFormat
{
  [JsonPropertyName("type")]
  public string Type { get; set; } = "json_object";
}

/// <summary>Forme (partielle) de la réponse brute de l'endpoint Chat Completions.</summary>
public sealed class RawChatCompletionResponse
{
  [JsonPropertyName("choices")]
  public List<RawChatCompletionResponseChoice> Choices { get; set; } = [];
}

public sealed class RawChatCompletionResponseChoice
{
  [JsonPropertyName("message")]
  public RawChatCompletionResponseMessage Message { get; set; } = new();
}

public sealed class RawChatCompletionResponseMessage
{
  [JsonPropertyName("content")]
  public string Content { get; set; } = string.Empty;
}
