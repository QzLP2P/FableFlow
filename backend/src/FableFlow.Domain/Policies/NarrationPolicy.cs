using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;

namespace FableFlow.Domain.Policies;

/// <summary>
/// Consignes de narration dérivées de l'âge du héros et du mode de lecture. Elles guident la
/// génération (vocabulaire, longueur des phrases, densité, traitement des sujets) sans prétendre
/// mesurer un niveau de lecture : le mode « l'enfant lit » n'est pas une preuve de compétence.
/// </summary>
/// <param name="AgeBand">Tranche d'âge (libellé lisible).</param>
/// <param name="MinWordsPerChapter">Nombre de mots minimal visé par chapitre.</param>
/// <param name="MaxWordsPerChapter">Nombre de mots maximal visé par chapitre.</param>
/// <param name="MaxWordsPerSentence">Longueur maximale visée d'une phrase, en mots.</param>
/// <param name="Vocabulary">Consigne de familiarité du vocabulaire.</param>
/// <param name="Sentences">Consigne de complexité des phrases.</param>
/// <param name="SubjectTreatment">Situations et intensité émotionnelle adaptées à l'âge.</param>
/// <param name="ReaderSupport">Consigne propre au mode de lecture (voix haute ou lecture autonome).</param>
public sealed record NarrationGuidance(
    string AgeBand,
    int MinWordsPerChapter,
    int MaxWordsPerChapter,
    int MaxWordsPerSentence,
    string Vocabulary,
    string Sentences,
    string SubjectTreatment,
    string ReaderSupport);

/// <summary>Politique de narration par âge et mode de lecture.</summary>
public static class NarrationPolicy
{
  private sealed record AgeBandPolicy(
      int MaxAge,
      string Label,
      (int Min, int Max, int Sentence) ReadAloud,
      (int Min, int Max, int Sentence) ChildReader,
      string Vocabulary,
      string Sentences,
      string SubjectTreatment);

  private static readonly AgeBandPolicy[] _bands =
  [
    new(
        MaxAge: 4,
        Label: "3 à 4 ans",
        ReadAloud: (70, 120, 10),
        ChildReader: (30, 60, 6),
        Vocabulary: "Mots très concrets du quotidien (maison, animaux, couleurs, émotions simples). Répétitions, ritournelles et onomatopées bienvenues. Aucun mot abstrait.",
        Sentences: "Phrases très courtes, une seule idée par phrase, ordre sujet-verbe-complément. Dialogues de quelques mots.",
        SubjectTreatment: "Monde familier et rassurant. Un petit problème concret (objet perdu, ami à aider) sans aucun danger réel ni peur durable. Résolution douce et rapide."),
    new(
        MaxAge: 6,
        Label: "5 à 6 ans",
        ReadAloud: (110, 170, 12),
        ChildReader: (50, 90, 8),
        Vocabulary: "Mots courants et concrets ; au plus un ou deux mots nouveaux par chapitre, rendus clairs par le contexte.",
        Sentences: "Phrases courtes ; quelques phrases reliées par « et », « mais », « alors ». Dialogues fréquents et simples.",
        SubjectTreatment: "Petite aventure avec un suspense léger (surprise, mystère gentil). Émotions nommées (peur passagère, joie, fierté), entraide. Pas de vrai méchant : au plus un personnage maladroit vite réconcilié."),
    new(
        MaxAge: 8,
        Label: "7 à 8 ans",
        ReadAloud: (170, 250, 16),
        ChildReader: (90, 150, 12),
        Vocabulary: "Vocabulaire riche mais accessible, quelques mots imagés ; les termes propres à l'univers sont expliqués.",
        Sentences: "Alternance de phrases courtes et moyennes, subordonnées simples (« quand », « parce que », « qui »).",
        SubjectTreatment: "Quête avec obstacles, petits mystères, rivalités amicales, humour. Tension modérée, toujours résolue sans violence."),
    new(
        MaxAge: 10,
        Label: "9 à 10 ans",
        ReadAloud: (240, 340, 20),
        ChildReader: (150, 230, 16),
        Vocabulary: "Vocabulaire varié et précis, expressions imagées, mots spécifiques à l'univers.",
        Sentences: "Structures variées (subordonnées, descriptions, dialogues, action) avec un vrai rythme narratif.",
        SubjectTreatment: "Enjeux plus complexes (dilemmes, loyauté, courage, responsabilité), suspense plus marqué, personnages secondaires nuancés, sans violence graphique."),
    new(
        MaxAge: HeroProfile.MaxAge,
        Label: "11 à 12 ans",
        ReadAloud: (300, 420, 24),
        ChildReader: (200, 300, 20),
        Vocabulary: "Vocabulaire riche et nuancé, quelques mots plus soutenus.",
        Sentences: "Phrases complexes et variées, narration plus littéraire.",
        SubjectTreatment: "Thèmes plus mûrs (identité, amitié qui évolue, choix moraux nuancés), mystère développé, toujours sans contenu violent, effrayant durablement ou inapproprié."),
  ];

  public static NarrationGuidance For(int age, ReaderMode readerMode)
  {
    if (age is < HeroProfile.MinAge or > HeroProfile.MaxAge)
    {
      throw new DomainException(
          $"L'âge du héros doit être compris entre {HeroProfile.MinAge} et {HeroProfile.MaxAge} ans.");
    }

    var band = _bands.First(b => age <= b.MaxAge);
    var (min, max, sentence) = readerMode == ReaderMode.ChildReader ? band.ChildReader : band.ReadAloud;

    return new NarrationGuidance(
        band.Label,
        min,
        max,
        sentence,
        band.Vocabulary,
        band.Sentences,
        band.SubjectTreatment,
        DescribeReaderSupport(age, readerMode));
  }

  private static string DescribeReaderSupport(int age, ReaderMode readerMode) => readerMode switch
  {
    ReaderMode.AdultReadAloud =>
        $"Un adulte lit l'histoire à voix haute : soigne la musicalité, le rythme et les dialogues vivants. Le texte peut être un peu plus riche que ce qu'un enfant de {age} ans lirait seul, mais doit rester compris à l'oreille.",
    ReaderMode.ChildReader when age <= 7 =>
        "L'enfant lit seul et débute peut-être : privilégie des mots fréquents à l'orthographe régulière, évite les mots longs, rares ou aux graphies complexes, répète les mots-clés, fais des paragraphes d'une ou deux phrases et attribue clairement chaque réplique.",
    ReaderMode.ChildReader =>
        "L'enfant lit seul : texte aéré en paragraphes courts, répliques clairement attribuées, aucun mot rare sans explication par le contexte.",
    _ => throw new DomainException($"Mode de lecture inconnu : {readerMode}.")
  };
}
