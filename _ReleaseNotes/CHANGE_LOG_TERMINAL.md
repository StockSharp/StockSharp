StockSharp Terminal Change log
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
* (feature) Terminal. Save and load annotations for Chart.

## v4.4.12:
* (bug) Fix storage format select.
* (bug) Fix cross-thread UI issues.
* (bug) Fix spashscreen info.
* (bug) Charting. Buy/Sell annotation theme fix.
* (feature) Portfolios panel. Provide withdraw operation.
* (feature) Orders panel. Conditional orders joined with regular orders.
* (feature) Crypto connectors. BalanceCheckInterval for refresh an account balances in case of deposit and withdraw operation.
* (bug) OrderGrid. Cancel group orders fix.
* (feature) OpenECry. v3.5.14.41
* (feature) FAST dialects. Made network settings configurable.
* (feature) InteractiveBrokers. v9.73.07
* (feature) InteractiveBrokers. SSL support.
* (bug) Fix securities lookup all processing.
* (feature) CurrencyTypes. DEM, LUF.
* (feature) ExchangeBoard. Globex board info added.
* (bug) Fix process multiples timeframes.
* (bug) Transactions snapshot storage. Fix orders and trades save.
* (bug) SecurityGrid. Do not show errors in security grid.
* (feature) Security lookup. Reset button to exchange board editor added.
* (bug) InteractiveBrokers. Fix expiry time parsing fix.
* (bug) InteractiveBrokers. Market depth fix.
* (feature) Index builder. Ignore errors as parameters.
* (feature) Save/load portfolio-connection mapping.
* (bug) Fix conditional orders save.

## v4.4.11:
* (bug) Quik. Fix portfolio receive fix.
* (bug) Image publish cancellation processing fix.
* (feature) QuikLua. Handle From To date range for market data requests. https://stocksharp.ru/forum/9460/korrektnoe-otobrazhenie-svechei/
* (bug) Charting. Fix Equity legend color.
* (bug) TimeSpanEditor. Fix DevExpress themes.
* (feature) Default implementation of order log-> order book builder.
* (feature) Bittrex. Web sockets supported.
* (bug) Charting. Fix drawing trades with string id.
* (feature) OrderGrid, ExecutionGrid, TradeGrid, MyTradeGrid. Sides coloring.
* (feature) SecurityGrid. BuyBackDate, BuyBackPrice columns.
* (bug) ConnectorWindow. Fix stub connectors check.
* (feature) IConnector. Method RegisterXXX accepts From and To range, build from option.
* (feature) ExecutionGrid. OriginSide coloring.
* (feature) Crypto connectors. BalanceCheckInterval for refresh an account balances in case of deposit and withdraw operation.
* (bug) Fix duplicate candles subscriptions.
* (feature) Huobi. Support HADAX.
* (feature) FIX connector. Time format parsing settings as public.
* (bug) Snapshot. Fix trade data save.
* (feature) Orders modify. Disable security and portfolio.
* (feature) Binary format by default.
* (feature) Default theme is VS2017Dark.

## v4.4.10:
* (feature) Themes. Icons auto coloring.
* (feature) CandleSeries.IsRegularTradingHours.
* (feature) Index and continuous securities support.
* (feature) Remote storage support.
* (bug) Logs. Designed fix.
* (feature) FIX connector. SSL support extended.
* (feature) FIX connector. QUIK FIX PreRrade support.
* (bug) MarketDataGrid. Fix further refreshes after error request.
* (bug) Chart. Fix Load indicators for deleted candles series.
* (feature) Default theme changed.
* (bug) Grids. Time zone column fix.
* (bug) Chart icons color fixes.
* (bug) Chart indicators change settings fix.
* (feature) Alerts panel.
* (feature) Chartings. Indicators settings while creation.
* (feature) MACD histogram. Draw signal and macd lines.
* (feature) Snapshot data refactoring.

## v4.4.9:
* (feature) Emulator support.
* (feature) Risk management.
* (bug) Charting. RVI and Gator rendeding fixes.
* (bug) Charting. Order moving fix.
* (bug) Themes fixes.
* (bug) Grids. Fix filters for enum based fields.
* (feature) Security storage. Forced updates for manual modified data only.
* (feature) FIX connector. IssueDate, IssueSize translation support.
* (bug) FIX connector. SpectraFixDialect. Order mass cancel fix.
* (bug) OrderGrid. Active filters fix.
* (feature) MarketEmulator. CheckMoney option.
* (bug) ConnectorWindow. Fix connector description for Quik lua.
* (feature) Plaza. v5.3.6
* (feature) Micex. Stock30, Currency28, Currency30 interfaces.
* (bug) Transaq. Fix shared dll initialization https://stocksharp.ru/forum/9421/podklyuchenie-sdata-k-tranzak-/

## v4.4.8:
* (feature) Charting. Auto select Security and Portfolio.
* (feature) Charting. Show non-charting orders on chart.
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
* (feature) Chartings. Series settings extended.
* (bug) Scalping market depth. Cancelling orders fix.
* (feature) Scalping market depth. Order registration by double-click.
* (feature) Scalping market depth. Sorting by price column only.
* (bug) Cluster profile build fix.
* (bug) Grids. Fix filters for enum based fields.

## v4.4.6.1:
* (bug) Option position chart. Legend binding fix.
* (bug) Option filter design fix.
* (bug) Options charts theme binding fix.
* (bug) Fix indicators build for multiple candle series.
* (feature) Auto and manual select candles series for indicators.
* (feature) Autoconfig turned off by default.
* (bug) Connector window. Show missed column names.
* (bug) File progress window. Closing fix.
* (bug) Edit security button enable fix.
* (bug) Show existing market data fix.
* (bug) Alerts loading fix.
* (bug) StochasticOscillator draw fix.
* (feature) Uses DateRangeWindow to set candle series From and To.
* (bug) Close and revert position fix.
* (bug) Orders conditional panel. Re register fix.
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

## v4.4.5.3:
* (feature) Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck connectors.
* (bug) Many bug fixes.

## v4.4.0:
* (feature) Performance improved.
* (bug) Options fixes.
* (bug) Oanda, InteractiveBrokers, Transaq, IQFeed, BTCE, LMAX connectors fixes.
* (bug) Alerts fixes.
* (bug) Exchanges panel. Fix single instance.
* (bug) Order book fixes.

## v4.3.27.1:
* (feature) UI redesign.
* (bug) Many bug fixes.

## v4.3.25.2:
* (feature) Available for download.