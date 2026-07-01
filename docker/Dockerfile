FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["backend/PriceTracker.API/PriceTracker.API.csproj", "backend/PriceTracker.API/"]
COPY ["backend/PriceTracker.Application/PriceTracker.Application.csproj", "backend/PriceTracker.Application/"]
COPY ["backend/PriceTracker.Domain/PriceTracker.Domain.csproj", "backend/PriceTracker.Domain/"]
COPY ["backend/PriceTracker.Infrastructure/PriceTracker.Infrastructure.csproj", "backend/PriceTracker.Infrastructure/"]

RUN dotnet restore "backend/PriceTracker.API/PriceTracker.API.csproj"

COPY . .
WORKDIR "/src/backend/PriceTracker.API"
RUN dotnet build "PriceTracker.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PriceTracker.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "PriceTracker.API.dll"]
