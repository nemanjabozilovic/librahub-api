using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Idempotency;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Library.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLibrarySwagger();
builder.Services.AddLibraryDatabase(builder.Configuration);
builder.Services.AddLibraryApplicationServices(builder.Configuration);
builder.Services.AddLibraryJwtAuthentication(builder.Configuration);
builder.Services.AddLibraryRabbitMq(builder.Configuration);
builder.Services.AddLibraryHealthChecks(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Library", "1.0.0");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseLibrarySwagger();
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
app.UseLibraryDatabaseMigrations();

app.Run();
