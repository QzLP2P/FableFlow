using FableFlow.Domain.Entities;

namespace FableFlow.Application.Abstractions;

/// <summary>
/// Fournit les thèmes disponibles et leurs garde-fous de contenu.
/// MVP : thèmes codés en dur. Évolution : base de données.
/// </summary>
public interface IThemePolicyProvider
{
  Task<IReadOnlyList<ThemeDefinition>> GetThemesAsync(CancellationToken cancellationToken);

  Task<ThemeDefinition?> FindThemeAsync(string themeId, CancellationToken cancellationToken);
}
