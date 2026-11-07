# Core performance / Native AOT audit

These are **diagnostic** projects; neither project changes the public S# API nor its normal build.

## Measure allocation and throughput

Run with .NET 10, **Release**, on a quiet machine:

```bash
dotnet run -c Release --project Benchmarks/Benchmarks.csproj -- --filter '*CoreMarketDataBenchmarks*'
```

The three paths intentionally measure different operations:

- `CloneDepth`: deep copy of `QuoteChangeMessage` (including bid/ask arrays).
- `OrderLogSnapshot`: materialize a complete book from `OrderLogMarketDepthBuilder`.
- `MatchingEngineSnapshot`: materialize a complete book from `MatchingEngine.OrderBook`.

`Levels` is the number of price levels *per side*. Check the `Allocated` and `Gen0` columns, not only the mean time. These are repeatable baselines, **not** measurements of live exchange/network latency or the full message pipeline. Do not compare them as equivalent implementations.

## Check compile-time AOT warnings without changing the package builds

```bash
dotnet build Messages/Messages.csproj -c Release -f net10.0 -p:StockSharpAotAudit=true
dotnet build MatchingEngine/MatchingEngine.csproj -c Release -f net10.0 -p:StockSharpAotAudit=true
dotnet build Algo/Algo.csproj -c Release -f net10.0 -p:StockSharpAotAudit=true
```

`StockSharpAotAudit=true` enables trim, single-file, and AOT analyzers only for the .NET 10 inner build. It does **not** mark libraries `IsAotCompatible`, and regular builds are unchanged.

## Check the linked application with Native AOT

The test application's `PublishAot` property is set in its own project (rather than passed globally, which would incorrectly apply it to netstandard source generators).

From a Linux machine with the native compilation prerequisites installed:

```bash
dotnet publish AotSmoke/AotSmoke.csproj -c Release -r linux-x64 \
  -p:StockSharpAotAudit=true \
  -p:TrimmerSingleWarn=false -bl:aot-audit.binlog
```

Run the resulting binary to verify this small set of reachable API paths. A successful smoke test **does not** prove every feature of S# is Native AOT compatible. More code paths require more smoke coverage, including reflection, runtime compilation, connector discovery and serialization. The dedicated GitHub Actions workflow saves a detailed publish log and binlog for investigation.
