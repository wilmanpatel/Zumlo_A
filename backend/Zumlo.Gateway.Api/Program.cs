
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Zumlo.Gateway.Application;
using Zumlo.Gateway.Application.Audio;
using Zumlo.Gateway.Domain;
using Zumlo.Gateway.Infrastructure;
using Zumlo.Gateway.Api.Middleware;
using Zumlo.Gateway.Api.WebSockets;
using Zumlo.Gateway.Api.Auth;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:4200") // Angular dev server origin
              .AllowAnyHeader()                     // allows 'Authorization', 'Content-Type', etc.
              .AllowAnyMethod()                     // GET, POST, OPTIONS, etc.
                                                    // .AllowCredentials()                      // only if you plan to send cookies; not needed for Bearer tokens
    );
});


// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zumlo Voice Gateway", Version = "v1" });
});

// Fake JWT auth (accepts any non-empty bearer token) for REST endpoints
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = FakeJwtAuthenticationHandler.SchemeName;
    options.DefaultChallengeScheme = FakeJwtAuthenticationHandler.SchemeName;
}).AddScheme<AuthenticationSchemeOptions, FakeJwtAuthenticationHandler>(FakeJwtAuthenticationHandler.SchemeName, null);

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", c =>
    {
        c.Window = TimeSpan.FromSeconds(1);
        c.PermitLimit = 20; // basic throttling per server instance
        c.QueueLimit = 20;
        c.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});


builder.Services.AddLogging();

// Custom services
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<AudioNormalizer>();
builder.Services.AddSingleton<TranscriptionSimulator>();
builder.Services.AddSingleton<AssistantResponseSimulator>();
builder.Services.AddSingleton<ConversationWebSocketHandler>();


builder.Services.AddHttpClient();
var app = builder.Build();

app.UseMiddleware<ErrorEnvelopeMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// WebSocket endpoint (auth via `?token=` query for browser compatibility)
app.UseWebSockets();
app.Map("/ws", async (HttpContext context, ConversationWebSocketHandler handler) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Expected WebSocket request");
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await handler.HandleAsync(context, socket);
}).RequireRateLimiting("fixed");

app.Run();
