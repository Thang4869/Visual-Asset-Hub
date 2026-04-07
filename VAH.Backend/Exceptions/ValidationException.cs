namespace VAH.Backend.Exceptions;

using FluentValidation.Results;

/// <summary>
/// Thrown when a business rule or input validation fails. Maps to HTTP 400.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Per-field validation errors (empty if this is a general validation failure).</summary>
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}
