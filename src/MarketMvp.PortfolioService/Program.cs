using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var accounts = new[]
{
    new AccountDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Guid.Parse("11111111-1111-1111-1111-111111111111"), "ACC-10001"),
    new AccountDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Guid.Parse("11111111-1111-1111-1111-111111111111"), "ACC-10002"),
    new AccountDto(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), Guid.Parse("22222222-2222-2222-2222-222222222222"), "ACC-20001"),
    new AccountDto(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), Guid.Parse("33333333-3333-3333-3333-333333333333"), "ACC-30001")
};

var positions = new[]
{
    new PortfolioPositionDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Guid.Parse("f1111111-1111-1111-1111-111111111111"), 15, 180.25m, new DateOnly(2026, 5, 14)),
    new PortfolioPositionDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Guid.Parse("f3333333-3333-3333-3333-333333333333"), 4, 1042.10m, new DateOnly(2026, 4, 28)),
    new PortfolioPositionDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Guid.Parse("f4444444-4444-4444-4444-444444444444"), 500, 168.70m, new DateOnly(2026, 3, 17)),
    new PortfolioPositionDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Guid.Parse("f5555555-5555-5555-5555-555555555555"), 250, 301.15m, new DateOnly(2026, 2, 8)),
    new PortfolioPositionDto(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), Guid.Parse("f2222222-2222-2222-2222-222222222222"), 8, 433.33m, new DateOnly(2026, 1, 26)),
    new PortfolioPositionDto(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), Guid.Parse("f1111111-1111-1111-1111-111111111111"), 5, 199.95m, new DateOnly(2026, 6, 1)),
    new PortfolioPositionDto(Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), Guid.Parse("f5555555-5555-5555-5555-555555555555"), 120, 309.40m, new DateOnly(2026, 5, 29))
};

app.MapGet("/clients/{clientId:guid}/accounts", (Guid clientId) =>
{
    var clientAccounts = accounts.Where(x => x.ClientId == clientId).ToArray();
    return Results.Ok(clientAccounts);
});

app.MapGet("/accounts/{accountId:guid}/positions", (Guid accountId) =>
{
    var accountPositions = positions.Where(x => x.AccountId == accountId).ToArray();
    return Results.Ok(accountPositions);
});

app.Run();
