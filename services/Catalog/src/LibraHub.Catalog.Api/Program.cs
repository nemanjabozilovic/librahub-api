using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Catalog.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCatalogSwagger();
builder.Services.AddCatalogDatabase(builder.Configuration);
builder.Services.AddCatalogApplicationServices();
builder.Services.AddCatalogJwtAuthentication(builder.Configuration);
builder.Services.AddCatalogRabbitMq(builder.Configuration);

// Health checks
builder.Services.AddCatalogHealthChecks(builder.Configuration);

// Observability
builder.Services.AddTelemetry("LibraHub.Catalog", "1.0.0");

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCatalogSwagger();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Database migrations
app.UseCatalogDatabaseMigrations();

app.Run();
