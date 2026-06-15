# market-mvp

Учебный highload/Messaging стенд про портфели, рыночные цены, Kafka, Redis и read models.

## Зачем этот проект

Это не просто CRUD-витрина и не набор случайных микросервисов. `market-mvp` нужен как учебная площадка, на которой можно руками разобрать:

- зачем нужен `Kafka`, а не просто HTTP между всеми сервисами,
- где в системе живёт `hot read path`,
- зачем тут `Redis`,
- как выглядит `projection/read model` слой,
- почему `BFF` не должен пересчитывать всё сам,
- где будут bottlenecks, lag и точки масштабирования.

Проект специально собран так, чтобы его можно было:

- запускать локально через Docker,
- показывать на интервью,
- использовать как базу для дальнейших highload-экспериментов.

## Что уже есть

- `src/MarketMvp.ClientService` - клиенты
- `src/MarketMvp.PortfolioService` - счета и позиции
- `src/MarketMvp.InstrumentService` - справочник инструментов
- `src/MarketMvp.PriceProjectionService` - current price projection
- `src/MarketMvp.MarketDataIngestor` - симулятор рыночных тиков и auto-ticker
- `src/MarketMvp.PortfolioValuationService` - valuation и account summary read models по счёту
- `src/MarketMvp.Bff` - UI-агрегатор
- `src/MarketMvp.Contracts` - DTO-контракты
- `ui/` - React UI
- `Kafka + ZooKeeper` в docker-compose для event-driven price flow
- `Kafka UI` для просмотра topic, offsets и consumer groups
- `Redis` для хранения hot current prices и valuation snapshots

## Архитектурная идея

### Source of truth

Сейчас master/business данные живут в domain-сервисах:

- `ClientService` - кто клиент
- `PortfolioService` - какие у клиента счета и позиции
- `InstrumentService` - что это за инструменты

### Streaming path

Рыночные цены не ходят синхронным HTTP-веером по всем сервисам.

Вместо этого:

`MarketDataIngestor -> Kafka(topic: market.price-ticks) -> PriceProjectionService`

Это даёт нормальную учебную модель event-driven потока.

### Hot read path

`PriceProjectionService` получает тики из Kafka и складывает актуальные цены в `Redis`.

Это и есть первый hot read path:

- данные часто обновляются,
- данные часто читаются,
- держать их только в обычной in-memory структуре или каждый раз дёргать цепочку сервисов было бы плохой моделью.

### Read models

`PortfolioValuationService` периодически собирает:

- позиции из `PortfolioService`
- инструменты из `InstrumentService`
- current prices из `PriceProjectionService`

и пишет в `Redis` уже готовые read models:

- valuation snapshot по счёту
- account summary по счёту

То есть UI и BFF работают не с сырой domain-моделью, а с подготовленной проекцией под чтение.

### BFF

`BFF` здесь нужен не как business owner, а как фасад для UI.

Он:

- агрегирует данные для экранов,
- скрывает внутреннюю топологию сервисов,
- отдаёт фронту удобные DTO,
- не должен сам быть местом, где живёт тяжёлая бизнес-агрегация.

## Текущий поток данных

```text
MarketDataIngestor
  -> auto-ticks
  -> Kafka (market.price-ticks)

PriceProjectionService
  -> consume ticks
  -> Redis (price:{instrumentId})

PortfolioValuationService
  -> read positions + instruments + prices
  -> Redis (valuation:{accountId})
  -> Redis (valuation-summary:{accountId})

Bff
  -> /ui/clients
  -> /ui/clients/{clientId}/accounts
  -> /ui/accounts/{accountId}/summary
  -> /ui/accounts/{accountId}/positions
  -> /ui/instruments
  -> /ui/instruments/{instrumentId}

UI
  -> portfolio view
  -> account summary
  -> positions table
  -> instruments list
```

## Что показывает текущий MVP

- выбор клиента
- выбор счёта клиента
- summary по счёту (`TotalValue`, `TotalPnL`, `PositionsCount`)
- таблица позиций по счёту
- карточка инструмента с текущей рыночной ценой
- отдельная страница со списком инструментов и текущими ценами
- auto-refresh UI через polling

## Почему тут Kafka

Kafka здесь нужна не ради модного слова, а ради понятной роли:

- decouple producer и consumers,
- показать event stream,
- показать consumer group,
- later показать lag, throughput и поведение под нагрузкой.

Если делать всё только через HTTP, учебная ценность схемы сильно хуже.

## Почему тут Redis

Redis здесь тоже не для галочки.

Его роль:

- хранить `current prices` как hot mutable state,
- хранить `valuation snapshots`,
- хранить `account summaries`,
- быстро обслуживать read path.

Это гораздо ближе к реальной read-model архитектуре, чем пересчитывать всё на лету в BFF.

## Что именно можно показывать на интервью

На проекте уже можно объяснять:

- difference between source of truth и read model,
- зачем event-driven path через Kafka,
- зачем Redis как hot store,
- почему BFF не должен становиться толстым business-сервисом,
- как можно масштабировать consumers отдельно от UI,
- где может появиться lag,
- какие части системы read-heavy, а какие write/update-heavy.

## Запуск через Docker

```bash
docker compose up --build
```

Если до этого уже был старый kafka-state, лучше один раз пересоздать стек так:

```bash
docker compose down -v
docker compose up --build
```

## Что будет доступно после старта

- UI: `http://localhost:5173`
- BFF API: `http://localhost:5032`
- BFF Swagger: `http://localhost:5032/swagger`
- ClientService Swagger: `http://localhost:5101/swagger`
- PortfolioService Swagger: `http://localhost:5102/swagger`
- InstrumentService Swagger: `http://localhost:5103/swagger`
- PriceProjectionService Swagger: `http://localhost:5104/swagger`
- MarketDataIngestor Swagger: `http://localhost:5105/swagger`
- PortfolioValuationService Swagger: `http://localhost:5106/swagger`
- Kafka UI: `http://localhost:8081`
- Redis: `localhost:6379`
- Kafka broker для хоста: `localhost:9092`
- Kafka broker внутри docker network: `kafka:9092`

Авто-тикер по умолчанию включён, интервал задаётся через `AUTO_TICK_INTERVAL_SECONDS`.

## Полезные endpoints

### BFF
- `GET /ui/clients`
- `GET /ui/clients/{clientId}/accounts`
- `GET /ui/accounts/{accountId}/summary`
- `GET /ui/accounts/{accountId}/positions`
- `GET /ui/instruments`
- `GET /ui/instruments/{instrumentId}`

### PriceProjectionService
- `GET /prices`
- `GET /prices/{instrumentId}`
- `GET /diagnostics`

### PortfolioValuationService
- `GET /valuations/{accountId}`
- `GET /account-summaries/{accountId}`
- `GET /diagnostics`

### MarketDataIngestor
- `GET /ticks`
- `POST /simulate-tick`

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

## Где проект ещё шероховатый

Это уже хороший учебный стенд, но ещё не production-grade система.

Самые заметные упрощения сейчас:

- в `PortfolioValuationService` ещё жёстко зашит список `clientIds`
- UI пока сидит на polling, не на WebSocket/SignalR
- нагрузочный режим ещё не выделен отдельно
- часть данных всё ещё seeded/mock-like

## Нагрузочный demo-path

Отдельный outline для стресс-сценария лежит здесь:

- `docs/load-test-outline.md`

Там описано:
- как ускорить поток тиков,
- как дать burst через `simulate-tick`,
- что смотреть в `Kafka UI`,
- как читать diagnostics endpoints,
- как объяснять bottlenecks.

## Логичные следующие шаги

- убрать жёстко зашитый список `clientIds` из valuation worker
- перейти с polling на WebSocket/SignalR
- добавить более формальные metrics/observability hooks
- позже, если нужно, добавить отдельный live push слой и более явную scale story
