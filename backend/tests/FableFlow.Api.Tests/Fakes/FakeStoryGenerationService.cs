using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Enums;

namespace FableFlow.Api.Tests.Fakes;

/// <summary>
/// Génération déterministe pour les tests d'intégration, sans appel réseau vers Azure OpenAI.
/// Respecte le contrat attendu selon la nature du chapitre (actions uniquement pour un moment de
/// choix) ; un marqueur dans l'idée d'histoire permet de simuler une panne du fournisseur.
/// </summary>
public sealed class FakeStoryGenerationService : IStoryGenerationService
{
  /// <summary>Fait échouer la génération à partir du chapitre 2 lorsqu'il figure dans l'idée d'histoire.</summary>
  public const string FailFromChapterTwoMarker = "PANNE-CHAPITRE-2";

  public Task<GeneratedChapter> GenerateChapterAsync(StoryPrompt prompt, CancellationToken cancellationToken)
  {
    if (prompt.ChapterNumber >= 2 && prompt.UserPrompt.Contains(FailFromChapterTwoMarker, StringComparison.Ordinal))
    {
      throw new HttpRequestException("Fournisseur indisponible (simulé) : détail interne à ne jamais exposer.");
    }

    GeneratedChoice[] choices = prompt.Kind == ChapterKind.Decision
        ? [new("a", "Suivre la piste lumineuse"), new("b", "Demander de l'aide à un ami"), new("c", "Observer en silence")]
        : [];

    var text = prompt.Kind == ChapterKind.Ending
        ? $"Conclusion chaleureuse de l'histoire (chapitre {prompt.ChapterNumber})."
        : $"Texte du chapitre {prompt.ChapterNumber}.\n\nDeuxième paragraphe.";

    return Task.FromResult(new GeneratedChapter(
        text,
        choices,
        $"Résumé jusqu'au chapitre {prompt.ChapterNumber}.",
        [$"Fait du chapitre {prompt.ChapterNumber}"],
        "Un enfant héros dans un décor lumineux.",
        prompt.ChapterNumber == 1 ? ["Début", "Aventure", "Retour à la maison"] : null,
        prompt.ChapterNumber == 1 ? "Titre généré" : null));
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
