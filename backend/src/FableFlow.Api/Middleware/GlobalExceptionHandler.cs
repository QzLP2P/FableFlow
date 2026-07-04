using FableFlow.Application.Common.Exceptions;
using FableFlow.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FableFlow.Api.Middleware;

/// <summary>
/// Traduit les exceptions applicatives en réponses <see cref="ProblemDetails"/> cohérentes,
/// sans jamais exposer de détails internes (stack trace, message d'infrastructure) au client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, extensions) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Erreur non gérée lors du traitement de la requête");
        }
        else
        {
            _logger.LogWarning(exception, "Requête refusée : {Title}", title);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}"
        };

        foreach (var (key, value) in extensions)
        {
            problemDetails.Extensions[key] = value;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, Dictionary<string, object?> Extensions) Map(Exception exception) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Une ou plusieurs erreurs de validation sont survenues.",
                new Dictionary<string, object?>
                {
                    ["errors"] = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }),

            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                notFoundException.Message,
                []),

            DomainException domainException => (
                StatusCodes.Status400BadRequest,
                domainException.Message,
                []),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Une erreur inattendue est survenue.",
                [])
        };
}
