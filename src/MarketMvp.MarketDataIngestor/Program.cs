using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("price-service", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PRICE_SERVICE_URL") ?? "http://price-service:8080");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var random = new Random();

var priceState = new Dictionary<Guid, SimulatedPriceTickDto>
{
    [Guid.Parse("f1111111-1111-1111-1111-111111111111")] = new(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", 213.42m, DateTime.UtcNow),
    [Guid.Parse("f2222222-2222-2222-2222-222222222222")] = new(Guid.Parse("f2222222-2222-2222-2222-222222222222"), "MSFT", 487.11m, DateTime.UtcNow),
    [Guid.Parse("f3333333-3333-3333-3333-333333333333")] = new(Guid.Parse("f3333333-3333-3333-3333-333333333333"), "NVDA", 1288.55m, DateTime.UtcNow),
    [Guid.Parse("f4444444-4444-4444-4444-444444444444")] = new(Guid.Parse("f4444444-4444-4444-4444-444444444444"), "GAZP", 173.34m, DateTime.UtcNow),
    [Guid.Parse("f5555555-5555-5555-5555-555555555555")] = new(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", 319.80m, DateTime.UtcNow)
};

app.MapGet("/ticks", () => Results.Ok(priceState.Values.OrderBy(x => x.Ticker)));

app.MapPost("/simulate-tick", async (IHttpClientFactory httpClientFactory) =>
{
    var selected = priceState.Values.ElementAt(random.Next(priceState.Count));
    var delta = Math.Round((decimal)(random.NextDouble() * 6 - 3), 2);
    var nextPrice = Math.Max(1m, selected.MarketPrice + delta);
    var updatedAt = DateTime.UtcNow;

    var nextTick = selected with
    {
        MarketPrice = nextPrice,
        LastUpdatedAtUtc = updatedAt
    };

    priceState[nextTick.InstrumentId] = nextTick;

    var priceService = httpClientFactory.CreateClient("price-service");
    var response = await priceService.PutAsJsonAsync($"/prices/{nextTick.InstrumentId}", new PriceUpdateRequest(nextTick.InstrumentId, nextTick.MarketPrice, nextTick.LastUpdatedAtUtc));
    response.EnsureSuccessStatusCode();

    return Results.Ok(nextTick);
});

app.Run();
