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

# Copy Gateway project
COPY ["services/Gateway/src/LibraHub.Gateway.Api/LibraHub.Gateway.Api.csproj", "services/Gateway/src/LibraHub.Gateway.Api/"]

# Restore dependencies
RUN dotnet restore "services/Gateway/src/LibraHub.Gateway.Api/LibraHub.Gateway.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Gateway/ services/Gateway/

# Build
WORKDIR "/src/services/Gateway/src/LibraHub.Gateway.Api"
RUN dotnet build "LibraHub.Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Gateway.Api.dll"]

