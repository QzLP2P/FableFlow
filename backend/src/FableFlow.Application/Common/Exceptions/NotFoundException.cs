namespace FableFlow.Application.Common.Exceptions;

/// <summary>Ressource métier introuvable.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} introuvable (clé : {key}).")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
