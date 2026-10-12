using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.AdvanceChapter;

/// <summary>
/// Passe au chapitre suivant depuis un chapitre de récit (sans choix). Le numéro du chapitre vu par
/// le lecteur permet de rejeter les doublons (double clic, onglet périmé).
/// </summary>
public sealed record AdvanceChapterCommand(Guid AdventureId, int FromChapterNumber) : IRequest<AdventureDto>;
