using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Gateway.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGatewaySwagger();
builder.Services.AddGatewayJwtAuthentication(builder.Configuration);
builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddGatewayReverseProxy(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Gateway", "1.0.0");

var app = builder.Build();

app.UseCors();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseGatewaySwagger();
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
app.MapReverseProxy();

app.Run();
