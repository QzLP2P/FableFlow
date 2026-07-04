namespace FableFlow.Infrastructure.Options;

/// <summary>Configuration de connexion et de déploiements Azure OpenAI.</summary>
public sealed class AzureOpenAIOptions
{
  public const string SectionName = "AzureOpenAI";

  /// <summary>Endpoint du compte Azure OpenAI (ex. https://mon-compte.openai.azure.com/).</summary>
  public string Endpoint { get; set; } = string.Empty;

  /// <summary>
  /// Clé API. Utilisée uniquement en développement local ; en production,
  /// préférer <see cref="UseManagedIdentity"/> avec <c>DefaultAzureCredential</c>.
  /// </summary>
  public string? ApiKey { get; set; }

  /// <summary>Utiliser Managed Identity (via DefaultAzureCredential) plutôt qu'une clé API.</summary>
  public bool UseManagedIdentity { get; set; } = true;

  /// <summary>Nom du déploiement du modèle orchestrateur (ex. gpt-5.4-mini, gpt-4o, gpt-4.1).</summary>
  public string ChatDeployment { get; set; } = "gpt-5.4-mini";

  /// <summary>
  /// Nom du déploiement de génération d'image de la série gpt-image (ex. gpt-image-1).
  /// Utilisé uniquement par <see cref="Ai.AzureOpenAIImageGenerationService"/> (alternative à FLUX).
  /// </summary>
  public string ImageDeployment { get; set; } = "gpt-image-1";
}
