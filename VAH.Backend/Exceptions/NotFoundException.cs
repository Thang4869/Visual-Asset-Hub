namespace VAH.Backend.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found. Maps to HTTP 404.
/// </summary>
public sealed class NotFoundException(string message) : Exception(message)
{
    public NotFoundException(string entityName, object key)
        : this($"{entityName} with ID '{key}' was not found.") { }
}
