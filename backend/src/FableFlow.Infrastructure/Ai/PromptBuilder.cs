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
        .Replace("{{audience}}", request.Theme.Audience.ToString())
        .Replace("{{vocabulary_level}}", request.Theme.VocabularyLevel.ToString())
        .Replace("{{safety_constraints}}", FormatBulletList(request.Theme.SafetyConstraints))
        .Replace("{{current_scene_number}}", (request.Session.CurrentSceneNumber + 1).ToString())
        .Replace("{{target_scene_count}}", request.Session.TargetSceneCount.ToString())
        .Replace("{{bad_choice_count}}", request.Session.BadChoiceCount.ToString())
        .Replace("{{max_bad_choices}}", request.Session.MaxBadChoices.ToString())
        .Replace("{{scene_kind}}", request.Kind.ToString())
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
        .Replace("{{expected_structure}}", BuildExpectedStructure(request.Kind));

    return new StoryPrompt(
        PromptTemplateRegistry.SceneSystemPrompt,
        userPrompt,
        PromptTemplateRegistry.SceneTemplateVersion,
        request.Kind,
        request.Session.CurrentSceneNumber + 1);
  }

  public StoryImagePrompt BuildImagePrompt(SceneGenerationRequest request, string genericSceneDescription)
  {
    // "genericSceneDescription" provient du champ "imagePrompt" généré par le LLM (voir
    // PromptTemplateRegistry.SceneSystemPrompt) : une description purement visuelle de la scène,
    // sans nom de thème, de personnage ni de marque déposée, afin de rester conforme aux filtres
    // de contenu du fournisseur d'image (voir README, section propriété intellectuelle).
    var prompt =
        $"Illustration pour une histoire interactive. " +
        $"Scène : {Truncate(genericSceneDescription, 500)} " +
        $"Contraintes : {string.Join(" ", request.Theme.SafetyConstraints)} " +
        "Aucun texte ni typographie dans l'image.";

    return new StoryImagePrompt(prompt, request.Theme.ImageStyle);
  }

  public StoryPremisePrompt BuildPremisePrompt(ThemeDefinition theme, int count)
  {
    var userPrompt = PromptTemplateRegistry.PremiseUserTemplate
        .Replace("{{theme_name}}", theme.DisplayName)
        .Replace("{{narrative_universe}}", theme.NarrativeUniverse)
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

  private static string BuildExpectedStructure(SceneKind kind) => kind switch
  {
    SceneKind.Initial =>
        "Pose le décor et le personnage principal, introduis un premier enjeu clair, " +
        "puis propose 2 à 3 choix d'action distincts.",
    SceneKind.Continuation =>
        "Fais suite directement au choix précédent, fais progresser l'enjeu principal, " +
        "puis propose 2 à 3 nouveaux choix distincts.",
    SceneKind.Ending =>
        "Conclus l'histoire de façon cohérente avec tout ce qui précède. Aucun choix (tableau vide).",
    _ => throw new ArgumentOutOfRangeException(nameof(kind))
  };

  private static string FormatBulletList(IReadOnlyList<string> items)
  {
    if (items.Count == 0)
    {
      return "(aucune contrainte spécifique)";
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
