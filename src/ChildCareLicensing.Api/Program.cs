using ChildCareLicensing.Api.Components;
using ChildCareLicensing.Api.Middleware;
using ChildCareLicensing.Application;
using ChildCareLicensing.Infrastructure;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
