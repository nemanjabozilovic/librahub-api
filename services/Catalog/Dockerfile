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

# Copy Catalog projects
COPY ["services/Catalog/src/LibraHub.Catalog.Domain/LibraHub.Catalog.Domain.csproj", "services/Catalog/src/LibraHub.Catalog.Domain/"]
COPY ["services/Catalog/src/LibraHub.Catalog.Application/LibraHub.Catalog.Application.csproj", "services/Catalog/src/LibraHub.Catalog.Application/"]
COPY ["services/Catalog/src/LibraHub.Catalog.Infrastructure/LibraHub.Catalog.Infrastructure.csproj", "services/Catalog/src/LibraHub.Catalog.Infrastructure/"]
COPY ["services/Catalog/src/LibraHub.Catalog.Api/LibraHub.Catalog.Api.csproj", "services/Catalog/src/LibraHub.Catalog.Api/"]

# Restore dependencies
RUN dotnet restore "services/Catalog/src/LibraHub.Catalog.Api/LibraHub.Catalog.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Catalog/ services/Catalog/

# Build
WORKDIR "/src/services/Catalog/src/LibraHub.Catalog.Api"
RUN dotnet build "LibraHub.Catalog.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Catalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Catalog.Api.dll"]

