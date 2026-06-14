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

## Запуск BFF

```bash
cd src/MarketMvp.Bff
dotnet run
```

По умолчанию API будет доступен примерно на:
- `http://localhost:5032`
- `https://localhost:7032`

## Запуск UI

```bash
cd ui
npm install
npm run dev
```

По умолчанию Vite UI будет доступен на:
- `http://localhost:5173`

## Следующие шаги

- вынести mock domain-данные в отдельные сервисы
- добавить `market-data-ingestor`
- добавить `price-projection-service`
- перевести поток цен на Kafka
- затем усилить read-path через Redis и live updates
