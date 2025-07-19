# StockSharp.Alerts.Interfaces

## Overview

`StockSharp.Alerts.Interfaces` provides the common interfaces and data types required to implement alert systems within the [StockSharp](https://stocksharp.com) trading platform. The package defines a standard way for libraries or applications to create, configure, and deliver notifications when specific trading conditions occur.

This project only contains contracts and helper classes. Actual implementations of notification channels or alert processing engines are expected to be provided by other StockSharp components or by third‑party libraries.

## Key Components

The library exposes several core types that can be used to build a fully featured alert system:

- **`AlertNotifications`** – an enumeration describing possible notification channels such as sound, desktop popups, log files and Telegram.
- **`AlertServicesRegistry`** – static helpers that retrieve alert services from the current dependency injection container.
- **`IAlertNotificationService` / `IDesktopPopupService`** – interfaces used to send alerts through various channels.
- **`IAlertProcessingService`** – the main contract for managing alert schemas and evaluating incoming messages.
- **`AlertRuleField`, `AlertRule` and `AlertSchema`** – classes used to describe alert conditions and related metadata.

These components are designed to work with StockSharp message types (`ExecutionMessage`, `QuoteChangeMessage`, etc.) and rely on other StockSharp assemblies such as `StockSharp.BusinessEntities` and `StockSharp.Messages`.

## Installation

Alternatively, reference `StockSharp.Alerts.Interfaces` from your own project by adding the corresponding project or NuGet package reference.

## Usage Example

Below is a simplified example illustrating how to create an alert schema and register it with an alert processing service:

```csharp
using StockSharp.Alerts;
using StockSharp.Messages;

var schema = new AlertSchema(typeof(ExecutionMessage))
{
    AlertType = AlertNotifications.Telegram,
    Caption   = "Large trade",
    Message   = "A trade larger than expected has appeared",
};

schema.Rules.Add(new AlertRule
{
    Field    = new AlertRuleField(typeof(ExecutionMessage).GetProperty(nameof(ExecutionMessage.TradePrice))),
    Operator = ComparisonOperator.Greater,
    Value    = 1000m,
});

AlertServicesRegistry.ProcessingService.Register(schema);
```

When the `IAlertProcessingService` receives an `ExecutionMessage` that satisfies the rule, the configured notification service will deliver a message to the selected channel.


