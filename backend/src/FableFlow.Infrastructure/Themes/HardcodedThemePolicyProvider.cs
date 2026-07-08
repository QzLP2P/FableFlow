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
  private static readonly IReadOnlyList<ThemeDefinition> _themes =
  [
      new ThemeDefinition(
            id: "pokemon",
            displayName: "Pokémon — Aventure de dresseur",
            audience: AudienceTarget.Child,
            vocabularyLevel: VocabularyLevel.Simple,
            narrativeUniverse:
                "Sacha Ketchoum est le jeune dresseur au centre de l'histoire, toujours accompagné de " +
                "son fidèle Pikachu (attaques Éclair, Tonnerre). Selon les régions traversées, il voyage " +
                "avec des compagnons récurrents : Ondine et Pierre à Kanto/Johto, Flora et Max à Hoenn, " +
                "Aurore à Sinnoh, Iris et Rachid à Unys, Serena, Lem et Clem à Kalos, puis Lilie, Kiawe, " +
                "Néphie, Chrys et enfin Gladio à Alola. Il croise aussi des rivaux marquants selon les " +
                "sagas : Régis (rival historique depuis ses débuts), Paul (rival intense de Sinnoh), Niko " +
                "(rival d'Unys), Liam (rival de Kalos), Alain (adversaire majeur de la Ligue de Kalos) et " +
                "Gladio (rival d'Alola) ; d'autres dresseurs croisés en chemin comme Richie, Harrison, " +
                "Morrison, Tyson, Barry, Tobias, Bianca, Jules, Alexis, Virgile, Tierno, Trovato ou Tili " +
                "peuvent aussi apparaître ponctuellement. Son équipe compte, en plus de Pikachu, des " +
                "Pokémon emblématiques comme Dracaufeu (évolution de Salamèche puis Reptincel ; " +
                "Lance-Flammes, Déflagration), Roucarnage (évolution de Roucool puis Roucoups ; " +
                "Vive-Attaque, Aile d'Acier), Papillusion (évolution de Chenipan puis Chrysacier), " +
                "Bulbizarre (Fouet Lianes, Lance-Soleil), Carabaffe ou Tortank (Pistolet à Ô, Hydrocanon), " +
                "Krabboss, Rattatac, Colossinge (Poing-Karaté), Tauros, Lokhlass (Pistolet à Ô, Blizzard) " +
                "et Ronflex (Coup de Mâchoire, Plaquage). Il part parfois à la découverte de Pokémon " +
                "légendaires, déjoue les plans souvent maladroits de la Team Rocket, vit des combats " +
                "inopinés en chemin ainsi que des combats d'arène face à des champions, et progresse vers " +
                "un objectif (badge, tournoi, Ligue Pokémon, exploration).",
            safetyConstraints:
            [
                "Aucune violence graphique : les combats restent sportifs et sans blessure décrite, " +
                "y compris face à la Team Rocket ou aux rivaux, toujours plus compétitifs que réellement " +
                "menaçants.",
                "Aucun langage grossier ni thème adulte.",
                "Ton positif, encourageant, orienté amitié et persévérance."
            ],
            imageStyle: "illustration colorée style dessin animé, douce, adaptée aux enfants",
            recurringStoryBeats:
            [
                "Sacha tente de capturer un nouveau Pokémon sauvage rencontré en chemin.",
                "Sacha fait une nouvelle rencontre marquante (dresseur, professeur Pokémon ou habitant de la région).",
                "Un combat Pokémon a lieu : contre un dresseur croisé en chemin, un combat d'entraînement, ou un " +
                    "combat d'Arène face à un champion.",
                "Sacha progresse vers l'obtention d'un badge d'Arène ou vers la Ligue Pokémon.",
                "La Team Rocket (Jessie, James et Miaouss) apparaît et tente un plan pour voler des Pokémon : " +
                    "fais-la réapparaître régulièrement au fil de l'aventure (grossièrement une scène sur deux " +
                    "ou trois pour une aventure longue), toujours déjouée sans violence, souvent avec humour."
            ]),

        new ThemeDefinition(
            id: "spidey",
            displayName: "Spidey — Super-héros de quartier",
            audience: AudienceTarget.Child,
            vocabularyLevel: VocabularyLevel.Simple,
            narrativeUniverse:
                "Spidey (Peter Parker) forme une équipe de jeunes héros avec Ghost-Spider (Gwen Stacy), " +
                "agile et dotée d'ailes de toile lui permettant de planer, et Spin (Miles Morales), capable " +
                "de devenir invisible et très agile. Chaque héros utilise son sens d'araignée, ses tirs de " +
                "toiles et son agilité pour protéger le quartier. Ensemble, ils affrontent des super-vilains " +
                "hauts en couleur : Electro (contrôle et projette l'électricité), le Bouffon Vert (génie " +
                "facétieux du chaos, gadgets, planeur et bombes-lanternes), Docteur Octopus (bras " +
                "mécaniques puissants qui attrapent et manipulent des objets à distance), Rhino (force " +
                "surhumaine et armure cuirassée), l'Homme de Sable (corps de sable capable de s'étirer et " +
                "d'encaisser les chocs) et Zola (menace technologique, robots et gadgets plutôt que force " +
                "physique). Les plans de ces vilains sont toujours déjoués par la ruse et l'entraide plutôt " +
                "que par la violence.",
            safetyConstraints:
            [
                "Aucune violence graphique : les affrontements avec les super-vilains se règlent par la " +
                "ruse ou l'agilité, jamais par des coups ou blessures décrits.",
                "Aucun langage grossier ni thème adulte.",
                "Ton héroïque, rassurant, orienté entraide et courage."
            ],
            imageStyle: "illustration style bande dessinée lumineuse, dynamique, adaptée aux enfants",
            recurringStoryBeats:
            [
                "Un méfait ou un plan d'un super-vilain récurrent (Electro, le Bouffon Vert, Docteur Octopus, " +
                    "Rhino, l'Homme de Sable ou Zola) survient dans le quartier : fais-en réapparaître un " +
                    "régulièrement au fil de l'aventure (grossièrement une scène sur deux ou trois pour une " +
                    "aventure longue), toujours déjoué par la ruse ou l'entraide plutôt que par la force.",
                "L'équipe fait la rencontre d'un nouvel habitant du quartier à rassurer ou à aider.",
                "Un moment d'action met en valeur les toiles, l'agilité ou le pouvoir spécial de chacun des " +
                    "héros (Spidey, Ghost-Spider ou Spin).",
                "Un moment de travail d'équipe entre Spidey, Ghost-Spider et Spin fait la différence face à " +
                    "la difficulté du moment."
            ]),

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
            imageStyle: "illustration colorée style dessin animé, chaleureuse, adaptée aux enfants",
            recurringStoryBeats:
            [
                "Découverte d'une nouvelle espèce de dinosaure ou d'un nouveau recoin de la vallée préservée.",
                "Rencontre avec un dinosaure qui a besoin d'aide, de réconfort ou de compagnie.",
                "Une petite énigme naturelle à résoudre (météo à anticiper, chemin à retrouver, nourriture, " +
                    "abri à construire).",
                "Un moment d'amitié ou de jeu avec les dinosaures déjà rencontrés plus tôt dans l'aventure."
            ]),

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
            imageStyle: "illustration colorée style dessin animé, douce et chaleureuse, adaptée aux enfants",
            recurringStoryBeats:
            [
                "Arrivée d'un nouvel animal à accueillir et à rassurer, à la clinique ou sur le terrain.",
                "Un petit souci de santé bénin à observer puis à soigner avec douceur et patience.",
                "Rencontre avec le propriétaire de l'animal ou un habitant du quartier.",
                "Apprentissage d'un nouveau geste ou d'une nouvelle astuce de soin vétérinaire simple."
            ])
  ];


  public Task<IReadOnlyList<ThemeDefinition>> GetThemesAsync(CancellationToken cancellationToken) =>
      Task.FromResult(_themes);

  public Task<ThemeDefinition?> FindThemeAsync(string themeId, CancellationToken cancellationToken)
  {
    var theme = _themes.FirstOrDefault(t => string.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase));
    return Task.FromResult(theme);
  }
}
