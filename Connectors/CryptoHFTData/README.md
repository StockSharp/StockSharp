# CryptoHFTData Connector

This connector adds [CryptoHFTData](https://cryptohftdata.com) as a historical
cryptocurrency market-data source for StockSharp. It downloads the provider's
hourly Parquet/Zstandard files directly and emits native StockSharp messages.

## Supported data

- historical trades as `ExecutionMessage` ticks
- historical order-book snapshots and incremental `QuoteChangeMessage` updates
- symbol discovery for the selected exchange
- anonymous free-tier downloads or authenticated downloads with an API key

CryptoHFTData is historical-only and does not provide order routing or live
streaming. Timestamps are normalized to UTC; maker-side flags are converted to
the aggressor side used by StockSharp. Order-book event types are preserved:
provider snapshots become `SnapshotComplete` messages and update rows become
sequenced increments. Some exchange intervals contain updates only, so consumers
that require a fully initialized book should include their normal warm-up range.

## Usage

```csharp
var adapter = new CryptoHFTDataMessageAdapter(new IncrementalIdGenerator())
{
    Exchange = "binance_futures",
    Token = Environment.GetEnvironmentVariable("CRYPTOHFTDATA_API_KEY")?.Secure(),
};

var connector = new Connector();
connector.Adapter.InnerAdapters.Add(adapter);
connector.SubscriptionsOnConnect.Clear();
connector.Connect();

var security = new Security
{
    Id = "KAVAUSDT@binance_futures",
    Code = "KAVAUSDT",
    Board = ExchangeBoard.GetOrCreateBoard("binance_futures"),
};
var subscription = new Subscription(DataType.Ticks, security)
{
    From = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero),
    To = new DateTimeOffset(2026, 7, 11, 0, 59, 59, TimeSpan.Zero),
};
connector.Subscribe(subscription);
```

Use one of the exchange identifiers returned by CryptoHFTData, such as
`binance_spot`, `binance_futures`, `bybit`, `okx_futures`, or
`hyperliquid_futures`. The API key is optional; keyless downloads are paced to
the free-tier limit of 60 requests per minute. Set `Token` from
`CRYPTOHFTDATA_API_KEY` for account-level limits.
