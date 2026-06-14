# market-mvp

Учебный MVP для подготовки по highload, messaging и микросервисной архитектуре.

## Что уже есть

- `src/MarketMvp.Bff` - минимальный BFF/API для UI
- `src/MarketMvp.Contracts` - DTO-контракты
- `ui/` - React UI

## Что показывает текущий MVP

- выбор клиента
- выбор счёта клиента
- таблица позиций по счёту
- карточка инструмента с текущей рыночной ценой
- отдельная страница со списком инструментов и текущими ценами

Сейчас данные пока seeded/mock, чтобы быстро получить end-to-end витрину.

## Запуск через Docker

```bash
docker compose up --build
```

После старта будет доступно:
- UI: `http://localhost:5173`
- BFF API: `http://localhost:5032`
- Swagger: `http://localhost:5032/swagger`

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

- вынести mock domain-данные в отдельные сервисы
- добавить `market-data-ingestor`
- добавить `price-projection-service`
- перевести поток цен на Kafka
- затем усилить read-path через Redis и live updates
