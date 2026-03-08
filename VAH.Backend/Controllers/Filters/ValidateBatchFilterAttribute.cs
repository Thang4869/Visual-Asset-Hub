using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VAH.Backend.Controllers.Filters;

/// <summary>
/// Action filter that validates any <c>AssetIds</c> collection on the request body
/// against empty-check and <see cref="BulkOperationLimits.MaxBatchSize"/>.
/// Eliminates duplicated guard clauses across bulk/reorder endpoints.
/// </summary>
/// <remarks>
/// Convention: the request DTO must expose a property <c>AssetIds</c> of type
/// <see cref="ICollection{T}"/> or <see cref="IReadOnlyCollection{T}"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidateBatchFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Find the body parameter with an AssetIds property
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;

            var prop = arg.GetType().GetProperty("AssetIds");
            if (prop is null) continue;

            var value = prop.GetValue(arg);

            var count = value switch
            {
                ICollection<int> col => col.Count,
                IReadOnlyCollection<int> rc => rc.Count,
                _ => -1
            };

            if (count == 0)
            {
                context.Result = new BadRequestObjectResult(ApiErrors.EmptyBatch());
                return;
            }

            if (count > BulkOperationLimits.MaxBatchSize)
            {
                context.Result = new BadRequestObjectResult(ApiErrors.BatchSizeExceeded());
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
