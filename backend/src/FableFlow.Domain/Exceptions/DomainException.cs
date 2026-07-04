namespace FableFlow.Domain.Exceptions;

/// <summary>Erreur levée lorsqu'un invariant métier est violé.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
