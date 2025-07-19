# StockSharp.BusinessEntities

`StockSharp.BusinessEntities` is the core library that defines the trading entities used throughout the S# platform. It contains models describing exchanges, instruments, orders, trades and other objects along with provider interfaces for market data and order management.

## Key classes
- **Exchange** and **ExchangeBoard** — descriptions of an exchange and a specific electronic board.
- **Security** — represents a financial instrument with fields such as symbol, type, price step and currency.
- **Portfolio** and **Position** — trading account information and open positions.
- **Order**, **MyTrade** and **Trade** — objects that reflect registered orders and trades.
- **MarketDepth** — order book (best bid/ask and current quotes).
- **News** — news item information.
- **Candle** and its derivations (e.g. `TimeFrameCandle`, `TickCandle`) — candle models for analyzing market data.

## Provider interfaces
The library declares base interfaces for interacting with various data sources and trading systems:
- `IConnector` is the main interface combining connectivity, subscriptions, orders and data access. It inherits from `IMarketDataProvider`, `ITransactionProvider`, `ISecurityProvider` and others. A fragment of the interface:

```csharp
public interface IConnector : IMessageChannel, IPersistable, ILogReceiver,
        IMarketDataProvider, ITransactionProvider, ISecurityProvider,
        ISubscriptionProvider, ITimeProvider,
        IPortfolioProvider, IPositionProvider
{
    event Action<Message> NewMessage;
    event Action Connected;
    event Action Disconnected;
    // ...
}
```

- `IMarketDataProvider` — receiving market data and price levels.
- `ITransactionProvider` — registering, canceling and modifying orders.
- `ISubscriptionProvider` — managing real‑time data subscriptions.
- `ISecurityProvider`, `IPortfolioProvider`, `IPositionProvider` — access to lists of securities, portfolios and positions.
- `ITimeProvider` — current time source.

## Extensions and helpers
`EntitiesExtensions.cs` contains many utilities:
- Converting price values to pips or points.
- Cloning and re-registering orders.
- Transforming entities to `StockSharp.Messages` types.
- Enumerating all registered exchanges and boards.

Example method:
```csharp
public static IEnumerable<Exchange> EnumerateExchanges()
    => typeof(Exchange)
        .GetMembers<PropertyInfo>(_publicStatic, typeof(Exchange))
        .Select(prop => (Exchange)prop.GetValue(null, null));
```

## Project structure
`BusinessEntities.csproj` connects shared settings and package dependencies:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\common_target_net.props" />
  <PropertyGroup>
    <ProjectGuid>{DCE69DB8-53CA-4B7F-9368-02F175A31074}</ProjectGuid>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ecng.Configuration" Version="$(EcngVer)" />
    <PackageReference Include="Ecng.Drawing" Version="$(EcngVer)" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Messages\Messages.csproj" />
  </ItemGroup>
</Project>
```

## Usage
`BusinessEntities` is used in all `Samples` projects and production connectors. Add the project to your solution or install the package from the private S# NuGet feed. You can then create `Security` objects, subscribe to data through the interfaces and register orders.

### Example
A code fragment for subscribing to an instrument and receiving market data can be found in `Samples/01_Basic/01_ConnectAndDownloadInstruments`.


