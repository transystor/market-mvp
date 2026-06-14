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

var instruments = new[]
{
    new InstrumentDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", "Apple Inc.", "Stock", "USD", "NASDAQ", "US0378331005"),
    new InstrumentDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), "MSFT", "Microsoft Corp.", "Stock", "USD", "NASDAQ", "US5949181045"),
    new InstrumentDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), "NVDA", "NVIDIA Corp.", "Stock", "USD", "NASDAQ", "US67066G1040"),
    new InstrumentDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), "GAZP", "Газпром", "Stock", "RUB", "MOEX", "RU0007661625"),
    new InstrumentDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", "Сбербанк", "Stock", "RUB", "MOEX", "RU0009029540")
};

app.MapGet("/instruments", () => Results.Ok(instruments));
app.MapGet("/instruments/{instrumentId:guid}", (Guid instrumentId) =>
{
    var instrument = instruments.FirstOrDefault(x => x.InstrumentId == instrumentId);
    return instrument is null ? Results.NotFound() : Results.Ok(instrument);
});

app.Run();
