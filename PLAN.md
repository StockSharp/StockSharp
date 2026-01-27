# План: Исправления, тестирование, рефакторинг

## ВАЖНО: Общие правила выполнения

1. **Не подгонять тесты под ошибочную реализацию.** Если тест выявляет баг — исправлять реализацию, а не тест.
2. **Последовательное выполнение.** Этапы идут по порядку. После каждого этапа — прогнать тесты, убедиться что всё зелёное, и записать краткий отчёт в секцию «Отчёты» в конце этого файла.
3. **Тесты в отдельных классах** — каждая логическая группа в своём файле.

---

## Замечание 1: Неконсистентность путей подписки MarketData

### Текущее поведение

Два разных code path для подписки в `ProcessMarketDataRequest`:

**Path A — News/Board** (строка 1149-1161):
```csharp
if (mdMsg.DataType2 == DataType.News || mdMsg.DataType2 == DataType.Board)
{
    _subscriptionRouting.AddSubscription(..., adapters, ...);
    await ToChild(mdMsg, adapters).Select(pair => SendRequest(...)).WhenAll();
}
```
- Вызывает `ToChild()` → новый `TransactionId` для каждого адаптера
- Записывает маппинг в `ParentChildMap`
- Работает с массивом адаптеров (даже если один)

**Path B — всё остальное (Ticks, Level1, MarketDepth, свечи)** (строка 1170-1217):
```csharp
else
{
    adapter = (await GetAdapters())?.First();  // берёт ОДИН адаптер
    _subscriptionRouting.AddSubscription(..., new[] { adapter }, ...);
    await SendRequest(mdMsg, adapter, cancellationToken);  // без ToChild
}
```
- Берёт только первый адаптер через `.First()`
- НЕ вызывает `ToChild()` → оригинальный `TransactionId`
- НЕ записывает в `ParentChildMap`
- Ответы идут без ремаппинга ID

### Проблемы

1. **Неконсистентность**: один адаптер, два разных типа данных — разные пути обработки, разные ID
2. **ParentChildMap не видит** Ticks/Level1/MarketDepth подписки — любая логика завязанная на ParentChildMap пропустит их
3. **Только первый адаптер** получает подписку — если два адаптера поддерживают Ticks, второй игнорируется (для News — нет, идёт ко всем)

### Правильное поведение

Единый путь для всех типов данных:

- **Инструмент указан** (`SecurityId != default`) → роутить только в тот адаптер, который прислал нам этот инструмент (через `SecurityAdapterProvider`). Если маппинга нет — в первый поддерживающий.
- **Инструмент НЕ указан** → роутить во все адаптеры, которые поддерживают этот тип данных
- **Всегда** использовать `ToChild()` и `ParentChildMap` для единообразия маппинга ID

### Что нужно изменить

В `ProcessMarketDataRequest` убрать ветвление по `DataType.News || DataType.Board`. Сделать единый путь:

```csharp
private async ValueTask ProcessMarketDataRequest(MarketDataMessage mdMsg, CancellationToken cancellationToken)
{
    IMessageAdapter[] adapters;

    if (mdMsg.IsSubscribe)
    {
        var (a, isPended) = await GetSubscriptionAdapters(mdMsg, cancellationToken);
        if (isPended) return;

        adapters = a;
        if (adapters.Length == 0)
        {
            await SendOutMessageAsync(mdMsg.TransactionId.CreateNotSupported(), cancellationToken);
            return;
        }

        _subscriptionRouting.AddSubscription(mdMsg.TransactionId, (ISubscriptionMessage)mdMsg.Clone(), adapters, mdMsg.DataType2);
    }
    else
    {
        adapters = null;
    }

    // Единый путь: всегда ToChild
    await ToChild(mdMsg, adapters).Select(pair => SendRequest(pair.Key, pair.Value, cancellationToken)).WhenAll();
}
```

### Тесты для верификации

- Подписка на Ticks с одним адаптером → `ParentChildMap.AddMapping()` вызван
- Подписка на News с одним адаптером → `ParentChildMap.AddMapping()` вызван (тот же путь)
- Подписка на Ticks без маппинга с двумя адаптерами → идёт к первому поддерживающему (или ко всем?)
- Ответ SubscriptionResponse → ремаппинг childId → parentId для Ticks (как и для News)

---

## Замечание 2: Отсутствие фильтрации IsAllDownloadingSupported для OrderStatus и PortfolioLookup

### Текущее поведение

В `GetAdapters()` (строка 979-985) есть фильтрация только для `SecurityLookup`:

```csharp
else if (message.Type == MessageTypes.SecurityLookup)
{
    var isAll = ((SecurityLookupMessage)message).IsLookupAll();

    if (isAll)
        adapters = [.. adapters.Where(a => a.IsSupportSecuritiesLookupAll())];
}
```

Где `IsSupportSecuritiesLookupAll()` — это обёртка над `IsAllDownloadingSupported(DataType.Securities)`.

**Аналогичной фильтрации НЕТ для:**
- `OrderStatusMessage` (`DataType.Transactions`) — идёт через `ProcessOtherMessage` → `GetAdapters` → без фильтра
- `PortfolioLookupMessage` (`DataType.PositionChanges`) — аналогично

### Проблема

Когда приходит `OrderStatusMessage` без конкретного инструмента (запрос «дай все ордера»), basket отправляет его ко **всем** адаптерам, зарегистрированным для `MessageTypes.OrderStatus`. Но часть адаптеров может **не поддерживать** скачивание всех транзакций (`IsAllDownloadingSupported(DataType.Transactions) == false`).

Тот же баг для `PortfolioLookupMessage` — если адаптер не поддерживает `IsAllDownloadingSupported(DataType.PositionChanges)`, basket всё равно шлёт ему запрос «скачай все позиции».

В результате:
1. Адаптер получает запрос, который не может выполнить → вероятная ошибка или пустой ответ
2. Неконсистентность: `SecurityLookup` фильтруется, а `OrderStatus` и `PortfolioLookup` — нет

### Правильное поведение

Единообразная проверка `IsAllDownloadingSupported` для всех типов подписок-запросов, которые могут быть «скачай всё»:

```csharp
// Вместо проверки только SecurityLookup — универсальная проверка
if (message is ISubscriptionMessage subscrMsg && subscrMsg is ISecurityIdMessage secIdMsg)
{
    // Если запрос "без критериев" (IsLookupAll / пустой SecurityId)
    if (IsLookupAllRequest(message))
    {
        var dataType = subscrMsg.DataType;
        adapters = [.. adapters.Where(a => a.IsAllDownloadingSupported(dataType))];
    }
}
```

Либо, минимально — добавить аналогичные блоки:

```csharp
else if (message.Type == MessageTypes.SecurityLookup)
{
    if (((SecurityLookupMessage)message).IsLookupAll())
        adapters = [.. adapters.Where(a => a.IsAllDownloadingSupported(DataType.Securities))];
}
else if (message.Type == MessageTypes.OrderStatus)
{
    if (IsLookupAllOrders((OrderStatusMessage)message))
        adapters = [.. adapters.Where(a => a.IsAllDownloadingSupported(DataType.Transactions))];
}
else if (message.Type == MessageTypes.PortfolioLookup)
{
    if (IsLookupAllPortfolios((PortfolioLookupMessage)message))
        adapters = [.. adapters.Where(a => a.IsAllDownloadingSupported(DataType.PositionChanges))];
}
```

### Какие ещё подписки затронуты

Нужно проверить все `ISubscriptionMessage`, которые идут через `ProcessOtherMessage`:
- `OrderStatusMessage` — `DataType.Transactions`
- `PortfolioLookupMessage` — `DataType.PositionChanges`
- `SecurityLookupMessage` — `DataType.Securities` (уже есть фильтр)
- Другие типы подписок, если они поддерживают режим «скачай всё»

### Тесты для верификации

Для **каждого** типа подписки (SecurityLookup, OrderStatus, PortfolioLookup):

1. **Адаптер поддерживает IsAllDownloading + запрос без критериев** → сообщение доставлено
2. **Адаптер НЕ поддерживает IsAllDownloading + запрос без критериев** → сообщение НЕ доставлено (отфильтровано)
3. **Адаптер НЕ поддерживает IsAllDownloading + запрос С конкретным SecurityId** → сообщение доставлено (фильтр не применяется)
4. **Два адаптера: один поддерживает, другой нет + запрос без критериев** → сообщение идёт только к поддерживающему
5. **Два адаптера: оба поддерживают** → сообщение идёт к обоим

Матрица тестов (3 типа × 5 сценариев = 15 тестов):

| Тип сообщения | SecurityId | IsAllDownloading | Ожидание |
|---|---|---|---|
| SecurityLookup | пустой (IsLookupAll) | true | доставлено |
| SecurityLookup | пустой (IsLookupAll) | false | отфильтровано |
| SecurityLookup | задан | false | доставлено |
| OrderStatus | пустой | true | доставлено |
| OrderStatus | пустой | false | отфильтровано |
| OrderStatus | задан | false | доставлено |
| PortfolioLookup | пустой | true | доставлено |
| PortfolioLookup | пустой | false | отфильтровано |
| PortfolioLookup | задан | false | доставлено |

---

## Замечание 3: Retry при NotSupported работает только для MarketData

### Текущее поведение

В `ProcessSubscriptionResponse` (строка 1638-1652), когда подписка получает `NotSupported`, для **любого** `ISubscriptionMessage` выполняется:

```csharp
if (message.IsNotSupported() && originMsg is ISubscriptionMessage subscrMsg)
{
    if (subscrMsg.IsSubscribe)
    {
        var set = _nonSupportedAdapters.SafeAdd(originalTransactionId, _ => []);
        set.Add(GetUnderlyingAdapter(adapter));
        subscrMsg.LoopBack(this);
    }
    return (Message)subscrMsg;
}
```

Однако фильтрация `_nonSupportedAdapters` в `GetAdapters()` (строка 962-970) применяется **только** для `MessageTypes.MarketData`:

```csharp
if (message.Type == MessageTypes.MarketData)
{
    var set = _nonSupportedAdapters.TryGetValue(mdMsg1.TransactionId);
    if (set != null)
        adapters = [.. adapters.Where(a => !set.Contains(GetUnderlyingAdapter(a)))];
}
```

### Проблемы

1. **Потенциальный бесконечный цикл**: если `SecurityLookupMessage`, `OrderStatusMessage` или `PortfolioLookupMessage` получают `NotSupported` от адаптера → loopback → `GetAdapters()` возвращает тот же адаптер (фильтр не применён) → снова NotSupported → loopback → ...
2. **Мёртвые записи**: `_nonSupportedAdapters` заполняется для не-MarketData подписок, но эти записи никогда не читаются и не очищаются (только при Reset)
3. **Неконсистентность**: retry-механизм либо должен работать для всех типов подписок, либо loopback не должен вызываться для не-MarketData

### Правильное поведение

Вариант A — распространить фильтр на все типы:

```csharp
// В GetAdapters(), после получения adapters:
var set = _nonSupportedAdapters.TryGetValue(transactionId);
if (set != null)
    adapters = [.. adapters.Where(a => !set.Contains(GetUnderlyingAdapter(a)))];
```

Вариант B — loopback только для MarketData (если для других типов retry не нужен):

```csharp
if (message.IsNotSupported() && originMsg is MarketDataMessage subscrMsg)  // не ISubscriptionMessage
```

### Тесты

- SecurityLookup → NotSupported от одного из двух адаптеров → retry → второй адаптер получает запрос (сейчас не работает)
- OrderStatus → NotSupported → не зацикливается
- Очистка `_nonSupportedAdapters` при отписке (сейчас не очищается)

---

---

# Порядок выполнения

## Этап 1: Тесты → правильное ожидаемое поведение

Прежде чем менять код basket — **сначала написать/обновить тесты**, которые описывают **правильное** поведение:

1. По каждому замечанию (1, 2, 3) написать тесты, которые проверяют **ожидаемое правильное** поведение, а не текущее
2. Запустить тесты — увидеть какие падают (это подтверждение что баг есть)
3. Зафиксировать: «вот тест, вот ожидание, вот что реально происходит»

Тесты должны проверять моки внутренних состояний на каждом шаге:
- Отправили сообщение в basket → проверили что `ParentChildMap`, `SubscriptionRouting`, `OrderRouting` обновились правильно
- Получили ответ от inner-адаптера → проверили что состояния обновились, выходное сообщение корректное

## Этап 2: Фиксы замечаний

После того как тесты написаны и падают в нужных местах:

1. Применить исправления из Замечания 1 (единый путь MarketData)
2. Применить исправления из Замечания 2 (IsAllDownloading для всех подписок)
3. Применить исправления из Замечания 3 (NotSupported retry для всех или только MarketData)
4. После каждого фикса — прогнать все тесты, убедиться что нужные стали зелёными, остальные не сломались

## Этап 3: Выделение роутинга в отдельный класс

Сейчас логика выбора адаптера (роутинг) вшита внутрь `BasketMessageAdapter`:
- `GetAdapters()` — поиск по `_messageTypeAdapters`, `_securityAdapters`, фильтрация по `IsAllDownloading`, `_nonSupportedAdapters`
- `GetSubscriptionAdapters()` — дополнительная фильтрация по `SupportedInMessages`, таймфреймам свечей
- `GetAdapter()` — поиск по портфолио
- `ProcessPortfolioMessage()` — роутинг ордеров по портфолио
- `ProcessOrderMessage()` — роутинг cancel/replace по `_orderRouting`

Всё это сложно тестировать отдельно от basket.

**Что сделать:**

1. Выделить интерфейс `IAdapterRouter` (или `IMessageRouter`) с методами:
   - `GetAdapters(Message message)` → `IMessageAdapter[]` — основной роутинг
   - `GetSubscriptionAdapters(MarketDataMessage mdMsg)` → `IMessageAdapter[]` — для MarketData
   - `GetOrderAdapter(string portfolioName, Message message)` → `IMessageAdapter` — для ордеров
   - `GetOrderAdapter(long originTransId)` → `IMessageAdapter` — для cancel/replace
   - `FilterNotSupported(long transId, IMessageAdapter[] adapters)` → `IMessageAdapter[]` — фильтр retry
   - `AddNotSupported(long transId, IMessageAdapter adapter)` — запомнить NotSupported

2. Реализовать `AdapterRouter` — вынести туда всю логику из `GetAdapters()`, `GetSubscriptionAdapters()`, `GetAdapter()`, фильтрацию `_nonSupportedAdapters`

3. `BasketMessageAdapter` получает `IAdapterRouter` через конструктор (как уже сделано для других состояний)

4. `BasketMessageAdapter` вызывает роутер вместо внутренней логики

**Зачем:**
- Роутер можно тестировать изолированно, без поднятия basket
- В тестах basket можно подставить мок-роутер и проверять что basket правильно вызывает роутер и правильно обрабатывает его результаты
- Разделение ответственности: basket = оркестрация + состояние, роутер = выбор адаптера

## Этап 4: Обновление тестов

После выделения роутера:

1. **Тесты роутера** (новые): подать на вход сообщение + набор адаптеров с разными capabilities → проверить какие адаптеры выбраны
   - Мок-адаптеры с разными `IsAllDownloadingSupported`, `SupportedInMessages`, `GetTimeFrames`
   - Проверить все комбинации из матриц Замечаний 1-3
   - Роутер тестируется как чистая функция, без async, без подключения

2. **Тесты basket** (обновить существующие): передать мок `IAdapterRouter` в basket
   - Проверить что basket вызывает роутер с правильными аргументами
   - Проверить что basket правильно обрабатывает результат роутера (отправляет сообщения нужным адаптерам, обновляет состояния)
   - Проверить что basket НЕ делает свой роутинг в обход роутера
   - Мок-роутер позволяет контролировать какой адаптер вернётся — и проверять реакцию basket

3. **Прогнать все тесты** — и старые и новые — убедиться что рефакторинг ничего не сломал

## Этап 5: Тестирование менеджеров-адаптеров с мок-состояниями

После этапа 4 — прогнать все тесты, убедиться что ничего не отвалилось. Далее переходим к менеджерам.

### Суть

В проекте есть менеджеры-адаптеры, у которых внутреннее состояние уже вынесено в отдельный класс с интерфейсом. Сейчас:
- Менеджеры тестируются со стандартным (реальным) состоянием
- Состояния тестируются изолированно сами по себе

**Чего не хватает:** тестов, где в менеджер засовывается **мок-объект состояния**, далее в менеджер передаются сообщения, и на каждом этапе (каждое новое сообщение) мок-состояние **валидирует себя** — проверяет что оно находится в корректном состоянии. Тем самым проверяется, что менеджер правильно дёргает состояние.

### Список менеджеров с вынесенным состоянием

| # | Менеджер/Адаптер | Файл | Интерфейс состояния | Реализация состояния |
|---|---|---|---|---|
| 1 | `HeartbeatMessageAdapter` | `Algo/HeartbeatMessageAdapter.cs` | `IHeartbeatManagerState` | `HeartbeatManagerState` |
| 2 | `LookupTrackingMessageAdapter` | `Algo/LookupTrackingMessageAdapter.cs` | `ILookupTrackingManagerState` | `LookupTrackingManagerState` |
| 3 | `OfflineMessageAdapter` | `Algo/OfflineMessageAdapter.cs` | `IOfflineManagerState` | `OfflineManagerState` |
| 4 | `SubscriptionMessageAdapter` | `Algo/SubscriptionMessageAdapter.cs` | `ISubscriptionManagerState` | `SubscriptionManagerState` |
| 5 | `SubscriptionOnlineMessageAdapter` | `Algo/SubscriptionOnlineMessageAdapter.cs` | `ISubscriptionOnlineManagerState` | `SubscriptionOnlineManagerState` |
| 6 | `OrderBookIncrementMessageAdapter` | `Algo/OrderBookIncrementMessageAdapter.cs` | `IOrderBookIncrementManagerState` | `OrderBookIncrementManagerState` |
| 7 | `OrderBookTruncateMessageAdapter` | `Algo/OrderBookTruncateMessageAdapter.cs` | `IOrderBookTruncateManagerState` | `OrderBookTruncateManagerState` |
| 8 | `Level1DepthBuilderAdapter` | `Algo/Level1DepthBuilderAdapter.cs` | `ILevel1DepthBuilderManagerState` | `Level1DepthBuilderManagerState` |
| 9 | `LatencyMessageAdapter` | `Algo/Latency/LatencyMessageAdapter.cs` | `ILatencyManagerState` | `LatencyManagerState` |
| 10 | `SlippageMessageAdapter` | `Algo/Slippage/SlippageMessageAdapter.cs` | `ISlippageManagerState` | `SlippageManagerState` |
| 11 | `PositionMessageAdapter` | `Algo/Positions/PositionMessageAdapter.cs` | `IPositionManagerState` | `PositionManagerState` |

Также менеджеры Basket (уже покрыты на этапах 1-4):

| # | Менеджер | Файл | Интерфейс состояния | Реализация состояния |
|---|---|---|---|---|
| 1 | `AdapterConnectionManager` | `Algo/Basket/AdapterConnectionManager.cs` | `IAdapterConnectionState` | `AdapterConnectionState` |
| 2 | `PendingMessageManager` | `Algo/Basket/PendingMessageManager.cs` | `IPendingMessageState` | `PendingMessageState` |

### Что сделать для каждого менеджера

1. **Создать мок-состояние** — реализация интерфейса `I*State`, которая:
   - Хранит все вызовы (какие методы вызвались, с какими аргументами)
   - На каждом вызове **валидирует** что текущее состояние допустимо (например: нельзя удалить подписку которая не была добавлена, нельзя начать disconnect если не было connect, и т.д.)
   - Может кидать исключение при нарушении инвариантов

2. **Написать тесты** — передать мок-состояние в менеджер через конструктор, далее:
   - Подавать последовательность сообщений (Connect, MarketData subscribe, данные, unsubscribe, Disconnect, Reset...)
   - После каждого сообщения проверять что мок-состояние получило правильные вызовы
   - Проверять что менеджер не пропустил обновление состояния и не вызвал лишнего

3. **Прогнать тесты** — убедиться что менеджеры корректно взаимодействуют с состоянием на каждом этапе

### Примерный шаблон мок-состояния

```csharp
class ValidatingSubscriptionManagerState : ISubscriptionManagerState
{
    private readonly HashSet<long> _activeSubscriptions = [];
    public List<(string method, object[] args)> Calls { get; } = [];

    public void AddSubscription(long id, ...)
    {
        Calls.Add(("AddSubscription", [id, ...]));

        // Валидация: подписка не должна уже существовать
        if (!_activeSubscriptions.Add(id))
            throw new InvalidOperationException($"Subscription {id} already exists");
    }

    public void RemoveSubscription(long id)
    {
        Calls.Add(("RemoveSubscription", [id]));

        // Валидация: подписка должна существовать
        if (!_activeSubscriptions.Remove(id))
            throw new InvalidOperationException($"Subscription {id} not found");
    }
}
```

### Примерный тест

```csharp
[TestMethod]
public async Task Subscribe_Unsubscribe_StateUpdatedCorrectly()
{
    var state = new ValidatingSubscriptionManagerState();
    var adapter = CreateManagerAdapter(state);

    // subscribe
    await adapter.SendInMessageAsync(subscribeMsg);
    state.Calls.Last().method.AssertEqual("AddSubscription");

    // response
    adapter.SimulateOutMessage(responseMsg);
    // state validates itself — no exception = OK

    // unsubscribe
    await adapter.SendInMessageAsync(unsubscribeMsg);
    state.Calls.Last().method.AssertEqual("RemoveSubscription");

    // double unsubscribe → state throws InvalidOperationException
}
```

---

## Этап 6: Покрытие тестами методов Messages/Extensions.cs

После этапа 5 — прогнать все тесты, убедиться что ничего не отвалилось. Далее покрываем Extensions.

Тесты в отдельном классе `ExtensionsMethodsTests.cs`. Выполнять по тирам, после каждого — отчёт.

### Tier 1 — Алгоритмы и сложная логика (методы 1-12)

| # | Метод | Строки | Почему тестировать |
|---|-------|--------|-------------------|
| 1 | **GetSpreadMiddle** (2 overloads) | 118-202 | Null-обработка bid/ask, округление по price step, несколько путей |
| 2 | **ShrinkPrice** (2 overloads) | 3698-3723 | Правила округления (Auto/Less), взаимодействие price step + decimals |
| 3 | **ToOrderSnapshot** | 4851-4927 | Слияние N execution diff в один снимок, сортировка по state, поле-за-полем перезапись |
| 4 | **AddDelta** (QuoteChange[]) | 3499-3565 | Алгоритм слияния стакана с направлением bid/ask, удаление нулевого объёма |
| 5 | **Group** (OrderBook) | 3265-3383 | Группировка стакана по ценовому диапазону |
| 6 | **GetCandleBounds** | 4390-4456 | Сложная математика времени: неделя/месяц/интрадей, рабочие периоды, клиринг |
| 7 | **Iso10962** + **Iso10962ToSecurityType** + **Iso10962ToOptionType** | 3992-4169 | Двунаправленный маппинг ISO 10962, много веток SecurityType |
| 8 | **CreateErrorResponse** | 1827-1879 | Switch по ~8 типам сообщений для построения типизированного ответа об ошибке |
| 9 | **IsTradeTime** (WorkingTime overload) | 4642-4662 | Проверка enabled, торговой даты, рабочих периодов, диапазонов времени дня |
| 10 | **AddOrSubtractTradingDays** | 4724-4746 | Цикл с +/- направлением, пропускает нерабочие дни |
| 11 | **GetOrderLogCancelReason** | 4347-4367 | Разбор битовых флагов (0x100000..0x800000) причин отмены |
| 12 | **DecodeToPeriods** / **EncodeToString** | 1215-1263 | Сериализация строка ↔ WorkingTimePeriod[] с вложенными диапазонами |

### Tier 2 — Важная логика состояний и конвертации (методы 13-24)

| # | Метод | Строки | Почему тестировать |
|---|-------|--------|-------------------|
| 13 | **ToReg** + **ToExec** | 3730-3811 | Двунаправленная конвертация (20+ полей), важна корректность roundtrip |
| 14 | **IsCanceled** / **IsMatched** / **IsMatchedPartially** / **IsMatchedEmpty** | 3869-3914 | 4 проверки состояния ордера — по отдельности просты, но критичны для корректности |
| 15 | **ApplyNewBalance** | 4937-4949 | Валидация: отрицательный баланс, увеличение баланса → логирование ошибок |
| 16 | **SafeGetVolume** | 4272-4287 | Ветвление trade vs order, разные сообщения об ошибках |
| 17 | **IsAllSecurity** | ~2195-2220 | Сравнение SecurityId с AssociatedBoardCode |
| 18 | **IsLookupAll** | 2870-2913 | Проверка дефолтности множества полей (security type, code, board и т.д.) |
| 19 | **ToMessageType2** | 2943-2973 | Маппинг DataType → MessageTypes через switch |
| 20 | **FileNameToDataType** / **DataTypeToFileName** | 687-729 | Парсинг типов свечей из/в имена файлов |
| 21 | **IsTradeDate** | 4681-4696 | Спец. праздники/рабочие дни, проверка выходных |
| 22 | **LastTradeDay** | 4705-4714 | Цикл назад в поиске торгового дня |
| 23 | **GetPlazaTimeInForce** | 4320-4330 | Извлечение TimeInForce из битовых флагов |
| 24 | **NearestSupportedDepth** | 1641-1660 | Поиск ближайшей поддерживаемой глубины из допустимого набора |

### Tier 3 — Средний приоритет (методы 25-30)

| # | Метод | Строки | Почему тестировать |
|---|-------|--------|-------------------|
| 25 | **ToType** (Level1Fields) | 2314-2340 | Switch-маппинг ~30 полей → CLR типы |
| 26 | **ToType** (PositionChangeTypes) | 2342-2351 | Switch-маппинг полей позиций → CLR типы |
| 27 | **ToReadableString** | 3960-3985 | TimeSpan → человекочитаемая строка (дни/часы/минуты/секунды) |
| 28 | **BuildIfNeed** | 4791-4811 | Инкрементальная сборка стакана через OrderBookIncrementBuilder |
| 29 | **Join** (OrderBook) | 3573-3590 | Слияние двух стаканов с правильной сортировкой bid/ask |
| 30 | **IsHalfEmpty** | 4831-4842 | XOR-логика для одностороннего стакана |

## Этап 7: Починка существующих сломанных тестов

После всех рефакторингов и новых тестов — починить тесты, которые были сломаны ещё до текущих изменений:

- `AsyncExtensionsTests.cs`
- `ConnectorRoutingTests.cs`
- `RemoteStorageClientTests.cs`
- `SubscriptionHolderTests.cs`
- `SubscriptionManagerConnectorTests.cs`
- `SubscriptionMessageAdapterTests.cs`
- `SubscriptionOnlineMessageAdapterTests.cs`
- `SubscriptionDataFeedTests.cs`
- `SubscriptionManagerTests.cs`
- `SubscriptionOnlineManagerTests.cs`
- `SubscriptionRoutingStateTests.cs`

Вероятно, реализация поменялась, а тесты не обновились. Или наоборот — тесты правильные, а реализация сломана. Нужно разобраться в каждом случае.

К этому моменту уже будут:
- Моки для `BasketMessageAdapter` (второй конструктор с инжекцией состояний)
- Выделенный `IAdapterRouter` с мок-реализацией
- Моки для всех менеджеров-адаптеров
- Полное понимание правильного поведения (через тесты этапов 1-6)

Всё это упростит починку. Подход:

1. Запустить каждый тестовый класс, собрать ошибки
2. Для каждого упавшего теста — разобраться: тест неправильный или реализация
3. Если тест неправильный (устарел) — обновить под текущую правильную логику
4. Если реализация неправильная — исправить реализацию
5. Прогнать все тесты — всё зелёное → отчёт

## Этап 8: Рефакторинг Leverage/Margin и UnrealizedPnL в эмуляторе

Источник: `Algo.Testing/REFACTORING_PLAN.md`

### 8.1. Leverage / MarginCall / StopOut: из MarketEmulatorSettings в IPortfolio

**Проблема:** `Leverage`, `MarginCallLevel`, `StopOutLevel`, `EnableStopOut` — глобальные настройки в `MarketEmulatorSettings`. В реальности это свойства конкретного портфеля: разные счета могут иметь разное плечо и маржинальные требования.

**Что сделать:**

1. В `IPortfolio` (`IPortfolioManager.cs`) добавить:
   - `Leverage` (default 1)
   - `MarginCallLevel` (default 0.5)
   - `StopOutLevel` (default 0.2)
   - `EnableStopOut` (default false)

2. В `EmulatedPortfolio` (`EmulatedPortfolioManager.cs`) реализовать эти поля. `ValidateFunds` должен учитывать Leverage: `needMoney = price * volume / Leverage`

3. Из `MarketEmulatorSettings` удалить эти свойства и их Save/Load

4. В `MarketEmulator` логику margin call / stop-out перевести на `portfolio.MarginCallLevel` / `portfolio.StopOutLevel`

### 8.2. CalculateUnrealizedPnL: из IPortfolio в MarketEmulator

**Проблема:** `IPortfolio.CalculateUnrealizedPnL(Func<SecurityId, decimal?>)` принимает делегат для получения цены. Портфель — state container (деньги, позиции), он не должен знать про рыночные цены. Делегат из MarketEmulator — обход инкапсуляции.

**Что сделать:**

1. Удалить `CalculateUnrealizedPnL` из `IPortfolio` и `EmulatedPortfolio`
2. Добавить приватный метод `CalculateUnrealizedPnL(IPortfolio)` в `MarketEmulator`, который сам берёт цены из `_securityEmulators`
3. В `AddPortfolioUpdate` заменить `portfolio.CalculateUnrealizedPnL(...)` на `CalculateUnrealizedPnL(portfolio)`

---

## Этап 9: Выделение механизма маржи и плеча в тестируемый компонент

После рефакторинга этапа 8 — логика margin call / stop-out / leverage будет в `MarketEmulator`, размазанная по методам обновления портфеля. Нужно вычленить её в отдельный механизм, который можно покрыть тестами изолированно.

### Что сделать

1. **Выделить интерфейс `IMarginController`** (или `IMarginValidator`) с чёткой ответственностью:
   - `ValidateOrder(order, portfolio)` → можно ли выставить ордер с учётом плеча и доступных средств
   - `CheckMarginLevel(portfolio, unrealizedPnL)` → текущий уровень маржи
   - `IsMarginCall(portfolio, unrealizedPnL)` → bool
   - `IsStopOut(portfolio, unrealizedPnL)` → bool
   - `GetRequiredMargin(price, volume, leverage)` → decimal

2. **Реализовать `MarginController`** — вынести сюда всю логику из `MarketEmulator`:
   - Расчёт необходимой маржи с учётом плеча
   - Проверка уровней margin call и stop-out
   - Генерация margin call / stop-out сообщений
   - Чистая логика, без зависимости от эмулятора — принимает данные, возвращает результат

3. **`MarketEmulator`** использует `IMarginController` вместо inline-логики

4. **НЕ делать** как сейчас с делегатом — никакого размывания ответственности. `MarginController` получает готовые данные (портфель, PnL, параметры ордера), возвращает решение. Без callback-ов и Func<>.

### Тесты

Покрыть `MarginController` юнит-тестами:

| Сценарий | Что проверяем |
|---|---|
| Ордер без плеча, достаточно средств | `ValidateOrder` → OK |
| Ордер без плеча, недостаточно средств | `ValidateOrder` → rejected |
| Ордер с плечом 10x, достаточно средств | требуемая маржа = price * vol / 10 |
| Margin call level достигнут | `IsMarginCall` → true |
| Margin call level не достигнут | `IsMarginCall` → false |
| Stop-out level достигнут, EnableStopOut=true | `IsStopOut` → true |
| Stop-out level достигнут, EnableStopOut=false | `IsStopOut` → false |
| Портфель с несколькими позициями | правильный расчёт unrealizedPnL влияет на уровень маржи |
| Leverage=1 (без плеча) | `GetRequiredMargin` = price * volume |
| Leverage=100 | `GetRequiredMargin` = price * volume / 100 |

## Этап 10: Выделение MarketEmulatorAdapter из MarketEmulator

### Проблема

`MarketEmulator` реализует `IMarketEmulator` (который наследует `IMessageAdapter`). Из-за этого в классе ~40 заглушек explicit interface implementation: свойства возвращающие `default`/`false`/`null`/`[]`, сеттеры кидающие `NotSupportedException`, событие `NewOutMessageAsync` с throwing accessors. Всё это шелуха, не имеющая отношения к логике эмуляции, но загрязняющая класс.

### Текущее состояние (строки 1127-1215 в MarketEmulator.cs)

Заглушки `IMessageAdapter`:

| Категория | Кол-во | Примеры |
|---|---|---|
| Свойства возвращающие default/false/null/[] | ~25 | `HeartbeatInterval`, `StorageName`, `IsNativeIdentifiers`, `Icon`, `Categories`, ... |
| Свойства с throwing setter | 2 | `MaxParallelMessages`, `FaultDelay` |
| Событие с throwing accessor | 1 | `NewOutMessageAsync` |
| Метод кидающий NotSupported | 1 | `SupportedOrderBookDepths` |
| Тривиальные методы | 3 | `Clone()`, `SendOutMessage()`, `CreateOrderLogMarketDepthBuilder()` |
| Списки поддерживаемых сообщений | 2 | `PossibleSupportedMessages`, `SupportedInMessages` |

### Что сделать

1. **Создать `MarketEmulatorAdapter`** — наследник `MessageAdapter` (или реализация `IMessageAdapterWrapper`):
   - Содержит внутри `MarketEmulator` как поле
   - Все методы `IMessageAdapter` реализуются нормально через базовый `MessageAdapter` (без заглушек)
   - `SendInMessageAsync` → делегирует в `MarketEmulator.SendInMessageAsync`
   - `NewOutMessage` от эмулятора → пробрасывает наружу через адаптерный `SendOutMessage`
   - `PossibleSupportedMessages` / `SupportedInMessages` — берёт из эмулятора или определяет сам

2. **`MarketEmulator`** — убрать реализацию `IMessageAdapter`:
   - Убрать все explicit interface implementations (строки 1127-1215)
   - Оставить чистый API эмуляции: `SendInMessageAsync`, `NewOutMessage` (sync event), настройки
   - Класс перестаёт реализовывать `IMarketEmulator` (или `IMarketEmulator` упрощается, убирая наследование от `IMessageAdapter`)

3. **Обновить места использования** — везде где `MarketEmulator` использовался как `IMessageAdapter`, заменить на `MarketEmulatorAdapter`:
   - `EmulationMessageAdapter.cs` — основной потребитель
   - Тесты: `MarketEmulatorTests.cs`, `MarginTradingTests.cs`, `MarketEmulatorComparisonTests.cs`, `EmulatorChannelIntegrationTests.cs`, `MarketEmulatorFeatureTests.cs`, `RiskTests.cs`, `BacktestingTests.cs`, `HistoryMarketDataManagerTests.cs`
   - Сэмплы: `Samples/07_Testing/04_HistoryConsole/Program.cs`

4. **Интерфейс `IMarketEmulator`** — упростить:
   - Убрать наследование от `IMessageAdapter`
   - Оставить только то, что относится к эмуляции: `Settings`, `SendInMessageAsync`, `NewOutMessage`, и т.д.
   - Либо вообще убрать интерфейс если он не нужен для мокирования

### Результат

- `MarketEmulator` — чистая логика эмуляции без адаптерной шелухи
- `MarketEmulatorAdapter` — тонкая обёртка, адаптирующая эмулятор к `IMessageAdapter`
- Можно тестировать эмулятор напрямую, без адаптерного контракта
- Можно тестировать адаптер отдельно, подставляя мок эмулятора

## Этап 11: Починка тестов PrivateTests (StockSharpApps)

Проект: `C:\StockSharp\StockSharpApps\Tests\PrivateTests\PrivateTests.csproj`

Заставить работать все тесты в следующих папках. Порядок по приоритету (если что-то сложно — двигаться дальше, вернуться потом):

### 11.1. FixServer (33 файла) — высший приоритет

| Группа | Файлы |
|---|---|
| Сериализация | `FixServerSerializerTests.cs`, `FixServerSerializerLogicTests.cs`, `FixServerFormatCompatibilityTests.cs`, `MockFixServerSerializerTests.cs` |
| Конвертация сообщений | `FixMessageConverterTests.cs` + `.Security`, `.User`, `.Session`, `.Other`, `.Transaction`, `.Position` |
| Совместимость диалектов | `DialectSerializerCompatibilityTests.cs` + `ToServer.*` (6 файлов) + `ToClient.*` (6 файлов) |
| Протокол и типы | `FixProtocolTests.cs`, `FixDataTypeRoundTripTests.cs` |
| Сервер | `FixServerConstructorTests.cs`, `FixServerAuthorizationTests.cs`, `FixServerLiveDataTests.cs`, `FixServerConnectorTests.cs` |
| Моки | `Mocks/TestAuthorization.cs`, `MockFixServerSerializer.cs`, `MockFixWriter.cs`, `MockFixReader.cs` |

### 11.2. Database (5 файлов)

- `DatabaseTestBase.cs` — базовый класс
- `SessionStorageProviderIntegrationTests.cs`
- `PositionStorageIntegrationTests.cs`
- `TransactionStorageIntegrationTests.cs`
- `PositionPaginationBugTest.cs`

### 11.3. Server (12 файлов)

| Группа | Файлы |
|---|---|
| Хелперы | `ServerTestHelper.cs`, `BaseServerModuleTests.cs`, `ModuleOutputCollector.cs` |
| Модули | `FeederModuleTests.cs`, `FeederModuleStorageTests.cs`, `FeederModuleFixServerIntegrationTests.cs`, `DataModuleTests.cs`, `HistoryModuleTests.cs`, `MatcherModuleTests.cs`, `OrdersForwardingModuleTests.cs` |
| Инфра | `ServerModuleProviderTests.cs`, `ConnectorResolverTests.cs` |

### 11.4. Udp (13 файлов)

| Группа | Файлы |
|---|---|
| Адаптер и парсинг | `UdpMessageAdapterTests.cs`, `UdpSettingsParserTests.cs`, `ReplayParserTests.cs` |
| Сервисы | `UdpDumperServiceTests.cs`, `UdpFeedServiceTests.cs`, `UdpFeedTests.cs`, `WorkerTests.cs` |
| Интеграция | `UdpIntegrationTests.cs`, `PcapPacketSourceTests.cs` |
| Моки | `Mocks/MockPacketProcessor.cs`, `MemoryPacketSource.cs`, `MockUdpSocket.cs`, `PacketSourceUdpSocket.cs` |

### 11.5. Hydra (12 файлов) — низший приоритет

| Группа | Файлы |
|---|---|
| Сервисы загрузки | `Services/Level1LoadServiceTests.cs`, `OrderLogLoadServiceTests.cs`, `PositionChangeLoadServiceTests.cs`, `TransactionLoadServiceTests.cs`, `NewsLoadServiceTests.cs`, `DepthLoadServiceTests.cs`, `TradeLoadServiceTests.cs`, `CandleLoadServiceTests.cs`, `ExportServiceTests.cs` |
| Хранилища | `Storages/HydraSecurityStorageTests.cs`, `AnalyticsStorageTests.cs`, `HydraTaskStorageTests.cs` |

### Подход

1. Запустить `dotnet test` по каждой группе (или `--filter`)
2. Собрать ошибки компиляции и runtime
3. Для каждой ошибки — разобраться: тест устарел или реализация сломана
4. Исправить (приоритет — реализация, не тест)
5. После каждой группы — отчёт

---

# Отчёты по выполнению

## Этап 1: Тесты правильного поведения Basket

**Статус:** завершён

**Файл:** `Tests/BasketMessageAdapterRoutingTests.cs` — 21 тест

**Результаты (14 passed, 7 failed — подтверждают баги):**

### Замечание 1: Единый путь MarketData (3 бага подтверждены)
| Тест | Результат | Пояснение |
|------|-----------|-----------|
| `Remark1_NewsSubscription_UsesParentChildMap` | PASSED | Baseline: News уже использует ToChild/ParentChildMap |
| `Remark1_TicksSubscription_UsesParentChildMap` | **FAILED** | Ticks не записывает маппинг в ParentChildMap |
| `Remark1_Level1Subscription_UsesParentChildMap` | **FAILED** | Level1 не записывает маппинг в ParentChildMap |
| `Remark1_MarketDepthSubscription_UsesParentChildMap` | **FAILED** | MarketDepth не записывает маппинг в ParentChildMap |
| `Remark1_TicksResponse_RemapsChildToParentId` | PASSED | Remapping уже работает (через ApplyParentLookupId) |

### Замечание 2: IsAllDownloading фильтрация (4 бага подтверждены)
| Тест | Результат | Пояснение |
|------|-----------|-----------|
| `Remark2_SecurityLookup_AllDownloadingSupported_Delivered` | PASSED | Baseline |
| `Remark2_SecurityLookup_AllDownloadingNotSupported_Filtered` | PASSED | Baseline |
| `Remark2_SecurityLookup_SpecificSecurity_Delivered` | PASSED | Baseline |
| `Remark2_OrderStatus_AllDownloadingSupported_Delivered` | PASSED | Доставка работает |
| `Remark2_OrderStatus_AllDownloadingNotSupported_Filtered` | **FAILED** | Нет фильтра IsAllDownloading для OrderStatus |
| `Remark2_OrderStatus_SpecificSecurity_Delivered` | PASSED | Конкретный Security — не фильтруется |
| `Remark2_OrderStatus_TwoAdapters_OnlySupporting_Receives` | **FAILED** | Оба адаптера получают (должен только поддерживающий) |
| `Remark2_OrderStatus_TwoAdapters_BothSupporting_BothReceive` | PASSED | Оба поддерживают — оба получают |
| `Remark2_PortfolioLookup_AllDownloadingSupported_Delivered` | PASSED | Доставка работает |
| `Remark2_PortfolioLookup_AllDownloadingNotSupported_Filtered` | **FAILED** | Нет фильтра IsAllDownloading для PortfolioLookup |
| `Remark2_PortfolioLookup_SpecificPortfolio_Delivered` | PASSED | Конкретный Portfolio — не фильтруется |
| `Remark2_PortfolioLookup_TwoAdapters_OnlySupporting_Receives` | **FAILED** | Оба адаптера получают (должен только поддерживающий) |

### Замечание 3: NotSupported retry (0 багов — всё работает)
| Тест | Результат | Пояснение |
|------|-----------|-----------|
| `Remark3_MarketData_NotSupported_RetryWorks` | PASSED | Retry работает через loopback |
| `Remark3_SecurityLookup_NotSupported_RetriesToNextAdapter` | PASSED | Retry работает |
| `Remark3_OrderStatus_NotSupported_DoesNotLoop` | PASSED | Не зацикливается |
| `Remark3_PortfolioLookup_NotSupported_DoesNotLoop` | PASSED | Не зацикливается |

**Примечание:** Тест-хелпер обновлён для обработки loopback-сообщений (как делает Connector).

## Этап 2: Фиксы замечаний Basket

**Статус:** не начато

## Этап 3: Выделение роутера

**Статус:** не начато

## Этап 4: Обновление тестов Basket + роутер

**Статус:** не начато

## Этап 5: Менеджеры с мок-состояниями

**Статус:** не начато

## Этап 6: Extensions — Tier 1 (методы 1-12)

**Статус:** не начато

## Этап 6: Extensions — Tier 2 (методы 13-24)

**Статус:** не начато

## Этап 6: Extensions — Tier 3 (методы 25-30)

**Статус:** не начато

## Этап 7: Починка сломанных тестов

**Статус:** не начато

**Тесты (11 файлов):**
- `AsyncExtensionsTests.cs`
- `ConnectorRoutingTests.cs`
- `RemoteStorageClientTests.cs`
- `SubscriptionHolderTests.cs`
- `SubscriptionManagerConnectorTests.cs`
- `SubscriptionMessageAdapterTests.cs`
- `SubscriptionOnlineMessageAdapterTests.cs`
- `SubscriptionDataFeedTests.cs`
- `SubscriptionManagerTests.cs`
- `SubscriptionOnlineManagerTests.cs`
- `SubscriptionRoutingStateTests.cs`

## Этап 8: Рефакторинг Leverage/Margin и UnrealizedPnL

**Статус:** не начато

## Этап 9: Выделение MarginController + тесты

**Статус:** не начато

## Этап 10: Выделение MarketEmulatorAdapter

**Статус:** не начато

## Этап 11: Починка PrivateTests

**Статус:** не начато

**Группы (по приоритету):**
- FixServer (33 файла)
- Database (5 файлов)
- Server (12 файлов)
- Udp (13 файлов)
- Hydra (12 файлов)
