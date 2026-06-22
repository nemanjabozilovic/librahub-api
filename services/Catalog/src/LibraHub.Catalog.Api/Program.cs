using LibraHub.BuildingBlocks.Hosting;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Catalog.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCatalogSwagger();
builder.Services.AddCatalogDatabase(builder.Configuration);
builder.Services.AddCatalogApplicationServices(builder.Configuration);
builder.Services.AddCatalogJwtAuthentication(builder.Configuration);
builder.Services.AddCatalogRabbitMq(builder.Configuration);
builder.Services.AddCatalogHealthChecks(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Catalog", "1.0.0");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCatalogSwagger();
}

app.UseLibraHubPipeline();
app.UseCatalogDatabaseMigrations();

app.Run();
