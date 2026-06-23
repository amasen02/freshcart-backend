# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Services/Reporting/FreshCart.Reporting.Api/FreshCart.Reporting.Api.csproj", "src/Services/Reporting/FreshCart.Reporting.Api/"]
COPY ["src/Services/Reporting/FreshCart.Reporting.Application/FreshCart.Reporting.Application.csproj", "src/Services/Reporting/FreshCart.Reporting.Application/"]
COPY ["src/Services/Reporting/FreshCart.Reporting.Infrastructure/FreshCart.Reporting.Infrastructure.csproj", "src/Services/Reporting/FreshCart.Reporting.Infrastructure/"]
COPY ["src/Services/Reporting/FreshCart.Reporting.Domain/FreshCart.Reporting.Domain.csproj", "src/Services/Reporting/FreshCart.Reporting.Domain/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks/FreshCart.BuildingBlocks.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/FreshCart.BuildingBlocks.Messaging.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks.Messaging/"]
COPY ["src/BuildingBlocks/FreshCart.ServiceDefaults/FreshCart.ServiceDefaults.csproj", "src/BuildingBlocks/FreshCart.ServiceDefaults/"]
RUN dotnet restore "src/Services/Reporting/FreshCart.Reporting.Api/FreshCart.Reporting.Api.csproj"

COPY . .
WORKDIR /src/src/Services/Reporting/FreshCart.Reporting.Api
RUN dotnet publish "FreshCart.Reporting.Api.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
ENTRYPOINT ["dotnet", "FreshCart.Reporting.Api.dll"]
