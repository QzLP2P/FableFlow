using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FableFlow.Application.Abstractions;
using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Policies;
using FableFlow.Infrastructure.Ai.Prompts;

namespace FableFlow.Infrastructure.Ai;

/// <summary>
/// Construit les prompts de chapitre, d'idées de départ et d'illustration à partir des gabarits
/// versionnés de <see cref="PromptTemplateRegistry"/>. Les données saisies par le parent (héros,
/// idée personnelle) sont isolées dans des blocs délimités et présentées comme des données, jamais
/// comme des instructions.
/// </summary>
public sealed partial class PromptBuilder : IPromptBuilder
{
  private const string _dataStart = "<<<DONNEES";
  private const string _dataEnd = "DONNEES>>>";
  private const int _maxImageDescriptionLength = 500;

  public StoryPrompt BuildChapterPrompt(ChapterGenerationRequest request)
  {
    var theme = request.Theme;
    var session = request.Session;
    var transition = request.Transition;
    var guidance = NarrationPolicy.For(session.Hero.Age, session.ReaderMode);

    var userPrompt = Render(PromptTemplateRegistry.ChapterUserTemplate, new Dictionary<string, string>
    {
      ["theme_name"] = theme.DisplayName,
      ["narrative_universe"] = theme.NarrativeUniverse,
      ["recurring_story_beats"] = FormatBulletList(
          theme.RecurringStoryBeats,
          "(aucun élément récurrent spécifique : improvise librement dans le cadre de l'univers.)"),
      ["safety_constraints"] = FormatBulletList(theme.SafetyConstraints),
      ["hero"] = DescribeHero(session.Hero),
      ["narration_guidance"] = DescribeGuidance(session.Hero.Age, session.ReaderMode, guidance),
      ["premise"] = DescribePremise(session),
      ["story_outline"] = DescribeOutline(session, transition),
      ["running_summary"] = string.IsNullOrWhiteSpace(session.RunningSummary)
          ? "(Aucun chapitre précédent : c'est le tout début de l'histoire.)"
          : session.RunningSummary,
      ["key_facts"] = FormatBulletList(session.KeyFacts, "(aucun pour l'instant)"),
      ["chapter_number"] = transition.NextChapterNumber.ToString(CultureInfo.InvariantCulture),
      ["total_chapters"] = session.TotalChapters.ToString(CultureInfo.InvariantCulture),
      ["chapter_kind"] = transition.NextKind.ToString(),
      ["decision_chapters"] = string.Join(", ", session.Schedule.DecisionChapters),
      ["selected_choice"] = DescribeSelectedChoice(transition),
      ["expected_structure"] = DescribeExpectedStructure(session, transition, guidance),
    });

    return new StoryPrompt(
        PromptTemplateRegistry.ChapterSystemPrompt,
        userPrompt,
        PromptTemplateRegistry.ChapterTemplateVersion,
        transition.NextKind,
        transition.NextChapterNumber);
  }

  public StoryPremisePrompt BuildPremisePrompt(StoryPremiseRequest request)
  {
    var guidance = NarrationPolicy.For(request.Hero.Age, request.ReaderMode);
    var schedule = ChapterSchedule.For(request.Length);

    var userPrompt = Render(PromptTemplateRegistry.PremiseUserTemplate, new Dictionary<string, string>
    {
      ["theme_name"] = request.Theme.DisplayName,
      ["narrative_universe"] = request.Theme.NarrativeUniverse,
      ["recurring_story_beats"] = FormatBulletList(
          request.Theme.RecurringStoryBeats,
          "(aucun élément récurrent spécifique : improvise librement dans le cadre de l'univers.)"),
      ["safety_constraints"] = FormatBulletList(request.Theme.SafetyConstraints),
      ["hero"] = DescribeHero(request.Hero),
      ["narration_guidance"] = DescribeGuidance(request.Hero.Age, request.ReaderMode, guidance),
      ["story_length"] = request.Length == StoryLength.Short
          ? $"histoire courte de {schedule.TotalChapters} chapitres, une seule intrigue simple"
          : $"histoire plus longue de {schedule.TotalChapters} chapitres, avec plusieurs étapes",
      ["count"] = request.Count.ToString(CultureInfo.InvariantCulture),
    });

    return new StoryPremisePrompt(
        PromptTemplateRegistry.PremiseSystemPrompt,
        userPrompt,
        PromptTemplateRegistry.PremiseTemplateVersion,
        request.Count);
  }

  public StoryImagePrompt BuildImagePrompt(ThemeDefinition theme, HeroProfile hero, string genericSceneDescription)
  {
    // La description de scène provient du champ "imagePrompt" du LLM : purement visuelle, sans nom
    // propre ni marque (voir PromptTemplateRegistry.ChapterSystemPrompt). Le prénom de l'enfant n'est
    // jamais transmis au fournisseur d'image ; seuls l'âge et l'apparence décrite par le parent le sont.
    var heroDescription = new StringBuilder()
        .Append(CultureInfo.InvariantCulture, $"un enfant de {hero.Age} ans");
    if (hero.Appearance is { } appearance)
    {
      heroDescription.Append(" (").Append(AsData(appearance)).Append(')');
    }

    var prompt =
        "Illustration de livre d'histoire pour enfants. " +
        $"L'enfant héros est {heroDescription}. " +
        $"Scène : {Truncate(AsData(genericSceneDescription), _maxImageDescriptionLength)} " +
        $"Contraintes : {string.Join(" ", theme.SafetyConstraints)} " +
        "Aucun texte, aucune lettre ni bulle dans l'image.";

    return new StoryImagePrompt(prompt, theme.ImageStyle);
  }

  private static string DescribeHero(HeroProfile hero)
  {
    var builder = new StringBuilder()
        .AppendLine(_dataStart)
        .AppendLine(CultureInfo.InvariantCulture, $"Prénom : {AsData(hero.Name)}")
        .AppendLine(CultureInfo.InvariantCulture, $"Âge : {hero.Age} ans")
        .AppendLine(CultureInfo.InvariantCulture, $"Compagnons : {(hero.Companions.Count == 0 ? "aucun" : string.Join(" ; ", hero.Companions.Select(AsData)))}")
        .AppendLine(CultureInfo.InvariantCulture, $"Centres d'intérêt : {OrNotProvided(hero.Interests)}")
        .AppendLine(CultureInfo.InvariantCulture, $"Caractère : {OrNotProvided(hero.Personality)}")
        .AppendLine(CultureInfo.InvariantCulture, $"Apparence : {OrNotProvided(hero.Appearance)}")
        .Append(_dataEnd);

    return builder.ToString();
  }

  private static string DescribeGuidance(int age, ReaderMode readerMode, NarrationGuidance guidance)
  {
    var reader = readerMode == ReaderMode.ChildReader
        ? "l'enfant lit lui-même l'histoire"
        : "un adulte lit l'histoire à voix haute à l'enfant";

    return string.Join(Environment.NewLine,
        $"Âge du héros : {age} ans (repères {guidance.AgeBand}).",
        $"Mode de lecture : {reader}.",
        $"Longueur de chaque chapitre : entre {guidance.MinWordsPerChapter} et {guidance.MaxWordsPerChapter} mots.",
        $"Phrases : {guidance.MaxWordsPerSentence} mots au plus. {guidance.Sentences}",
        $"Vocabulaire : {guidance.Vocabulary}",
        $"Sujets et émotions : {guidance.SubjectTreatment}",
        $"Lecture : {guidance.ReaderSupport}");
  }

  private static string DescribePremise(AdventureSession session)
  {
    var builder = new StringBuilder().AppendLine(_dataStart);
    if (!string.IsNullOrWhiteSpace(session.Title))
    {
      builder.AppendLine(CultureInfo.InvariantCulture, $"Titre choisi : {AsData(session.Title)}");
    }

    return builder
        .AppendLine(CultureInfo.InvariantCulture, $"Idée : {AsData(session.Premise)}")
        .Append(_dataEnd)
        .ToString();
  }

  private static string DescribeOutline(AdventureSession session, ChapterTransition transition)
  {
    if (transition.NextChapterNumber == 1)
    {
      return "(Aucun plan pour l'instant : définis-le dans le champ \"storyOutline\" de ta réponse.)";
    }

    return session.StoryOutline.Count == 0
        ? "(Aucun plan disponible : reste cohérent avec l'idée de départ et le résumé.)"
        : FormatBulletList(session.StoryOutline);
  }

  private static string DescribeSelectedChoice(ChapterTransition transition)
  {
    if (transition.SelectedChoice is { } choice)
    {
      return $"Le héros a choisi : « {AsData(choice.Label)} ». Commence le chapitre par la mise en œuvre de ce choix et montre qu'il compte pour la suite.";
    }

    return transition.NextChapterNumber == 1
        ? "(Aucune : c'est le début de l'histoire.)"
        : "(Aucune : le lecteur a tourné la page, poursuis directement le récit.)";
  }

  private static string DescribeExpectedStructure(
      AdventureSession session,
      ChapterTransition transition,
      NarrationGuidance guidance)
  {
    var number = transition.NextChapterNumber;
    var total = session.TotalChapters;
    var length = $"Vise entre {guidance.MinWordsPerChapter} et {guidance.MaxWordsPerChapter} mots.";

    if (number == 1)
    {
      var title = string.IsNullOrWhiteSpace(session.Title)
          ? "Invente un titre court et chaleureux (8 mots au plus) dans \"title\"."
          : "Reprends tel quel le titre choisi dans \"title\".";

      return string.Join(' ',
          "Ouvre l'histoire : présente le héros, fais-le entrer dans l'univers et installe l'idée de départ avec un premier enjeu clair.",
          title,
          $"Fournis aussi \"storyOutline\" : 3 à 5 grandes étapes couvrant toute l'histoire jusqu'à sa conclusion au chapitre {total}, assez larges pour laisser place aux choix du héros.",
          transition.NextKind == ChapterKind.Decision
              ? "Termine sur un moment de choix avec 2 ou 3 actions."
              : "Aucun choix dans ce chapitre.",
          length);
    }

    return transition.NextKind switch
    {
      ChapterKind.Decision => string.Join(' ',
          DescribeStage(number, total),
          "Amène naturellement un moment où le héros doit choisir et termine le texte juste avant la décision.",
          "Propose 2 ou 3 actions courtes, distinctes et toutes positives dans \"choices\".",
          length),
      ChapterKind.Ending => string.Join(' ',
          "Dernier chapitre : conclus l'histoire en dénouant l'enjeu principal posé depuis le début et en tenant compte des choix du héros.",
          "Montre ce que le héros a accompli ou appris, puis termine sur une note douce et apaisante. Aucun choix.",
          length),
      _ => string.Join(' ',
          DescribeStage(number, total),
          "Aucun choix dans ce chapitre : termine sur une transition qui donne envie de lire la suite.",
          length),
    };
  }

  private static string DescribeStage(int chapterNumber, int totalChapters)
  {
    var progress = (double)chapterNumber / totalChapters;
    var remaining = totalChapters - chapterNumber;

    return progress switch
    {
      <= 0.4 => "Développe l'aventure : le héros explore, fait une rencontre ou découvre un premier obstacle lié à l'enjeu.",
      <= 0.75 => "Cœur de l'histoire : une péripétie nouvelle fait évoluer l'enjeu et les liens avec les personnages déjà rencontrés.",
      _ => $"Approche de la fin (encore {remaining} chapitre(s) après celui-ci) : resserre l'intrigue autour de l'enjeu principal et prépare la conclusion.",
    };
  }

  /// <summary>
  /// Substitution en une seule passe : une donnée injectée contenant un jeton ({{...}}) n'est jamais
  /// réinterprétée, et un jeton inconnu dans un gabarit est une erreur de programmation.
  /// </summary>
  private static string Render(string template, IReadOnlyDictionary<string, string> values) =>
      TokenPattern().Replace(template, match => values.TryGetValue(match.Groups[1].Value, out var value)
          ? value
          : throw new InvalidOperationException($"Jeton de gabarit inconnu : {match.Value}."));

  /// <summary>Neutralise une donnée saisie : une seule ligne, sans délimiteur de bloc.</summary>
  private static string AsData(string value)
  {
    var singleLine = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    return singleLine.Replace("<<<", string.Empty, StringComparison.Ordinal)
        .Replace(">>>", string.Empty, StringComparison.Ordinal);
  }

  private static string OrNotProvided(string? value) =>
      string.IsNullOrWhiteSpace(value) ? "non précisé" : AsData(value);

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
      text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "…");

  [GeneratedRegex(@"\{\{([a-z_]+)\}\}", RegexOptions.CultureInvariant)]
  private static partial Regex TokenPattern();
}
