namespace NewsletterPreferences.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyList<string> ValidationErrors { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        ValidationErrors = [];
    }

    private Result(string error, IReadOnlyList<string>? validationErrors = null)
    {
        IsSuccess = false;
        Error = error;
        ValidationErrors = validationErrors ?? [];
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
    public static Result<T> ValidationFailure(IReadOnlyList<string> errors) =>
        new("Validation failed.", errors);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IReadOnlyList<string> ValidationErrors { get; }

    private Result(bool success, string? error = null, IReadOnlyList<string>? validationErrors = null)
    {
        IsSuccess = success;
        Error = error;
        ValidationErrors = validationErrors ?? [];
    }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
    public static Result ValidationFailure(IReadOnlyList<string> errors) =>
        new(false, "Validation failed.", errors);
}
