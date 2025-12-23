using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Notifications.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddNotificationsSwagger();
builder.Services.AddNotificationsDatabase(builder.Configuration);
builder.Services.AddNotificationsApplicationServices(builder.Configuration);
builder.Services.AddNotificationsJwtAuthentication(builder.Configuration);
builder.Services.AddNotificationsRabbitMq(builder.Configuration);
builder.Services.AddNotificationsHealthChecks(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Notifications", "1.0.0");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseNotificationsSwagger();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IdempotencyKeyMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapNotificationsSignalRHub(builder.Configuration);
app.UseNotificationsDatabaseMigrations();

app.Run();
