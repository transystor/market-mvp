using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.AddHttpClient("clients", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("CLIENT_SERVICE_URL") ?? "http://client-service:8080");
});

builder.Services.AddHttpClient("portfolio", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PORTFOLIO_SERVICE_URL") ?? "http://portfolio-service:8080");
});

builder.Services.AddHttpClient("instruments", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("INSTRUMENT_SERVICE_URL") ?? "http://instrument-service:8080");
});

builder.Services.AddHttpClient("prices", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PRICE_SERVICE_URL") ?? "http://price-service:8080");
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.MapGet("/ui/clients", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("clients");
    var result = await client.GetFromJsonAsync<ClientDto[]>("/clients");
    return result is null ? Results.Problem("Clients service returned no data") : Results.Ok(result.Select(x => new UiClientDto(x.Id, x.Name)));
});

app.MapGet("/ui/clients/{clientId:guid}/accounts", async (Guid clientId, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("portfolio");
    var result = await client.GetFromJsonAsync<AccountDto[]>($"/clients/{clientId}/accounts");
    return result is null
        ? Results.Problem("Portfolio service returned no accounts")
        : Results.Ok(result.Select(x => new UiAccountDto(x.Id, x.AccountNumber)));
});

app.MapGet("/ui/accounts/{accountId:guid}/positions", async (Guid accountId, IHttpClientFactory httpClientFactory) =>
{
    var portfolioClient = httpClientFactory.CreateClient("portfolio");
    var instrumentClient = httpClientFactory.CreateClient("instruments");
    var priceClient = httpClientFactory.CreateClient("prices");

    var positions = await portfolioClient.GetFromJsonAsync<PortfolioPositionDto[]>($"/accounts/{accountId}/positions") ?? [];
    var instruments = await instrumentClient.GetFromJsonAsync<InstrumentDto[]>("/instruments") ?? [];
    var prices = await priceClient.GetFromJsonAsync<MarketPriceDto[]>("/prices") ?? [];

    var instrumentMap = instruments.ToDictionary(x => x.InstrumentId);
    var priceMap = prices.ToDictionary(x => x.InstrumentId);

    var result = positions.Select(position =>
    {
        var instrument = instrumentMap[position.InstrumentId];
        var price = priceMap[position.InstrumentId];

        var marketValue = position.Quantity * price.MarketPrice;
        var costBasis = position.Quantity * position.AveragePrice;
        var unrealizedPnl = marketValue - costBasis;
        var unrealizedPnlPercent = costBasis == 0 ? 0 : Math.Round(unrealizedPnl / costBasis * 100, 2);

        return new UiAccountPositionDto(
            position.InstrumentId,
            instrument.Ticker,
            instrument.Name,
            position.Quantity,
            position.AveragePrice,
            position.PurchaseDate,
            price.MarketPrice,
            marketValue,
            unrealizedPnl,
            unrealizedPnlPercent,
            price.LastUpdatedAtUtc);
    });

    return Results.Ok(result);
});

app.MapGet("/ui/instruments", async (IHttpClientFactory httpClientFactory) =>
{
    var instrumentClient = httpClientFactory.CreateClient("instruments");
    var priceClient = httpClientFactory.CreateClient("prices");

    var instruments = await instrumentClient.GetFromJsonAsync<InstrumentDto[]>("/instruments") ?? [];
    var prices = await priceClient.GetFromJsonAsync<MarketPriceDto[]>("/prices") ?? [];
    var priceMap = prices.ToDictionary(x => x.InstrumentId);

    var result = instruments.Select(instrument =>
    {
        var price = priceMap[instrument.InstrumentId];

        return new UiInstrumentListItemDto(
            instrument.InstrumentId,
            instrument.Ticker,
            instrument.Name,
            instrument.Type,
            instrument.Currency,
            price.MarketPrice,
            price.LastUpdatedAtUtc);
    });

    return Results.Ok(result);
});

app.MapGet("/ui/instruments/{instrumentId:guid}", async (Guid instrumentId, IHttpClientFactory httpClientFactory) =>
{
    var instrumentClient = httpClientFactory.CreateClient("instruments");
    var priceClient = httpClientFactory.CreateClient("prices");

    var instrument = await instrumentClient.GetFromJsonAsync<InstrumentDto>($"/instruments/{instrumentId}");
    var price = await priceClient.GetFromJsonAsync<MarketPriceDto>($"/prices/{instrumentId}");

    if (instrument is null || price is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new UiInstrumentDetailsDto(
        instrument.InstrumentId,
        instrument.Ticker,
        instrument.Name,
        instrument.Type,
        instrument.Currency,
        price.MarketPrice,
        price.LastUpdatedAtUtc,
        instrument.Exchange,
        instrument.Isin));
});

app.Run();
