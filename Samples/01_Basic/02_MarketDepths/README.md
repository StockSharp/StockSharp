# How to Work with Market Data in StockSharp

## Overview

This guide explains how to handle market data using the StockSharp trading framework in a .NET application. The application subscribes to market data updates for selected securities and displays this information in the UI.

## Key Components

1. **Market Depths**: [Market depth](https://doc.stocksharp.com/topics/api/order_books.html) data represents the bids and offers at various price levels for a particular security.
2. **Trades**: [Real-time](https://doc.stocksharp.com/topics/api/market_data/subscriptions.html) trade data shows actual trades that have occurred.

## Setup and Event Handlers

### Installation

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Find and add required connector via [NuGet package](https://doc.stocksharp.com/topics/api/setup.html#private-nuget-server)
4. Build and run the application.

### Initialize and Connect

First, the application initializes the connector and sets up event handlers for receiving market data:

```csharp
public MainWindow()
{
    InitializeComponent();
    // Register all connectors and load settings if config file exists
    if (File.Exists(_connectorFile))
    {
        _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
    }
}

private void Connect_Click(object sender, RoutedEventArgs e)
{
    SecurityPicker.SecurityProvider = _connector;
    SecurityPicker.MarketDataProvider = _connector;

    _connector.TickTradeReceived += ConnectorOnTickTradeReceived;
    _connector.OrderBookReceived += ConnectorOnMarketDepthReceived;

    _connector.Connect();
}
```

### Handling Market Depth Data

When the market depth data is received, the application updates the [UI component](https://doc.stocksharp.com/topics/api/graphical_user_interface/market_data/order_book.html) showing the market depth:

```csharp
private void ConnectorOnMarketDepthReceived(Subscription sub, IOrderBookMessage depth)
{
    if (depth.SecurityId == _selectedSecurityId)
        MarketDepthControl.UpdateDepth(depth);
}
```

### Handling Trade Data

When trade data is received, it is added to a [grid](https://doc.stocksharp.com/topics/api/graphical_user_interface/market_data/ticks.html) that displays trades:

```csharp
private void ConnectorOnTickTradeReceived(Subscription sub, ITickTradeMessage trade)
{
    if (trade.SecurityId == _selectedSecurityId)
        TradeGrid.Trades.Add(trade);
}
```

## Subscribing to Market Data

When a security is selected, the application [subscribes](https://doc.stocksharp.com/topics/api/market_data/subscriptions.html) to level 1 data, trades, and market depth for that security:

```csharp
private void SecurityPicker_SecuritySelected(Security security)
{
    UnsubscribeAll();
    _selectedSecurityId = security?.ToSecurityId();

    if (_selectedSecurityId == null)
        return;

    void subscribe(DataType dt)
    {
        var sub = new Subscription(dt, security);
        _subscriptions.Add(sub);
        _connector.Subscribe(sub);
    }

    subscribe(DataType.Level1);
    subscribe(DataType.Ticks);
    subscribe(DataType.MarketDepth);
}
```

## Unsubscribing

It is important to manage subscriptions properly by unsubscribing from old data when no longer needed:

```csharp
private void UnsubscribeAll()
{
    foreach (var sub in _subscriptions)
        _connector.UnSubscribe(sub);
    _subscriptions.Clear();
}
```

## Conclusion

This setup enables the application to handle real-time market data effectively, providing insights into market dynamics and trade execution for selected securities. The code snippets provided illustrate how to integrate and manage market data within a trading application using the StockSharp framework.