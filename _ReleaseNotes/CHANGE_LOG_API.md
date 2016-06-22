StockSharp API Change log
========================
###current:
* (feature) IConnector.CancelOrders. SecurityType filter.
* (feature) FIX connector. Exante dialect
* (feature) TwimeTrader.PortfolioName
* (feature) QuikLua. ConvertToLatin now optional.
* (feature) Transaq. Address. IP -> Host.
* (bug) SecurityAdapter fix.
* (feature) BasePosition.Currency
* (feature) TimeZoneInfo. Serialization -> ID https://github.com/StockSharp/StockSharp/issues/228#issuecomment-227014493
* (feature) SecurityLookupMessage.CFICode
* (bug) RandomGen fixes.
* (bug) MarketEmulator. AvgPosPrice fix.
* (feature) SecurityId.NativeAsInt

###v4.3.15:
* (feature) TWIME connector.
* (feature) IConnector. MassOrderCanceled, MassOrderCancelFailed.
* (feature) SecurityAdapter.
* (bug) OptionDesk fixes.
* (bug) SmartCOM. Fractal price step fixes.
* (feature) TraderHelper. Removed obsolete GetPosition overloads.
* (bug) Micex TEAP. Fix struct downloading.
* (feature) Micex TEAP. New enum statuses.
* (feature) Charting. ChartDrawData.
* (feature) Plaza. Spectra 5.0 + performance refactoring.
* (feature) Grid controls (SecurityGrid, OrderGrid etc.) now based on DevExpress.
* (feature) Quik lua. Average price for positions.
* (feature) FIX server. Handle transaction errors.
* (bug) ADX, Fractals indicator fixes.
* (bug) Position manager fix.
* (bug) SecurityPicker. Show common columns fix.
* (feature) Connectors. Removed ref from Xceed.
* (feature) IPnLManager. UnrealizedPnL is nullable.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/227
* (feature) PnLManager. Uses leverage.

###v4.3.14.2:
* (feature) Chart. Active orders.
* (bug) https://github.com/StockSharp/StockSharp/issues/222
* (feature) IBTrader -> InteractiveBrokersTrader, OECTrader -> OpenECryTrader
* (bug) TargetPlatformWindow fix.

###v4.3.14.1:
* (bug) Transaq. double <-> decimal conversation fix.
* (feature) Blackwood. Embed zlib into resources.
* (bug) Nuget fixed.

###v4.3.14:
* (feature) BaseCandleBuilderSource.RaiseProcessing perf fixes.
* (feature) Ecng.Backup
* (feature) Ecng.Roslyn
* (feature) GuiObjectHelper removed.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/20
* (feature) Chart themes.
* (feature) Csv storage perf boost.
* (feature) Quik lua. Tick OI.
* (feature) Twime connector.
* (feature) SPB exchange.
* (feature) Order.Status. Nullable long.
* (feature) TraderHelper. Plaza extensions.
* (feature) IConnector. ConnectedEx, DisconnectedEx, ConnectionErrorEx.
* (feature) Plaza. Cancel On Disconnect support.
* (bug) OpenECry. Connect/disconnect fixes.
* (feature) BinExporter -> StockSharpExporter.
* (feature) WpfToolkit v.2.6.0.
* (feature) OrderMessage.TransactionId
* (bug) LogManager. Wait for disposing
* (bug) TransactionBinarySerializer fix.
* (bug) QuoteBinarySerializer. Fix empty depths handling.
* (feature) IMessageAdapter.OrderCancelVolumeRequired
* (feature) Samples. Group order cancel.
* (bug) SecurityIdTextBox small fix.
* (bug) RealTimeCandleBuilderSource. Raise Stopped event.
* (feature) TraderHelper. IsGtc, IsToday
* (feature) FIX connector. Dialects.
* (feature) Transaq. v2.20.5
* (feature) TimeMessage.TransactionId. String -> Long
* (feature) Connector.ChangePassword
* (bug) MarketDataGrid. Fix candle values.
* (feature) Plaza.IsDemo
* (feature) OpenECry. Uuid as SecureString.
* (bug) Order.Type nullable fix.
* (feature) Chart performance improved.
* (feature) ExecutionMessage.BrokerCode
* (feature) IStorageRegistry.GetTransactionStorage
* (feature) ExecMsg. HasOrderInfo, HasTradeInfo.
* (feature) ExecTypes. Order -> Transaction. Trade -> Obsolete.
* (feature) ExecMsg. Volume -> OrderVolume + TradeVolume.
* (bug) ProgGrid. TimeZoneInfo edit fix.
* (feature) ConnectorSupportedMessagesPanel
* (feature) Alerts. Removed Actipro dependency.
* (bug) FIX connector. SUR currency fix.
* (bug) Equity chart fix.
* (feature) Message.LocalTime. DateTime -> DateTimeOffset
* (bug) CandleHelper.GetCandleBounds fix.
* (feature) ISecurityProvider. Performance improve.
* (feature) Ecng. Strong names.
* (feature) CandleSerializer. CandlePriceLevel serialization support.
* (feature) StorageMessageAdapter.
* (feature) Blackwood. v3.2.0
* (feature) SecurityExternalId is struct.
* (bug) SecurityEditor. Autocomplete fix.
* (feature) ExcelExporter update.
* (feature) Micex. ExtraSettings
* (bug) CodeReferencesWindow. Fix loading non .NET assemblies.
* (feature) ExchangeComboBox.
* (feature) IConnector. Single value events.
* (feature) ExecMsg. Price -> OrderPrice
* (feature) ChartPanel.SecurityProvider
* (feature) Plaza. OverrideDll
* (feature) SecurityGrid performance improve.
* (bug) OrderLog process fix.
* (bug) AdvertisePanel fix.
* (feature) FortsDownloader
* (feature) C# 6.0 features.
* (feature) Chart cluster and box.
* (bug) Fix http://stocksharp.com/forum/yaf_postsm35888_LChI-Viewer.aspx#post35888
* (bug) FilterableSecurityProvider. Moved to Algo.
* (bug) ISecurityStorage.NewSecurity event.
* (bug) BasketMessageAdapter. Save/Load fix.
* (feature) ConnectorWindow.
* (bug) Transaq fix.
* (feature) Backtesting. Use history source (Finam, Google, Yahoo) directly.

###v4.3.13:
* (bug) MessageAdaptersPanel fix.
* (bug) Chart fix (draw values in hidden mode).
* (bug) SmartCOM sec info fix.
* (feature) Connectors. Doc + Icon attributes.

###v4.3.12:
* (feature) ISecurityStorage. CSV implementation.
* (feature) ICandleBuilder. Direct value processing.
* (feature) Correlation indicator.
* (feature) Covariance indicator.
* (feature) Chart.TimeZone
* (feature) Oanda + BitStamp. Control error count.
* (feature) WpfToolkit 2.5.0
* (bug) FixServer. Error handling fix.
* (bug) Fix http://stocksharp.com/forum/yaf_postst5724_QuikLua--System-InvalidOperationException-pri-poluchienii-ordierov-biez-transactionId.aspx
* (feature) Plaza. CGate router 1.3.12.5
* (bug) YahooHistorySource fix
* (feature) Interactive Brokers. 9.72
* (feature) Backtesting on level1.

###v4.3.11:
* (feature) CSV storage. Time zone.
* (feature) Transaq. v2.16.1
* (feature) level1 -> depths.
* (feature) MarketDataMessage. Nullable fields.
* (bug) DateTime to DateTimeOffset casting fix.
* (feature) CSV storage perf improve.
* (bug) BasketMessageAdapter. Subscription fix.
* (feature) Storage (bin). Allow different time zones.
* (feature) FIX connector. TimeSpan -> TimeZoneInfo.
* (feature) SecurityGrid. TimeZone column
* (bug) Connector. Unsubscription fixes.
* (feature) CandleManager. Priority source switch in runtime.
* (feature) SmartCOM. Extended quote price check.
* (feature) Plaza. Multi connections.
* (feature) Storage. Volume-less ticks support.
* (feature) SampleHistoryTesting. ES mini test.
* (bug) ChartPanel.Save fix.
* (feature) Leve1 -> Ticks + Candles.
* (bug) Quik lua. Fix zero transaction id.
* (feature) ILogListener. Implements IDisposable
* (bug) IQFeed. Candle timezone fix.
* (bug) NewsSerializer fix.
* (feature) NewsGrid. Request story + open url.
* (bug) IQFeed. News fixes.
* (bug) Lmax, IQFeed, IB. Tick subscription fix.
* (feature) MarketEmulator. Fill server time.
* (feature) IQFeed. COMM3 security type.
* (feature) SampleRealTimeEmulation. Look up securities.
* (feature) SampleRealTimeEmulation. IQFeed support.
* (bug) TraderHelper.ToDecimal fix.

###v4.3.10
* (bug) Storage. Level1 fix.
* (bug) ITCH. Fixes.
* (feature) HistoryEmulationConnector refactoring.
* (feature) History emulation. Support all candle types.
* (bug) LocalMarketDataDrive.Dates fix.
* (feature) Candles history update.
* (feature) Real time emulation refactoring.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/88
* (feature) CurrencyTypes.GHS
* (bug) BlackScholes fixes.
* (bug) CsvMarketDataSerializer. Fix BOM char.
* (feature) Grids. TimeZone column
* (bug) FIX connector. Fix ExecMsg.ServerTime
* (feature) HelpButton.
* (feature) IOrderLogMarketDepthBuilder.
* (feature) IpAddressEditor.
* (feature) AdvertisePanel.

###v4.3.9.1:
* (feature) Candle.RelativeVolume is nullable.
* (feature) Candle. Ticks fields are nullable.
* (feature) CandleSerializer. Ticks fields.

###v4.3.9:
* (bug) RecoveryFactorParameter fix.
* (bug) OrderLogMarketDepthBuilder fix.
* (feature) Algo.Storages.Backup - clients for cloud storage backup services.
* (feature) FIX connector. More level1 fields support.
* (feature) LicenseHelper.LicenseError
* (feature) Connector. Not track ticks option.

###v4.3.8:
* (feature) ITCH connector.
* (bug) FIX connector and FixServer. Many fixes.
* (bug) CSV storage fix.
* (bug) Excel export boost.
* (bug) Storage. Fix delete range.
* (bug) TargetPlatformWindow fix.
* (feature) BarChart connector (history mode).
* (feature) SampleLogging. New sample.
* (feature) CandleStates. Started + Changed -> Active.
* (feature) OrderWindow. Set default price and volume.
* (feature) ExecutionMessage.ClientCode
* (feature) MyTradeGrid, OrderGrid and OrderWindow. Display ClientCode.
* (feature) Order, Trade, MarketDepth. New field Currency.
* (feature) FilterableSecurityProvider. Indexing Security.ExternalId
* (feature) PF combo. Insert unknown portfolio.
* (feature) OrderCancelMessage. New field Side.
* (bug) Connector. Fix overflow.
* (bug) SampleSmartCandles. Fix
* (feature) ExternalCandleSource.Stopped event.
* (feature) TraceSource
* (bug) LogManager. FlushInterval lower bound check fix.
* (feature) SampleHistoryTesting. Order book emulation option.
* (feature) Blackwood. 3.1.9
* (feature) Currency. GBX
* (feature) Storage. Date cache bin->txt format.
* (bug) CSV storage. Fix save NewsMessage.SecurityId.
* (bug) CSV storage. Fix append data with same time for order log and tick trades.
* (feature) FIX connector. ExecMsg.ClientCode.
* (feature) FIX connector. Read/write timeouts.

###v4.3.7:
* (feature) MessageDirections. Removed.
* (feature) Connector. OnRegisterXXX OnUnRegisterXXX removed.
* (feature) Micex. Update protocol.
* (bug) Quik. Stop order fixes.
* (feature) Samples. StopOrderWindow refactoring.
* (feature) BasketMessageAdapter. Save Load implementation.
* (bug) Protective strategies. Fixes.
* (feature) Plaza. COD. Extended license.
* (feature) Monitor. Clear method.
* (bug) https://github.com/StockSharp/StockSharp/issues/113
* (bug) Security.IsExpired. Bug fix
* (bug) Micex. Reset fix.
* (bug) Connector. Dispose fix.
* (bug) QuotingStrategy. Fix stopping.
* (feature) Rithmic 8.5.0
* (bug) Back testing. Fix suspend/resume.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/101
* (feature) Storage. Removed DataStorageReader.
* (feature) Chart update.
* (feature) WPF Toolkit. 2.4.0

###v4.3.6:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/90
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/93
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/92
* (feature) https://github.com/StockSharp/StockSharp/commit/62a19979280ab678679aee7660f73c9b9614de93

###v4.3.5:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/87
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/83
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/70
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/79
* (feature) FIX connector. Check sum is uint.
* (feature) https://github.com/StockSharp/StockSharp/pull/74
* (bug) https://github.com/StockSharp/StockSharp/pull/81
* (bug) Fix http://stocksharp.com/forum/yaf_postsm35263_FixServer-System-ArgumentOutOfRangeException.aspx#post35263

###v4.3.4:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/46
* (feature) OEC 3.5.14
* (feature) ILogSource.IsRoot
* (bug) Back testings. Generated data fixes.
* (bug) Emulator. Prevent big order book generation.
* (feature) Storage. Replace entity by messages.
* (feature) BitStamp. Level1 refresh interval is 10 sec.
* (feature) FIX. Check sum is uint.
* (bug) FixServer. Sync writers.

###v4.3.3:
* (feature) OverrideDll option.
* (bug) BasketMessageAdapter. Disconnect fix.
* (feature) Transaq. UTC
* (bug) Back testing. Fixes.

###v4.3.2:
* (bug) BTCE. Security decimals fix.

###v4.3.1:
* (feature) BitStamp, IQFeed, ETrade and Oanda source code.
* (bug) Fix http://stocksharp.com/forum/yaf_postst5619_Oshibka-nie-udalos--naiti-chast--puti.aspx

###v4.3.0:
* (feature) IMessageChannel. Message thread model refactoring.
* (feature) IConnector. Removed Start/Stop export.
* (feature) Connector uses BasketMessageAdapter.
* (feature) Order.Id is nullable
* (feature) IMessageSessionHolder removed.
* (feature) Order.ExpiryDate is nullable.
* (feature) IConnector. ProcessDataError -> Error.
* (bug) BitStamp market data fixes.
* (bug) SmartCom transaction fixes.
* (bug) LMAX fixes.
* (feature) OrderGrid. Show long and string identifiers.
* (feature) Rss. Subscribe/unsubscribe support.
* (feature) Transaq. v 2.10.10
* (feature) IConnector.NewDataExported removed.
* (feature) BasketConnector removed.
* (bug) https://github.com/StockSharp/StockSharp/issues/40
* (feature) ReConnectionSettings. Moved to Messages.
* (feature) ReConnectionSettings. Export settings removed.
* (feature) BasketMessageAdapter refactoring.
* (feature) OrderCancelMesage.OrderId is nullable.
* (feature) HeartbeatAdapter.
* (feature) Order. BrokerCode and ClientCode fields.
* (bug) https://github.com/StockSharp/StockSharp/issues/49
* (bug) Micex. Fix 32-bit mode.
* (bug) Plaza. Anonym deals stream. Fast repl mode.
* (bug) OrderGrid. Sort ordering fixes.
* (feature) MyTradeGrid. Multi ids.
* (feature) Indicators. IsFormed initialized only by IsFinal value.
* (feature) Indicators refactoring. Removed IIndicator.CanProcess.
* (bug) Connector. Fix Connect/Disconnect messages for a few adapters.
* (bug) Quik lua. Commission fill fix.
* (bug) FixServer. Close session fix.
* (feature) Fix http://stocksharp.com/forum/yaf_postst5622_Logh-soobshchieniia-MarketDataSnapshotFullRefresh.aspx
* (feature) Plaza. Schema update.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/36
* (feature) FixServer. Logout fixes.
* (bug) FIX connector. Request portfolios support.

###v4.2.75:
* (feature) FixServer. No longer use QuickFix.
* (feature) FixServer. Implements IMessageChannel.
* (feature) FixServer. Separate market data and transactional endpoints.
* (feature) FIX connector. FixMessageWriter.
* (feature) FixServer. Use FixMessageWriter for outgoing messages.
* (feature) FixServer. Support candles.
* (feature) Order.TimeInForce and ExecMsg.TimeInForce are nullable.
* (feature) CandleMessage. OpenVolume, HighVolume, LowVolume, CloseVolume are nullable.
* (feature) NewsMessage.SecurityId is nullable.
* (bug) OrderWnd. Fix step while nullable info.
* (bug) EntityCache. Fix trade msg for unknown order.
* (feature) Logging. Error handling.
* (feature) Samples. OrdersWindow. Move order ability.
* (feature) MessageConverterHelper. MessageTypes <-> MarketDataTypes for candles
* (feature) PlazaTrader.IsControlConnectionLost

###v4.2.74:
* (feature) FIX connector. IFixWriter IFixReader interfaces.
* (feature) TextExporter refactoring.
* (feature) Xaml. NewsMessageGrid
* (feature) SmartCom. SmallComService utility class.
* (feature) Plaza. Fix control connection state in CGate mode.
* (feature) Transaq 2.10.8
* (bug) FIX connector and Quik LUA. Fix MarketDepth subscription for ALL security.
* (bug) Messages and Entities. Serialization fixes.
* (feature) Plaza. Spectra 4 (ASTS).
* (feature) Security. PriceStep, VolumeStep, Decimals, Multiplier, MinPrice, MaxPrice are nullable.

###v4.2.73:
* (feature) FIX connector. Client side no longer use QuickFix
* (bug) LuaFixServer. Level1 thread safety.
* (bug) EntityCache. Fix ExecMsg.OridinTransId == OrderStatusMsg.TransactionId
* (feature) ExecutionMessage. OrderId, Balance, Volume, VisibleVolume, TradeId, TradePrice, TradeStatus is nullable.
* (feature) Security.Strike is nullable
* (feature) QuikOrderCondition. Nullable fields.

###v4.2.72:
* (feature) SecurityMessage.Decimals
* (feature) Security.State nullable
* (feature) IOHelper.ToFullPath
* (bug) FIX connector. Reconnection fix. Lost connection control fix.
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/33
* (bug) Micex. Decimals fix
* (bug) SecurityGrid. Fix PriceStep, Decimals and VolumeStep columns.

###v4.2.71:
* (feature) Strategy.StartedTime is DTO
* (bug) Fix http://stocksharp.com/forum/yaf_postst5556_S--Api.aspx
* (bug) Fix RtsHistorySource

###v4.2.70:
* (feature) SecurityGrid. Add columns.
* (feature) Security. O H L C V fields marked as nullable.
* (bug) FIX connector. Level1 small fix.
* (bug) Quik lua. Level1 value type fix.
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/31

###v4.2.69:
* (feature) LicensePanel. Xaml -> Licensing
* (feature) MarketEmu. Depth fill improve.
* (bug) LicenseTool fixes.

###v4.2.68:
* (bug) Quik lua. Turned off license check.
* (bug) Protective strategies. Fix price calc with big offset value.
* (bug) Fix http://stocksharp.com/forum/yaf_postsm34658_Kotirovaniie.aspx#post34658
* (feature) Security.MinPrice = 0.01 by default.
* (feature) (MarketEmu.ProcessTime performance improve.
* (feature) WorkingTime.Clone performance improve.
* (bug) MarketEmu. Board update fix.
* (feature) Monitor. StrategyRoot is sub node CoreRoot.
* (bug) Connector.ClearCache fix
* (bug) SecurityEditor. Update text fix.

###v4.2.67:
* (feature) Ecng update.

###v4.2.66:
* (feature) Quik lua. Support ALL@ALL security for market data subscription.
* (feature) Quik lua. Level1 subscription check optimization.
* (feature) Quik lua. Check Level1 duplicates.
* (feature) FixServer request id mapping refactoring.
* (bug) Plaza. Fix level1 time.
* (feature) OrderStatMsg. Single order details.
* (bug) Emulator small fix.

###v4.2.65:
* (feature) QuandlHistorySource
* (feature) Quoting refactoring
* (bug) Fix Quik LUA. Fix http://stocksharp.com/forum/yaf_postst5525_Oshibka-Lua-podkliuchieniia-pri-rabotie-s-aktsiiami.aspx
* (bug) Fix Quik LUA. Fix exception handling

###v4.2.64:
* (feature) Source codes for Quik and InteractiveBrokers

###v4.2.63:
* (bug) OpenECry. Fix double <-> decimal casting.
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/16

###v4.2.62:
* (bug) Micex. Format price fix
* (feature) Source codes for Messages, BE, Algo, Xaml, Localization, Logging, Community and few connectors (SmartCOM, AlfaDirect, Transaq, BTCE, OpenECry, LMAX, MatLab, CQG, Sterling, RSS, Alor)

###v4.2.61:
* (bug) Quik. Fix https://github.com/stocksharp/stocksharp/issues/13
* (bug) Fusion/Blackwood. Fix http://stocksharp.com/forum/yaf_postst5511_4-2-60---Exception-pri-otpravkie-ordiera.aspx
* (bug) LogManager.Application. Replacing fix
* (bug) Plaza. Level1 ServerTime fill
* (bug) YahooHistorySource. Time fix

###v4.2.60:
* (bug) Fusion/Blackwood. Fix http://stocksharp.com/forum/yaf_postst5498_Probliema-na-rieal-nom-schietie.aspx
* (feature) Quik. Process request performance boost
* (feature) OrderWindow. Disable ByMarket checkbox
* (feature) Plaza. Anonym deals turned on by default
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/11

###v4.2.59:
* (feature) Fusion/Blackwood. 3.1.8
* (bug) Micex. OrderBookDepth fix
* (bug) Micex. RequestAllDepths fix
* (bug) OpenECry. Order processing fix

###v4.2.58:
* (feature) Micex. RequestAllDepths
* (bug) Micex. Tick subscribe fix

###v4.2.57:
* (feature) QuikLua. Removed atomic reregister for micex
* (feature) Micex. Added IFC_Broker24 interface
* (feature) Micex. OrderBookDepth
* (bug) Unit fixes. Fix http://stocksharp.com/forum/yaf_postst5489_Izmieniena-loghika-raboty-s-Unit-v-novykh-viersiiakh.aspx
* (bug) OpenECry. Remoting fix
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/7
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/3

###v4.2.56:
* (bug) AlfaDirect. Fix cadle subscription. Fix http://stocksharp.com/forum/yaf_postst5483_primier-SampleAlfaCandles.aspx
* (bug) OpenECry fixes
* (bug) Localization fixes
* (bug) Chart. Fix indicator adding

###v4.2.55:
* (feature) Export executions
* (bug) Localization fixes

###v4.2.54:
* (bug) Execution storage fix
* (bug) Plaza. Fix handling non Message based transaction
* (bug) Localization fixes

###v4.2.53:
* (bug) Localization fixes

###v4.2.52:
* (bug) Localization fixes

###v4.2.51:
* (bug) Oanda. Security lookup and market data subscription fixes
* (bug) Localization fixes
* (feature) OrderFail. ServerTime and LocalTime fields
* (bug) TrueFX and GainCapital historical sources fixes
* (bug) Unit. Fix serialization

###v4.2.50:
* (bug) Error loading candles from storage
* (bug) FortsDailyData.GetRate fix

###v4.2.49:
* (bug) PriceStep fixes for Oanda
* (bug) Filling empty Arg for Candles from CandleSeries
* (feature) Tick origin side added for FIX (QuikLua) http://stocksharp.com/forum/yaf_postst5476_S--API.aspx
* (feature) TimeZoneComboBox

###v4.2.48:
* (feature) Plaza supports MM and limit transactions
* (bug) Localization fixes for Xaml

###v4.2.47:
* (bug) Localization fixes

###v4.2.46:
* (feature) Filling Security.Status for QuikLua http://stocksharp.com/forum/yaf_postsm34270_Novyi-konniektor-k-Quik.aspx#post34270
* (bug) Building market depth from OL fixes
* (feature) Zero or negative prices for spreads in Storage
* (feature) ExpirationDate added to OrderWindow

###v4.2.0-4.2.43
Available on [forum](http://stocksharp.com/forum/yaf_postst4219_S--API-4-2.aspx)