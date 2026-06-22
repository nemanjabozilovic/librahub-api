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

# Copy Library projects
COPY ["services/Library/src/LibraHub.Library.Domain/LibraHub.Library.Domain.csproj", "services/Library/src/LibraHub.Library.Domain/"]
COPY ["services/Library/src/LibraHub.Library.Application/LibraHub.Library.Application.csproj", "services/Library/src/LibraHub.Library.Application/"]
COPY ["services/Library/src/LibraHub.Library.Infrastructure/LibraHub.Library.Infrastructure.csproj", "services/Library/src/LibraHub.Library.Infrastructure/"]
COPY ["services/Library/src/LibraHub.Library.Api/LibraHub.Library.Api.csproj", "services/Library/src/LibraHub.Library.Api/"]

# Restore dependencies
RUN dotnet restore "services/Library/src/LibraHub.Library.Api/LibraHub.Library.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Library/ services/Library/

# Build
WORKDIR "/src/services/Library/src/LibraHub.Library.Api"
RUN dotnet build "LibraHub.Library.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Library.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Library.Api.dll"]

