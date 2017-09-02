StockSharp Designer Change log
========================
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