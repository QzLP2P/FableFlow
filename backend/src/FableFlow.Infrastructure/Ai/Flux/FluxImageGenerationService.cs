using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FableFlow.Infrastructure.Ai.Flux;

/// <summary>
/// Génère les illustrations de scène via le modèle FLUX.2-pro (Black Forest Labs) exposé par
/// Microsoft Foundry. Le proxy Azure Foundry répond de façon synchrone, au format OpenAI Images
/// (base64), contrairement à l'API BFL brute qui est asynchrone avec polling.
/// Actif uniquement si <c>Features:ImageGeneration</c> est activé.
/// </summary>
public sealed class FluxImageGenerationService : IImageGenerationService
{
  private static readonly TokenRequestContext _foundryTokenContext = new(["https://ai.azure.com/.default"]);

  // Le déploiement FLUX.2-pro tourne avec une capacité volontairement faible (coûts) : des HTTP 429
  // (limitation de débit) sont donc fréquents en usage normal, constatés en pratique via les
  // dépendances Application Insights. On retente quelques fois avant d'abandonner, en respectant
  // l'en-tête "Retry-After" du serveur quand il est présent. Sans impact sur la réponse HTTP au
  // client : tout ceci se déroule dans le travail en arrière-plan (voir ISceneImageJobScheduler).
  private const int MaxThrottleRetries = 2;
  private static readonly TimeSpan DefaultThrottleDelay = TimeSpan.FromSeconds(3);
  private static readonly TimeSpan MaxThrottleDelay = TimeSpan.FromSeconds(15);

  private readonly HttpClient _httpClient;
  private readonly FluxImageOptions _options;
  private readonly DefaultAzureCredential? _credential;
  private readonly ILogger<FluxImageGenerationService> _logger;

  public FluxImageGenerationService(
      IHttpClientFactory httpClientFactory,
      IOptions<FluxImageOptions> options,
      ILogger<FluxImageGenerationService> logger)
  {
    _httpClient = httpClientFactory.CreateClient(nameof(FluxImageGenerationService));
    _options = options.Value;
    _credential = _options.UseManagedIdentity ? new DefaultAzureCredential() : null;
    _logger = logger;

    if (!string.IsNullOrWhiteSpace(_options.Endpoint))
    {
      _httpClient.BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/");
    }

    _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
  }

  public bool IsEnabled => true;

  public async Task<string?> GenerateImageAsync(StoryImagePrompt prompt, CancellationToken cancellationToken)
  {
    var scenePrompt = $"{prompt.Prompt} Style : {prompt.Style}.";
    var (image, errorCode) = await TryGenerateWithThrottleRetryAsync(scenePrompt, cancellationToken);
    if (image is not null)
    {
      return image;
    }

    // Azure filtre parfois même les descriptions génériques d'un personnage très connu
    // (le blocage RAI ne se limite pas aux noms propres). Dans ce cas précis, on retente
    // une seule fois avec un prompt totalement générique (juste le style du thème, sans
    // aucune description de scène) afin d'afficher au moins une illustration d'ambiance
    // plutôt que rien du tout.
    if (errorCode == "content_safety_violation")
    {
      var fallbackPrompt =
          $"Illustration d'ambiance pour une histoire interactive destinée aux enfants, " +
          $"sans aucun personnage identifiable, juste un décor accueillant. Style : {prompt.Style}. " +
          "Aucun texte ni typographie dans l'image.";

      _logger.LogInformation(
          "Nouvelle tentative de génération d'image avec un prompt générique de repli après un blocage RAI.");

      (image, _) = await TryGenerateWithThrottleRetryAsync(fallbackPrompt, cancellationToken);
    }

    return image;
  }

  /// <summary>
  /// Exécute <see cref="TryGenerateAsync"/> pour un prompt donné, en retentant jusqu'à
  /// <see cref="MaxThrottleRetries"/> fois en cas de limitation de débit (HTTP 429), en respectant
  /// l'en-tête "Retry-After" du serveur si présent (sinon un délai par défaut).
  /// </summary>
  private async Task<(string? Image, string? ErrorCode)> TryGenerateWithThrottleRetryAsync(
      string promptText,
      CancellationToken cancellationToken)
  {
    for (var attempt = 0; ; attempt++)
    {
      var (image, errorCode, retryAfter) = await TryGenerateAsync(promptText, cancellationToken);
      if (image is not null || retryAfter is null || attempt >= MaxThrottleRetries)
      {
        return (image, errorCode);
      }

      _logger.LogWarning(
          "Génération d'image FLUX limitée (429) : nouvelle tentative dans {DelaySeconds:F1}s ({Attempt}/{MaxAttempts}).",
          retryAfter.Value.TotalSeconds,
          attempt + 1,
          MaxThrottleRetries);
      await Task.Delay(retryAfter.Value, cancellationToken);
    }
  }

  private async Task<(string? Image, string? ErrorCode, TimeSpan? RetryAfter)> TryGenerateAsync(
      string promptText,
      CancellationToken cancellationToken)
  {
    try
    {
      await AuthenticateAsync(cancellationToken);

      var request = new FluxGenerationRequest
      {
        Model = _options.DeploymentName,
        Prompt = promptText
      };

      // Le proxy Azure Foundry pour FLUX.2-pro exige un en-tête Content-Length explicite
      // (erreur "no_content_length_header" sinon). JsonContent/ObjectContent sérialise en
      // flux et ne précalcule pas sa longueur : HttpClient bascule alors sur
      // Transfer-Encoding: chunked, que ce proxy rejette. StringContent, elle, encapsule un
      // tableau d'octets de taille connue à l'avance, ce qui force l'envoi d'un en-tête
      // Content-Length classique. On force aussi HTTP/1.1 par sécurité.
      var json = JsonSerializer.Serialize(request);
      using var httpRequest = new HttpRequestMessage(
          HttpMethod.Post,
          $"providers/blackforestlabs/v1/{_options.ModelSlug}")
      {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
        Version = HttpVersion.Version11,
        VersionPolicy = HttpVersionPolicy.RequestVersionExact
      };

      var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

      if (!response.IsSuccessStatusCode)
      {
        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Échec de la génération d'image FLUX ({StatusCode}), la scène sera affichée sans illustration. Réponse : {ErrorBody}",
            (int)response.StatusCode,
            errorBody);

        var retryAfter = response.StatusCode == HttpStatusCode.TooManyRequests
            ? GetRetryAfterDelay(response)
            : (TimeSpan?)null;

        return (null, ExtractErrorCode(errorBody), retryAfter);
      }

      var generation = await response.Content.ReadFromJsonAsync<FluxGenerationResponse>(cancellationToken);
      var image = generation?.Data.FirstOrDefault();

      if (image?.Base64Json is { } base64)
      {
        return ($"data:image/jpeg;base64,{base64}", null, null);
      }

      return (image?.Url, null, null);
    }
    catch (Exception ex)
    {
      // La génération d'image est une amélioration non bloquante : on journalise et on
      // renvoie null plutôt que de faire échouer toute la scène narrative.
      _logger.LogWarning(ex, "Échec de la génération d'image FLUX, la scène sera affichée sans illustration.");
      return (null, null, null);
    }
  }

  /// <summary>Détermine le délai à attendre avant de retenter après un HTTP 429, borné à <see cref="MaxThrottleDelay"/>.</summary>
  private static TimeSpan GetRetryAfterDelay(HttpResponseMessage response)
  {
    var retryAfter = response.Headers.RetryAfter;

    if (retryAfter?.Delta is { } delta)
    {
      return Clamp(delta);
    }

    if (retryAfter?.Date is { } date)
    {
      var delay = date - DateTimeOffset.UtcNow;
      if (delay > TimeSpan.Zero)
      {
        return Clamp(delay);
      }
    }

    return DefaultThrottleDelay;
  }

  private static TimeSpan Clamp(TimeSpan value)
  {
    if (value < TimeSpan.Zero)
    {
      return TimeSpan.Zero;
    }

    return value > MaxThrottleDelay ? MaxThrottleDelay : value;
  }

  private static string? ExtractErrorCode(string errorBody)
  {
    try
    {
      using var document = JsonDocument.Parse(errorBody);
      return document.RootElement.TryGetProperty("error", out var error) &&
             error.TryGetProperty("code", out var code)
          ? code.GetString()
          : null;
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private async Task AuthenticateAsync(CancellationToken cancellationToken)
  {
    if (_credential is not null)
    {
      var token = await _credential.GetTokenAsync(_foundryTokenContext, cancellationToken);
      _httpClient.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Bearer", token.Token);
      return;
    }

    if (!string.IsNullOrWhiteSpace(_options.ApiKey))
    {
      _httpClient.DefaultRequestHeaders.Remove("api-key");
      _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
    }
  }
}
