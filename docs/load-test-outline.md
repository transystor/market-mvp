# load-test-outline

## Зачем нужен этот сценарий

Сейчас `market-mvp` уже хорошо показывает архитектуру. Следующий шаг, который делает его сильнее как highload-стенд, это понятный нагрузочный сценарий.

Цель не в том, чтобы сразу устроить настоящий performance benchmark, а в том, чтобы получить управляемый demo-flow:

- увеличить частоту price ticks,
- посмотреть Kafka поток,
- посмотреть, успевает ли `PriceProjectionService`,
- посмотреть, успевает ли `PortfolioValuationService`,
- увидеть, где появляется lag или stale read model.

---

## Что уже можно использовать прямо сейчас

В проекте уже есть полезные точки наблюдения:

### Kafka UI
- `http://localhost:8081`

Можно смотреть:
- topic `market.price-ticks`
- consumer groups
- offsets
- messages

### PriceProjectionService diagnostics
- `GET http://localhost:5104/diagnostics`

Можно смотреть:
- `LastProcessedTickAtUtc`
- `LastRedisSyncAtUtc`
- `CachedPricesCount`
- `ConsumerGroup`
- `Topic`

### PortfolioValuationService diagnostics
- `GET http://localhost:5106/diagnostics`

Можно смотреть:
- `LastRefreshAtUtc`
- `CachedValuationsCount`
- `LastKnownPriceCount`
- `LastSuccessfulAccountId`

---

## Базовый demo-сценарий

### 1. Поднять стек

```bash
docker compose down -v
docker compose up --build
```

### 2. Открыть три окна

- UI: `http://localhost:5173`
- Kafka UI: `http://localhost:8081`
- diagnostics:
  - `http://localhost:5104/diagnostics`
  - `http://localhost:5106/diagnostics`

### 3. Убедиться, что baseline стабилен

Проверить:
- цены в UI обновляются
- в Kafka UI виден `market.price-ticks`
- diagnostics endpoints отвечают

---

## Сценарий 1. Ускорение auto-ticker

Самый простой способ усилить поток, не меняя код:

в `docker-compose.yml` временно поменять:

```yaml
AUTO_TICK_INTERVAL_SECONDS: "3"
```

на

```yaml
AUTO_TICK_INTERVAL_SECONDS: "1"
```

или даже:

```yaml
AUTO_TICK_INTERVAL_SECONDS: "0"
```

Но лучше для .NET-таймера и стабильности держать минимум `1`.

После этого пересобрать стек:

```bash
docker compose down -v
docker compose up --build
```

### Что смотреть

- насколько быстрее обновляется UI
- как растёт поток сообщений в Kafka UI
- успевает ли `PriceProjectionService`
- не начинает ли отставать `PortfolioValuationService`

---

## Сценарий 2. Burst через simulate-tick

Даже при включённом auto-ticker можно отдельно дать burst вручную.

### Вариант для PowerShell

```powershell
1..100 | ForEach-Object { Invoke-RestMethod -Method Post http://localhost:5105/simulate-tick | Out-Null }
```

### Вариант для bash

```bash
for i in {1..100}; do curl -s -X POST http://localhost:5105/simulate-tick > /dev/null; done
```

### Что смотреть

- как быстро растёт topic activity в Kafka UI
- остаётся ли `LastProcessedTickAtUtc` свежим
- отстаёт ли valuation refresh
- обновляется ли summary в UI с задержкой

---

## Сценарий 3. Mixed load

Комбинация:

- `AUTO_TICK_INTERVAL_SECONDS=1`
- плюс burst из `simulate-tick`
- плюс открытый UI
- плюс polling UI каждые 3 секунды

Это уже даст очень правдоподобный учебный режим.

---

## Как интерпретировать результаты

### Если тормозит PriceProjectionService
Значит bottleneck ближе к:
- Kafka consumer throughput
- Redis writes
- сериализации/deserialization

### Если тормозит PortfolioValuationService
Значит bottleneck ближе к:
- полному пересчёту account valuations
- частым refresh loops
- fan-out чтению из upstream services

### Если тормозит UI
Значит проблема может быть в:
- polling frequency
- избыточных API refresh calls
- BFF/UI rendering path

---

## Что можно честно сказать на интервью

> Я сделал не только архитектуру, но и сценарий, в котором можно вручную увеличить поток событий, открыть Kafka UI, посмотреть диагностику projection и valuation слоёв и увидеть, где система начинает отставать.

Это хороший ответ, потому что он показывает понимание не только дизайна, но и runtime-поведения.

---

## Что логично сделать следующим шагом

Если захочется усилить именно нагрузочную часть, дальше можно добавить:

### 1. отдельный burst endpoint
Например:
- `POST /simulate-burst?count=1000`

### 2. background stress mode
Например env var:
- `AUTO_TICK_INTERVAL_MS`
- `AUTO_TICK_BATCH_SIZE`

### 3. формальные метрики
Например:
- processed ticks/sec
- valuation refresh duration
- Redis key count
- last successful refresh delay

### 4. отдельный profile для stress-demo
Например compose profile или override-файл.

---

## Практический минимум

Если нужен минимальный и уже полезный demo-path, достаточно вот этого:

1. поднять стек
2. открыть UI + Kafka UI + diagnostics
3. уменьшить `AUTO_TICK_INTERVAL_SECONDS` до `1`
4. дать burst на `/simulate-tick`
5. посмотреть, как ведут себя Kafka, projections и summary

Этого уже достаточно, чтобы проект выглядел как настоящий учебный highload-стенд, а не просто набор сервисов.
