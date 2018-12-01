StockSharp Designer Change log
========================
## v4.4.15:
* (feature) LiveCoin. Candles support.
* (feature) Position. SettlementPrice added.
* (feature) OKEx. V3 protocol supported.
* (feature) Bitmex. Stop orders extended.
* (feature) OpenECry. v3.5.14.53
* (bug) Bitmex. ExecInst fixes.
* (bug) Candles. Fix duplicate candles subscription.
* (bug) Market depth. Fix build depths from OL and L1.
* (feature) MT4, MT5 connectors.
* (bug) Remove live strategies fixes.
* (bug) Remove area from chart fixes.
* (bug) Remove items from solution tree fixes.
* (feature) Saving and restoring order for items.
* (bug) Clone source code element fixes.
* (feature) Candle series settings added to diagram element.
* (bug) Wrong element names fixes.
* (feature) Add breaks to multiple elements.
* (feature) Do not show breaks without settings.

## v4.4.14:
* (feature) Chart. Multi axes supported.
* (bug) Finam downloading fix.
* (feature) Status bar redesign.
* (bug) Close live tab fix.
* (bug) Index securities edit fix.
* (feature) Volume continous securit.
* (bug) Many bug fixes.

## v4.4.12:
* (bug) Fix storage format select.
* (bug) Fix cross-thread UI issues.
* (bug) Fix spashscreen info.
* (bug) Charting. Buy/Sell annotation theme fix.
* (feature) Orders panel. Conditional orders joined with regular orders.
* (bug) SecurityGrid. Do not show errors in security grid.
* (feature) Security lookup. Reset button to exchange board editor added.
* (bug) InteractiveBrokers. Fix expiry time parsing fix.
* (bug) InteractiveBrokers. Market depth fix.
* (feature) Index builder. Ignore errors as parameters.
* (feature) Save/load portfolio-connection mapping.
* (bug) Fix conditional orders save.
* (feature) Tooltip for pallete and solution tree added.
* (bug) Fix download history enable/disable.
* (bug) MarketDepthTruncateDiagramElement. Description fix.
* (feature) Live. Show only one button (start or stop).

## v4.4.11:
* (bug) Many live trading fixes.
* (feature) Offline. Support replace for pending orders.
* (bug) InteractiveBrokers. End date for candles request fix https://stocksharp.ru/posts/m/43390/.
* (bug) InteractiveBrokers. SecurityLookup error response handling fix.
* (bug) InteractiveBrokers. Candles request fix.
* (feature) Http -> Https.
* (feature) Portfolio pickup option.
* (bug) Binance, Coinbase, Bitfinex, Bitstamp, IQFeed fixes.
* (feature) SmartCOM. V4 as default.
* (bug) Transaq. Fix locked file issue.
* (feature) Compress candles from smaller time-frames.
* (bug) PnF candles store fixes.
* (bug) Grids. Fix filters for enum based fields.
* (bug) ConnectorWindow. Fix connector description for Quik lua.
* (feature) FIX connector. IssueDate, IssueSize translation support.
* (bug) FIX connector. SpectraFixDialect. Order mass cancel fix.
* (bug) OrderGrid. Active filters fix.
* (feature) MarketEmulator. CheckMoney option.
* (feature) Plaza. v5.3.6
* (feature) Micex. Stock30, Currency28, Currency30 interfaces.
* (bug) Transaq. Fix shared dll initialization https://stocksharp.ru/forum/9421/podklyuchenie-sdata-k-tranzak-/
* (bug) Fix Finam 1day candles downloading.
* (feature) Index and continuous securities support.
* (feature) Themes. Icons auto coloring.
* (feature) CandleSeries.IsRegularTradingHours.
* (bug) Logs. Designed fix.
* (feature) FIX connector. SSL support.
* (feature) FIX connector. QUIK FIX PreRrade and DukasCopy support.
* (bug) MarketDataGrid. Fix further refreshes after error request.
* (feature) Default theme changed.
* (bug) Grids. Time zone column fix.
* (bug) Chart icons color fixes.
* (feature) MACD histogram. Draw signal and macd lines.
* (feature) Snapshot data refactoring.
* (bug) Quik. Fix portfolio receive fix.
* (bug) Image publish cancellation processing fix.
* (feature) QuikLua. Handle From To date range for market data requests. https://stocksharp.ru/forum/9460/korrektnoe-otobrazhenie-svechei/
* (feature) StrategiesDashboard. Set security and portfolio directly. Allow trading and last error columns.
* (bug) Charting. Fix Equity legend color.
* (feature) Diagram. MarketDepthTruncateDiagramElement.
* (bug) TimeSpanEditor. Fix DevExpress themes.
* (feature) Default implementation of order log-> order book builder.
* (feature) Bittrex. Web sockets supported.
* (bug) Charting. Fix drawing trades with string id.
* (feature) OrderGrid, ExecutionGrid, TradeGrid, MyTradeGrid. Sides coloring.
* (feature) SecurityGrid. BuyBackDate, BuyBackPrice columns.
* (bug) ConnectorWindow. Fix stub connectors check.
* (feature) ExecutionGrid. OriginSide coloring.
* (feature) Crypto connectors. BalanceCheckInterval for refresh an account balances in case of deposit and withdraw operation.
* (feature) Huobi. Support HADAX.
* (feature) FIX connector. Time format parsing settings as public.
* (bug) Transaction snapshot. Fix trade data save.
* (feature) Binary format by default.
* (feature) Default theme is VS2017Dark.
* (feature) OpenECry. v3.5.14.41
* (feature) FAST dialects. Made network settings configurable.
* (feature) InteractiveBrokers. v9.73.07
* (feature) InteractiveBrokers. SSL support.
* (bug) Fix securities lookup all processing.
* (feature) CurrencyTypes. DEM, LUF.
* (feature) ExchangeBoard. Globex board info added.
* (bug) Candle building from smaller tf. Fix process multiples timeframes.

## v4.4.6.1:
* (bug) Option position chart. Legend binding fix.
* (bug) Options charts theme binding fix.
* (feature) Auto and manual select candles series for indicators.
* (feature) Autoconfig turned off by default.
* (bug) Connector window. Show missed column names.
* (bug) File progress window. Closing fix.
* (bug) Alerts loading fix.
* (bug) StochasticOscillator draw fix.
* (bug) InteractiveBrokers. Time zone fix.
* (feature) Bitfinex, Okcoin. Track account subscriptions.
* (bug) FIX connector. Fix process unknown outgoing messages.
* (bug) Kraken. Signature calc fix.
* (bug) InteractiveBrokers. Historical data fix.
* (bug) Crypto. Market data loading in non EN culture fix.
* (bug) Localization fixes.

## v4.4.6:
* (bug) Fix drag n drop from palette http://stocksharp.com/forum/9268/Drag-and-Drop-S-Designer-error/
* (feature) Quik DDE turned off.
* (bug) Yahoo restored.
* (bug) IQFeed. Fix parse fundamental messages with empty exchange code.
* (feature) Level1 fields. Dividends, AfterSplit, BeforeSplit.
* (bug) Themes fix http://stocksharp.ru/forum/9257/v-gidre-i-v-dizainere-otsutstvuyut-biblioteki-devexpressxpfthemesvs2017/
* (feature) FIX connector. Process unknown transactions option.
* (feature) Embedded links of crypto connectors documentation.

## v4.4.5.4:
* (feature) Crypto connectors Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck updates.
* (feature) Source-stubs for Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, Kucoin, BITEXBOOK, CoinExchange.
* (bug) Offline mode. Cancel pending orders fix.
* (feature) Charting. Reset Y axis range on double click.
* (feature) Charting. Time zone settings for each axis.
* (feature) Charting. Draw lines on mouse down event.
* (bug) Charting. X axis scaling fix.
* (bug) Charting. Chart annotation editor related fixes.
* (bug) Charting. Tooltip fix for chart line display style.
* (feature) Charting. X0 style support.
* (feature) Crypto connectors. Withdraw support.
* (feature) Order. IsMargin property.
* (feature) Order. Slippage property.
* (feature) FIX connector. SSL support.
* (feature) FIX connector. Dukascopy support.
* (feature) ConnectorWindow. Lookup connections.
* (feature) Performance improved.
* (bug) Options fixes.
* (bug) Oanda, InteractiveBrokers, Transaq, IQFeed, BTCE, LMAX connectors fixes.
* (bug) Alerts fixes.
* (bug) Exchanges panel. Fix single instance.
* (bug) Order book fixes.
* (bug) Many bug fixes.

## v4.3.27:
* (feature) Breakpoints. Extended conditions.
* (feature) CSV storage. Performance boost.
* (bug) Fix symbol mapping http://stocksharp.ru/forum/8433/problema-podklyucheniya-k-quik-lua-v-designer-versii-43252/
* (bug) Double click on optimization iteration result.
* (bug) Sample candles strategy restart fixes.
* (feature) OrderGrid, ExecutionGrid, OrderWindow. Show IsMarketMaker property.
* (feature) Extended storages. Delete support.
* (feature) Security.Turnover.
* (feature) SmartCOM 4.
* (bug) FIX server. Fix crash while client's disconnecting.
* (bug) QuikLua. Fix authorization http://stocksharp.ru/forum/8432/problema-pri-podklyuchenii-k-quik-(lua)/
* (feature) QuikLua. Market maker support.
* (bug) FAST fixes.
* (bug) SpbEx fixes.
* (feature) QuikLua. Index securities support (O H L C values).
* (feature) QuantFEED.
* (bug) Alerts fixes.
* (feature) OrderLog panel.
* (feature) EquityChart, OptionPositionChart, OptionSmileChart. Support IPersistable.
* (bug) PortfolioGrid. Binding State fix.
* (feature) Process mass order cancel result.
* (feature) Sockets for order failed added.
* (bug) Wrong date format fixes.
* (bug) Copy/paste/load composition elements fixes.
* (bug) Ternar operator scheme fixes.
* (bug) News panel fixes.
* (bug) Share chart button fixes.
* (bug) Change candles style fixes.
* (bug) Show positions fixes.
* (bug) Suspend undomanager for live strategies fixes.
* (feature) Autonaming added for indexer, part, hedge, level1 elements.
* (bug) Reset chart fixes.
* (bug) Open result strategies fixes.
* (feature) Save BarManager and DXWindow settings.
* (feature) Shrink prices for order elements.

## v4.3.25.2:
* (feature) ServerCredentials. Save password for auto logon only.
* (feature) Finam. OriginSide support.
* (bug) Renko and PnF candles fix.
* (feature) Market data panel. Show Transaction data.
* (feature) ExchangeBoard. Currenex, Fxcm, CmeMini.
* (feature) Tables. Share image into Yandex.Disk.
* (feature) Oanda. REST 2.0 support.
* (feature) FXCM connector.
* (feature) Quik lua. Performance boost.
* (feature) Feedback window. Usability improvements.
* (feature) Depths. Show histogram for volumes.
* (feature) Connectors window. Enable/disable checkbox moved to grid row.
* (feature) Create indicators from C# embedded editor.
* (feature) DevExp 17.1 update.
* (feature) C# editor. Support C# 7.0.
* (feature) Save only subscribed market data.
* (feature) IndicatorPainterAttribute.
* (bug) Show all allowed adapters in conditional window.
* (bug) Quik stop order condition. Fix conditional window.
* (feature) Save only subscribed market data.
* (bug) Transaq fix.
* (feature) Logs. Clear messages from context menu.
* (bug) Minor live connection fixes.
* (bug) BTCE fixes.
* (feature) Symbol mapping manager.

## v4.3.24.1:
* (feature) CQG continuum.
* (feature) Securities. Custom sorting for extended info added.
* (feature) SpbEx connector (binary).
* (bug) LogicalConditionDiagramElement small fix.
* (bug) Time span editors mask changed.
* (bug) Fix closing without logon.
* (feature) Risk rules.
* (feature) Optmization. Multiple securities support.
* (bug) Create security fix.
* (feature) Show sockets option added.
* (feature) OrderMassCancelElement added.
* (bug) Multiple market depths fixes.
* (bug) Copy/paste fixes.
* (bug) EndPoint list in property grid fixes.
* (bug) SecurityIndexDiagramElement fix. http://stocksharp.ru/posts/m/39908/
* (bug) Close tabs on strategy removed fixes.
* (feature) Check for can remove securities.
* (feature) Simulator settings added.
* (bug) Save/load settings for alert schemas fixes.
* (feature) OrderRegisterDiagramElement. Conditional settings added.

## v4.3.24:
* (bug) Fix splash screen http://stocksharp.ru/posts/m/39084/
* (feature) Option basic strategy.
* (bug) Fix grouped market depth. http://stocksharp.ru/posts/m/38969/
* (feature) Multi external sockets for C# strategy. 
* (bug) Fix storage window layout http://stocksharp.ru/posts/m/39137/
* (bug) Partial fix small tf candles http://stocksharp.ru/posts/m/39176/
* (feature) Storage format and data type added into ribbon http://stocksharp.ru/posts/m/39177/
* (bug) Gallery identifiers fix http://stocksharp.ru/posts/m/39180/
* (bug) Crash fix http://stocksharp.ru/posts/m/39181/
* (bug) Live trading many fixes.
* (bug) Auto connect fix.
* (bug) Reconnection fixes.
* (bug) InteractiveBrokers fix.
* (bug) Commission rules fixes.
* (feature) Variable. Auto set output type, auto set name http://stocksharp.ru/posts/m/38967/
* (feature) Auto generate schema for code.
* (feature) Security mandatory fields http://stocksharp.ru/posts/m/38903/
* (feature) Live market data simulator.
* (feature) Show connection for selected portfolio http://stocksharp.ru/posts/m/39193/ 
* (feature) Boards panel.
* (feature) Documentation embedded links.
* (feature) Micex TEAP, Micex FIX. Market maker orders support.
* (bug) Quik lua. Duration and BuyBackDate fixes.
* (feature) Arca and BATS.
* (bug) Portfolios. Multiselect rows fixes.
* (bug) FinamHistorySource fix. http://stocksharp.ru/forum/8190/ne-zagruzhayutsya-dannye-s-finama/
* (bug) Transaq. Set TPlusLimits for positions.
* (bug) Transaq. Fix http://stocksharp.ru/forum/8098/ne-vidit-pozitsii-schet-edp-finam-transaq-connector/
* (bug) Micex TEAP. Equity pos fix.
* (bug) Transaq. Fix https://github.com/StockSharp/StockSharp/issues/288
* (bug) SmartCOM. PriceStep fixes.

## v4.3.19.4:
* (bug) FIX connection establish fix http://stocksharp.ru/posts/m/37571/
* (bug) x86 launch fix http://stocksharp.ru/posts/m/37572/

## v4.3.19.3:
* (bug) refs update fix.

## v4.3.19.2:
* (feature) C# code editor. Design elements and strategies via C#. http://stocksharp.ru/posts/m/37286/
* (feature) Embed into diagram ready to use dll strategies. http://stocksharp.ru/posts/m/37188/
* (feature) Index element. http://stocksharp.ru/posts/m/37459/
* (feature) Option "greeks", strikes filtering, delta-hedge elements. http://stocksharp.com/posts/m/37317/
* (feature) Market data panel + securities panel + finam panel - joined into single panel.
* (feature) Download securities + historical data via live connections.
* (feature) Live dashboard panel.
* (bug) Live connection fixes.
* (bug) Converter element fixes http://stocksharp.ru/posts/m/37153/
* (bug) Optimizer fixes.

## v4.3.17:
* (feature) Selection lines on hover http://stocksharp.ru/posts/m/37025/
* (bug) Fix candle settings editor http://stocksharp.ru/posts/m/36987/
* (feature) Autonaming some elements.
* (bug) Protective position element fix http://stocksharp.ru/posts/m/37055/
* (feature) Order cancel + order replace elements.
* (bug) Minor fixes and changes.
* (bug) Fix http://stocksharp.ru/posts/m/36990/
* (feature) Auto save http://stocksharp.ru/posts/m/37023/
* (feature) Break points for a whole element.
* (feature) Open pos element. Price socket hide/show http://stocksharp.ru/posts/m/37025/
* (feature) Show error element http://stocksharp.ru/posts/m/37044/

## v4.3.16.1:
* (feature) Optimizator.
* (feature) Strategy gallery.
* (feature) Import/export with optional encryption.
* (bug) Themes change fix.
* (bug) Finam downloading fix.
* (feature) SQLite -> csv.
* (feature) Live trading.
* (bug) Many minor fixes.

## v4.3.16:
* (feature) See next release.

## v4.3.14.2:
* (feature) New design.
* (feature) Finam history downloading.
* (feature) Chat embedded.

## v4.3.14:
* (feature) Available for download