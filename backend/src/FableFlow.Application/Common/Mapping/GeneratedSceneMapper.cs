using FableFlow.Application.Abstractions.Generation;
using FableFlow.Domain.Entities;

namespace FableFlow.Application.Common.Mapping;

/// <summary>Convertit une scène générée par le LLM en entité du domaine.</summary>
public static class GeneratedSceneMapper
{
  public static AdventureScene ToDomainScene(this GeneratedScene generated, int sceneNumber)
  {
    var choices = generated.Choices
        .Select(c => new SceneChoice(c.Id, c.Label, c.Outcome))
        .ToArray();

    return new AdventureScene(sceneNumber, generated.Text, choices);
  }
}
