FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
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

# Copy Content projects
COPY ["services/Content/src/LibraHub.Content.Domain/LibraHub.Content.Domain.csproj", "services/Content/src/LibraHub.Content.Domain/"]
COPY ["services/Content/src/LibraHub.Content.Application/LibraHub.Content.Application.csproj", "services/Content/src/LibraHub.Content.Application/"]
COPY ["services/Content/src/LibraHub.Content.Infrastructure/LibraHub.Content.Infrastructure.csproj", "services/Content/src/LibraHub.Content.Infrastructure/"]
COPY ["services/Content/src/LibraHub.Content.Api/LibraHub.Content.Api.csproj", "services/Content/src/LibraHub.Content.Api/"]

# Restore dependencies
RUN dotnet restore "services/Content/src/LibraHub.Content.Api/LibraHub.Content.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Content/ services/Content/

# Build
WORKDIR "/src/services/Content/src/LibraHub.Content.Api"
RUN dotnet build "LibraHub.Content.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Content.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Content.Api.dll"]

