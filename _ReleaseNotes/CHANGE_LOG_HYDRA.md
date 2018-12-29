StockSharp Data (Hydra) Change log
========================
## v4.4.15:
* (feature) LiveCoin. Candles support.
* (feature) OKEx. V3 protocol supported.
* (feature) OpenECry. v3.5.14.53
* (bug) Candles. Fix duplicate candles subscription.
* (bug) Market depth. Fix build depths from OL and L1.
* (feature) MT4, MT5 connectors.
* (feature) Quandl. Lookup securities. Support newest protocol changes.
* (feature) MFD. Filter for options lookup.
* (feature) Hydra server. Board lookup support.
* (feature) AlfaDirect. v4 by default.
* (feature) LMAX. Download securities from several places.
* (feature) Server supported SecurityMapping requests.

## v4.4.14:
* (feature) MFD. Filter for options lookup.
* (feature) Quandl. Lookup securities. Support newest protocol changes.
* (feature) Erase data. All dates options.
* (feature) Basket security refactoring.
* (bug) Build candles from bid/ask fix.
* (feature) FixServer mode. User lookup support.
* (feature) Hydra server. Board lookup support.
* (feature) Status bar redesign.
* (bug) Fix From date for Connector's based tasks.
* (feature) Level1. CommissionMaker, CommissionTaker columns added.
* (bug) Minor sources fix.

## v4.4.12:
* (bug) Fix cross-thread UI issues.
* (bug) Fix spashscreen info.
* (feature) OpenECry. v3.5.14.41
* (feature) FAST dialects. Made network settings configurable.
* (feature) InteractiveBrokers. v9.73.07
* (feature) InteractiveBrokers. SSL support.
* (bug) Fix securities lookup all processing.
* (feature) CurrencyTypes. DEM, LUF.
* (feature) ExchangeBoard. Globex board info added.
* (bug) SecurityGrid. Do not show errors in security grid.
* (feature) Security lookup. Reset button to exchange board editor added.
* (bug) InteractiveBrokers. Fix expiry time parsing fix.
* (bug) InteractiveBrokers. Market depth fix.
* (feature) Index builder. Ignore errors as parameters.
* (bug) Fix begin date set for downloaded candles.
* (bug) Fix board info loading for index securities.
* (feature) FIX server. Skip unknown security requests.

## v4.4.11:
* (feature) Support dates range for ticks, order book and order log subscriptions.
* (feature) Yahoo. Intraday interval supported.
* (bug) Yahoo. Fix downloading history for futures.
* (bug) Bithumb. Ticks subscription fix.
* (bug) Image publish cancellation processing fix.
* (feature) QuikLua. Handle From To date range for market data requests. https://stocksharp.ru/forum/9460/korrektnoe-otobrazhenie-svechei/
* (feature) Bittrex. Web sockets supported.
* (feature) Orders, Executions, Trades, MyTrades. Sides coloring.
* (feature) Securities. BuyBackDate, BuyBackPrice columns.

## v4.4.10:
* (feature) Themes. Icons auto coloring.
* (feature) CandleSeries.IsRegularTradingHours.
* (bug) Logs. Designed fix.
* (feature) FIX connector. SSL support extended.
* (bug) MarketDataGrid. Fix further refreshes after error request.
* (feature) Default theme changed.
* (bug) Grids. Time zone column fix.
* (feature) Start and stop dates for separate security and data types.

## v4.4.9:
* (bug) Charting. RVI and Gator rendeding fixes.
* (bug) Themes fixes.
* (bug) Grids. Fix filters for enum based fields.
* (bug) FXCM live source fix.
* (bug) Themes fixes.
* (feature) Support board edit by FIX protocol.
* (feature) Modify securities by FIX connection.
* (feature) Plaza. v5.3.6
* (feature) Micex. Stock30, Currency28, Currency30 interfaces.
* (bug) Transaq. Fix shared dll initialization https://stocksharp.ru/forum/9421/podklyuchenie-sdata-k-tranzak-/

## v4.4.8:
* (feature) Task pane redesign.
* (feature) Candles build from support (mostly fro FX market).
* (bug) InteractiveBrokers. End date for candles request fix https://stocksharp.ru/posts/m/43390/.
* (bug) InteractiveBrokers. SecurityLookup error response handling fix.
* (bug) InteractiveBrokers. Candles request fix.
* (feature) Change limits while downloading a lot of data.
* (feature) Http -> Https.
* (feature) More stable for custom plugins errors.
* (feature) Order log. Show spread and tick charts.
* (feature) TimeZone auto conversion while exporting.

## v4.4.7:
* (feature) Compress candles from smaller time-frames.
* (bug) PnF candles store fixes.
* (bug) Csv importer. Fix candles processing.
* (bug) Binary storage. Fix local time save http://stocksharp.ru/forum/9296/isklyuchenie-pri-sohranenii-executionmessage
* (feature) Finam and MFD uses Pause interval to prevent ip ban.

## v4.4.6.2:
* (bug) Reconnect fix.
* (bug) Binance, Coinbase, Bitfinex, Bitstamp, IQFeed fixes.

## v4.4.6.1:
* (bug) File progress window. Closing fix.
* (bug) StochasticOscillator draw fix.
* (bug) InteractiveBrokers. Time zone fix.
* (feature) Bitfinex, Okcoin. Track account subscriptions.
* (bug) FIX connector. Fix process unknown outgoing messages.
* (bug) Kraken. Signature calc fix.
* (bug) InteractiveBrokers. Historical data fix.
* (bug) Crypto. Market data loading in non EN culture fix.
* (bug) Localization fixes.

## v4.4.6:
* (feature) Quik DDE turned off.
* (bug) Yahoo restored.
* (bug) IQFeed. Fix parse fundamental messages with empty exchange code.
* (feature) Level1 fields. Dividends, AfterSplit, BeforeSplit.
* (bug) Themes fix http://stocksharp.ru/forum/9257/v-gidre-i-v-dizainere-otsutstvuyut-biblioteki-devexpressxpfthemesvs2017/
* (feature) Embedded links of crypto connectors documentation.

## v4.4.5.4:
* (feature) Crypto connectors Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck updates.
* (feature) Source-stubs for Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, Kucoin, BITEXBOOK, CoinExchange.
* (feature) Heartbeat option for crypto connectors http://stocksharp.ru/forum/9089/hydra---zagruzka-dannyh-s-birzhki-kraken/
* (bug) Interval increased fix http://stocksharp.ru/forum/9082/hydra--vo-vremya-raboty-kripto-konnektorov-inogda-voznikaet-oshibka-kol-va-zaprosov-v-ed-vremeni/

## v4.4.5.2:
* (feature) Do not store empty Level1 and Pos change messages.
* (feature) Transaq. Logging settings.
* (feature) Connectors plugin. Heartbeat interval option.
* (feature) OrderLog. Security column added.
* (bug) Crypto connectors fixes.

## v4.4.5.1:
* (bug) PropertyGrid. Fix SecureString editor.
* (feature) Importing securities. More fields.
* (feature) Importing. Enum fields has default mapping.
* (feature) Importing. Field order.
* (bug) Csv importing. Fix quoted lines parsing.
* (feature) BitStamp. ClientId. int->string http://stocksharp.ru/posts/m/42535/
* (bug) Kraken. FIX NRE, ticks subscription.
* (bug) InteractiveBrokers. Greenwich time zone parsing fix.

## v4.4.5:
* (feature) Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck connectors.
* (feature) Show ALL checked for security lookup window.
* (feature) Check volume step.
* (bug) InteractiveBrokers. Resubscribe fix.
* (bug) Fix data bounds.
* (bug) Fix chart drawing stop while error occurred.
* (feature) Envelope painter.

## v4.4.3:
* (bug) FIX server. Fix security lookup. http://stocksharp.ru/forum/8874/problemy-podklyucheniya-k-hydra-cherez-fix-/
* (bug) Level1 binary storage. Fixes.
* (bug) Binary storage. Fix non adjust price steps.
* (bug) IQFeed. Symbol lookup fixes.
* (bug) Tick and quotes binary storage. Fix store highly fractional prices.
* (feature) Order log binary storage. Nullable volumes posibilites.
* (bug) Quotes storage. Fix zero and negative prices store.
* (bug) FIX server. Fix market data request for the specified dates.

## v4.4.2:
* (bug) InteractiveBrokers fixes.
* (bug) Quik. Subscribe/unsubscribe fixes.
* (feature) Binary quotes. Allow save bid > ask.
* (bug) Candles pane. Fix arg validation.

## v4.4.1:
* (bug) InteractiveBrokers fixes.
* (bug) LMAX fixes.
* (bug) Quandl lookup securities fixes.
* (bug) Hydra. Market depth export txt preview fix. http://stocksharp.ru/forum/8805/kak-vygruzit-istoriyu-po-instrumentu/
* (bug) Hydra. Fix Analytic pane.
* (feature) Hydra. BitStamp. ClientId
* (feature) Lookup securities panel redesign.
* (feature) Security. UnderlyingSecurityType, IssueDate and IssueSize.
* (feature) OptionDesk. Show expiration date columns.

## v4.4.0:
* (bug) OptionDesk fixes.
* (bug) Finam history. Fix security lookup. http://stocksharp.ru/posts/m/41218/
* (bug) FIX server. Security expiration date fix http://stocksharp.ru/forum/8703/data-ehkspiratsii-optsionov-na-forts/
* (bug) IQFeed. Level1 fix.
* (bug) Oanda. Fix large candle's range request.
* (bug) InteractiveBrokers. Market data fixes.
* (bug) CandleMessage.State init fix.
* (bug) Fix DepthPane with big interval set.
* (bug) Position changes track fix.
* (bug) MFD. Lookup fixes.
* (bug) TrayIcon fixes.
* (bug) Fix loading TaskPane for non exist task.

## v4.3.28:
* (bug) BitStamp. Market data fix.
* (bug) FIX protocol. Exante market-data fix.
* (feature) Binary storage. Support non adjust prices for order book and level1.
* (bug) FIX connector. Position average price receive fix.
* (bug) FIX connector. Order book gathering fix.
* (bug) FIX connector. Check input values while logon.
* (bug) QuikLua. Fix CurrentValue for money positions http://stocksharp.ru/posts/m/41082/
* (feature) IQFeed. Security file parsing into separate thread.
* (bug) BTCE. Fix security price step.
* (bug) Security create window fix.
* (feature) Storage. Turned off saving active candles.
* (bug) Storage. Fix filter first data.
* (bug) BTCE. Fix market-data only mode.
* (bug) IQFeed. Connection error handling fix.
* (feature) InteractiveBrokers. Support historical ticks.
* (bug) QuikLua. Fix candle states http://stocksharp.ru/posts/m/41144/
* (feature) Changed default drive. http://stocksharp.ru/posts/m/41138/
* (bug) Indicator pane fix.
* (feature) Save/Load main window state.
* (bug) Fix long time security downloading.
* (feature) IQFeed. Check memory size for security downloading.
* (feature) Reset settings option if database corrupted.
* (bug) Order log pane fix.
* (feature) IQFeed. SecurityLookupSupportTypes.SupportAll option for downloading instruments from site.
* (feature) Uses predefined SQLite dll (x64 version).

## v4.3.27.2:
* (feature) More tooltips.
* (feature) Connector validation settings.
* (bug) QuikLua. Candle downloading fix.
* (bug) SecurityGrid. ExpiryDate, Strike columns sorting fix.
* (feature) BitStamp. V2 protocol.
* (bug) Interactive Brokers. Fix historical market data request.
* (bug) SmartCom. Price step fixes.
* (feature) BTCE. Domain update.
* (bug) Positions pane open fix.
* (bug) Indicators pane. Fix Level1 indicator use.
* (bug) Fix single security selection.
* (bug) Big interval task fix.
* (bug) Convert task fixes.
* (bug) Alor candles fix.

## v4.3.27.1:
* (feature) Multiple securities support for data panes.
* (bug) FIX server. Fix depths subscription for ALL security.
* (feature) FIX connector. Candle state support.
* (feature) QuikLua. Send candle states.
* (bug) Fix security remove.
* (feature) Auto fix corrupted data.
* (bug) Level1 panel fix (default visible columns).
* (bug) News quick open pane fix.
* (feature) FXCM. Masked as x64 bit only.
* (bug) Expression fix http://stocksharp.ru/forum/8586/skleennye-fyuchersy-s-finama/
* (feature) Plaza. 5.3.1 support.
* (feature) Micex. Stock28 interface.
* (bug) Position binary storage fix.
* (feature) SmartCOM. Removed V2 support.
* (bug) Option desk minor fixes.
* (bug) Empty date bounds loading fix.
* (bug) AlfaDirect. Security lookup fixes.
* (feature) Support paired indicators.

## v4.3.27:
* (bug) QuikLua. Candles fix.
* (feature) Analytics refactoring.
* (bug) Build from composites fix.
* (feature) Add saving wait into securities downloading.
* (bug) FixServer. Fix processing real time subscriptions.
* (bug) Export task upper date bound fix.
* (feature) Save/load ribbon state.
* (feature) QuikLua. Ignore case for security lookup.
* (feature) Level1 storage. Support IssueSize, Duration, BuyBackPrice, BuyBackDate. http://stocksharp.ru/posts/m/40757/
* (bug) Level1CsvSerializer fix.

## v4.3.26.4:
* (bug) Index build fix.
* (bug) Select drive window. Fix OK enable.
* (bug) SecurityPickerButton. Fix content with special characters.
* (bug) Indicator pane fixes.
* (bug) Remove security fix.

## v4.3.26.3:
* (feature) QuantFEED.
* (feature) Quik lua. Candle history support.
* (feature) RSS. Disabled security lookup.
* (bug) Indicators fixes + usability improved.

## v4.3.26.1:
* (feature) Finam. Ticks origin side as optional.
* (feature) Grids. Export fixes http://stocksharp.ru/posts/m/40578/

## v4.3.26:
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

## v4.3.25.1:
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

## v4.3.23.1:
* (feature) Server mode. Edit users.
* (feature) Remote storage. Manage server.
* (bug) Hydra. Database connection creation fix.
* (bug) Database export fix.
* (bug) Finam. Small fix.

## v4.3.23:
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

## v4.3.22.1:
* (bug) Remove security fix.
* (bug) Fix http://stocksharp.ru/forum/8158/gidra-padaet-pri-nastroike-micex-teap-i-drugie-bagi/
* (bug) Fix synchronization http://stocksharp.ru/posts/m/39516/
* (bug) Fix credentials window (OK btn enable).
* (bug) Fix InteractiveBrokers source.

## v4.3.22:
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

## v4.3.20.1:
* (bug) Hydra freezed while start up. http://stocksharp.ru/posts/m/39082/
* (bug) Splash screen fix http://stocksharp.ru/posts/m/39083/
* (bug) Hydra. Show credentials fix.

## v4.3.20:
* (feature) Import pane refactoring.
* (bug) Fix http://stocksharp.ru/forum/7013/oshibka-sohraneniya-tikovyh-dannyh-s-finama-v-formate-bin/
* (feature) Options desk.
* (feature) Options. Volatility smile chart.
* (feature) Server mode. Users panel.

## v4.3.19.5:
* (bug) http://stocksharp.ru/posts/m/37591/

## v4.3.19.4:
* (bug) FIX connection establish fix http://stocksharp.ru/posts/m/37571/
* (bug) x86 launch fix http://stocksharp.ru/posts/m/37572/

## v4.3.19.3:
* (bug) refs update fix.

## v4.3.19.2:
* (feature) Connector tasks. Historical ticks refactoring.
* (feature) Security extended info
* (feature) IQFeed. Protocol 5.2 support.
* (bug) ExportTask. Candles fix.

## v4.3.19.1:
* (bug) Backward settings compatibility fix.
* (bug) Yahoo lookup fix.
* (bug) Transaq, SmartCOM candles downloading fix.
* (bug) TrueFX downloading fix.

## v4.3.19:
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

## v4.3.13:
* (bug) DB export fix.
* (bug) http://stocksharp.com/forum/yaf_postst5740_oshibka-pri-eksportie-dannykh-v-sqlite.aspx

## v4.3.11:
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

## v4.3.10:
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

## v4.3.9.1:
* (feature) Hydra. Import pane improve.
* (bug) Release 4.3.9 fix (missed files).

## v4.3.9:
* (feature) Backup plugin (Amazon S3 cloud storage)
* (feature) Txt/csv export preview window.
* (feature) Edit txt/csv export templates.
* (feature) Task categories. Filterable task creation.
* (bug) Fix txt/csv level1 export.
* (bug) Fix sync directories.
* (bug) Task settings serialization fix.
* (feature) Help buttons.
* (bug) Fix time bounds log.

## v4.3.8:
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

## v4.3.7:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/95

## v4.3.6:
* (bug) Hydra. Continuous security fixes.
* (bug) CSV candle storage fix.
* (bug) Rithmic plugin fix.

## v4.3.5:
* (bug) Quik plugin fix.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/79

## v4.3.4:
* (bug) UX lookup fix.
* (feature) Micex sec cache update.
* (feature) BitStamp. Level1 refresh interval is 10 sec.
* (feature) Transaq. UTC

## v4.3.2:
* (bug) Hydra. Btce and BitStamp anonymous access fix.
* (bug) Hydra. Ref xNet fix.
* (bug) Hydra. Price step icon fix.

## v4.3.1:
* (bug) Fix http://stocksharp.com/forum/yaf_postst5619_Oshibka-nie-udalos--naiti-chast--puti.aspx
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/62
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/63
* (feature) Quik plugin. Lua settings.
* (feature) Plaza. Spectra 4 (ASTS).
* (bug) Fix CSV storage
* (bug) Fix http://stocksharp.com/forum/yaf_postst5562_Nie-eksportiruietsia-data-pri-eksportie-sdielok-v-txt-excel.aspx

## v4.2.71:
* (bug) Fix Sterling plugin
* (bug) Fix http://stocksharp.com/forum/yaf_postst5556_S--Api.aspx
* (bug) Fix Rts plugin

## v4.2.70:
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

## v4.1.10-4.2.50:
Available on [forum](http://stocksharp.com/forum/yaf_postst2541_S--Data---biesplatnaia-proghramma-zaghruzki-i-khranieniie-rynochnykh-dannykh.aspx)