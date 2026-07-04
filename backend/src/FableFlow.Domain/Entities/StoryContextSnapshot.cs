namespace FableFlow.Domain.Entities;

/// <summary>
/// Instantané de la mémoire narrative, transmis au LLM pour garantir la cohérence
/// entre les scènes (résumé courant + faits clés à ne pas contredire).
/// </summary>
public sealed class StoryContextSnapshot
{
  public StoryContextSnapshot(string runningSummary, IReadOnlyList<string> keyFacts)
  {
    RunningSummary = runningSummary;
    KeyFacts = keyFacts;
  }

  /// <summary>Résumé cumulatif de l'histoire jusqu'à la scène courante.</summary>
  public string RunningSummary { get; }

  /// <summary>Faits marquants à préserver (personnages, objets, enjeux).</summary>
  public IReadOnlyList<string> KeyFacts { get; }

  public static StoryContextSnapshot Empty { get; } = new(string.Empty, []);
}
