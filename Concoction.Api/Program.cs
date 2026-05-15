using Concoction.Api.Authentication;
using Concoction.Api.Routes;
using Concoction.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("ApiKey")
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("api", opt =>
{
    opt.Window = TimeSpan.FromMinutes(1);
    opt.PermitLimit = 100;
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    opt.QueueLimit = 0;
}));

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddConcoctionApplication(seed: 42);
builder.Services.AddConcoctionInfrastructure(opts =>
{
    opts.Provider = "sqlite";
    opts.ConnectionString = "Data Source=concoction.db";
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.MapAccountRoutes().RequireAuthorization();
app.MapWorkspaceRoutes().RequireAuthorization();
app.MapProjectRoutes().RequireAuthorization();
app.MapRunRoutes().RequireAuthorization();
app.MapWorkflowRoutes().RequireAuthorization();
app.MapChatRoutes().RequireAuthorization();
app.MapApiKeyRoutes().RequireAuthorization();

app.Run();
