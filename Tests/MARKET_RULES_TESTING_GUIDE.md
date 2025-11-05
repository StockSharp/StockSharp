# 📋 Руководство по тестированию Market Rules

## Обзор

**Market Rules** - это реактивная система для обработки рыночных событий в StockSharp. Она позволяет создавать правила вида "**когда** произойдет событие X, **выполни** действие Y".

### Архитектура

```
┌─────────────────┐
│  IMarketRule    │  ← Интерфейс правила
└────────┬────────┘
         │
         ↓
┌─────────────────────────┐
│ MarketRule<TToken, TArg>│  ← Базовый класс
└────────┬────────────────┘
         │
         ├→ ConnectedRule         (WhenConnected)
         ├→ DisconnectedRule      (WhenDisconnected)
         ├→ OrderRule             (WhenRegistered, WhenCanceled, etc.)
         ├→ CandleRule            (WhenNewCandle)
         └→ ... другие правила
```

### Основные компоненты

1. **`IMarketRule`** - интерфейс правила
2. **`MarketRule<TToken, TArg>`** - базовый класс для всех правил
3. **`IMarketRuleContainer`** - контейнер для управления правилами
4. **`MarketRuleHelper`** - extension методы для создания правил

### Жизненный цикл правила

```
1. Создание      → connector.WhenConnected()
2. Настройка     → .Do(action)
3. Активация     → .Apply(container)
4. Событие       → событие происходит
5. Выполнение    → action выполняется
6. Завершение    → правило удаляется (или продолжает работу)
```

---

## 🎯 Стратегия тестирования

### Уровень 1: Базовая функциональность (MarketRuleTests.cs)

#### ✅ Что тестировать:

1. **Создание и начальное состояние**
   - Правило создается с корректным Token
   - IsSuspended = false
   - IsActive = false
   - IsReady = false (до добавления в контейнер)

2. **Активация правила (Apply)**
   - Правило становится Ready после Apply()
   - Container устанавливается корректно
   - Нельзя добавить в два контейнера

3. **Выполнение действий (Do)**
   - `Do(Action)` - выполняется без параметров
   - `Do(Action<TArg>)` - получает аргумент при активации
   - `Do(Func<TArg, TResult>)` - возвращает результат
   - `Activated<T>()` - получает результат от Do()

4. **Приостановка (Suspend/Resume)**
   - IsSuspended = true → действие НЕ выполняется
   - IsSuspended = false → действие выполняется

5. **Периодичность (Until)**
   - Правило удаляется когда canFinish возвращает true
   - Правило продолжает работать пока canFinish возвращает false
   - One-time правила удаляются после первого выполнения

6. **Взаимоисключающие правила (ExclusiveRules)**
   - При активации правила удаляются exclusive правила
   - Используется для "либо-либо" сценариев

7. **Dispose**
   - После Dispose правило IsReady = false
   - Dispose очищает Container

8. **Container функциональность**
   - SuspendRules() / ResumeRules()
   - IsRulesSuspended
   - Rules коллекция

---

### Уровень 2: Конкретные правила (MarketRuleHelperTests.cs)

#### ✅ Connector Rules

**WhenConnected**
```csharp
[TestMethod]
public void WhenConnected_TriggersOnConnection()
{
    var connector = new Connector(...);
    var triggered = false;

    connector
        .WhenConnected()
        .Do(adapter => triggered = true)
        .Apply(connector);

    connector.Connect();

    triggered.AssertTrue();
}
```

**WhenDisconnected**
- Триггерится при отключении
- Получает IMessageAdapter в аргументе

**WhenConnectionLost**
- Триггерится при ошибке подключения
- Получает Tuple<IMessageAdapter, Exception>

#### ✅ Order Rules

**WhenRegistered**
```csharp
order
    .WhenRegistered(connector)
    .Do(o => { /* order registered */ })
    .Apply(connector);
```

**WhenCanceled**
- Триггерится при отмене заявки

**WhenMatched**
- Триггерится при полном исполнении

**WhenChanged**
- Триггерится при любом изменении заявки
- Обычно периодическое (Until(() => false))

**WhenPartiallyMatched**
- Триггерится при частичном исполнении

#### ✅ Subscription Rules

**WhenNewTrade**
```csharp
subscription
    .WhenNewTrade(connector)
    .Do(trade => { /* new trade received */ })
    .Apply(connector);
```

**WhenNewCandle**
- Триггерится при получении новой свечи

**WhenStopped**
- Триггерится при остановке подписки

**WhenFailed**
- Триггерится при ошибке подписки

#### ✅ Portfolio/Position Rules

**Portfolio.WhenChanged**
```csharp
portfolio
    .WhenChanged(connector)
    .Do(pf => { /* portfolio updated */ })
    .Apply(connector);
```

**Position.WhenChanged**
- Триггерится при изменении позиции

#### ✅ Time Rules

**WhenIntervalElapsed**
```csharp
timeProvider
    .WhenIntervalElapsed(TimeSpan.FromMinutes(1))
    .Do(() => { /* every minute */ })
    .Apply();
```

**WhenTimeCome**
```csharp
timeProvider
    .WhenTimeCome(targetTime)
    .Do(() => { /* at specific time */ })
    .Apply();
```

#### ✅ Candle Rules

**WhenCandlesStarted**
- Триггерится при начале получения свечей

**WhenCandlesChanged**
- Триггерится при изменении свечи

**WhenCandleFinished**
- Триггерится при завершении свечи

---

### Уровень 3: Комбинированные правила

#### ✅ And Operator
```csharp
var rule1 = connector.WhenConnected();
var rule2 = security.WhenLastTradeChanged();

rule1.And(rule2)
    .Do(() => { /* both conditions met */ })
    .Apply(connector);
```

#### ✅ Or Operator
```csharp
var rule1 = order.WhenRegistered(connector);
var rule2 = order.WhenRegisterFailed(connector);

rule1.Or(rule2)
    .Do(() => { /* either registered or failed */ })
    .Apply(connector);
```

#### ✅ Exclusive Rules
```csharp
var connectedRule = connector.WhenConnected()
    .Do(() => { /* connected */ })
    .Apply(connector);

var errorRule = connector.WhenConnectionLost()
    .Do(() => { /* error */ })
    .Apply(connector);

// Mutual exclusion
connectedRule.ExclusiveRules.Add(errorRule);
errorRule.ExclusiveRules.Add(connectedRule);
```

---

## 🔧 Паттерны тестирования

### 1. Создание тестового правила

```csharp
private class TestRule : MarketRule<string, int>
{
    public TestRule(string token) : base(token) { }

    public void TriggerActivate(int value)
    {
        Activate(value);
    }
}
```

### 2. Проверка выполнения действия

```csharp
var executed = false;
var receivedValue = 0;

var rule = new TestRule("token")
    .Do((int value) =>
    {
        executed = true;
        receivedValue = value;
    })
    .Apply();

rule.TriggerActivate(42);

executed.AssertTrue();
receivedValue.AssertEqual(42);
```

### 3. Тестирование периодичности

```csharp
var count = 0;
var maxExecutions = 3;

var rule = new TestRule("token")
    .Do(() => count++)
    .Until(() => count >= maxExecutions)
    .Apply();

for (int i = 0; i < 10; i++)
{
    if (rule.IsReady)
        rule.TriggerActivate(i);
}

count.AssertEqual(maxExecutions); // Остановилось на 3
```

### 4. Тестирование с реальным Connector

```csharp
using var connector = new Connector(new InMemoryMessageAdapter(new IdGenerator()));
connector.Connect();

var triggered = false;

connector
    .WhenConnected()
    .Do(() => triggered = true)
    .Apply(connector);

Thread.Sleep(200); // Ждем async обработки

triggered.AssertTrue();
```

---

## 🐛 Типичные проблемы и решения

### Проблема 1: Правило не срабатывает

**Причины:**
- Правило не добавлено в контейнер (забыли вызвать `.Apply()`)
- Правило приостановлено (IsSuspended = true)
- Container остановлен (ProcessState != Started)
- Правило уже удалено

**Решение:**
```csharp
rule.IsReady.AssertTrue();         // Проверить готовность
rule.IsSuspended.AssertFalse();    // Проверить приостановку
rule.Container.AssertNotNull();    // Проверить контейнер
```

### Проблема 2: Правило срабатывает несколько раз

**Причина:**
- Забыли настроить `Until()` для one-time правила

**Решение:**
```csharp
// Для однократного выполнения используйте default Until
rule.Do(() => { ... }).Apply();

// Для периодического явно укажите
rule.Do(() => { ... })
    .Until(() => false)  // Никогда не завершается
    .Apply();
```

### Проблема 3: Async проблемы в тестах

**Причина:**
- События обрабатываются асинхронно

**Решение:**
```csharp
connector.Connect();
Thread.Sleep(200);  // Дать время на обработку

// Или использовать SpinWait
var sw = new SpinWait();
while (!triggered && sw.Count < 1000)
{
    sw.SpinOnce();
}
```

### Проблема 4: ExclusiveRules не работают

**Причина:**
- Удаление происходит асинхронно

**Решение:**
```csharp
mainRule.TriggerActivate(1);
Thread.Sleep(100);  // Дать время на удаление

container.Rules.Contains(exclusiveRule).AssertFalse();
```

---

## 📊 Покрытие тестами

### Текущее состояние:

| Компонент | Файлы | Покрытие |
|-----------|-------|----------|
| IMarketRule базовые | `MarketRuleTests.cs` | ✅ Создано |
| MarketRuleHelper (Connector) | `MarketRuleHelperTests.cs` | ✅ Создано |
| MarketRuleHelper (Orders) | `MarketRuleHelperTests.cs` | ✅ Создано |
| MarketRuleHelper (Candles) | - | ⚠️ Частично |
| MarketRuleHelper (Time) | - | ⚠️ Частично |
| MarketRuleHelper (Subscription) | `MarketRuleHelperTests.cs` | ✅ Создано |
| And/Or операторы | - | ❌ Требуется |
| Complex rules | - | ❌ Требуется |

### Что еще нужно:

1. **MarketRuleHelper_Candle.cs**
   - WhenCandlesStarted
   - WhenCandlesChanged
   - WhenCandleFinished
   - WhenCurrentCandleChanged

2. **MarketRuleHelper_Time.cs**
   - WhenIntervalElapsed (детальное тестирование)
   - WhenTimeCome (детальное тестирование)

3. **MarketRuleHelper_Security.cs**
   - WhenLastTradeChanged
   - WhenBestBidChanged
   - WhenBestAskChanged
   - WhenLevel1Changed

4. **Composite Rules**
   - And() оператор
   - Or() оператор
   - Plus() оператор

5. **Integration Tests**
   - Комплексные сценарии с несколькими правилами
   - Performance тесты
   - Thread-safety тесты

---

## 🚀 Запуск тестов

```bash
# Все тесты MarketRule
dotnet test --filter "FullyQualifiedName~MarketRule"

# Только базовые тесты
dotnet test --filter "FullyQualifiedName~MarketRuleTests"

# Только helper тесты
dotnet test --filter "FullyQualifiedName~MarketRuleHelperTests"
```

---

## 📚 Дополнительные ресурсы

- **IMarketRule.cs** - интерфейс и базовый класс
- **MarketRuleHelper.cs** - Connector/Transaction правила
- **MarketRuleHelper_Order.cs** - Order правила
- **MarketRuleHelper_Candle.cs** - Candle правила
- **MarketRuleHelper_Time.cs** - Time правила
- **MarketRuleHelper_Security.cs** - Security правила
- **MarketRuleHelper_Position.cs** - Portfolio/Position правила
- **MarketRuleHelper_Subscription.cs** - Subscription правила

---

## ✅ Чеклист для нового правила

При добавлении нового правила в MarketRuleHelper:

- [ ] Создать private класс, наследующий MarketRule<TToken, TArg>
- [ ] Подписаться на событие в конструкторе
- [ ] Отписаться в DisposeManaged()
- [ ] Создать public extension метод
- [ ] Написать unit-тест для правила:
  - [ ] Проверить, что правило создается
  - [ ] Проверить, что Do() выполняется при событии
  - [ ] Проверить, что правильный аргумент передается
  - [ ] Проверить Dispose (отписка от событий)
- [ ] Добавить integration test с реальным Connector
- [ ] Обновить документацию

---

**Примечание:** Эти тесты используют InMemoryMessageAdapter для упрощения. Для более детального тестирования используйте MarketEmulator или mock адаптеры.
