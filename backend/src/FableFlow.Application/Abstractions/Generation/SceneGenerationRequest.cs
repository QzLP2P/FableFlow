using FableFlow.Domain.Entities;

namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Contexte fourni au constructeur de prompts pour générer une scène.</summary>
public sealed record SceneGenerationRequest(
    ThemeDefinition Theme,
    AdventureSession Session,
    SceneKind Kind,
    SceneChoice? SelectedChoice);
