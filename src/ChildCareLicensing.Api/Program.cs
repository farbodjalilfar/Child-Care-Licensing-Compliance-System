using ChildCareLicensing.Api.Components;
using ChildCareLicensing.Api.Middleware;
using ChildCareLicensing.Api.Security;
using ChildCareLicensing.Application;
using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Infrastructure;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "childcare.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // API callers should get a status code rather than a redirect to a sign-in page.
        options.Events.OnRedirectToLogin = context => Challenge(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => Challenge(context, StatusCodes.Status403Forbidden);

        static Task Challenge(
            Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context,
            int statusCode)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Operator, policy =>
        policy.RequireRole(nameof(UserRole.Operator)))
    .AddPolicy(AuthorizationPolicies.Reviewer, policy =>
        policy.RequireRole(nameof(UserRole.Reviewer)))
    .AddPolicy(AuthorizationPolicies.Ministry, policy =>
        policy.RequireRole(nameof(UserRole.Reviewer), nameof(UserRole.Inspector)));

builder.Services.AddControllers();
builder.Services.AddResponseCaching();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// The integration test host composes its own services, so the timer-driven worker stays off there.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddLicenceMaintenanceWorker();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DatabaseInitializer.InitializeDevelopmentDatabaseAsync(app.Services);
}
else
{
    app.UseHsts();
}

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var isNotFound = feature?.Error is KeyNotFoundException;

    context.Response.StatusCode = isNotFound
        ? StatusCodes.Status404NotFound
        : StatusCodes.Status500InternalServerError;

    await Results.Problem(
        title: isNotFound ? "Resource not found." : "An unexpected error occurred.",
        statusCode: context.Response.StatusCode,
        instance: context.Request.Path).ExecuteAsync(context);
}));

app.UseStatusCodePages();
app.UseCorrelationId();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCaching();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapAccountEndpoints();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
