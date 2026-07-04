namespace FableFlow.Application.Common.Options;

/// <summary>Feature flags pilotant les capacités optionnelles du backend.</summary>
public sealed class FeatureOptions
{
    public const string SectionName = "Features";

    /// <summary>Active la génération d'images pour illustrer les scènes.</summary>
    public bool ImageGeneration { get; set; }
}
