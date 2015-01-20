StockSharp Release Notes
========================

Welcome to StockSharp
---------------------
StockSharp is a trading and algorithmic trading platform (stock markets, forex, bincoins and options).

What's New
----------
###v4.2.57:
* (feature) QuikLua. Removed atomic reregister for micex
* (feature) Micex. Added IFC_Broker24 interface
* (bug) Unit fixes. Fix http://stocksharp.com/forum/yaf_postst5489_Izmieniena-loghika-raboty-s-Unit-v-novykh-viersiiakh.aspx

###v4.2.56:
* (bug) AlfaDirect. Fix cadle subscrition. Fix http://stocksharp.com/forum/yaf_postst5483_primier-SampleAlfaCandles.aspx
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
