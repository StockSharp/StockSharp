StockSharp API Change log
========================
###v4.3.4:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/46
* (feature) OEC 3.5.14
* (feature) ILogSource.IsRoot
* (bug) Back testings. Generated data fixes.
* (bug) Emulator. Prevent big order book generation.
* (feature) Storage. Replace entity by messages.
* (feature) BitStamp. Level1 refresh interval is 10 sec.

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