namespace VAH.Backend.Exceptions;

/// <summary>
/// Thrown when user identity cannot be resolved from the authentication context.
/// Mapped to HTTP 401 by <see cref="Middleware.GlobalExceptionHandler"/>.
/// Distinct from <see cref="UnauthorizedAccessException"/> to differentiate
/// "missing identity" from "insufficient permissions".
/// </summary>
public sealed class AuthContextMissingException()
    : Exception("User identity not found in authentication context.");
