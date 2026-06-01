using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PakAccountingERP.Interfaces;

namespace PakAccountingERP.Services;

/// <summary>
/// Authorization filter for module-level permissions.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Module { get; }
    public string Action { get; }

    public PermissionRequirement(string module, string action = "View")
    {
        Module = module;
        Action = action;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("SuperAdmin") || context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Role-based module access mapping
        var roleAccess = new Dictionary<string, string[]>
        {
            ["Accountant"] = ["Dashboard", "Invoice", "Bills", "Banking", "ChartOfAccounts", "Customers", "Vendors", "Inventory", "Reports", "TaxSettings"],
            ["SalesUser"] = ["Dashboard", "Invoice", "Customers", "Inventory"],
            ["PurchaseUser"] = ["Dashboard", "Bills", "Vendors", "Inventory"],
            ["ReportsUser"] = ["Dashboard", "Reports", "AuditLogs"]
        };

        foreach (var role in context.User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role))
        {
            if (roleAccess.TryGetValue(role.Value, out var modules) && modules.Contains(requirement.Module))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireModuleAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public string Module { get; }

    public RequireModuleAttribute(string module)
    {
        Module = module;
        Policy = $"Module_{module}";
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (user.IsInRole("SuperAdmin") || user.IsInRole("Admin")) return;

        var restrictedModules = new[] { "Settings", "Company", "UserManagement" };
        if (restrictedModules.Contains(Module) && !user.IsInRole("SuperAdmin") && !user.IsInRole("Admin"))
        {
            context.Result = new ForbidResult();
        }
    }
}

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await auditService.LogAsync("System Error", null, null, null, ex.Message);

            if (context.Request.Headers.Accept.ToString().Contains("application/json") ||
                context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
