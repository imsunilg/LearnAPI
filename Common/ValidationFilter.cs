using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LearnAPI.Common;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument.
/// Failures throw ValidationException which GlobalExceptionHandler converts into a 400 ProblemDetails.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (services.GetService(validatorType) is IValidator validator)
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(arg), context.HttpContext.RequestAborted);
                if (!result.IsValid) throw new ValidationException(result.Errors);
            }
        }
        await next();
    }
}
