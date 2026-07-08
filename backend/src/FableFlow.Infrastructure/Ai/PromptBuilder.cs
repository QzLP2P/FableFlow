using System.Text;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Entities;
using FableFlow.Infrastructure.Ai.Prompts;

namespace FableFlow.Infrastructure.Ai;

/// <summary>
/// Construit les prompts narratifs et d'illustration à partir des gabarits versionnés
/// de <see cref="PromptTemplateRegistry"/>, en y injectant le thème, le public cible,
/// la mémoire narrative et l'état de session.
/// </summary>
public sealed class PromptBuilder : IPromptBuilder
{
  public StoryPrompt BuildScenePrompt(SceneGenerationRequest request)
  {
    var userPrompt = PromptTemplateRegistry.SceneUserTemplate
        .Replace("{{theme_name}}", request.Theme.DisplayName)
        .Replace("{{narrative_universe}}", request.Theme.NarrativeUniverse)
        .Replace("{{recurring_story_beats}}", FormatBulletList(
            request.Theme.RecurringStoryBeats,
            "(aucun élément récurrent spécifique : improvise librement dans le cadre du thème.)"))
        .Replace("{{audience}}", request.Theme.Audience.ToString())
        .Replace("{{vocabulary_level}}", request.Theme.VocabularyLevel.ToString())
        .Replace("{{safety_constraints}}", FormatBulletList(request.Theme.SafetyConstraints))
        .Replace("{{current_scene_number}}", (request.Session.CurrentSceneNumber + 1).ToString())
        .Replace("{{target_scene_count}}", request.Session.TargetSceneCount.ToString())
        .Replace("{{bad_choice_count}}", request.Session.BadChoiceCount.ToString())
        .Replace("{{max_bad_choices}}", request.Session.MaxBadChoices.ToString())
        .Replace("{{scene_kind}}", request.Kind.ToString())
        .Replace("{{story_outline}}", BuildStoryOutlineSection(request))
        .Replace("{{running_summary}}", string.IsNullOrWhiteSpace(request.Session.RunningSummary)
            ? "(Aucune scène précédente, il s'agit du tout début de l'aventure.)"
            : request.Session.RunningSummary)
        .Replace("{{narrative_premise}}", string.IsNullOrWhiteSpace(request.Session.NarrativePremise)
            ? "(Aucun axe imposé : improvise librement dans le cadre du thème.)"
            : $"L'utilisateur a choisi cet axe de départ, respecte-le tout au long de l'aventure : « {request.Session.NarrativePremise} »")
        .Replace("{{selected_choice}}", request.SelectedChoice is null
            ? "(Aucun, il s'agit de la première scène.)"
            : $"L'utilisateur a choisi : « {request.SelectedChoice.Label} ».")
        .Replace("{{victory_defeat_conditions}}", BuildVictoryDefeatConditions(request))
        .Replace("{{expected_structure}}", BuildExpectedStructure(request));

    return new StoryPrompt(
        PromptTemplateRegistry.SceneSystemPrompt,
        userPrompt,
        PromptTemplateRegistry.SceneTemplateVersion,
        request.Kind,
        request.Session.CurrentSceneNumber + 1);
  }

  public StoryImagePrompt BuildImagePrompt(ThemeDefinition theme, string genericSceneDescription)
  {
    // "genericSceneDescription" provient du champ "imagePrompt" généré par le LLM (voir
    // PromptTemplateRegistry.SceneSystemPrompt) : une description purement visuelle de la scène,
    // sans nom de thème, de personnage ni de marque déposée, afin de rester conforme aux filtres
    // de contenu du fournisseur d'image (voir README, section propriété intellectuelle).
    var prompt =
        $"Illustration pour une histoire interactive. " +
        $"Scène : {Truncate(genericSceneDescription, 500)} " +
        $"Contraintes : {string.Join(" ", theme.SafetyConstraints)} " +
        "Aucun texte ni typographie dans l'image.";

    return new StoryImagePrompt(prompt, theme.ImageStyle);
  }

  public StoryPremisePrompt BuildPremisePrompt(ThemeDefinition theme, int count)
  {
    var userPrompt = PromptTemplateRegistry.PremiseUserTemplate
        .Replace("{{theme_name}}", theme.DisplayName)
        .Replace("{{narrative_universe}}", theme.NarrativeUniverse)
        .Replace("{{recurring_story_beats}}", FormatBulletList(
            theme.RecurringStoryBeats,
            "(aucun élément récurrent spécifique : improvise librement dans le cadre du thème.)"))
        .Replace("{{audience}}", theme.Audience.ToString())
        .Replace("{{vocabulary_level}}", theme.VocabularyLevel.ToString())
        .Replace("{{safety_constraints}}", FormatBulletList(theme.SafetyConstraints))
        .Replace("{{count}}", count.ToString());

    return new StoryPremisePrompt(
        PromptTemplateRegistry.PremiseSystemPrompt,
        userPrompt,
        PromptTemplateRegistry.PremiseTemplateVersion,
        count);
  }

  /// <summary>
  /// Construit la section "plan d'ensemble" du prompt utilisateur. Pour une scène Initial, le plan
  /// n'existe pas encore (c'est au LLM de le produire dans sa réponse, voir la règle système
  /// "storyOutline") ; pour une continuation/fin, on redonne le plan déjà fixé sur la session
  /// (<see cref="AdventureSession.StoryOutline"/>) comme fil conducteur.
  /// </summary>
  private static string BuildStoryOutlineSection(SceneGenerationRequest request)
  {
    if (request.Kind == SceneKind.Initial)
    {
      return "(Aucun plan existant : à toi de le définir dans le champ \"storyOutline\" de ta réponse JSON.)";
    }

    return request.Session.StoryOutline.Count == 0
        ? "(Aucun plan d'ensemble disponible : improvise en cohérence avec le résumé et l'axe narratif ci-dessus.)"
        : FormatBulletList(request.Session.StoryOutline);
  }

  private static string BuildVictoryDefeatConditions(SceneGenerationRequest request)
  {
    var remainingScenes = request.Session.TargetSceneCount - request.Session.CurrentSceneNumber;
    var remainingAllowedMistakes = request.Session.MaxBadChoices - request.Session.BadChoiceCount;

    return request.Kind switch
    {
      SceneKind.Ending when remainingAllowedMistakes <= 0 =>
          "Le joueur a atteint le nombre maximum de mauvais choix : conclus sur une défaite " +
          "cohérente avec l'histoire, sans dramatisation excessive pour le public cible.",
      SceneKind.Ending =>
          "Le joueur a atteint la dernière scène cible : conclus sur une fin cohérente " +
          "(victoire si le parcours a été globalement positif, fin neutre sinon).",
      _ =>
          $"Il reste {remainingScenes} scène(s) avant la fin cible et " +
          $"{remainingAllowedMistakes} mauvais choix possible(s) avant une défaite. " +
          "Prépare progressivement une issue cohérente à l'approche de ces limites."
    };
  }

  /// <summary>
  /// Décrit la structure attendue de la prochaine scène. Pour une continuation, la position relative
  /// dans l'arc narratif (scène courante / nombre total de scènes, entre <see cref="AdventureSession.MinSceneCount"/>
  /// et <see cref="AdventureSession.MaxSceneCount"/>) détermine le rythme à adopter, afin qu'une
  /// aventure courte aille à l'essentiel et qu'une aventure longue prenne le temps d'installer
  /// plusieurs péripéties avant le dénouement.
  /// </summary>
  private static string BuildExpectedStructure(SceneGenerationRequest request) => request.Kind switch
  {
    SceneKind.Initial =>
        $"Pose le décor et le personnage principal, introduis un premier enjeu clair adapté à une " +
        $"aventure de {request.Session.TargetSceneCount} scène(s) au total, puis termine par 2 à 3 " +
        "choix d'action distincts.",
    SceneKind.Continuation =>
        BuildContinuationStructure(request.Session.CurrentSceneNumber + 1, request.Session.TargetSceneCount),
    SceneKind.Ending =>
        "Conclus l'histoire de façon cohérente avec tout ce qui précède, en dénouant l'enjeu " +
        "principal installé depuis le début de l'aventure. Aucun choix (tableau vide).",
    _ => throw new ArgumentOutOfRangeException(nameof(request))
  };

  private static string BuildContinuationStructure(int sceneNumber, int targetSceneCount)
  {
    var progressRatio = (double)sceneNumber / targetSceneCount;
    var remainingScenes = targetSceneCount - sceneNumber;

    var stageGuidance = progressRatio switch
    {
      <= 0.34 =>
          "Phase de développement initial : fais progresser un premier enjeu ou une première " +
          "péripétie issue de l'univers, en gardant de la place pour la suite de l'aventure.",
      <= 0.7 =>
          "Cœur de l'aventure : introduis une nouvelle péripétie ou un enjeu secondaire, fais évoluer " +
          "les relations avec les personnages déjà rencontrés, et augmente progressivement la difficulté.",
      _ =>
          $"Approche du dénouement (encore {remainingScenes} scène(s) avant la fin) : resserre " +
          "l'intrigue autour de l'enjeu principal et prépare une montée en tension vers la conclusion."
    };

    return $"Fais suite directement au choix précédent. {stageGuidance} Termine par 2 à 3 nouveaux choix distincts.";
  }

  private static string FormatBulletList(
      IReadOnlyList<string> items,
      string emptyMessage = "(aucune contrainte spécifique)")
  {
    if (items.Count == 0)
    {
      return emptyMessage;
    }

    var builder = new StringBuilder();
    foreach (var item in items)
    {
      builder.Append("- ").AppendLine(item);
    }

    return builder.ToString().TrimEnd();
  }

  private static string Truncate(string text, int maxLength) =>
      text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "...");
}
