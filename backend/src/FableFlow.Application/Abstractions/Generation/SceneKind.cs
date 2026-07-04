namespace FableFlow.Application.Abstractions.Generation;

/// <summary>Nature de la scène à générer, pilotant la construction du prompt.</summary>
public enum SceneKind
{
    /// <summary>Première scène de l'aventure.</summary>
    Initial = 0,

    /// <summary>Scène intermédiaire faisant suite à un choix.</summary>
    Continuation = 1,

    /// <summary>Scène de conclusion (victoire, défaite ou fin neutre).</summary>
    Ending = 2
}
