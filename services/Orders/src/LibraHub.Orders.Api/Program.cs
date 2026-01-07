using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Orders.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOrdersSwagger();
builder.Services.AddOrdersDatabase(builder.Configuration);
builder.Services.AddOrdersApplicationServices(builder.Configuration);
builder.Services.AddOrdersJwtAuthentication(builder.Configuration);
builder.Services.AddOrdersRabbitMq(builder.Configuration);
builder.Services.AddOrdersHealthChecks(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Orders", "1.0.0");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOrdersSwagger();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IdempotencyKeyMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseOrdersDatabaseMigrations();

app.Run();
