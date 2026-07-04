namespace FableFlow.Infrastructure.Ai.Prompts;

/// <summary>
/// Registre des gabarits de prompt versionnés utilisés pour la génération narrative.
/// Toute évolution de formulation doit créer une nouvelle version plutôt que muter l'existante,
/// afin de garder les générations passées reproductibles et auditable.
/// </summary>
public static class PromptTemplateRegistry
{
  /// <summary>Version courante du gabarit de génération de scène.</summary>
  public const string SceneTemplateVersion = "scene-generation@v2";

  /// <summary>
  /// Instructions système : rôle, contrat de sortie JSON, garde-fous de contenu génériques.
  /// Les contraintes spécifiques au thème et au public sont injectées dans le prompt utilisateur.
  /// </summary>
  public const string SceneSystemPrompt = """
        Tu es le narrateur d'une aventure interactive pour l'application FableFlow.
        Tu écris une scène à la fois, en tenant compte de tout le contexte fourni, afin que
        l'histoire reste cohérente du début à la fin (pas de contradiction de ton, de personnages
        ou d'enjeux par rapport aux scènes précédentes).

        Règles impératives :
        - Réponds UNIQUEMENT avec un objet JSON valide, sans texte hors JSON, au format :
          {
            "text": "texte de la scène",
            "choices": [ { "id": "a", "label": "...", "outcome": "Good|Bad|Neutral" } ],
            "updatedSummary": "résumé cumulatif mis à jour de toute l'histoire",
            "keyFacts": ["fait clé 1", "fait clé 2"],
            "imagePrompt": "description visuelle générique de la scène"
          }
        - Fournis entre 2 et 3 choix maximum, jamais ambigus, clairement distincts dans leurs conséquences.
        - Pour une scène de fin (ending), le tableau "choices" doit être vide.
        - Respecte strictement le niveau de vocabulaire et le public cible indiqués.
        - Respecte strictement les contraintes de sécurité de contenu indiquées (texte uniquement,
          aucune image n'est générée par toi).
        - Garde un ton cohérent avec le thème donné.
        - Prépare une issue cohérente si la session est proche de sa fin (victoire ou défaite).

        Règle spécifique au champ "imagePrompt" (IMPORTANT) :
        - Le champ "text" peut nommer librement les personnages, créatures ou univers du thème
          (ex. noms propres de franchises), car c'est un contenu narratif destiné à la lecture.
        - Le champ "imagePrompt" sert en revanche à générer une illustration via un service tiers qui
          bloque tout nom propre ou marque déposée. Il doit donc décrire la MÊME scène de façon
          purement visuelle et générique, SANS AUCUN nom propre, marque, titre de franchise ou nom de
          personnage : décris plutôt l'apparence physique, les couleurs, la posture, l'environnement.
          Exemple : au lieu de « Pikachu », écris « une petite créature souris jaune aux pouvoirs électriques,
          avec de longues oreilles et des joues rouges ». Au lieu de « Spider-Man », écris « un super héros homme araignée
          masqué en combinaison rouge et bleue qui lance des toiles depuis ses mains ».
        """;


  /// <summary>Gabarit du prompt utilisateur, avec jetons de substitution.</summary>
  public const string SceneUserTemplate = """
        # Thème
        {{theme_name}} — {{narrative_universe}}

        # Public cible et vocabulaire
        Public : {{audience}}
        Niveau de vocabulaire : {{vocabulary_level}}

        # Contraintes de sécurité de contenu
        {{safety_constraints}}

        # État de la session
        Scène courante : {{current_scene_number}} / {{target_scene_count}}
        Mauvais choix jusqu'ici : {{bad_choice_count}} / {{max_bad_choices}}
        Type de scène à générer : {{scene_kind}}

        # Mémoire narrative (résumé cumulatif des scènes précédentes)
        {{running_summary}}

        # Axe narratif principal choisi par l'utilisateur au démarrage
        {{narrative_premise}}

        # Choix effectué par l'utilisateur
        {{selected_choice}}

        # Conditions de victoire / défaite
        {{victory_defeat_conditions}}

        # Structure attendue de la scène suivante
        {{expected_structure}}
        """;

  /// <summary>Version courante du gabarit de génération de propositions d'axe narratif.</summary>
  public const string PremiseTemplateVersion = "story-premises@v1";

  /// <summary>Instructions système pour la génération de plusieurs propositions d'axe narratif.</summary>
  public const string PremiseSystemPrompt = """
        Tu proposes des idées de point de départ pour une aventure interactive de l'application
        FableFlow. Chaque proposition est un axe narratif court : un titre accrocheur et une
        accroche de 1 à 2 phrases qui donne envie de jouer, sans dévoiler toute l'histoire.

        Règles impératives :
        - Réponds UNIQUEMENT avec un objet JSON valide, sans texte hors JSON, au format :
          {
            "premises": [ { "title": "...", "hook": "..." } ]
          }
        - Fournis exactement le nombre de propositions demandé.
        - Chaque proposition doit être clairement différente des autres (enjeu, lieu ou objectif différent).
        - Respecte strictement le niveau de vocabulaire et le public cible indiqués.
        - Respecte strictement les contraintes de sécurité de contenu indiquées.
        - Tu peux nommer librement les personnages, créatures ou univers du thème (contenu narratif
          destiné à la lecture, aucune image n'est générée à partir de ce texte).
        """;

  /// <summary>Gabarit du prompt utilisateur pour la génération de propositions d'axe narratif.</summary>
  public const string PremiseUserTemplate = """
        # Thème
        {{theme_name}} — {{narrative_universe}}

        # Public cible et vocabulaire
        Public : {{audience}}
        Niveau de vocabulaire : {{vocabulary_level}}

        # Contraintes de sécurité de contenu
        {{safety_constraints}}

        # Nombre de propositions attendues
        {{count}}
        """;
}
