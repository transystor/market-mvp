using System.Collections.Concurrent;
using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var now = DateTime.UtcNow;

var prices = new ConcurrentDictionary<Guid, MarketPriceDto>(new[]
{
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f1111111-1111-1111-1111-111111111111"), new MarketPriceDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), 213.42m, now.AddSeconds(-3))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f2222222-2222-2222-2222-222222222222"), new MarketPriceDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), 487.11m, now.AddSeconds(-2))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f3333333-3333-3333-3333-333333333333"), new MarketPriceDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), 1288.55m, now.AddSeconds(-1))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f4444444-4444-4444-4444-444444444444"), new MarketPriceDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), 173.34m, now.AddSeconds(-4))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f5555555-5555-5555-5555-555555555555"), new MarketPriceDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), 319.80m, now.AddSeconds(-5)))
});

app.MapGet("/prices", () => Results.Ok(prices.Values.OrderBy(x => x.InstrumentId)));
app.MapGet("/prices/{instrumentId:guid}", (Guid instrumentId) =>
{
    return prices.TryGetValue(instrumentId, out var price)
        ? Results.Ok(price)
        : Results.NotFound();
});

app.MapPut("/prices/{instrumentId:guid}", (Guid instrumentId, PriceUpdateRequest request) =>
{
    if (instrumentId != request.InstrumentId)
    {
        return Results.BadRequest();
    }

    var updated = new MarketPriceDto(request.InstrumentId, request.MarketPrice, request.LastUpdatedAtUtc);
    prices[instrumentId] = updated;
    return Results.Ok(updated);
});

app.Run();
