using FluentValidation;
using MediatR;

namespace FableFlow.Application.Common.Behaviors;

/// <summary>
/// Exécute les validateurs FluentValidation enregistrés avant que la requête n'atteigne
/// son handler. Centralise la validation hors des handlers (règle CQRS).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
  private readonly IEnumerable<IValidator<TRequest>> _validators;

  public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) =>
      _validators = validators;

  public async Task<TResponse> Handle(
      TRequest request,
      RequestHandlerDelegate<TResponse> next,
      CancellationToken cancellationToken)
  {
    if (!_validators.Any())
    {
      return await next();
    }

    var context = new ValidationContext<TRequest>(request);

    var results = await Task.WhenAll(
        _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

    var failures = results
        .SelectMany(r => r.Errors)
        .Where(f => f is not null)
        .ToArray();

    if (failures.Length != 0)
    {
      throw new ValidationException(failures);
    }

    return await next();
  }
}
