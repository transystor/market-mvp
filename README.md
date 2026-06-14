# market-mvp

Учебный MVP для подготовки по highload, messaging и микросервисной архитектуре.

## Что уже есть

- `src/MarketMvp.ClientService` - клиенты
- `src/MarketMvp.PortfolioService` - счета и позиции
- `src/MarketMvp.InstrumentService` - справочник инструментов
- `src/MarketMvp.PriceProjectionService` - текущие рыночные цены
- `src/MarketMvp.Bff` - UI-агрегатор
- `src/MarketMvp.Contracts` - DTO-контракты
- `ui/` - React UI

## Что показывает текущий MVP

- выбор клиента
- выбор счёта клиента
- таблица позиций по счёту
- карточка инструмента с текущей рыночной ценой
- отдельная страница со списком инструментов и текущими ценами

Сейчас данные пока seeded/mock, но уже разнесены по отдельным сервисам, а BFF агрегирует их по HTTP.

## Запуск через Docker

```bash
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

- добавить `market-data-ingestor`
- перевести поток цен на Kafka
- сделать live price updates вместо статических seed values
- затем усилить read-path через Redis и live updates
