using FableFlow.Domain.Entities;
using FableFlow.Domain.Enums;
using FableFlow.Domain.Exceptions;
using FluentAssertions;

namespace FableFlow.Domain.Tests.Entities;

public class AdventureSessionTests
{
  [Fact]
  public void Start_WithValidArguments_CreatesInProgressSession()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 5);

    session.Status.Should().Be(SessionStatus.InProgress);
    session.ThemeId.Should().Be("pokemon");
    session.TargetSceneCount.Should().Be(5);
    session.BadChoiceCount.Should().Be(0);
    session.CurrentSceneNumber.Should().Be(0);
    session.NarrativePremise.Should().BeNull();
  }

  [Fact]
  public void Start_WithNarrativePremise_StoresIt()
  {
    var session = AdventureSession.Start(
        Guid.NewGuid(),
        "pokemon",
        targetSceneCount: 5,
        narrativePremise: "Un tournoi régional où un rival mystérieux sème le trouble.");

    session.NarrativePremise.Should().Be("Un tournoi régional où un rival mystérieux sème le trouble.");
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(21)]
  public void Start_WithSceneCountOutOfRange_Throws(int sceneCount)
  {
    var act = () => AdventureSession.Start(Guid.NewGuid(), "pokemon", sceneCount);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void Start_WithEmptyTheme_Throws()
  {
    var act = () => AdventureSession.Start(Guid.NewGuid(), string.Empty, targetSceneCount: 5);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void AttachScene_WithExpectedSceneNumber_AdvancesCurrentSceneNumber()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 5);
    var scene = CreateScene(sceneNumber: 1);

    session.AttachScene(scene);

    session.CurrentSceneNumber.Should().Be(1);
    session.CurrentScene.Should().Be(scene);
  }

  [Fact]
  public void AttachScene_WithUnexpectedSceneNumber_Throws()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 5);
    var scene = CreateScene(sceneNumber: 2);

    var act = () => session.AttachScene(scene);

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void RecordChoice_WithGoodChoiceBeforeLastScene_RequiresNextScene()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 3);
    session.AttachScene(CreateScene(sceneNumber: 1));

    var result = session.RecordChoice("good");

    result.RequiresNextScene.Should().BeTrue();
    result.IsTerminal.Should().BeFalse();
    session.Status.Should().Be(SessionStatus.InProgress);
    session.BadChoiceCount.Should().Be(0);
  }

  [Fact]
  public void RecordChoice_ReachingTargetSceneWithNoBadChoices_ResultsInWon()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 3);
    session.AttachScene(CreateScene(sceneNumber: 1));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(sceneNumber: 2));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(sceneNumber: 3));

    var result = session.RecordChoice("good");

    result.IsTerminal.Should().BeTrue();
    session.Status.Should().Be(SessionStatus.Won);
  }

  [Fact]
  public void RecordChoice_ReachingTargetSceneWithSomeBadChoices_ResultsInCompleted()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 3, maxBadChoices: 3);
    session.AttachScene(CreateScene(sceneNumber: 1));
    session.RecordChoice("bad");
    session.AttachScene(CreateScene(sceneNumber: 2));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(sceneNumber: 3));

    var result = session.RecordChoice("good");

    result.IsTerminal.Should().BeTrue();
    session.Status.Should().Be(SessionStatus.Completed);
  }

  [Fact]
  public void RecordChoice_ReachingMaxBadChoices_ResultsInLostBeforeTargetScene()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 10, maxBadChoices: 2);
    session.AttachScene(CreateScene(sceneNumber: 1));
    session.RecordChoice("bad");
    session.AttachScene(CreateScene(sceneNumber: 2));

    var result = session.RecordChoice("bad");

    result.IsTerminal.Should().BeTrue();
    result.RequiresNextScene.Should().BeFalse();
    session.Status.Should().Be(SessionStatus.Lost);
    session.CurrentSceneNumber.Should().Be(2);
    session.TargetSceneCount.Should().Be(10);
  }

  [Fact]
  public void RecordChoice_WithUnknownChoiceId_Throws()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 3);
    session.AttachScene(CreateScene(sceneNumber: 1));

    var act = () => session.RecordChoice("does-not-exist");

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void RecordChoice_OnFinishedSession_Throws()
  {
    var session = CreateWonSession();

    var act = () => session.RecordChoice("good");

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void SetOutcome_WithMismatchedStatus_Throws()
  {
    var session = CreateWonSession();

    var act = () => session.SetOutcome(AdventureOutcome.Lost("message"));

    act.Should().Throw<DomainException>();
  }

  [Fact]
  public void SetOutcome_WithMatchingStatus_SetsOutcome()
  {
    var session = CreateWonSession();

    session.SetOutcome(AdventureOutcome.Won("Bravo !"));

    session.Outcome.Should().NotBeNull();
    session.Outcome!.Message.Should().Be("Bravo !");
  }

  [Fact]
  public void AttachImageToScene_WithExistingSceneNumber_AttachesImageEvenIfNotCurrentScene()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 5);
    session.AttachScene(CreateScene(sceneNumber: 1));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(sceneNumber: 2));

    // L'image de la scène 1 arrive alors que la scène 2 est déjà la scène courante (génération
    // asynchrone en arrière-plan) : elle doit tout de même être attachée à la bonne scène.
    session.AttachImageToScene(1, "https://images.example/scene-1.png");

    session.Scenes.Single(s => s.SceneNumber == 1).ImageUrl.Should().Be("https://images.example/scene-1.png");
    session.CurrentScene!.ImageUrl.Should().BeNull();
  }

  [Fact]
  public void AttachImageToScene_WithUnknownSceneNumber_DoesNothing()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 5);
    session.AttachScene(CreateScene(sceneNumber: 1));

    var act = () => session.AttachImageToScene(99, "https://images.example/scene.png");

    act.Should().NotThrow();
  }

  [Fact]
  public void AttachImageToOutcome_AfterSetOutcome_AttachesImageToExistingOutcome()
  {
    var session = CreateWonSession();
    session.SetOutcome(AdventureOutcome.Won("Bravo !"));

    session.AttachImageToOutcome("https://images.example/outcome.png");

    session.Outcome!.ImageUrl.Should().Be("https://images.example/outcome.png");
    session.Outcome!.Message.Should().Be("Bravo !");
  }

  [Fact]
  public void AttachImageToOutcome_BeforeOutcomeIsSet_DoesNothing()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 3);

    var act = () => session.AttachImageToOutcome("https://images.example/outcome.png");

    act.Should().NotThrow();
    session.Outcome.Should().BeNull();
  }

  /// <summary>Fait progresser une session de 3 scènes jusqu'à la victoire (choix systématiquement bons).</summary>
  private static AdventureSession CreateWonSession()
  {
    var session = AdventureSession.Start(Guid.NewGuid(), "pokemon", targetSceneCount: 3);
    session.AttachScene(CreateScene(sceneNumber: 1));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(sceneNumber: 2));
    session.RecordChoice("good");
    session.AttachScene(CreateScene(sceneNumber: 3));
    session.RecordChoice("good");
    return session;
  }

  private static AdventureScene CreateScene(int sceneNumber) =>
      new(
          sceneNumber,
          $"Texte de la scène {sceneNumber}",
          [
              new SceneChoice("good", "Faire le bon choix", ChoiceOutcome.Good),
                new SceneChoice("bad", "Faire le mauvais choix", ChoiceOutcome.Bad)
          ]);
}
