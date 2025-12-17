namespace LogTableRenameTest.Domain.Exceptions;

public class InvalidScoreException : DomainException
{
    public InvalidScoreException(decimal score)
        : base($"Score '{score}' is outside the valid range (0-100).")
    {
    }
}
