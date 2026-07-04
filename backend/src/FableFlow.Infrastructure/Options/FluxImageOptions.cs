namespace FableFlow.Infrastructure.Options;

/// <summary>
/// Configuration de connexion au modèle d'image FLUX (Black Forest Labs) exposé par
/// Microsoft Foundry via l'API fournisseur BFL (<c>/providers/blackforestlabs/v1/{model}</c>).
/// </summary>
public sealed class FluxImageOptions
{
    public const string SectionName = "Flux";

    /// <summary>Endpoint de la ressource Foundry (ex. https://mon-compte.services.ai.azure.com).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Clé API de la ressource Foundry. Utilisée uniquement en développement local ; en production,
    /// préférer <see cref="UseManagedIdentity"/>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Utiliser Managed Identity (via DefaultAzureCredential) plutôt qu'une clé API.</summary>
    public bool UseManagedIdentity { get; set; }

    /// <summary>Slug du modèle dans l'URL du fournisseur BFL (ex. flux-2-pro pour FLUX.2-pro).</summary>
    public string ModelSlug { get; set; } = "flux-2-pro";

    /// <summary>
    /// Nom exact du déploiement, attendu dans le champ "model" du corps de requête
    /// (ex. FLUX.2-pro tel que nommé dans Microsoft Foundry).
    /// </summary>
    public string DeploymentName { get; set; } = "FLUX.2-pro";

    /// <summary>Délai maximum d'attente de la requête HTTP, en secondes.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
