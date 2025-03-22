# Handling Orders in a StockSharp Trading Application

## Overview

This section of the documentation explains how to manage orders in a .NET application using the StockSharp trading framework. The code facilitates operations like buying and selling securities by interacting with the trading service.

## Key Components

1. **Order Handling**: Submitting buy and sell [orders](https://doc.stocksharp.com/topics/api/orders_management.html).
2. **UI Components**: `SecurityEditor` and `PortfolioEditor` for selecting securities and portfolios.
3. **Order Feedback**: Handling responses for [new orders and failed orders](https://doc.stocksharp.com/topics/api/orders_management/orders_states.html).

## Code Explanation and Usage

### Installation

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Find and add required connector via [NuGet package](https://doc.stocksharp.com/topics/api/setup.html#private-nuget-server)
4. Build and run the application.

### Initialization and Connection Setup

The constructor initializes the application and [loads existing settings](https://doc.stocksharp.com/topics/api/connectors/save_and_load_settings.html). The `Connect_Click` method sets up necessary bindings and event handlers for order operations:

```csharp
public MainWindow()
{
    InitializeComponent();
    if (File.Exists(_connectorFile))
    {
        _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
    }
}

private void Connect_Click(object sender, RoutedEventArgs e)
{
    SecurityEditor.SecurityProvider = _connector;
    PortfolioEditor.Portfolios = new PortfolioDataSource(_connector);

    _connector.NewOrder += OrderGrid.Orders.Add;
    _connector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;
    _connector.NewMyTrade += MyTradeGrid.Trades.Add;

    _connector.Connect();
}
```

### Submitting Orders

The application provides methods to submit buy and sell [orders](https://doc.stocksharp.com/topics/api/orders_management.html). These methods are triggered by UI events (e.g., button clicks) and use the selected security and portfolio details to create and register orders with the trading service:

```csharp
private void Buy_Click(object sender, RoutedEventArgs e)
{
    var order = new Order
    {
        Security = SecurityEditor.SelectedSecurity,
        Portfolio = PortfolioEditor.SelectedPortfolio,
        Price = decimal.Parse(TextBoxPrice.Text),
        Volume = 1,  // Example volume
        Side = Sides.Buy,
    };

    _connector.RegisterOrder(order);
}

private void Sell_Click(object sender, RoutedEventArgs e)
{
    var order = new Order
    {
        Security = SecurityEditor.SelectedSecurity,
        Portfolio = PortfolioEditor.SelectedPortfolio,
        Price = decimal.Parse(TextBoxPrice.Text),
        Volume = 1,  // Example volume
        Side = Sides.Sell,
    };

    _connector.RegisterOrder(order);
}
```

## Handling Order Responses

The application listens for new orders and failed order registration events to update the [UI](https://doc.stocksharp.com/topics/api/graphical_user_interface/trading/orders.html) appropriately:

```csharp
_connector.NewOrder += OrderGrid.Orders.Add;
_connector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;
```

## Conclusion

This setup allows users to interactively manage trading orders [through a UI](https://doc.stocksharp.com/topics/api/graphical_user_interface/trading/orders.html), offering capabilities to buy and sell securities using configured trading services. This guide should help users understand the order handling process within the application and provide clear instructions on how to extend or modify this functionality.

Feel free to adjust the snippets and explanations to better fit the actual implementation details or specific configurations in your project.