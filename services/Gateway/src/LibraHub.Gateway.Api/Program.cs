using LibraHub.BuildingBlocks.Caching;
using LibraHub.BuildingBlocks.Correlation;
using LibraHub.BuildingBlocks.Http;
using LibraHub.BuildingBlocks.Middlewares;
using LibraHub.BuildingBlocks.Observability;
using LibraHub.Gateway.Api.Extensions;
using LibraHub.Gateway.Api.Options;
using LibraHub.Gateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGatewaySwagger();
builder.Services.AddGatewayJwtAuthentication(builder.Configuration);
builder.Services.AddGatewayReverseProxy(builder.Configuration);
builder.Services.AddTelemetry("LibraHub.Gateway", "1.0.0");
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.Configure<ServicesOptions>(builder.Configuration.GetSection("Services"));

builder.Services.AddServiceClientHelper();
builder.Services.AddScoped<IDashboardService, DashboardService>();

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

