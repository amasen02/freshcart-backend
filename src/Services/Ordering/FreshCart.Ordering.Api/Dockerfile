# syntax=docker/dockerfile:1.7
# ---------------------------------------------------------------------------
# Multi-stage build for FreshCart.Ordering.Api
#  - build stage: SDK image, full restore + publish
#  - runtime stage: chiselled ASP.NET runtime, non-root user
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore: copy only csproj + central package files first to keep layer cache hot
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Services/Ordering/FreshCart.Ordering.Api/FreshCart.Ordering.Api.csproj", "src/Services/Ordering/FreshCart.Ordering.Api/"]
COPY ["src/Services/Ordering/FreshCart.Ordering.Application/FreshCart.Ordering.Application.csproj", "src/Services/Ordering/FreshCart.Ordering.Application/"]
COPY ["src/Services/Ordering/FreshCart.Ordering.Infrastructure/FreshCart.Ordering.Infrastructure.csproj", "src/Services/Ordering/FreshCart.Ordering.Infrastructure/"]
COPY ["src/Services/Ordering/FreshCart.Ordering.Domain/FreshCart.Ordering.Domain.csproj", "src/Services/Ordering/FreshCart.Ordering.Domain/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks/FreshCart.BuildingBlocks.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/FreshCart.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/FreshCart.ServiceDefaults/FreshCart.ServiceDefaults.csproj", "src/BuildingBlocks/FreshCart.ServiceDefaults/"]
RUN dotnet restore "src/Services/Ordering/FreshCart.Ordering.Api/FreshCart.Ordering.Api.csproj"

COPY . .
WORKDIR /src/src/Services/Ordering/FreshCart.Ordering.Api
RUN dotnet publish "FreshCart.Ordering.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Non-root user is the default in the chiselled image.
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_HTTPS_PORTS=8081 \
    DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "FreshCart.Ordering.Api.dll"]
