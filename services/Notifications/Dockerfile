FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["LibraHub.sln", "."]
COPY ["global.json", "."]
COPY ["nuget.config", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]

# Copy shared libraries project files
COPY ["shared/BuildingBlocks/src/LibraHub.BuildingBlocks/LibraHub.BuildingBlocks.csproj", "shared/BuildingBlocks/src/LibraHub.BuildingBlocks/"]
COPY ["shared/Contracts/src/LibraHub.Contracts/LibraHub.Contracts.csproj", "shared/Contracts/src/LibraHub.Contracts/"]

# Copy Notifications projects
COPY ["services/Notifications/src/LibraHub.Notifications.Domain/LibraHub.Notifications.Domain.csproj", "services/Notifications/src/LibraHub.Notifications.Domain/"]
COPY ["services/Notifications/src/LibraHub.Notifications.Application/LibraHub.Notifications.Application.csproj", "services/Notifications/src/LibraHub.Notifications.Application/"]
COPY ["services/Notifications/src/LibraHub.Notifications.Infrastructure/LibraHub.Notifications.Infrastructure.csproj", "services/Notifications/src/LibraHub.Notifications.Infrastructure/"]
COPY ["services/Notifications/src/LibraHub.Notifications.Api/LibraHub.Notifications.Api.csproj", "services/Notifications/src/LibraHub.Notifications.Api/"]

# Restore dependencies
RUN dotnet restore "services/Notifications/src/LibraHub.Notifications.Api/LibraHub.Notifications.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Notifications/ services/Notifications/

# Build
WORKDIR "/src/services/Notifications/src/LibraHub.Notifications.Api"
RUN dotnet build "LibraHub.Notifications.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Notifications.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Notifications.Api.dll"]

