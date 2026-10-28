using Microsoft.AspNetCore.Authorization;

namespace MafaliCrm.Web.Services;

public sealed class AuthenticatedUnlessBlazorInfrastructure : IAuthorizationRequirement
{
}

public sealed class AuthenticatedUnlessBlazorInfrastructureHandler
    : AuthorizationHandler<AuthenticatedUnlessBlazorInfrastructure>
{
    private static readonly string[] AnonymousPaths = ["/_framework", "/_blazor"];

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthenticatedUnlessBlazorInfrastructure requirement)
    {
        if (context.Resource is HttpContext http
            && AnonymousPaths.Any(p => http.Request.Path.StartsWithSegments(p)))
        {
            context.Succeed(requirement);
        }
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
