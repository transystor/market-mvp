FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MarketMvp.sln ./
COPY src/MarketMvp.Bff/MarketMvp.Bff.csproj src/MarketMvp.Bff/
COPY src/MarketMvp.Contracts/MarketMvp.Contracts.csproj src/MarketMvp.Contracts/
RUN dotnet restore src/MarketMvp.Bff/MarketMvp.Bff.csproj

COPY src/ ./src/
RUN dotnet publish src/MarketMvp.Bff/MarketMvp.Bff.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MarketMvp.Bff.dll"]
