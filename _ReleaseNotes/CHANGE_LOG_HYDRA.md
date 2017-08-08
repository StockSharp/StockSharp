StockSharp Data (Hydra) Change log
========================
###current:
* (feature) QuantFEED.

###v4.3.26.1:
* (feature) Finam. Ticks origin side as optional.
* (feature) Grids. Export fixes http://stocksharp.ru/posts/m/40578/

###v4.3.26:
* (feature) Symbol mapping manager.
* (feature) FAST protocol plugin.
* (bug) BTCE fix.
* (bug) Fix index creation http://stocksharp.ru/forum/8409/v-gidre-pri-raschete-indeksa-vydaetsya-soobshshenie-method-must-have-a-return-type/
* (feature) Import data plugin.
* (bug) Quick pane navigation fix.
* (bug) Navigation pane layout fix.
* (feature) Indicator values building.
* (feature) Import position changes.
* (bug) Import level1 fix.
* (feature) Auto importing tool.
* (bug) Database export connections fixes.
* (feature) Erase data for positions and news.
* (feature) CSV importing. Extended fields for securities.
* (feature) Extended storages window. Fields info.
* (bug) Fix MFD http://stocksharp.ru/posts/m/40528/
* (bug) SpbEx fixes.
* (feature) QuikLua. Index securities support (O H L C values).
* (feature) Security.Turnover.

###v4.3.25.1:
* (feature) CQG continuum.
* (feature) Securities. Custom sorting for extended info added.
* (feature) SpbEx connector (binary).
* (feature) Security. CfiCode.
* (bug) Index create fix.
* (feature) ServerCredentials. Save password for auto logon only.
* (feature) FXCM source. Historical + real time.
* (feature) Documentation embedded links.
* (feature) Positions tracking.
* (feature) Depths. Show histogram for volumes.
* (feature) Depths and level1. Show spread chart.
* (feature) Oanda. REST 2.0 support.
* (feature) Ticks pane. Show tick chart.
* (feature) Feedback feature.
* (feature) Micex TEAP. Stock27 interface.
* (feature) Micex TEAP. Addresses design time fix.
* (feature) Security. CfiCode.
* (feature) DevExp 17.1 update.
* (bug) OrderLog -> Ticks fix.
* (feature) Finam. OriginSide support.
* (bug) Candles. Volume profile chart fix.
* (bug) Renko and PnF candles fix.
* (feature) Market data panel. Show Transaction data.
* (feature) ExchangeBoard. Currenex, Fxcm, CmeMini.
* (feature) Tables. Share image into Yandex.Disk.
* (bug) Binary storage. Fix local time saving.

###v4.3.23.1:
* (feature) Server mode. Edit users.
* (feature) Remote storage. Manage server.
* (bug) Hydra. Database connection creation fix.
* (bug) Database export fix.
* (bug) Finam. Small fix.

###v4.3.23:
* (bug) Fix http://stocksharp.ru/forum/8164/gidra-servernyi-rezhim-i-samplefix-nablyudeniya/
* (bug) Fix http://stocksharp.ru/posts/m/39529/
* (feature) Server mode. IP address restrictions.
* (feature) Option desk in Market data ribbon section.
* (feature) Extended FIX server settings.
* (feature) Server mode credentials. Custom permissions.
* (feature) Server mode. Exchanges and boards support. Delete securities, exchanges and boards support.
* (feature) Server mode. UploadSecurities. BasketSecurity instances support.
* (bug) UI localization fixes.
* (feature) RemoteStorage. Extended security info.
* (feature) LogControl. Like filter.
* (bug) Finam fix. http://stocksharp.ru/forum/8190/ne-zagruzhayutsya-dannye-s-finama/

###v4.3.22.1:
* (bug) Remove security fix.
* (bug) Fix http://stocksharp.ru/forum/8158/gidra-padaet-pri-nastroike-micex-teap-i-drugie-bagi/
* (bug) Fix synchronization http://stocksharp.ru/posts/m/39516/
* (bug) Fix credentials window (OK btn enable).
* (bug) Fix InteractiveBrokers source.

###v4.3.22:
* (feature) FIX server mode. Live and historical streaming.
* (feature) Analytics restored.
* (feature) IndexSecurity and ContinuousSecurity. Create, save, load, build data.
* (feature) OptionDesk export.
* (feature) Suggestions. Logs as attachments, app name + version.
* (feature) Save built data.
* (bug) ConvertTask. Settings fix.
* (bug) Filter data fix.
* (feature) Erase data. Data type.
* (feature) ExportTxtPreview. Do not show again. Templates removed from app.config
* (feature) Candles. Build from bid, ask, mid or last options.
* (feature) Candles. Bin format. Non aligned price support.

###v4.3.20.1:
* (bug) Hydra freezed while start up. http://stocksharp.ru/posts/m/39082/
* (bug) Splash screen fix http://stocksharp.ru/posts/m/39083/
* (bug) Hydra. Show credentials fix.

###v4.3.20:
* (feature) Import pane refactoring.
* (bug) Fix http://stocksharp.ru/forum/7013/oshibka-sohraneniya-tikovyh-dannyh-s-finama-v-formate-bin/
* (feature) Options desk.
* (feature) Options. Volatility smile chart.
* (feature) Server mode. Users panel.

###v4.3.19.5:
* (bug) http://stocksharp.ru/posts/m/37591/

###v4.3.19.4:
* (bug) FIX connection establish fix http://stocksharp.ru/posts/m/37571/
* (bug) x86 launch fix http://stocksharp.ru/posts/m/37572/

###v4.3.19.3:
* (bug) refs update fix.

###v4.3.19.2:
* (feature) Connector tasks. Historical ticks refactoring.
* (feature) Security extended info
* (feature) IQFeed. Protocol 5.2 support.
* (bug) ExportTask. Candles fix.

###v4.3.19.1:
* (bug) Backward settings compatibility fix.
* (bug) Yahoo lookup fix.
* (bug) Transaq, SmartCOM candles downloading fix.
* (bug) TrueFX downloading fix.

###v4.3.19:
* (feature) New design.
* (bug) Finam, Quandl, Google, QUIK, Transaq, OpenECry, Oanda fixes.
* (feature) Hydra. ExportTypes. Bin -> StockSharpBin + StockSharpCsv.
* (feature) Csv storage perf boost.
* (feature) Data type refactoring.
* (feature) Erase data for all drives.
* (feature) Candle day interval.
* (feature) Connector's tasks refactoring.
* (bug) Advertise fix.
* (bug) Hydra. Export task fix.
* (feature) Hydra. Show news counter.
* (feature) Xignite support.

###v4.3.13:
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

###v4.2.70:
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

###v4.1.10-4.2.50:
Available on [forum](http://stocksharp.com/forum/yaf_postst2541_S--Data---biesplatnaia-proghramma-zaghruzki-i-khranieniie-rynochnykh-dannykh.aspx)