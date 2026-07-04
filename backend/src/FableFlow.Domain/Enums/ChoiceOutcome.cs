namespace FableFlow.Domain.Enums;

/// <summary>Impact narratif d'un choix sur l'issue de l'aventure.</summary>
public enum ChoiceOutcome
{
  /// <summary>Choix neutre : l'histoire progresse sans pénalité.</summary>
  Neutral = 0,

  /// <summary>Bon choix : rapproche d'une issue positive.</summary>
  Good = 1,

  /// <summary>Mauvais choix : incrémente le compteur d'échecs.</summary>
  Bad = 2
}
