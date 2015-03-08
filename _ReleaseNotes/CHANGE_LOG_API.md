StockSharp API Change log
========================
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