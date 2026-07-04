using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Enums;

namespace FableFlow.Api.Tests.Fakes;

/// <summary>
/// Génération narrative déterministe pour les tests d'intégration, évitant tout appel réseau
/// vers Azure OpenAI. Propose toujours un choix "a" (bon) et "b" (mauvais) pour piloter
/// précisément victoire/défaite dans les tests.
/// </summary>
public sealed class FakeStoryGenerationService : IStoryGenerationService
{
  public Task<GeneratedScene> GenerateSceneAsync(StoryPrompt prompt, CancellationToken cancellationToken)
  {
    if (prompt.Kind == SceneKind.Ending)
    {
      return Task.FromResult(new GeneratedScene(
          $"Fin de l'aventure (après la scène {prompt.SceneNumber}).",
          [],
          "Résumé final de l'aventure.",
          [],
          "Description générique de la scène finale."));
    }

    GeneratedChoice[] choices =
    [
        new("a", "Faire le bon choix", ChoiceOutcome.Good),
            new("b", "Faire le mauvais choix", ChoiceOutcome.Bad)
    ];

    return Task.FromResult(new GeneratedScene(
        $"Texte généré pour la scène {prompt.SceneNumber}.",
        choices,
        $"Résumé jusqu'à la scène {prompt.SceneNumber}.",
        [],
        "Description générique de la scène."));
  }

  public Task<IReadOnlyList<GeneratedPremise>> GeneratePremisesAsync(
      StoryPremisePrompt prompt,
      CancellationToken cancellationToken)
  {
    var premises = Enumerable.Range(1, prompt.Count)
        .Select(i => new GeneratedPremise($"Titre {i}", $"Accroche générée {i}."))
        .ToArray();

    return Task.FromResult<IReadOnlyList<GeneratedPremise>>(premises);
  }
}
