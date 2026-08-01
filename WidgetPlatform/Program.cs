using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using WidgetPlatform.Database;
using WidgetPlatform.Models;
using WidgetPlatform.Repositories;
using WidgetPlatform.Services;
using WidgetPlatform.Services.Geo;
using WidgetPlatform.Services.Notifications;

try { DotNetEnv.Env.TraversePath().Load(); } catch { }


var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing Jwt__Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Missing Jwt__Issuer");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtIssuer,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<WebhookBackgroundService>();

builder.Services.AddSingleton<ITenantRepository, PostgresTenantRepository>();
builder.Services.AddSingleton<IWidgetRepository, PostgresWidgetRepository>();
builder.Services.AddSingleton<ISubmissionRepository, PostgresSubmissionRepository>();
builder.Services.AddSingleton<IGeoProvider, PrimaryGeoProvider>();
builder.Services.AddSingleton<IGeoProvider, SecondaryGeoProvider>();
builder.Services.AddSingleton<ISubmissionNotificationQueue, InMemoryNotificationQueue>();

builder.Services.AddSingleton<GeoProviderSwitch>();
builder.Services.AddSingleton<WebhookSwitch>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WidgetService>();
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<GeoEnricher>();


var connectionString = builder.Configuration.GetConnectionString("Widgets")
    ?? throw new InvalidOperationException("Missing ConnectionStrings__Widgets. Copy .env.example to .env.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

builder.Services.AddCors(o => o.AddPolicy("public", p => p
.AllowAnyOrigin()
.AllowAnyHeader()
.WithMethods("GET", "POST", "OPTIONS")));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    o.AddPolicy("submissions", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

const string WidgetScriptVersion = "1";

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.Equals("widget.js", StringComparison.OrdinalIgnoreCase))
            ctx.Context.Response.Headers.CacheControl = "public, max-age=3600";
    }
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

await DatabaseInitializer.InitialiseAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.MapGet("/", () => Results.Ok(new { message = "Widget platform is running" }));

app.MapPost("/auth/register", async (AuthRequest req, AuthService auth, CancellationToken ct) =>
     {
         var token = await auth.RegisterAsync(req.Email, req.Password, ct);
         return token is null
         ? Results.Conflict(new { error = "Email already registered" })
         : Results.Ok(new { token });
     });

app.MapPost("/auth/login", async (AuthRequest req, AuthService auth, CancellationToken ct) =>
{
    var token = await auth.LoginAsync(req.Email, req.Password, ct);
    return token is null ? Results.Unauthorized() : Results.Ok(new { token });
});

app.MapGet("/me", (ClaimsPrincipal user) =>
    Results.Ok(new { tenantId = user.FindFirstValue("tenantId") }))
   .RequireAuthorization();

static Guid TenantId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("tenantId")!);

app.MapPost("/widgets", async (WidgetRequest req, ClaimsPrincipal user, WidgetService widgets, CancellationToken ct) =>
{
    try
    {
        var widget = await widgets.CreateAsync(TenantId(user), req.Type, req.Title, req.Fields, ct);
        return Results.Created($"/widgets/{widget.Id}", widget);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();


app.MapGet("/widgets", async (ClaimsPrincipal user, WidgetService widgets, CancellationToken ct)
    => Results.Ok(await widgets.GetAllAsync(TenantId(user), ct)))
   .RequireAuthorization();

app.MapGet("/widgets/{id:guid}", async (Guid id, ClaimsPrincipal user,
    WidgetService widgets, CancellationToken ct) =>
{
    var widget = await widgets.GetAsync(id, TenantId(user), ct);
    return widget is null ? Results.NotFound() : Results.Ok(widget);
}).RequireAuthorization();

app.MapPut("/widgets/{id:guid}", async (Guid id, WidgetRequest req, ClaimsPrincipal user,
    WidgetService widgets, CancellationToken ct) =>
{
    try
    {
        var ok = await widgets.UpdateAsync(
            id, TenantId(user), req.Type, req.Title, req.Fields, req.IsActive, ct);

        return ok ? Results.NoContent() : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();


app.MapGet("/widgets/{id:guid}/embed", async (Guid id, ClaimsPrincipal user,
    HttpContext http, WidgetService widgets, CancellationToken ct) =>
{
    var widget = await widgets.GetAsync(id, TenantId(user), ct);
    if (widget is null) return Results.NotFound();

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var snippet = $"<script src=\"{baseUrl}/widget.js?v={WidgetScriptVersion}\" data-widget-id=\"{widget.Id}\" async></script>";

    return Results.Ok(new { snippet });
}).RequireAuthorization();

app.MapGet("/widgets/{id:guid}/config", async (Guid id, HttpContext http,
    WidgetService widgets, CancellationToken ct) =>
{
    var widget = await widgets.GetPublicAsync(id, ct);
    if (widget is null) return Results.NotFound();

    var etag = $"\"{widget.Id}-{widget.Version}\"";

    if (http.Request.Headers.IfNoneMatch == etag)
        return Results.StatusCode(StatusCodes.Status304NotModified);

    http.Response.Headers.CacheControl = "public, max-age=300";
    http.Response.Headers.ETag = etag;

    return Results.Ok(new
    {
        id = widget.Id,
        type = widget.Type,
        title = widget.Title,
        fields = widget.Fields,
        v = widget.Version
    });
}).RequireCors("public");

app.MapPost("/submissions", async (SubmissionRequest req, HttpContext http,
    SubmissionService submissions, ILoggerFactory loggerFactory, CancellationToken ct) =>
{
    var ip = http.Connection.RemoteIpAddress?.ToString();

    if (!string.IsNullOrWhiteSpace(req.Website))
    {
        loggerFactory.CreateLogger("Honeypot")
            .LogWarning("Honeypot triggered from {Ip}", ip);

        return Results.Created($"/submissions/{Guid.NewGuid()}", new { ok = true });
    }

    try
    {
        var created = await submissions.CreateAsync(req.WidgetId, req.Data, ip, ct);
        return created is null
            ? Results.NotFound(new { error = "Widget not found or inactive" })
            : Results.Created($"/submissions/{created.Id}", new { ok = true });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireCors("public")
.RequireRateLimiting("submissions");


app.MapPost("/debug/geo-primary", (bool up, GeoProviderSwitch sw) =>
{
    sw.PrimaryIsUp = up;
    return Results.Ok(new { primaryIsUp = sw.PrimaryIsUp });
});

app.MapPost("/debug/webhook", (WebhookSwitch sw, ILoggerFactory lf) =>
{

    if (!sw.IsUp)
        return Results.StatusCode(500);

    lf.CreateLogger("FakeWebhook").LogInformation("Webhook receiver got a notification");
    return Results.Ok();

});

app.MapPost("/debug/webhook-switch", (bool up, WebhookSwitch sw) =>
{
    sw.IsUp = up;
    return Results.Ok(new { webhookIsUp = sw.IsUp });
});

app.MapGet("/dashboard/submissions", async (Guid? widgetId, int? limit, int? offset,
    ClaimsPrincipal user, SubmissionService submissions, CancellationToken ct) =>
{
    var list = await submissions.GetForTenantAsync(
        TenantId(user), widgetId, limit ?? 20, offset ?? 0, ct);

    var result = list.Select(s => new
    {
        s.Id,
        s.WidgetId,
        Data = JsonSerializer.Deserialize<Dictionary<string, string>>(s.DataJson),
        s.Country,
        s.City,
        s.CreatedAtUtc
    });

    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/dashboard/stats", async (ClaimsPrincipal user,
    SubmissionService submissions, CancellationToken ct)
    => Results.Ok(await submissions.GetStatsAsync(TenantId(user), ct)))
   .RequireAuthorization();

app.Run();

public sealed record AuthRequest(string Email, string Password);
public sealed record WidgetRequest(
    string Type, string Title, List<WidgetField> Fields, bool IsActive = true);
public sealed record SubmissionRequest(
    Guid WidgetId,
    Dictionary<string, string> Data,
    string? Website);

public partial class Program { }