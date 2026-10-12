namespace FableFlow.Domain.Enums;

/// <summary>
/// Qui lit l'histoire. Complète l'âge du héros : l'âge seul ne dit rien de la capacité de décodage.
/// </summary>
public enum ReaderMode
{
  /// <summary>Un adulte lit l'histoire à voix haute (par défaut).</summary>
  AdultReadAloud = 0,

  /// <summary>L'enfant lit lui-même.</summary>
  ChildReader = 1
}
