StockSharp API Change log
========================
###current:
* (feature) ExchangeBoard. Currenex.
* (feature) IndexCandleBuilder. TotalVolume as extended fields.
* (bug) SecurityCsvList. Save exchange board info.
* (bug) SecurityNativeIdMessageAdapter. Suspended messages fix.
* (bug) Samples. Order book fetching small fix.
* (feature) Plaza. Forecast IM support.
* (feature) ExtendedInfoStorageSelectWindow.
* (feature) SpbEx. AddressConfig refactoring. Xaml support.
* (feature) IQFeed. Level1 columns. Xaml support.
* (feature) Micex TEAP. Stock27 interface.
* (feature) Micex TEAP. Addresses design time fix.
* (bug) Plaza. Revisions save fix.
* (feature) Flat files. Fail over improved.
* (feature) StrategiesStatisticsPanel. ShowSecurity option.
* (bug) Plaza. Blocked money translation fix.
* (feature) IExternalCandleSource. Removed from connectors.
* (feature) Monitor. Clear command.
* (feature) SamplePlaza. Storage support.
* (bug) BufferMessageAdapter. FilterSubscription fix.
* (feature) File client. Cancel operation option.
* (feature) Security. CfiCode.
* (feature) FileProgressWindow.
* (feature) ServerCredentials. Save password for auto logon only.
* (bug) CandleBuilder. Fix High/Low volumes.
* (bug) RenkoCandleBuilder fix.
* (bug) PnFCandleBuilder fix.
* (feature) Grids. Share images.
* (feature) CandleMessage.IsFinished removed.
* (feature) CurrencyTypes.CNT.
* (feature) Transaq. Security id refactoring.
* (bug) SecurityGrid. Binding fixes.
* (bug) Alerts. Popup window. Icon quality dix.

###v4.3.24:
* (feature) Remote storage. Edit users.
* (feature) Remote storage. Manage server.
* (feature) Xaml. All XXXWindows's derived from DXWindow.
* (bug) Database export fix.
* (bug) FinamHistorySource. Small fix.
* (feature) CQG continuum.
* (feature) Order. IsMarketMaker property.
* (bug) HeartbeatMessageAdapter. Auto reconnect fix.
* (feature) Micex TEAP, Micex FIX. Market maker orders support.
* (bug) Quik lua. Fix Level1 translation after reconnect.
* (bug) Quik lua. Fix transaction status check.
* (feature) FIX connector. Dump performance boost.
* (bug) Quik lua. Duration and BuyBackDate fixes.
* (feature) CollectionHelper. TryGetAndRemove and TryPeek.
* (feature) TraderHelper.Filter. Overload for SecurityMessage.
* (bug) PortfolioGrid. Multiselect rows fixes.
* (feature) SPB Exchange connector.
* (feature) ExchangeBoard. Arca and BATS.
* (bug) WhenRegistered rule. Track matched order.
* (feature) WhenActivated rule. Removed.
* (feature) BaseGridControl. Items count added to group summary.
* (feature) SecurityGrid. Custom sorting for extended info added.
* (feature) Validation attributes added to Security.
* (feature) TimeSpanEditorAttribute added.

###v4.3.23:
* (feature) InteractiveBrokers. OptionParameters, Histogram and news story requests.
* (bug) BasketMessageAdapter and SubscriptionMessageAdapter. Fix derived MarketDataMessage types handling.
* (bug) Strategy.MyTrades fill fix.
* (feature) InteractiveBrokers. Soft Dollar support.
* (feature) Grid controls. Search columns by name.
* (feature) MarketRuleGrid.
* (feature) Strategy. Process child order for risk management.
* (feature) MessageConverterHelper.CreateRegisterMessage. SecurityId as optional.
* (bug) RiskRule. Title update fix.
* (feature) Connector.NewTrade. Raise when all data initialized.
* (feature) MarketEmulator. Cross trades. Failed -> Canceled.
* (bug) RiskManager.Load fix.
* (bug) InMemoryExchangeInfoProvider. Save data fixes.
* (feature) Strategy. Save/load pnl and risk settings.
* (feature) IRemoteStorage. UploadSecurities, BasketSecurity instances support.
* (feature) BasketSecurity. From/To serialized string.
* (feature) Risk rule. Logging support.
* (feature) Market rule WhenChanged for Portfolio.
* (feature) IExchangeInfoProvider. Delete board and exchanges.
* (feature) ISecurityStorage. DeleteById extension method.
* (feature) IRemoteStorage. Exchanges and boards support. Delete securities, exchanges and boards support.
* (bug) FIX server. http://stocksharp.ru/posts/m/39529/ Fix ObjectDisposedException error handling.
* (feature) RemoteStorage. Extended security info.
* (feature) CandleBuilder. Uses messages.
* (feature) CandleHelper. ToCandles. Only formed option.
* (feature) ExchangeBoard. Serialization fixes (WCF and Xml).
* (feature) Storage. CandleBinarySerializer. Support big time frames.
* (feature) Storage. Binary format. Tick time precision.
* (feature) LogControl. Like filter.
* (bug) FinamHistorySource fix. http://stocksharp.ru/forum/8190/ne-zagruzhayutsya-dannye-s-finama/
* (feature) IRemoteAuthorization. Moved from Algo.History -> Algo.
* (bug) RiskPanel and CommissionPanel. Refresh Title fix.

###v4.3.22:
* (bug) SampleOptionsQuoting fix.
* (bug) Transaq. Demo address fix.
* (bug) ContinuousSecurityWindow fixes.
* (feature) Connector. Removed BasketSecurity support.
* (feature) BasketSecuriry. Store internally SecurityId instead of Security instance.
* (bug) TraderHelper.GetFortsJumps fix.
* (bug) CsvEntityList. String key normalization.
* (feature) CollectionSecurityProvider. Track notifiable collections.
* (feature) IndexSecurity. Removed Board initialization.
* (feature) IEntityRegistry. Removed Orders, MyTrades, Trades, OrderFails and News properties.
* (bug) Alerts. Loc fixes.
* (bug) ExpressionFormula. Parsing fixes.
* (feature) SecurityCreateWindow, ContinuousSecurityWindow. Uses ISecurityStorage.
* (feature) SecurityIdTextBox. Removed Security property.
* (bug) Transaq. Set TPlusLimits for positions.
* (bug) Transaq. Fix http://stocksharp.ru/forum/8098/ne-vidit-pozitsii-schet-edp-finam-transaq-connector/
* (bug) Micex TEAP. Equity pos fix.
* (bug) Transaq. Fix https://github.com/StockSharp/StockSharp/issues/288
* (bug) SecurityJumpsEditor fixes.
* (bug) SmartCOM. PriceStep fixes.
* (feature) ISecurityDownloader. Removed INativeIdStorage and IExchangeBoardProvider.
* (feature) Candle storage. Non aligned price support.
* (feature) ContinuousSecurityMarketDataStorage and IndexSecurityMarketDataStorage.
* (bug) CandleMessage. Clone fix.
* (feature) IndexSecurity. Values rounding.
* (feature) INotificationService.SendMessage. Attachments.
* (feature) CandleSettingsEditor. Removed fixes width.
* (feature) ISecurityStorage. Removed GetSecurityIds
* (feature) ExpressionFormula.Functions

###v4.3.21:
* (feature) IConnector.OrderStatusFailed
* (bug) CQG fixes.
* (bug) InteractiveBrokers fixes.
* (feature) SampleIB. Options, Scanner.
* (feature) Plaza. v5.3
* (feature) Twime. V2.1
* (feature) FIX connector. Dialect refactoring.
* (feature) FixServer. Passwords. String -> SecureString.
* (bug) Quik lua. Fix http://stocksharp.ru/forum/7023/problema-s-opredelenie-sostoyaniya-instrumenta/
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/285
* (feature) AboutWindow
* (bug) HistoryEmulationConnector. Process candle time fixes. https://github.com/StockSharp/StockSharp/issues/283
* (bug) Commission rules fixes.
* (bug) Fix http://stocksharp.ru/forum/7015/stocksharpxamlcharting-shablony-securitygrid-oshibka-v-kolonkah/
* (feature) MyTradeGrid. PnL and Position columns.
* (feature) OptionDesk control refactoring.
* (bug) Oanda. Transaction fixes.
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/282
* (feature) Strategy. NewMyTrades -> NewMyTrade
* (feature) MyTradeGrid. UserOrderId column.
* (bug) ConnectorWindow. Fix ToolbarImageStyle resource not found issue.
* (feature) StrategiesDashboard
* (feature) SampleStrategies
* (feature) Plaza. Nanoseconds support.
* (feature) TraderHelper. IsSystem -> IsPlazaSystem
* (feature) ICandleBuilderSourceValue. Volume is nullable.
* (feature) Candles. Removed ConvertableCandleBuilderSource.
* (feature) Ecng. Removed Xceed ref.
* (feature) Currency conversion. Moved to Messages.
* (feature) SecurityCreateWindow. Multi securities edit mode.
* (feature) OrderStatusMessage. From and To params.
* (feature) SampleFIX. Candles support.
* (bug) FIX server. Candle subscription fixes.
* (bug) FIX server. Invalid authorization handling fix.
* (bug) FIX server. Security list error response fix.
* (bug) PortfolioGrid refresh fix.
* (feature) FIX connector. Candles support.
* (bug) CandleSettingsEditor fix. https://github.com/StockSharp/StockSharp/issues/290
* (bug) Fix http://stocksharp.ru/forum/8058/problemy-s-sootvetstviem-neskolkih-kodov-klienta-i-torgovogo-scheta-v-kvike/

###v4.3.19.5:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/279

###v4.3.19.4:
* (bug) FIX connection establish fix http://stocksharp.ru/posts/m/37571/

###v4.3.19.2:
* (feature) SecurityPicker performance improve.
* (feature) OptionVolatilitySmileChart.
* (bug) OptionDesk fix.
* (bug) Candle storage fix.
* (bug) TextExporter fix.
* (feature) IExtendedInfoStorage.
* (feature) IQFeed. Protocol 5.2 support.
* (bug) SubscriptionMessageAdapter fix.
* (feature) MarketDataFinishedMessage.
* (feature) WhenNewTrades -> WhenNewTrade
* (feature) NativeIdStorage. Algo -> Algo.Storages
* (bug) MarketDataStorage fix.
* (bug) StorageMessageAdapter. Process MarketDataMessage fixes.
* (feature) IMessageAdapter. IsNativeIdentifiersPersistable
* (feature) BasketMessageAdapter. Send MarketData message to specified adapter.
* (feature) SampleIQFeed. Refactoring.
* (bug) https://github.com/StockSharp/StockSharp/issues/278 fix

###v4.3.19.1:
* (bug) SmartCOM candles fix.
* (bug) OpenECry candles fix.
* (bug) Backtesting fix.
* (bug) SecurityNativeIdMessageAdapter. Clone fixes.
* (feature) IndexSecurity. IgnoreErrors.

###v4.3.19:
* (feature) IConnector. SubscribeMarketData and UnSubscribeMarketData. MarketDataTypes -> MarketDataMessage.
* (feature) ExchangeBoard. Removed IsSupportAtomicReRegister, IsSupportMarketOrders
* (feature) Plaza router. v5.1.3.754
* (feature) Plaza. Flood control handling.
* (bug) BarChart fixes.
* (bug) OpenECry fixes.
* (feature) OpenECryAddressComboBox. Prod address.
* (bug) SecurityTrie fix.
* (bug) Message.IsBack. Fix BasketMessageAdapter.
* (bug) CsvEntityRegistry fixes.
* (feature) OfflineMessageAdapter.
* (bug) MarketDepthControl. Own volume fix.
* (bug) Level1 csv storage fix.
* (feature) Transaction storage. LocalTime support.
* (feature) Chart. Support area style.
* (feature) IConnector. LookupSecuritiesResult, LookupPortfoliosResult - error argument added.
* (feature) SecurityLookupWindow
* (feature) NativeIdMessageAdapter
* (bug) SubscriptionMessageAdapter fix.
* (bug) SmartCOM fixes.
* (bug) LMAX fixes.
* (feature) ISecurityProvider.GetNativeId removed.
* (feature) OrderWindow, OrderConditionalWindow, SecurityJump - re-designed with DevExp.
* (bug) CandleCsvSerializer fix.
* (feature) IConnector. MarketDataUnSubscriptionSucceeded, MarketDataUnSubscriptionFailed
* (feature) Connector. IsRestorSubscriptioneOnReconnect
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/47
* (bug) QUIK. ALL subscription fix

###v4.3.18:
* (feature) Rithmic. v9.5.0.0
* (bug) Transaq. UseCredit fixes.
* (bug) Finam. New address.
* (bug) Chart. Active orders fix.
* (feature) CandlePriceLevel.TotalVolume
* (feature) XigniteHistorySource added.
* (feature) QuandlHistorySource. v3 API.
* (bug) Samples. DevExp refs fixes.
* (bug) Chart samples fixes.
* (bug) Build index candles.

###v4.3.17:
* (feature) Quik lua. Duration, IssueSize, BuyBackDate, BuyBackPrice.
* (bug) Quik lua. Fix ClientCode duplicate issue.
* (feature) Position.ClientCode
* (bug) Fix Oanda connector.
* (feature) TransaqAddresses.FinamHft
* (feature) Transaq. v2.20.13
* (bug) CSV storage. TimeSpan serialization fix.
* (bug) Fix http://stocksharp.ru/posts/m/36986/
* (bug) LoggingHelper. Fix xml doc.
* (feature) Xaml.Diagram update.
* (feature) TimeSpanEditor
* (feature) OrderCancelVolumeRequireTypes enum.
* (feature) MatLab connector update.

###v4.3.16.1:
* (feature) MicexDownloader + FortsDownloader -> MoexDownloader
* (feature) StockSharp.Xaml.Code

###v4.3.16:
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
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/260
* (feature) MarketRuleHelper.WhenTimeCome. Removed Security param
* (feature) OpenECry. v3.5.14.1
* (feature) OpenECry. Address changed to gainfutures domain.
* (feature) Micex TEAP. Currency26 interface.
* (feature) Micex TEAP. IgnoreCurrencies.
* (bug) PropertyGridEx. Nullable and unsigned number fixes.
* (bug) Xaml.Diagram minor fixes.

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