namespace FableFlow.Infrastructure.Ai.Prompts;

/// <summary>
/// Registre des gabarits de prompt versionnés utilisés pour la génération narrative.
/// Toute évolution de formulation doit créer une nouvelle version plutôt que muter l'existante,
/// afin de garder les générations passées reproductibles et auditable.
/// </summary>
public static class PromptTemplateRegistry
{
    /// <summary>Version courante du gabarit de génération de scène.</summary>
    public const string SceneTemplateVersion = "scene-generation@v1";

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
            "keyFacts": ["fait clé 1", "fait clé 2"]
          }
        - Fournis entre 2 et 3 choix maximum, jamais ambigus, clairement distincts dans leurs conséquences.
        - Pour une scène de fin (ending), le tableau "choices" doit être vide.
        - Respecte strictement le niveau de vocabulaire et le public cible indiqués.
        - Respecte strictement les contraintes de sécurité de contenu indiquées (texte uniquement,
          aucune image n'est générée par toi).
        - Garde un ton cohérent avec le thème donné.
        - Prépare une issue cohérente si la session est proche de sa fin (victoire ou défaite).
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

        # Choix effectué par l'utilisateur
        {{selected_choice}}

        # Conditions de victoire / défaite
        {{victory_defeat_conditions}}

        # Structure attendue de la scène suivante
        {{expected_structure}}
        """;
}
