using OAS.API.Components;
using OAS.API.Extensions;
using OAS.API.Middleware;
using OAS.Application;
using OAS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiFoundation(builder.Configuration);
builder.Services.AddHealthChecks();

// Register each simple CRUD entity explicitly here (or in a feature-specific registration class):
// builder.Services.AddCrudFeature<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>();

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (origins.Length > 0) app.UseCors("ConfiguredOrigins");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health");
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OAS.Client.ClientAssemblyMarker).Assembly);

app.Run();

public partial class Program;
