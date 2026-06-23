# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Services/Delivery/FreshCart.Delivery.Api/FreshCart.Delivery.Api.csproj", "src/Services/Delivery/FreshCart.Delivery.Api/"]
COPY ["src/Services/Delivery/FreshCart.Delivery.Application/FreshCart.Delivery.Application.csproj", "src/Services/Delivery/FreshCart.Delivery.Application/"]
COPY ["src/Services/Delivery/FreshCart.Delivery.Infrastructure/FreshCart.Delivery.Infrastructure.csproj", "src/Services/Delivery/FreshCart.Delivery.Infrastructure/"]
COPY ["src/Services/Delivery/FreshCart.Delivery.Domain/FreshCart.Delivery.Domain.csproj", "src/Services/Delivery/FreshCart.Delivery.Domain/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks/FreshCart.BuildingBlocks.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/FreshCart.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/FreshCart.ServiceDefaults/FreshCart.ServiceDefaults.csproj", "src/BuildingBlocks/FreshCart.ServiceDefaults/"]
RUN dotnet restore "src/Services/Delivery/FreshCart.Delivery.Api/FreshCart.Delivery.Api.csproj"

COPY . .
WORKDIR /src/src/Services/Delivery/FreshCart.Delivery.Api
RUN dotnet publish "FreshCart.Delivery.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
ENTRYPOINT ["dotnet", "FreshCart.Delivery.Api.dll"]
