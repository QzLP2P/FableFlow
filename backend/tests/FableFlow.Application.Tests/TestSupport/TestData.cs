using FableFlow.Application.Abstractions.Generation;
using FableFlow.Application.Common.Heroes;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;

namespace FableFlow.Application.Tests.TestSupport;

internal static class TestData
{
  public static ThemeDefinition Theme() =>
      new(
          "dinosaur",
          "Dinosaures",
          "Une vallée préhistorique pleine de surprises.",
          AudienceTarget.Child,
          "Une vallée préhistorique peuplée de dinosaures bienveillants.",
          ["Aucune violence."],
          "Illustration douce aux couleurs chaudes.");

  public static HeroInput HeroInput() => new("Léa", 6, ["Max, son chien"], "les étoiles");

  public static AdventureSession NewSession(StoryLength length = StoryLength.Short, string? title = null) =>
      AdventureSession.Start(
          Guid.NewGuid(),
          "dinosaur",
          HeroProfile.Create("Léa", 6),
          ReaderMode.AdultReadAloud,
          length,
          "Un œuf mystérieux",
          title);

  public static StoryPrompt Prompt(ChapterKind kind = ChapterKind.Narrative, int number = 1) =>
      new("system", "user", "chapter-generation@test", kind, number);

  public static GeneratedChapter Narrative(string text = "Il était une fois…") =>
      new(text, [], "Résumé", ["Fait"], "Une vallée verdoyante au lever du soleil", ["Début", "Milieu", "Fin"], "Le grand œuf");

  public static GeneratedChapter Decision(params string[] labels) =>
      new(
          "Deux chemins s'ouvrent.",
          [.. labels.Select((l, i) => new GeneratedChoice($"x{i}", l))],
          "Résumé",
          [],
          "Un carrefour dans la forêt");

  /// <summary>Joue l'ouverture (chapitre 1) sans passer par la génération.</summary>
  public static AdventureSession Opened(StoryLength length = StoryLength.Short)
  {
    var session = NewSession(length);
    var transition = session.BeginOpening();
    session.CompleteTransition(transition, new AdventureChapter(1, ChapterKind.Narrative, "Ouverture", []));
    return session;
  }

  /// <summary>Histoire courte arrivée à son premier moment de choix (chapitre 2).</summary>
  public static AdventureSession AtFirstDecision()
  {
    var session = Opened();
    var transition = session.BeginAdvance(1);
    session.CompleteTransition(
        transition,
        new AdventureChapter(2, ChapterKind.Decision, "Un choix", [new ChapterChoice("a", "Entrer"), new ChapterChoice("b", "Attendre")]));
    return session;
  }
}
