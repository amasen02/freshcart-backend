# syntax=docker/dockerfile:1.7
# ---------------------------------------------------------------------------
# Multi-stage build for FreshCart.Notification.Api
#  - build stage: SDK image, restore + publish
#  - runtime stage: chiselled ASP.NET runtime, non-root user
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Services/Notification/FreshCart.Notification.Api/FreshCart.Notification.Api.csproj", "src/Services/Notification/FreshCart.Notification.Api/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks/FreshCart.BuildingBlocks.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/FreshCart.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/FreshCart.ServiceDefaults/FreshCart.ServiceDefaults.csproj", "src/BuildingBlocks/FreshCart.ServiceDefaults/"]
RUN dotnet restore "src/Services/Notification/FreshCart.Notification.Api/FreshCart.Notification.Api.csproj"

COPY . .
WORKDIR /src/src/Services/Notification/FreshCart.Notification.Api
RUN dotnet publish "FreshCart.Notification.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
EXPOSE 8080

# Non-root user is the default in the chiselled image.
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Liveness/readiness are probed by the orchestrator against /alive and /ready.
# The chiselled runtime ships without a shell or curl, so no in-image HEALTHCHECK.

ENTRYPOINT ["dotnet", "FreshCart.Notification.Api.dll"]
