using FableFlow.Application.Adventures.Dtos;
using MediatR;

namespace FableFlow.Application.Adventures.Commands.MakeChoice;

/// <summary>
/// Retient une action lors d'un moment de choix et écrit le chapitre qui en découle. L'action n'est
/// enregistrée qu'une fois ce chapitre écrit : un échec de génération ne la consomme pas.
/// </summary>
public sealed record MakeChoiceCommand(Guid AdventureId, int ChapterNumber, string ChoiceId) : IRequest<AdventureDto>;
