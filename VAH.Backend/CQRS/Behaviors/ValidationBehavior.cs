using FluentValidation;
using MediatR;

namespace VAH.Backend.CQRS.Behaviors;

/// <summary>
/// Intercepts MediatR requests to run FluentValidation validators before handlers execute.
/// Throws a <see cref="Exceptions.ValidationException"/> containing structured errors if validation fails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            // Throw a custom exception that our Global Exception Handler will catch
            // and translate to RFC 9457 ProblemDetails (HTTP 400).
            throw new Exceptions.ValidationException(failures);
        }

        return await next();
    }
}