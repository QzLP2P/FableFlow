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
  private static readonly TokenRequestContext FoundryTokenContext = new(["https://ai.azure.com/.default"]);

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
    try
    {
      await AuthenticateAsync(cancellationToken);

      var request = new FluxGenerationRequest
      {
        Model = _options.DeploymentName,
        Prompt = $"{prompt.Prompt} Style : {prompt.Style}."
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
        return null;
      }

      var generation = await response.Content.ReadFromJsonAsync<FluxGenerationResponse>(cancellationToken);
      var image = generation?.Data.FirstOrDefault();

      if (image?.Base64Json is { } base64)
      {
        return $"data:image/jpeg;base64,{base64}";
      }

      return image?.Url;
    }
    catch (Exception ex)
    {
      // La génération d'image est une amélioration non bloquante : on journalise et on
      // renvoie null plutôt que de faire échouer toute la scène narrative.
      _logger.LogWarning(ex, "Échec de la génération d'image FLUX, la scène sera affichée sans illustration.");
      return null;
    }
  }

  private async Task AuthenticateAsync(CancellationToken cancellationToken)
  {
    if (_credential is not null)
    {
      var token = await _credential.GetTokenAsync(FoundryTokenContext, cancellationToken);
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
