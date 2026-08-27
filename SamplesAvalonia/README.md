# StockSharp Avalonia samples

This tree contains standalone Avalonia counterparts for the 28 public WPF UI samples in `../Samples`.
The original WPF heads remain unchanged. Each migrated sample has two project files:

- `*.Avalonia.csproj` consumes the corresponding published StockSharp Avalonia UI package;
- `*.Avalonia_fromsrc.csproj` consumes the adjacent StockSharpApps Avalonia source project.

The shared application, connector-settings workflow, UI callback routing, and lifecycle helpers live in
`Common` and are included by `common_samples_avalonia.props`.

The migration contains all 28 public WPF UI heads: Basic 01-03, Candles 01-02,
Storage 04, Indicators 01-03, Chart 01-03, Strategies 01-10, Testing 01-03, Misc 01,
and Advanced 01-02. The seven remaining public samples are console/cross-platform projects and
do not require an Avalonia window head.

The history and realtime strategy/testing heads run real storage, connector, emulation,
optimization, order-routing, charting, and lifecycle pipelines; they do not substitute generated
UI-only demonstrations for the original lessons. The advanced heads share an operational Avalonia
workspace while retaining distinct connector and local-storage ownership policies.

The StockSharp Avalonia UI packages are not yet published to the configured external NuGet sources.
The package heads were nevertheless restored and built against locally packed versions of the adjacent
source projects; `_fromsrc` remains the directly reproducible validation path until publication.

The Stage 18 completion gate built all 28 `_fromsrc` heads and all 28 package heads in Release and ran the
complete sample contract suite: 72 passed, 0 failed. Package publication is release work, not a remaining
sample-migration gap.

Targeted validation:

```powershell
dotnet test SamplesAvalonia.Tests/SamplesAvalonia.Tests.csproj --filter "FullyQualifiedName~BasicSampleInventoryTests|FullyQualifiedName~ConnectorConfigurationCoordinatorTests|FullyQualifiedName~EventSubscriptionTests|FullyQualifiedName~SampleUiEventRouterTests"
dotnet build 01_Basic/01_ConnectAndDownloadInstruments/01_Basic.ConnectAndDownloadInstruments.Avalonia_fromsrc.csproj
dotnet build 01_Basic/02_MarketDepths/02_Basic.MarketDepths.Avalonia_fromsrc.csproj
dotnet build 01_Basic/03_Orders/03_Basic.Orders.Avalonia_fromsrc.csproj
dotnet build 02_Candles/01_Realtime/01_Candles.Realtime.Avalonia_fromsrc.csproj
dotnet build 02_Candles/02_CombineHistoryRealtime/02_Candles.CombineHistoryRealtime.Avalonia_fromsrc.csproj
dotnet build 03_Storage/04_HydraServerConnect/04_Storage.HydraServerConnect.Avalonia_fromsrc.csproj
dotnet test SamplesAvalonia.Tests/SamplesAvalonia.Tests.csproj --filter "FullyQualifiedName~HistoryStrategySampleTests|FullyQualifiedName~HistoryAdvancedSampleTests"
dotnet build 06_Strategies/01_HistorySMA/01_Strategies.HistorySMA.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/02_HistoryBollingerBands/02_Strategies.HistoryBollingerBands.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/03_HistoryTrend/03_Strategies.HistoryTrend.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/04_HistoryMarketRule/04_Strategies.HistoryMarketRule.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/05_HistoryIndex/05_Strategies.HistoryIndex.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/06_HistoryQuoting/06_Strategies.HistoryQuoting.Avalonia_fromsrc.csproj
dotnet test SamplesAvalonia.Tests/SamplesAvalonia.Tests.csproj --filter "FullyQualifiedName~LiveStrategySampleTests"
dotnet build 06_Strategies/07_LiveSpread/07_Strategies.LiveSpread.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/08_LiveArbitrage/08_Strategies.LiveArbitrage.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/09_LiveOptionsQuoting/09_Strategies.LiveOptionsQuoting.Avalonia_fromsrc.csproj
dotnet build 06_Strategies/10_LiveTerminal/10_Strategies.LiveTerminal.Avalonia_fromsrc.csproj
dotnet build 07_Testing/01_History/01_Testing.History.Avalonia_fromsrc.csproj
dotnet build 07_Testing/02_Optimization/02_Testing.Optimization.Avalonia_fromsrc.csproj
dotnet build 07_Testing/03_RealTime/03_Testing.RealTime.Avalonia_fromsrc.csproj
dotnet build 09_Advanced/01_MultiConnect/01_Advanced.MultiConnect.Avalonia_fromsrc.csproj
dotnet build 09_Advanced/02_StoreDataLocal/02_Advanced.SaveDataLocal.Avalonia_fromsrc.csproj
dotnet test SamplesAvalonia.Tests/SamplesAvalonia.Tests.csproj --filter "FullyQualifiedName~SpecializedLiveStrategySampleTests|FullyQualifiedName~TerminalStrategyPersistenceTests|FullyQualifiedName~RealTimeSampleCompositionTests|FullyQualifiedName~AdvancedSampleCompositionTests|FullyQualifiedName~PublicSampleInventoryTests"
```

`PublicSampleInventoryTests` is the exact 28-head parity gate. Every package and `_fromsrc` project
must remain present when public WPF samples are added, renamed, or removed.
