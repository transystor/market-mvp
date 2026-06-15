FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MarketMvp.sln ./
COPY src/MarketMvp.MarketDataIngestor/MarketMvp.MarketDataIngestor.csproj src/MarketMvp.MarketDataIngestor/
COPY src/MarketMvp.Contracts/MarketMvp.Contracts.csproj src/MarketMvp.Contracts/
RUN dotnet restore src/MarketMvp.MarketDataIngestor/MarketMvp.MarketDataIngestor.csproj

COPY src/ ./src/
RUN dotnet publish src/MarketMvp.MarketDataIngestor/MarketMvp.MarketDataIngestor.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MarketMvp.MarketDataIngestor.dll"]
