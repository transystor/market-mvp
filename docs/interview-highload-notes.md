# interview-highload-notes

## Как коротко презентовать проект

`market-mvp` это учебный highload-стенд про портфельную витрину, где рыночные цены идут через Kafka, hot state хранится в Redis, а UI читает готовые read models через BFF.

Ключевая мысль: проект показывает не просто микросервисы, а разделение на:

- source of truth
- streaming path
- projection/read model layer
- hot cache/store
- UI facade

---

## Из каких частей он состоит

### Domain/source services
- `ClientService`
- `PortfolioService`
- `InstrumentService`

Это владельцы основной бизнес-информации.

### Event producer
- `MarketDataIngestor`

Он генерирует price ticks и публикует их в Kafka.

### Projection service
- `PriceProjectionService`

Он читает `market.price-ticks` из Kafka и обновляет current prices в Redis.

### Read model service
- `PortfolioValuationService`

Он собирает позиции, инструменты и цены, после чего кладёт в Redis:

- valuation snapshot
- account summary

### UI facade
- `BFF`

Он отдаёт фронту удобные view DTO и скрывает внутреннюю структуру сервисов.

### UI
- `React`

Показывает портфель, позиции, summary и список инструментов.

---

## Зачем здесь Kafka

Kafka здесь нужна для event-driven price flow.

Что это даёт:

- producer и consumers не связаны напрямую HTTP-вызовами
- можно масштабировать consumers независимо
- можно обсуждать partitions, offsets, consumer groups, lag
- можно наращивать downstream consumers без переписывания producer

Если бы цены шли просто через HTTP, проект был бы слабее как учебный highload-кейс.

---

## Зачем здесь Redis

Redis хранит hot state:

- current prices
- valuation snapshots
- account summaries

Это полезно потому что:

- цены часто меняются
- UI часто читает их
- valuation удобно хранить как уже подготовленную read model

Идея в том, что Redis здесь это не database of record, а быстрый store для serving/read path.

---

## Почему BFF не считает всё сам

Если BFF начнёт сам каждый раз:

- ходить за позициями
- ходить за инструментами
- ходить за ценами
- считать valuation на лету

то он быстро станет толстым aggregation bottleneck.

Поэтому тяжёлая сборка вынесена в `PortfolioValuationService`, а BFF лишь отдаёт готовую модель UI.

---

## Где тут CQRS/read model mindset

В проекте уже есть разделение:

### Write/update side
- domain services
- event producer
- Kafka stream

### Read side
- price projection
- valuation projection
- account summary projection
- Redis
- BFF/UI DTO

То есть UI работает не с сырой доменной моделью, а с моделью, подготовленной для чтения.

---

## Какие bottlenecks можно обсуждать

### 1. PriceProjectionService
Если тиков станет слишком много:
- consumer может начать отставать
- lag будет расти
- Redis write throughput станет важен

### 2. PortfolioValuationService
Если аккаунтов/позиций станет слишком много:
- полный пересчёт всех аккаунтов каждые N секунд станет дорогим
- придётся переходить к более инкрементальной модели

### 3. BFF
Если превратить BFF в тяжёлый бизнес-агрегатор, он станет узким местом.

### 4. UI polling
Polling годится для MVP, но under scale лучше переходить на push-модель.

---

## Что бы я улучшал дальше

### 1. Убрать жёстко зашитые clientIds
Valuation worker сейчас знает клиентов заранее. Это упрощение для MVP.

### 2. Добавить нагрузочный сценарий
Чтобы можно было показать:
- throughput
- lag
- offset growth
- влияние burst traffic

### 3. Перейти с polling на WebSocket/SignalR
Для рыночных данных это естественнее.

### 4. Добавить более формальные метрики
Например:
- processed ticks per second
- valuation refresh duration
- cache key count
- last successful refresh time

---

## Как отвечать на вопрос “зачем тут вообще столько всего?”

Хороший короткий ответ:

> Потому что цель проекта не просто показать UI и несколько сервисов, а показать, как разделяются source of truth, event stream, projections, hot cache и UI facade в системе, где данные часто обновляются и часто читаются.

---

## Как отвечать на вопрос “почему не просто одна база и один API?”

Потому что тогда теряется учебная ценность именно highload/event-driven части:

- не видно роли Kafka
- не видно роли Redis
- не видно read models
- не видно проблем lag и recomputation
- не видно причин для BFF и projection services

Для обычного CRUD это было бы оверкилл. Для учебного highload-стенда это как раз смысл проекта.
