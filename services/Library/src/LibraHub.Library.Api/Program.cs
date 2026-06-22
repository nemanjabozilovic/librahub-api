using LibraHub.BuildingBlocks.Hosting;
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

app.UseLibraHubPipeline(useIdempotency: true);
app.UseLibraryDatabaseMigrations();

app.Run();
