# market-mvp

Учебный MVP для подготовки по highload, messaging и микросервисной архитектуре.

## Что уже есть

- `src/MarketMvp.ClientService` - клиенты
- `src/MarketMvp.PortfolioService` - счета и позиции
- `src/MarketMvp.InstrumentService` - справочник инструментов
- `src/MarketMvp.PriceProjectionService` - текущие рыночные цены
- `src/MarketMvp.MarketDataIngestor` - симулятор рыночных тиков и авто-тикер
- `src/MarketMvp.PortfolioValuationService` - valuation read model по счёту
- `src/MarketMvp.Bff` - UI-агрегатор
- Kafka + ZooKeeper в docker-compose для event-driven потока цен
- Kafka UI для просмотра topic, offsets и consumer groups
- Redis для хранения hot current prices
- `src/MarketMvp.Contracts` - DTO-контракты
- `ui/` - React UI

## Что показывает текущий MVP

- выбор клиента
- выбор счёта клиента
- таблица позиций по счёту
- карточка инструмента с текущей рыночной ценой
- отдельная страница со списком инструментов и текущими ценами

Сейчас данные уже разнесены по отдельным сервисам, BFF агрегирует их по HTTP, а поток цен и valuation идут так: `MarketDataIngestor -> Kafka -> PriceProjectionService -> Redis -> PortfolioValuationService -> BFF -> UI`.

## Запуск через Docker

```bash
docker compose up --build
```

Если до этого уже был старый kafka-state, лучше один раз пересоздать стек так:

```bash
docker compose down -v
docker compose up --build
```

После старта будет доступно:
- UI: `http://localhost:5173`
- BFF API: `http://localhost:5032`
- BFF Swagger: `http://localhost:5032/swagger`
- ClientService Swagger: `http://localhost:5101/swagger`
- PortfolioService Swagger: `http://localhost:5102/swagger`
- InstrumentService Swagger: `http://localhost:5103/swagger`
- PriceProjectionService Swagger: `http://localhost:5104/swagger`
- MarketDataIngestor Swagger: `http://localhost:5105/swagger`
- Авто-тикер по умолчанию включён, интервал задаётся через `AUTO_TICK_INTERVAL_SECONDS`
- Kafka broker для хоста: `localhost:9092`
- Kafka broker внутри docker network: `kafka:9092`
- Redis: `localhost:6379`
- Kafka UI: `http://localhost:8081`
- PortfolioValuationService Swagger: `http://localhost:5106/swagger`

## Локальный запуск без Docker

### BFF
```bash
cd src/MarketMvp.Bff
dotnet run
```

### UI
```bash
cd ui
npm install
npm run dev
```

## Следующие шаги

- добавить более чистую Redis-backed valuation cache по account summary
- добавить account summary read model
- при желании перейти с polling на WebSocket/SignalR
