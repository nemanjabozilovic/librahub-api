using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Content.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddContentSwagger();
builder.Services.AddContentDatabase(builder.Configuration);
builder.Services.AddContentApplicationServices(builder.Configuration);
builder.Services.AddContentJwtAuthentication(builder.Configuration);
builder.Services.AddContentRabbitMq(builder.Configuration);
builder.Services.AddContentHealthChecks(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Content", "1.0.0");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseContentSwagger();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseContentDatabaseMigrations();

app.Run();
