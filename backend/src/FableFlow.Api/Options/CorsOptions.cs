namespace FableFlow.Api.Options;

/// <summary>Configuration CORS pour autoriser le frontend à appeler l'API.</summary>
public sealed class CorsOptions
{
  public const string SectionName = "Cors";

  /// <summary>Origines autorisées (ex. URL de la Static Web App).</summary>
  public string[] AllowedOrigins { get; set; } = [];
}
