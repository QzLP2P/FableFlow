using FableFlow.Application.Abstractions;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;

namespace FableFlow.Infrastructure.Themes;

/// <summary>
/// Fournit les thèmes disponibles, codés en dur pour le MVP.
/// Évolution prévue : chargement depuis une base de données (voir <see cref="IThemePolicyProvider"/>).
/// </summary>
public sealed class HardcodedThemePolicyProvider : IThemePolicyProvider
{
  private static readonly IReadOnlyList<ThemeDefinition> Themes =
  [
      new ThemeDefinition(
            id: "pokemon",
            displayName: "Pokémon — Aventure de dresseur",
            audience: AudienceTarget.Child,
            vocabularyLevel: VocabularyLevel.Simple,
            narrativeUniverse:
                "Un jeune dresseur explore un monde peuplé de créatures Pokémon, noue des amitiés, " +
                "affronte des rivaux bienveillants et progresse vers un objectif (badge, tournoi, exploration).",
            safetyConstraints:
            [
                "Aucune violence graphique : les combats restent sportifs et sans blessure décrite.",
                "Aucun langage grossier ni thème adulte.",
                "Ton positif, encourageant, orienté amitié et persévérance."
            ],
            imageStyle: "illustration colorée style dessin animé, douce, adaptée aux enfants"),

        new ThemeDefinition(
            id: "spidey",
            displayName: "Spidey — Super-héros de quartier",
            audience: AudienceTarget.Child,
            vocabularyLevel: VocabularyLevel.Simple,
            narrativeUniverse:
                "Un jeune super-héros protège son quartier, aide ses voisins et déjoue des plans de " +
                "vilains inoffensifs à l'aide de ruse et d'entraide, sans jamais recourir à une violence réelle.",
            safetyConstraints:
            [
                "Aucune violence graphique : les affrontements se règlent par la ruse ou l'agilité.",
                "Aucun langage grossier ni thème adulte.",
                "Ton héroïque, rassurant, orienté entraide et courage."
            ],
            imageStyle: "illustration style bande dessinée lumineuse, dynamique, adaptée aux enfants")
  ];

  public Task<IReadOnlyList<ThemeDefinition>> GetThemesAsync(CancellationToken cancellationToken) =>
      Task.FromResult(Themes);

  public Task<ThemeDefinition?> FindThemeAsync(string themeId, CancellationToken cancellationToken)
  {
    var theme = Themes.FirstOrDefault(t => string.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase));
    return Task.FromResult(theme);
  }
}
