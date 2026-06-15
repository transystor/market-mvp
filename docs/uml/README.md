# UML examples for market-mvp

Здесь лежат учебные PlantUML-диаграммы по проекту `market-mvp`.

## Файлы

- `sequence-diagram.puml` - как идут price ticks, valuation refresh и UI чтение
- `component-diagram.puml` - как разложены компоненты и зависимости
- `class-diagram.puml` - ключевые DTO, read models и их связи

## Зачем именно PlantUML

Потому что это:
- текстовый формат,
- удобно учить UML,
- легко править руками,
- просто рендерить в PNG/SVG позже.

## Как читать эти диаграммы

### Sequence Diagram
Показывает поведение во времени:
- кто кому пишет,
- в каком порядке,
- где event flow,
- где read flow.

### Component Diagram
Показывает архитектурные блоки:
- UI,
- BFF,
- source services,
- Kafka,
- Redis,
- projection services.

### Class Diagram
Показывает структуру ключевых контрактов:
- domain-ish DTO,
- price events,
- valuation read models,
- UI DTO mappings.

## Важно

Это учебные диаграммы под текущую версию проекта.
Они специально упрощены, чтобы на них было легче учиться UML, а не тонуть в деталях.
