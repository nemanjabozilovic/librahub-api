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

# Copy Orders projects
COPY ["services/Orders/src/LibraHub.Orders.Domain/LibraHub.Orders.Domain.csproj", "services/Orders/src/LibraHub.Orders.Domain/"]
COPY ["services/Orders/src/LibraHub.Orders.Application/LibraHub.Orders.Application.csproj", "services/Orders/src/LibraHub.Orders.Application/"]
COPY ["services/Orders/src/LibraHub.Orders.Infrastructure/LibraHub.Orders.Infrastructure.csproj", "services/Orders/src/LibraHub.Orders.Infrastructure/"]
COPY ["services/Orders/src/LibraHub.Orders.Api/LibraHub.Orders.Api.csproj", "services/Orders/src/LibraHub.Orders.Api/"]

# Restore dependencies
RUN dotnet restore "services/Orders/src/LibraHub.Orders.Api/LibraHub.Orders.Api.csproj"

# Copy all source files
COPY shared/ shared/
COPY services/Orders/ services/Orders/

# Build
WORKDIR "/src/services/Orders/src/LibraHub.Orders.Api"
RUN dotnet build "LibraHub.Orders.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibraHub.Orders.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraHub.Orders.Api.dll"]

