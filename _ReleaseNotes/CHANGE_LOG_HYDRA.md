StockSharp Data (Hydra) Change log
========================
###current
* (feature) Hydra. ExportTypes. Bin -> StockSharpBin + StockSharpCsv.
* (feature) Csv storage perf boost.
* (feature) Data type refactoring.
* (feature) Erase data for all drives.
* (feature) Candle day interval.
* (feature) Connector's tasks refactoring.
* (bug) Advertise fix.
* (bug) Hydra. Export task fix.
* (feature) Hydra. Show news counter.

###v4.3.13
* (bug) DB export fix.
* (bug) http://stocksharp.com/forum/yaf_postst5740_oshibka-pri-eksportie-dannykh-v-sqlite.aspx

###v4.3.11:
* (bug) Config fix.
* (bug) Chart pane fix.
* (feature) Transaq. v2.16.1
* (feature) Level1 export improved.
* (feature) Build level1 -> depths.
* (bug) DateTime to DateTimeOffset casting fix.
* (feature) Storage (bin). Allow different time zones.
* (feature) Leve1 -> Ticks + Candles.
* (bug) IQFeed fix.
* (bug) NewsSerializer fix.
* (feature) AdvertisePanel changes.

###v4.3.10:
* (bug) Hydra. Level1 csv export fix.
* (bug) Storage. Level1 fix.
* (bug) ITCH fixes.
* (bug) LocalMarketDataDrive.Dates fix.
* (bug) CsvMarketDataSerializer. Fix BOM char.
* (feature) Grids. TimeZone column
* (bug) FIX connector. Fix ExecMsg.ServerTime
* (feature) Hydra. Export csv. Header.
* (feature) Hydra. SourcesWindow. Find by name.
* (feature) AdvertisePanel.
* (bug) Hydra. Depth csv export fix.

###v4.3.9.1:
* (feature) Hydra. Import pane improve.
* (bug) Release 4.3.9 fix (missed files).

###v4.3.9:
* (feature) Backup plugin (Amazon S3 cloud storage)
* (feature) Txt/csv export preview window.
* (feature) Edit txt/csv export templates.
* (feature) Task categories. Filterable task creation.
* (bug) Fix txt/csv level1 export.
* (bug) Fix sync directories.
* (bug) Task settings serialization fix.
* (feature) Help buttons.
* (bug) Fix time bounds log.

###v4.3.8:
* (feature) BarChart (history mode).
* (bug) Excel export boost.
* (bug) CSV storage fix.
* (bug) Storage. Fix delete range
* (bug) Auto save config fix.
* (bug) Help url fix.
* (feature) Storage. Date cache bin->txt format.
* (bug) CSV storage. Fix save NewsMessage.SecurityId.
* (bug) CSV storage. Fix append data with same time for order log and tick trades.
* (feature) Edit Security.Decimals.

###v4.3.7:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/95

###v4.3.6:
* (bug) Hydra. Continuous security fixes.
* (bug) CSV candle storage fix.
* (bug) Rithmic plugin fix.

###v4.3.5:
* (bug) Quik plugin fix.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/79

###v4.3.4:
* (bug) UX lookup fix.
* (feature) Micex sec cache update.
* (feature) BitStamp. Level1 refresh interval is 10 sec.
* (feature) Transaq. UTC

###v4.3.2:
* (bug) Hydra. Btce and BitStamp anonymous access fix.
* (bug) Hydra. Ref xNet fix.
* (bug) Hydra. Price step icon fix.

###v4.3.1:
* (bug) Fix http://stocksharp.com/forum/yaf_postst5619_Oshibka-nie-udalos--naiti-chast--puti.aspx
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/62
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/63
* (feature) Quik plugin. Lua settings.
* (feature) Plaza. Spectra 4 (ASTS).
* (bug) Fix CSV storage
* (bug) Fix http://stocksharp.com/forum/yaf_postst5562_Nie-eksportiruietsia-data-pri-eksportie-sdielok-v-txt-excel.aspx

###v4.2.71:
* (bug) Fix Sterling plugin
* (bug) Fix http://stocksharp.com/forum/yaf_postst5556_S--Api.aspx
* (bug) Fix Rts plugin

###v4.2.70
* (feature) Sterling plugin.
* (feature) Quandl plugin.
* (feature) Micex. OrderBookDepth and MicexLogLevel.
* (feature) OpenECry. Uuid
* (feature) Support execution storage.
* (bug) Fix display second timeframes.
* (feature) Log level for historical sources.
* (feature) Zero or negative prices for spreads.
* (bug) Import pane fixes.
* (bug) Connector's candle processing fixes.
* (bug) Fix Quik, Micex, Plaza, Yahoo, Oanda, OpenECry, DukasCopy, GainCapital, MBTrading, TrueFX, RTS, UX plugins.
* (bug) Fix http://stocksharp.com/forum/yaf_postsm34362_S--Data---biesplatnaia-proghramma-zaghruzki-i-khranieniie-rynochnykh-dannykh.aspx#post34362

###v4.1.10-4.2.50
Available on [forum](http://stocksharp.com/forum/yaf_postst2541_S--Data---biesplatnaia-proghramma-zaghruzki-i-khranieniie-rynochnykh-dannykh.aspx)