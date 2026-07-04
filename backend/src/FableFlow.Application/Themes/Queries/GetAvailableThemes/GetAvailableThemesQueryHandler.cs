using FableFlow.Application.Abstractions;
using FableFlow.Application.Common.Mapping;
using FableFlow.Application.Themes.Dtos;
using MediatR;

namespace FableFlow.Application.Themes.Queries.GetAvailableThemes;

public sealed class GetAvailableThemesQueryHandler
    : IRequestHandler<GetAvailableThemesQuery, IReadOnlyList<ThemeDto>>
{
    private readonly IThemePolicyProvider _themeProvider;

    public GetAvailableThemesQueryHandler(IThemePolicyProvider themeProvider) =>
        _themeProvider = themeProvider;

    public async Task<IReadOnlyList<ThemeDto>> Handle(
        GetAvailableThemesQuery request,
        CancellationToken cancellationToken)
    {
        var themes = await _themeProvider.GetThemesAsync(cancellationToken);
        return [.. themes.Select(t => t.ToDto())];
    }
}
