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

  /// <summary>Nom du déploiement du modèle qui écrit les chapitres (ex. gpt-5.4-mini, gpt-5.5, gpt-4.1).</summary>
  public string ChatDeployment { get; set; } = "gpt-5.4-mini";

  /// <summary>
  /// Effort de raisonnement envoyé avec chaque requête de chapitre (ex. "none", "low", "medium"),
  /// selon les valeurs acceptées par le modèle déployé : "minimal" n'existe que pour les premiers
  /// gpt-5 (gpt-5, gpt-5-mini, gpt-5-nano) et est refusé à partir de gpt-5.1, où "none" le remplace.
  /// Laisser vide pour ne pas envoyer le champ : obligatoire pour un modèle sans raisonnement
  /// (ex. gpt-4.1, gpt-4o), qui le refuse.
  /// </summary>
  public string? ReasoningEffort { get; set; } = "none";

  /// <summary>
  /// Déploiement facultatif dédié aux idées de départ (tâche courte, un modèle plus rapide suffit).
  /// Vide : <see cref="ChatDeployment"/> est utilisé.
  /// </summary>
  public string? PremiseDeployment { get; set; }

  /// <summary>
  /// Effort de raisonnement des idées de départ. <c>null</c> : reprend <see cref="ReasoningEffort"/> ;
  /// chaîne vide : champ non envoyé.
  /// </summary>
  public string? PremiseReasoningEffort { get; set; }

  /// <summary>
  /// Nom du déploiement de génération d'image de la série gpt-image (ex. gpt-image-1).
  /// Utilisé uniquement par <see cref="Ai.AzureOpenAIImageGenerationService"/> (alternative à FLUX).
  /// </summary>
  public string ImageDeployment { get; set; } = "gpt-image-1";
}
