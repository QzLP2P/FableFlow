using FableFlow.Application.Abstractions;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;

namespace FableFlow.Infrastructure.Themes;

/// <summary>
/// Fournit les univers disponibles, codés en dur pour le MVP.
/// Évolution prévue : chargement depuis une base de données (voir <see cref="IThemePolicyProvider"/>).
/// Dans chaque univers, le héros est l'enfant de l'histoire : les personnages connus n'y sont que
/// des alliés, des guides ou des rencontres.
/// </summary>
public sealed class HardcodedThemePolicyProvider : IThemePolicyProvider
{
  private static readonly IReadOnlyList<ThemeDefinition> _themes =
  [
      new ThemeDefinition(
          id: "pokemon",
          displayName: "Pokémon — Apprenti dresseur",
          description: "Ton enfant débute comme dresseur de Pokémon : rencontres, premier compagnon et petits défis d'arène.",
          audience: AudienceTarget.Child,
          narrativeUniverse:
              "Le héros est un jeune dresseur ou une jeune dresseuse qui débute dans le monde des " +
              "Pokémon et choisit ou rencontre son premier Pokémon compagnon. Il voyage de ville en " +
              "ville, découvre des Pokémon sauvages (Pikachu, Salamèche, Bulbizarre, Carapuce, Évoli, " +
              "Rondoudou, Psykokwak...), vit des combats amicaux et sportifs, visite des Centres Pokémon " +
              "et progresse vers un badge d'arène. Sacha et son Pikachu, le Professeur Chen, l'Infirmière " +
              "Joëlle ou des champions d'arène comme Ondine et Pierre peuvent apparaître comme alliés, " +
              "guides ou adversaires bienveillants, jamais à la place du héros. La Team Rocket (Jessie, " +
              "James et Miaouss) tente parfois des plans maladroits, toujours déjoués avec humour.",
          safetyConstraints:
          [
              "Aucune violence graphique : les combats restent sportifs, sans blessure décrite, et se " +
              "terminent par une poignée de main ou un soin au Centre Pokémon.",
              "La Team Rocket est maladroite et comique, jamais réellement menaçante.",
              "Aucun langage grossier ni thème adulte.",
              "Ton positif, encourageant, orienté amitié avec les Pokémon et persévérance."
          ],
          imageStyle: "illustration colorée style dessin animé, douce, adaptée aux enfants",
          recurringStoryBeats:
          [
              "Le héros rencontre un Pokémon sauvage et tente de s'en faire un ami ou de le capturer avec douceur.",
              "Un combat amical a lieu : entraînement, dresseur croisé en chemin ou défi d'arène.",
              "Le héros prend soin de ses Pokémon (repas, soins au Centre Pokémon, entraînement).",
              "La Team Rocket tente un plan maladroit pour voler des Pokémon et le héros le déjoue avec ruse.",
              "Un allié connu (Sacha, le Professeur Chen, un champion d'arène) donne un conseil précieux."
          ]),

      new ThemeDefinition(
          id: "spidey",
          displayName: "Spidey — Héros de quartier",
          description: "Ton enfant rejoint l'équipe de Spidey pour protéger le quartier avec ruse et entraide.",
          audience: AudienceTarget.Child,
          narrativeUniverse:
              "Le héros est un enfant du quartier qui découvre qu'il peut aider les super-héros : grâce " +
              "à son courage, ses idées et parfois un gadget prêté par l'équipe, il devient un membre " +
              "à part entière de l'équipe Spidey. Spidey (Peter Parker), Ghost-Spider (Gwen Stacy, ailes " +
              "de toile pour planer) et Spin (Miles Morales, invisible et très agile) sont ses alliés et " +
              "mentors, jamais à sa place. Ils affrontent ensemble des super-vilains hauts en couleur : " +
              "Electro, le Bouffon Vert, Docteur Octopus, Rhino, l'Homme de Sable ou Zola, dont les plans " +
              "sont toujours déjoués par la ruse et l'entraide plutôt que par la force.",
          safetyConstraints:
          [
              "Aucune violence graphique : les affrontements se règlent par la ruse, l'agilité ou " +
              "l'entraide, jamais par des coups ou des blessures décrits.",
              "Les super-vilains sont plus farceurs que terrifiants et finissent toujours arrêtés sans mal.",
              "Aucun langage grossier ni thème adulte.",
              "Ton héroïque, rassurant, orienté entraide et courage."
          ],
          imageStyle: "illustration style bande dessinée lumineuse, dynamique, adaptée aux enfants",
          recurringStoryBeats:
          [
              "Un super-vilain récurrent prépare un méfait dans le quartier et le héros remarque un indice.",
              "Le héros aide un habitant du quartier à reprendre courage.",
              "Spidey, Ghost-Spider ou Spin montre au héros une astuce ou lui confie une mission.",
              "Le travail d'équipe entre le héros et les Spidey fait la différence face à la difficulté."
          ]),

      new ThemeDefinition(
          id: "dinosaur",
          displayName: "Dinosaures — Vallée préhistorique",
          description: "Ton enfant explore une vallée secrète peuplée de dinosaures paisibles et se lie d'amitié avec eux.",
          audience: AudienceTarget.Child,
          narrativeUniverse:
              "Le héros découvre une vallée préservée peuplée de dinosaures paisibles (tricératops, " +
              "diplodocus, ptéranodons, petits compsognathus curieux...). Il se lie d'amitié avec eux, " +
              "explore une jungle préhistorique, des grottes et des lacs, et résout de petites énigmes " +
              "naturelles : œufs à protéger, orage à anticiper, chemin à retrouver.",
          safetyConstraints:
          [
              "Aucune violence graphique : les dinosaures sont impressionnants mais toujours " +
              "bienveillants, jamais menaçants au point de faire peur.",
              "Aucun langage grossier ni thème adulte.",
              "Ton émerveillé, curieux, orienté découverte de la nature et entraide."
          ],
          imageStyle: "illustration colorée style dessin animé, chaleureuse, adaptée aux enfants",
          recurringStoryBeats:
          [
              "Découverte d'une nouvelle espèce de dinosaure ou d'un nouveau recoin de la vallée.",
              "Un dinosaure a besoin d'aide, de réconfort ou de compagnie.",
              "Une petite énigme naturelle à résoudre (météo, chemin, nourriture, abri).",
              "Un moment de jeu ou d'amitié avec les dinosaures déjà rencontrés."
          ]),

      new ThemeDefinition(
          id: "vet",
          displayName: "Vétérinaire — Soigneur des animaux",
          description: "Ton enfant devient apprenti vétérinaire et prend soin d'animaux avec douceur et patience.",
          audience: AudienceTarget.Child,
          narrativeUniverse:
              "Le héros est apprenti vétérinaire : il accueille et soigne des animaux dans une " +
              "clinique de quartier ou lors de sorties sur le terrain (ferme, forêt, bord de mer), " +
              "rencontre des animaux variés (chiots, chatons, oiseaux, animaux de ferme, hérissons), " +
              "les rassure et apprend à s'en occuper avec douceur, aux côtés d'une vétérinaire " +
              "bienveillante qui le guide.",
          safetyConstraints:
          [
              "Aucune scène médicale graphique ou effrayante : les soins sont toujours doux, " +
              "rassurants et sans détail clinique impressionnant.",
              "Aucun langage grossier ni thème adulte.",
              "Ton bienveillant, rassurant, orienté soin, patience et respect des animaux."
          ],
          imageStyle: "illustration colorée style dessin animé, douce et chaleureuse, adaptée aux enfants",
          recurringStoryBeats:
          [
              "Arrivée d'un nouvel animal à accueillir et à rassurer.",
              "Un petit souci de santé bénin à observer puis à soigner avec douceur.",
              "Rencontre avec le propriétaire de l'animal ou un habitant du quartier.",
              "Le héros apprend un nouveau geste de soin simple."
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
