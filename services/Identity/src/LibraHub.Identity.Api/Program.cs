using LibraHub.BuildingBlocks.Hosting;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Identity.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddIdentitySwagger();
builder.Services.AddIdentityDatabase(builder.Configuration);
builder.Services.AddIdentityApplicationServices(builder.Configuration);
builder.Services.AddIdentityJwtAuthentication(builder.Configuration);
builder.Services.AddIdentityRabbitMq(builder.Configuration);
builder.Services.AddIdentityHealthChecks(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Identity", "1.0.0");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseIdentitySwagger();
}

app.UseLibraHubPipeline();
app.UseIdentityDatabaseMigrations();
app.UseIdentityDatabaseSeeder();

app.Run();
