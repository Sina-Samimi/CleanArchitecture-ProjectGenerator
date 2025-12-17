namespace LogTableRenameTest.SharedKernel.BaseTypes;

public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success() => new(true, null);

    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message cannot be empty", nameof(error));
        }

        return new Result(false, error);
    }
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message cannot be empty", nameof(error));
        }

        return new Result<T>(false, default, error);
    }
}
