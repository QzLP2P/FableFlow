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
                "Un jeune dresseur explore un monde peuplé de créatures Pokémon, noue des amitiés et " +
                "affronte un rival récurrent, aussi compétitif que bienveillant. Il déjoue les plans " +
                "souvent maladroits de la Team Rocket, part parfois à la découverte de Pokémon " +
                "légendaires, vit des combats inopinés en chemin ainsi que des combats d'arène face à " +
                "des champions, et progresse vers un objectif (badge, tournoi, Ligue Pokémon, exploration).",
            safetyConstraints:
            [
                "Aucune violence graphique : les combats restent sportifs et sans blessure décrite, " +
                "y compris face à la Team Rocket, toujours plus maladroite que réellement menaçante.",
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
                "Un jeune super-héros protège son quartier, aide ses voisins et affronte des " +
                "super-vilains hauts en couleur - un Rhino cuirassé, un facétieux Bouffon Vert sur son " +
                "deltaplane, ou encore l'Homme de Sable capable de se transformer en tempête de sable - " +
                "à l'aide de ruse et d'entraide, sans jamais recourir à une violence réelle.",
            safetyConstraints:
            [
                "Aucune violence graphique : les affrontements avec les super-vilains se règlent par la " +
                "ruse ou l'agilité, jamais par des coups ou blessures décrits.",
                "Aucun langage grossier ni thème adulte.",
                "Ton héroïque, rassurant, orienté entraide et courage."
            ],
            imageStyle: "illustration style bande dessinée lumineuse, dynamique, adaptée aux enfants"),

        new ThemeDefinition(
            id: "dinosaur",
            displayName: "Dinosaures — Expédition préhistorique",
            audience: AudienceTarget.Child,
            vocabularyLevel: VocabularyLevel.Simple,
            narrativeUniverse:
                "Un jeune explorateur ou une jeune exploratrice découvre une vallée préservée peuplée de " +
                "dinosaures paisibles, se lie d'amitié avec eux, explore une jungle préhistorique et résout " +
                "de petites énigmes naturelles (œufs à protéger, orage à anticiper, chemin à retrouver).",
            safetyConstraints:
            [
                "Aucune violence graphique : les dinosaures sont impressionnants mais toujours bienveillants, " +
                "jamais menaçants au point de faire peur.",
                "Aucun langage grossier ni thème adulte.",
                "Ton émerveillé, curieux, orienté découverte de la nature et entraide."
            ],
            imageStyle: "illustration colorée style dessin animé, chaleureuse, adaptée aux enfants"),

        new ThemeDefinition(
            id: "vet",
            displayName: "Vétérinaire — Soigneur des animaux",
            audience: AudienceTarget.Child,
            vocabularyLevel: VocabularyLevel.Simple,
            narrativeUniverse:
                "Un jeune apprenti ou une jeune apprentie vétérinaire accueille et soigne des animaux dans " +
                "une clinique de quartier ou lors de sorties sur le terrain, rencontre des animaux variés " +
                "(chiots, chatons, oiseaux, animaux de ferme), les rassure et apprend à s'en occuper avec douceur.",
            safetyConstraints:
            [
                "Aucune scène médicale graphique ou effrayante : les soins sont toujours doux, rassurants et " +
                "sans détail clinique impressionnant.",
                "Aucun langage grossier ni thème adulte.",
                "Ton bienveillant, rassurant, orienté soin, patience et respect des animaux."
            ],
            imageStyle: "illustration colorée style dessin animé, douce et chaleureuse, adaptée aux enfants")
  ];


  public Task<IReadOnlyList<ThemeDefinition>> GetThemesAsync(CancellationToken cancellationToken) =>
      Task.FromResult(Themes);

  public Task<ThemeDefinition?> FindThemeAsync(string themeId, CancellationToken cancellationToken)
  {
    var theme = Themes.FirstOrDefault(t => string.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase));
    return Task.FromResult(theme);
  }
}
