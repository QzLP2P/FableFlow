using Azure.AI.OpenAI;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Images;

namespace FableFlow.Infrastructure.Ai;

/// <summary>
/// Génère les illustrations de scène via Azure OpenAI (modèle <c>gpt-image-1</c>).
/// Actif uniquement si <c>Features:ImageGeneration</c> est activé (voir <see cref="NullImageGenerationService"/>).
/// </summary>
public sealed class AzureOpenAIImageGenerationService : IImageGenerationService
{
    private readonly ImageClient _imageClient;
    private readonly ILogger<AzureOpenAIImageGenerationService> _logger;

    public AzureOpenAIImageGenerationService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIImageGenerationService> logger)
    {
        _logger = logger;

        var settings = options.Value;
        var azureClient = AzureOpenAIClientFactory.Create(settings);
        _imageClient = azureClient.GetImageClient(settings.ImageDeployment);
    }

    public bool IsEnabled => true;

    public async Task<string?> GenerateImageAsync(StoryImagePrompt prompt, CancellationToken cancellationToken)
    {
        var fullPrompt = $"{prompt.Prompt} Style : {prompt.Style}.";

        try
        {
            var options = new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024,
                ResponseFormat = GeneratedImageFormat.Bytes
            };

            var image = await _imageClient.GenerateImageAsync(fullPrompt, options, cancellationToken);

            if (image.Value.ImageBytes is { } bytes)
            {
                return $"data:image/png;base64,{Convert.ToBase64String(bytes.ToArray())}";
            }

            return image.Value.ImageUri?.ToString();
        }
        catch (Exception ex)
        {
            // La génération d'image est une amélioration non bloquante : on journalise et on
            // renvoie null plutôt que de faire échouer toute la scène narrative.
            _logger.LogWarning(ex, "Échec de la génération d'image, la scène sera affichée sans illustration.");
            return null;
        }
    }
}
