FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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

# Copy Identity projects
COPY ["services/Identity/src/LibraHub.Identity.Domain/LibraHub.Identity.Domain.csproj", "services/Identity/src/LibraHub.Identity.Domain/"]
COPY ["services/Identity/src/LibraHub.Identity.Application/LibraHub.Identity.Application.csproj", "services/Identity/src/LibraHub.Identity.Application/"]
COPY ["services/Identity/src/LibraHub.Identity.Infrastructure/LibraHub.Identity.Infrastructure.csproj", "services/Identity/src/LibraHub.Identity.Infrastructure/"]
COPY ["services/Identity/src/LibraHub.Identity.Api/LibraHub.Identity.Api.csproj", "services/Identity/src/LibraHub.Identity.Api/"]

# Restore dependencies
RUN dotnet restore "services/Identity/src/LibraHub.Identity.Api/LibraHub.Identity.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Identity/ services/Identity/

# Build
WORKDIR "/src/services/Identity/src/LibraHub.Identity.Api"
RUN dotnet build "LibraHub.Identity.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Identity.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Identity.Api.dll"]

