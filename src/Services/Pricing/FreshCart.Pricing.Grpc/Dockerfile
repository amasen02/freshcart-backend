# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only csproj and central package files first so the restore layer stays cached
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Services/Pricing/FreshCart.Pricing.Grpc/FreshCart.Pricing.Grpc.csproj", "src/Services/Pricing/FreshCart.Pricing.Grpc/"]
COPY ["src/BuildingBlocks/FreshCart.BuildingBlocks/FreshCart.BuildingBlocks.csproj", "src/BuildingBlocks/FreshCart.BuildingBlocks/"]
COPY ["src/BuildingBlocks/FreshCart.ServiceDefaults/FreshCart.ServiceDefaults.csproj", "src/BuildingBlocks/FreshCart.ServiceDefaults/"]
RUN dotnet restore "src/Services/Pricing/FreshCart.Pricing.Grpc/FreshCart.Pricing.Grpc.csproj"

COPY . .
WORKDIR /src/src/Services/Pricing/FreshCart.Pricing.Grpc
RUN dotnet publish "FreshCart.Pricing.Grpc.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app
EXPOSE 8080

# Non-root user is the default in the chiselled image. The SQLite file lives under /data, which
# deployment must mount as a volume writable by the app user; /app stays read-only.
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ConnectionStrings__pricingdb="Data Source=/data/pricing.db"

ENTRYPOINT ["dotnet", "FreshCart.Pricing.Grpc.dll"]
