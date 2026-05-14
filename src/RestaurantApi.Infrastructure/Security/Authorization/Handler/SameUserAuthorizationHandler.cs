using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantApi.Domain.Constants;
using RestaurantApi.Infrastructure.Security.Authorization.Requirements;

namespace RestaurantApi.Infrastructure.Security.Authorization.Handler;

public class SameUserAuthorizationHandler : AuthorizationHandler<SameUserRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SameUserRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst("Id")?.Value;

        if (context.User.IsInRole(AppRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.Resource is HttpContext httpContext)
        {
            var routeUserId = httpContext.GetRouteValue("id")?.ToString();

            if (
                !string.IsNullOrEmpty(routeUserId) &&
                routeUserId.Equals(userIdClaim, StringComparison.OrdinalIgnoreCase)
            )
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}