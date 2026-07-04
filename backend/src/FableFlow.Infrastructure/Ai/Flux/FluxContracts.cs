using System.Text.Json.Serialization;

namespace FableFlow.Infrastructure.Ai.Flux;

/// <summary>Corps de requête envoyé au fournisseur BFL pour générer une image.</summary>
public sealed class FluxGenerationRequest
{
    /// <summary>Nom du déploiement du modèle (requis par le proxy Azure Foundry).</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 1024;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 1024;
}

/// <summary>Une image générée, encodée en base64 (format aligné sur l'API OpenAI Images).</summary>
public sealed class FluxGeneratedImage
{
    [JsonPropertyName("b64_json")]
    public string? Base64Json { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Réponse synchrone du proxy Azure Foundry pour FLUX.2-pro : contrairement à l'API BFL brute
/// (asynchrone avec polling), Azure normalise la réponse au format OpenAI Images (b64_json direct).
/// </summary>
public sealed class FluxGenerationResponse
{
    [JsonPropertyName("data")]
    public List<FluxGeneratedImage> Data { get; set; } = [];
}

