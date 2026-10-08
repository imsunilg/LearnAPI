using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace LearnAPI.Common;

/// <summary>Claim type used for permissions inside the JWT (one claim per permission, e.g. "customers.delete").</summary>
public static class PermissionClaim
{
    public const string Type = "permission";
}

/// <summary>Use on any controller/action: <c>[HasPermission("reports.view")]</c>.</summary>
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute($"{PermissionPolicy.Prefix}{permission}");

public static class PermissionPolicy
{
    public const string Prefix = "perm:";
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(PermissionClaim.Type, requirement.Permission)) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>Creates a policy on demand for every "perm:xxx" name so we do not have to register each permission.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PermissionPolicy.Prefix, StringComparison.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName[PermissionPolicy.Prefix.Length..]))
                .Build();
        }
        return await base.GetPolicyAsync(policyName);
    }
}

/// <summary>Implemented by CRUD controllers: names the permission module (e.g. "customers").</summary>
public interface IPermissionScoped
{
    string PermissionModule { get; }
}

/// <summary>
/// Global filter: for controllers that implement <see cref="IPermissionScoped"/> the HTTP verb decides the permission
/// (GET → view, POST → create, PUT/PATCH → edit, DELETE → delete). A missing permission becomes a 403 ProblemDetails.
/// </summary>
public sealed class PermissionFilter : IAsyncActionFilter, Microsoft.AspNetCore.Mvc.Filters.IOrderedFilter
{
    /// <summary>Runs BEFORE [ApiController]'s automatic model-state 400 (order -2000): a caller without permission must get 403, never validation hints.</summary>
    public int Order => -3000;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // An explicit [HasPermission(...)] on the action replaces the verb-based rule (e.g. policies.issue on POST /issue).
        var explicitPolicy = context.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>()
            .Any(a => a.Policy?.StartsWith(PermissionPolicy.Prefix, StringComparison.Ordinal) == true);

        if (!explicitPolicy && context.Controller is IPermissionScoped scoped)
        {
            var verb = context.HttpContext.Request.Method switch
            {
                "POST" => "create",
                "PUT" or "PATCH" => "edit",
                "DELETE" => "delete",
                _ => "view",
            };
            var needed = $"{scoped.PermissionModule}.{verb}";
            if (!context.HttpContext.User.HasClaim(PermissionClaim.Type, needed))
                throw new ForbiddenException($"Missing permission: {needed}");
        }
        await next();
    }
}
