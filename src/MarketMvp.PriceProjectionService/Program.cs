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

var prices = new[]
{
    new MarketPriceDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), 213.42m, now.AddSeconds(-3)),
    new MarketPriceDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), 487.11m, now.AddSeconds(-2)),
    new MarketPriceDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), 1288.55m, now.AddSeconds(-1)),
    new MarketPriceDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), 173.34m, now.AddSeconds(-4)),
    new MarketPriceDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), 319.80m, now.AddSeconds(-5))
};

app.MapGet("/prices", () => Results.Ok(prices));
app.MapGet("/prices/{instrumentId:guid}", (Guid instrumentId) =>
{
    var price = prices.FirstOrDefault(x => x.InstrumentId == instrumentId);
    return price is null ? Results.NotFound() : Results.Ok(price);
});

app.Run();
