using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();

var now = DateTime.UtcNow;

var clients = new[]
{
    new UiClientDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Иван Петров"),
    new UiClientDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Мария Соколова"),
    new UiClientDto(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Алексей Воронов")
};

var accountsByClient = new Dictionary<Guid, UiAccountDto[]>
{
    [Guid.Parse("11111111-1111-1111-1111-111111111111")] =
    [
        new UiAccountDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "ACC-10001"),
        new UiAccountDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "ACC-10002")
    ],
    [Guid.Parse("22222222-2222-2222-2222-222222222222")] =
    [
        new UiAccountDto(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), "ACC-20001")
    ],
    [Guid.Parse("33333333-3333-3333-3333-333333333333")] =
    [
        new UiAccountDto(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), "ACC-30001")
    ]
};

var instruments = new[]
{
    new UiInstrumentDetailsDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", "Apple Inc.", "Stock", "USD", 213.42m, now.AddSeconds(-3), "NASDAQ", "US0378331005"),
    new UiInstrumentDetailsDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), "MSFT", "Microsoft Corp.", "Stock", "USD", 487.11m, now.AddSeconds(-2), "NASDAQ", "US5949181045"),
    new UiInstrumentDetailsDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), "NVDA", "NVIDIA Corp.", "Stock", "USD", 1288.55m, now.AddSeconds(-1), "NASDAQ", "US67066G1040"),
    new UiInstrumentDetailsDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), "GAZP", "Газпром", "Stock", "RUB", 173.34m, now.AddSeconds(-4), "MOEX", "RU0007661625"),
    new UiInstrumentDetailsDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", "Сбербанк", "Stock", "RUB", 319.80m, now.AddSeconds(-5), "MOEX", "RU0009029540")
};

var positionsByAccount = new Dictionary<Guid, UiAccountPositionDto[]>
{
    [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1")] =
    [
        new UiAccountPositionDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", "Apple Inc.", 15, 180.25m, new DateOnly(2026, 5, 14), 213.42m, now.AddSeconds(-3)),
        new UiAccountPositionDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), "NVDA", "NVIDIA Corp.", 4, 1042.10m, new DateOnly(2026, 4, 28), 1288.55m, now.AddSeconds(-1))
    ],
    [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2")] =
    [
        new UiAccountPositionDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), "GAZP", "Газпром", 500, 168.70m, new DateOnly(2026, 3, 17), 173.34m, now.AddSeconds(-4)),
        new UiAccountPositionDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", "Сбербанк", 250, 301.15m, new DateOnly(2026, 2, 8), 319.80m, now.AddSeconds(-5))
    ],
    [Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1")] =
    [
        new UiAccountPositionDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), "MSFT", "Microsoft Corp.", 8, 433.33m, new DateOnly(2026, 1, 26), 487.11m, now.AddSeconds(-2))
    ],
    [Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1")] =
    [
        new UiAccountPositionDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", "Apple Inc.", 5, 199.95m, new DateOnly(2026, 6, 1), 213.42m, now.AddSeconds(-3)),
        new UiAccountPositionDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", "Сбербанк", 120, 309.40m, new DateOnly(2026, 5, 29), 319.80m, now.AddSeconds(-5))
    ]
};

app.MapGet("/ui/clients", () => Results.Ok(clients));

app.MapGet("/ui/clients/{clientId:guid}/accounts", (Guid clientId) =>
{
    return accountsByClient.TryGetValue(clientId, out var accounts)
        ? Results.Ok(accounts)
        : Results.NotFound();
});

app.MapGet("/ui/accounts/{accountId:guid}/positions", (Guid accountId) =>
{
    return positionsByAccount.TryGetValue(accountId, out var positions)
        ? Results.Ok(positions)
        : Results.NotFound();
});

app.MapGet("/ui/instruments", () =>
{
    var result = instruments
        .Select(x => new UiInstrumentListItemDto(
            x.InstrumentId,
            x.Ticker,
            x.Name,
            x.Type,
            x.Currency,
            x.MarketPrice,
            x.LastUpdatedAtUtc));

    return Results.Ok(result);
});

app.MapGet("/ui/instruments/{instrumentId:guid}", (Guid instrumentId) =>
{
    var instrument = instruments.FirstOrDefault(x => x.InstrumentId == instrumentId);
    return instrument is null ? Results.NotFound() : Results.Ok(instrument);
});

app.Run();
