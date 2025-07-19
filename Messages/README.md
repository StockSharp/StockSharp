# StockSharp Messages Library

The **Messages** project contains the essential message definitions shared across the StockSharp (S#) trading framework. Messages are the contracts used by connectors, adapters and other services for all interactions such as connecting to brokers, requesting market data or placing orders.

## Overview

- **Message Types Enumeration** – [`MessageTypes`](MessageTypes.cs) lists every supported message kind.
- **Base Classes** – classes like [`Message`](Message.cs), [`BaseConnectionMessage`](BaseConnectionMessage.cs) and [`BaseSubscriptionMessage`](BaseSubscriptionMessage.cs) provide common fields and behaviour.
- **Asynchronous Processing** – [`AsyncMessageAdapter`](AsyncMessageAdapter.cs) and [`AsyncMessageProcessor`](AsyncMessageProcessor.cs) handle messages asynchronously.
- **Utility Helpers** – classes such as [`ReConnectionSettings`](ReConnectionSettings.cs) control reconnection and subscription options.

### Core Messages

The library defines messages for all major trading activities:

- **Connection** – [`ConnectMessage`](ConnectMessage.cs), [`DisconnectMessage`](DisconnectMessage.cs), [`ConnectionLostMessage`](ConnectionLostMessage.cs) and [`ConnectionRestoredMessage`](ConnectionRestoredMessage.cs).
- **Security Information** – [`SecurityMessage`](SecurityMessage.cs), [`BoardMessage`](BoardMessage.cs) and [`BoardStateMessage`](BoardStateMessage.cs).
- **Market Data** – [`MarketDataMessage`](MarketDataMessage.cs), [`Level1ChangeMessage`](Level1ChangeMessage.cs), [`QuoteChangeMessage`](QuoteChangeMessage.cs), [`TimeFrameCandleMessage`](CandleMessage.cs) and related candle messages.
- **Orders and Trades** – [`OrderRegisterMessage`](OrderRegisterMessage.cs), [`OrderReplaceMessage`](OrderReplaceMessage.cs), [`OrderCancelMessage`](OrderCancelMessage.cs), [`OrderGroupCancelMessage`](OrderGroupCancelMessage.cs), [`OrderStatusMessage`](OrderStatusMessage.cs) and [`ExecutionMessage`](ExecutionMessage.cs).
- **Portfolio and Positions** – [`PortfolioMessage`](PortfolioMessage.cs), [`PortfolioLookupMessage`](PortfolioLookupMessage.cs) and [`PositionChangeMessage`](PositionChangeMessage.cs).
- **Service Messages** – [`TimeMessage`](TimeMessage.cs), [`NewsMessage`](NewsMessage.cs), [`ErrorMessage`](ErrorMessage.cs) and others.

The project targets **.NET Standard 2.0** and **.NET 6.0** for cross-platform compatibility.

## Usage Example

Below is a minimal example of creating a market data subscription message:

```csharp
using StockSharp.Messages;

var mdMessage = new MarketDataMessage
{
    DataType2 = DataType.Ticks,
    IsSubscribe = true,
    TransactionId = 1,
    SecurityId = new SecurityId
    {
        SecurityCode = "AAPL",
        BoardCode = "NASDAQ"
    }
};
```

Such messages are passed to an adapter, which forwards them to the broker or data provider.

## Documentation

Further details on messages and architecture can be found in the [StockSharp documentation](https://doc.stocksharp.com/topics/api/messages.html).

