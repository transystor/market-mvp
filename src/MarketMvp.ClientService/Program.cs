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

var clients = new[]
{
    new ClientDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Иван Петров"),
    new ClientDto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Мария Соколова"),
    new ClientDto(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Алексей Воронов")
};

app.MapGet("/clients", () => Results.Ok(clients));
app.MapGet("/clients/{clientId:guid}", (Guid clientId) =>
{
    var client = clients.FirstOrDefault(x => x.Id == clientId);
    return client is null ? Results.NotFound() : Results.Ok(client);
});

app.Run();
