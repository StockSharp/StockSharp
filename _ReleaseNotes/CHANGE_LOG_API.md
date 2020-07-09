StockSharp API Change log
========================
## v5.0.0:
* (feature) .NET 4.8 and .NET Core 3.1
* (feature) INativeIdStorage. Clear method added.
* (bug) FinamHistorySource. Fix https://stocksharp.ru/forum/10395/skachivanie-tikovyh-i-svechnyh-istoricheskih-dannyh-gidroi-s-finama/
* (bug) DiMinus, DiPlus, IchimokuChinkouLine, IchimokuLine, RelativeVigorIndexAverage, RelativeVigorIndexSignal excluded from indicators list.
* (bug) Highest, Lowest indicators fix.
* (bug) Vidya fix.
* (feature) CSV storage. Save/load portfolio commissions.
* (bug) TraderHelper. GetPriceStep fix.
* (feature) Bitfinex. Supported market data loading.
* (feature) Bitfinex. Cancel On Disconnect.
* (feature) Bitfinex. PostOnly, CloseOnly, ReduceOnly and OCO orders.
* (bug) Bitmex. Fix order replace https://stocksharp.com/forum/10419/BitMEX-invalid-argument-clOrdID/
* (feature) Bitmex. Testnet supported https://stocksharp.com/forum/10420/BitMEX-Testnet/
* (bug) BitZ, BitMax, Bibox fixes.
* (bug) TraderHelper. Fix Filter securities by Id.
* (feature) IConnector. Removed obsolete LookupSecurity.
* (bug) MarketEmulator. Fix canceled balanced processing.
* (bug) BW fixes.
* (feature) Kucoin. Protocol upgraded.
* (feature) Kucoin. Sandbox supported.
* (feature) FIX connector. SuperDerivatives, SwissQuote and XOpenHub dialects.
* (feature) NewsGrid. Show Board column.
* (bug) PortfolioEditor. SelectedPortfolio binding fixes.
* (bug) Monitor. Fix LogManager freezing https://stocksharp.ru/forum/10502/logmanagerdispose()/
* (feature) OptionPositionChart. Set own Model.
* (feature) Sterling. Updated to 11.7 version.
* (bug) InteractiveBrokers. Fix https://stocksharp.com/forum/10465/There-was-an-error-in-the-S-Shell-operation-strategy!/
* (feature) OrderConditionAttribute added.
* (feature) Tradier connector.
* (feature) TraderHelper. IsFinal for OrderStates extension.
* (feature) Chart. ChartActiveOrdersElement save/load supported https://stocksharp.ru/forum/10517/ne-sohranyaetsya-chartactiveorderselement/
* (bug) Chart. Fix ChartArea.Height change from code https://stocksharp.ru/forum/10504/ustanovka-vysoty-chartarea-(programmno)/
* (feature) Chart. ChartArea.Height changed notification added. https://stocksharp.ru/forum/10503/kak-otlovit-sobytie-izmeneniya-vysoty-chartaaria/
* (feature) DukasCopyHistorySource.CandlesBuildFrom added.
* (feature) FIX connector. IFixDialect.QuotesAsLevel1 added.
* (bug) FIX connector. 5.XXX logon fix.
* (feature) IAnalyticsChart refactoring.
* (feature) IStorageRegistry.GetSecurityStorage removed.
* (feature) Chart. Refactoring ChartActiveOrdersElement. Removed ChartActiveOrderInfo.
* (bug) SampleOanda. Removed level1, added order book.
* (feature) IEntityRegistry. Subscriptions storage added.
* (feature) EntityRegistry. Encapsulated old storage lists.
* (feature) IExchangeInfoProvider. Init method added.
* (feature) MarketDataMessage. CopyTo method added.
* (feature) MessageConverterHelper. MarketDataMessage -> CandleSeries conversion added.
* (feature) OpenECry. Certification passed.
* (feature) Oanda. Level1 support removed.
* (feature) ExchangesPanel, ExchangeBoardsPanel. IExchangeInfoProvider delay registration supported.
* (feature) SubscriptionPanel added.
* (feature) IConnector. SubscribeMarketData/UnSubscribeMarketData overloads without Security added.
* (feature) OrderTypes. Repo and Rps moved to IXXXOrderCondition
* (bug) TransactionBinarySnapshotSerializer fix.
* (feature) IMessageAdapter. CandlesBuildFrom property added.
* (bug) Envelope and MACD fixes.
* (feature) DukasCopy, FxcmHistory, GainCapital, MBTrading, TrueFX, Google, Yahoo, MFD, Finam, AlorHistory, RTS FTP, UX, Xignite refactored into connectors.
* (feature) MessageAdapter. SendOutMarketDataReply method added.
* (feature) ISecurityProvider. Lookup method uses SecurityLookupMessage.
* (feature) SecurityLookupWindow. CriteriaMessage
* (feature) ISecurityDownloader. Refresh method uses SecurityLookupMessage.
* (feature) ITransactionIdMessage, IServerTimeMessage, ISecurityIdMessage interfaces.
* (bug) RemoteStorage. Fix available data types translation.
* (feature) SecurityNativeIdMessageAdapter. Skip external native ids.
* (feature) Tradier. New urls + sandbox supported.
* (feature) BaseDumpableHistorySource removed.
* (feature) Remote storage. ExchangeBoard -> BoardMessage, Security -> SecurityMessage.
* (feature) NewsMessage. Url property changed type from Uri to String.
* (feature) SecurityLookupMessage. SecurityTypes property changed from IEnumerable to Array type.
* (bug) Messages. Serialization fixed.
* (feature) QuoteChangeMessage. Quotes properties types changed from IEnumerable to Array.
* (feature) IQFeedMarketDataMessageAdapter -> IQFeedMessageAdapter.
* (feature) RssMarketDataMessageAdapter -> RssMessageAdapter.
* (feature) Configuration. QUIK DDE, QUIK Trans2Quik excluded as obsolete.
* (feature) RemoteStorage. BE->Messages usage.
* (bug) SecurityLookupWindow. OK enabling fix.
* (bug) BuySellPanel. Design fix.
* (bug) Bitfinex. Timestamp for own trade fix.
* (feature) CsvImporter. Uses Message as output parameter.
* (feature) IMessageAdapter. Changed property types Array -> IEnumerable.
* (feature) DataType. ToMarketDataType conversion method added.
* (feature) CandleSeries <-> DataType conversion added.
* (bug) UI log controls. Fix Message.IsDispose processing.
* (feature) ImportSettings controls.
* (feature) CSV connector.
* (feature) MessageAdapter. IsSupportNativeSecurityLookup -> IsSupportSecurityLookupResult, IsSupportNativePortfolioLookup -> IsSupportPortfolioLookupResult.
* (bug) Charting. Fix active orders element after chart loading. https://stocksharp.ru/posts/m/47302/
* (feature) PartialDownloadMessageAdapter added.
* (feature) IMessageAdapter. Removed TimeFrames property.
* (feature) IMessageAdapter. IsAllDownloadingSupported method added.
* (bug) SecurityNativeIdMessageAdapter. Fix processing suspended incoming messages.
* (feature) Security. MinVolume, Shortable, UnderlyingSecuityMinVolume and FaceValue properties added.
* (bug) QuikLua. Position average price fix.
* (feature) SampleMultiConnection. Order log, historical ticks and news requests supported.
* (feature) Deribit. V2 protocol supported.
* (feature) BitStamp. V2 web sockets supported.
* (feature) FieldMapping. ZeroAsNull added.
* (feature) CandleMessageGrid added.
* (feature) FIX connector. LMAX dialect added.
* (feature) ConnectorWindow. Allow change connector's name.
* (feature) IMessageAdapter. PossibleSupportedMessages property added.
* (feature) Level1BinarySerializer. Support Dividend, MinVolume, UnderlyingMinVolume, SpreadMiddle, Commission, Splits.
* (feature) PositionBinarySerializer. Support Commissions, SettlementPrice.
* (feature) Emulator. Security.MinVolume supported.
* (feature) Kraken. WebSocket supported.
* (feature) Adapter and subscription messages added.
* (feature) CandleBuilderMessageAdapter. Forced Finish prev non-finished candles.
* (feature) OrderPairReplaceMessage. Removed SecurityMessage inheritance.
* (feature) OfflineMessageAdapter. Support OrderPairReplaceMessage processing.
* (feature) ISecurityAssociationStorage removed.
* (feature) BasketMessageAdapter. SecurityAdapterProvider added.
* (bug) CandleBuilder. Fill CandleMessage.TotalTicks.
* (feature) CandleSeries. IsFinished filter added.
* (feature) Configuration. Auto scan new adapters from local assemblies.
* (bug) CandleBinarySerializer. Fix diff time zone times.
* (bug) Storage. Fix binary more 1 days range.
* (feature) IMessageAdapter. GetCandleArgs added.
* (feature) IMarketDataDrive. GetAvailableDataTypes returns all types for all securities.
* (feature) HistoryMessageAdapter. SupportedMarketDataTypes overload uses storage data types.
* (bug) Connector. Fix order's fail processing.
* (feature) Diagram. IndicatorDiagramElement. IsFinal and IsFormed params added.
* (feature) Diagram. OrderRegisterDiagramElement. ZeroAsMarket param added.
* (feature) Diagram. Converter show DateTimeOffset properties.
* (feature) Diagram. Position, Strategy, StrategyTrades Strategy socket added.
* (feature) IProfileClient, IAuthenticationClient, IFileClient, IStrategyClient interfaces created.
* (feature) Backtesting. Check shortable position option added.
* (feature) BasketMessageAdapter. Extracted mapping storage into CsvSecurityMessageAdapterProvider, CsvPortfolioMessageAdapterProvider.
* (feature) BaseChartIndicatorPainter refactoring.
* (feature) CollectionSecurityProvider. Check input nullable values.
* (feature) IFileService. Share and UnShare operations added.
* (feature) CSV importing. Supported native system identifier importing.
* (feature) Security and portfolio route messages. Security mapping message.
* (feature) IndexEditor. Uses ISecurityProvider https://stocksharp.ru/posts/m/47693/
* (bug) ProxySettings fix https://stocksharp.ru/forum/10804/ne-rabotaet-soedinenie-cherez-proksi-server/
* (feature) Level1. Fill Security.IssueSize from level1.
* (feature) Order. MinVolume, AveragePrice, Yield properties added.
* (feature) CouponDate, CouponPeriod, CouponValue, MarketPriceToday, MarketPriceYesterday, YieldVWAP, YieldVWAPPrev, VWAPPrev fields added.
* (feature) QuikLua. Securities. FaceValue, Dividend, Duration, CouponDate, CouponPeriod, CouponValue, MarketPriceToday, MarketPriceYesterday, YieldVWAP, YieldVWAPPrev, VWAPPrev translation added.
* (feature) QuikLua. Orders. Yield, MinVolume, AveragePrice translation added.
* (feature) QuikLua. VC++ 2019 runtime usage.
* (feature) SecurityGrid. Dividend, Duration, CouponDate, CouponPeriod, CouponValue, MarketPriceToday, MarketPriceYesterday, YieldVWAP, YieldVWAPPrev, VWAPPrev columns added.
* (feature) OrderGrid. AveragePrice, MinVolume and Yield columns added.
* (feature) Unit. Parse case insensitive.
* (feature) RealTimeEmulationTrader. Uses IPortfolioProvider.
* (bug) RealTimeEmulationTrader. Fix subscriptions processing.
* (feature) MarketEmulator. OrderStatusMessage and PortfolioLookupMessage supported.
* (feature) FixServer. IFixServerTransactionIdStorage usage added.
* (feature) Diagram. Bring link to front on mouse over.
* (bug) Diagram. Update composition item names in palette fixes.
* (bug) StorageMessageAdapter. Fix processing offline cancel order requests.
* (feature) IMessageAdapter. IsSupportExecutionsPnL properties added.
* (feature) PnLMessageAdapter. Translates PortfolioChangeMessage.
* (feature) InteractiveBrokers. V9.76.01
* (bug) PnLMessageAdapter. Fix processing for empty portfolio name trades.
* (feature) IConnector. Moved market data members to IMarketDataProvider.
* (feature) Strategy implemented IMarketDataProvider interface.
* (feature) Strategy implemented ICandleManager interface.
* (feature) PnLManager. UseXXX options added.
* (feature) IConnector. Moved transactional members to ITransactionProvider interface.
* (feature) UnitHelper. Parse method for empty string return null for the specified option.
* (bug) Unit. Limit values comparison fix.
* (feature) Diagrams. ProtectPositionDiagramElement. More options added.
* (feature) Diagrams. DiagramSocketType for OrderState added.
* (feature) Diagrams. OrderBaseDiagramElement. Trigger for all order's diagram elements.
* (feature) Diagrams. Font weight set to bold.
* (bug) StrategiesDashboard. Fix CanExecute handling.
* (feature) BaseGridControl. Copy context menu added.
* (bug) ContinuousSecurityBaseProcessor. Fix SecurityId for generated messages.
* (feature) IMessageListener interface added.
* (feature) SoundLogListener, SpeechLogListener moved from Logging to Xaml.
* (feature) Configuration.Adapters project added.
* (feature) Configure method moved from Configuration to Xaml.
* (feature) LicensePanel. Moved from Licensing to Xaml.
* (bug) PortfolioPnLManager. Fix processing trades with string id.
* (feature) IMarketDataProvider. MarketDataSubscriptionFailed2, MarketDataUnSubscriptionFailed2 events added.
* (feature) Subscriptions. Interpret non supported and non exist subscriptions as warning.
* (feature) Strategy implemented ITransactionProvider interface.
* (feature) Portfolio. CreateSimulator method added.
* (feature) BuySellGrid. AddPanel, RemovePanel methods added.
* (feature) GuiConnector removed.
* (feature) SampleSync removed as obsolete.
* (feature) Connector. CandleSeriesError event added.
* (feature) IMarketDataProvider. Added adapter parameter to subscription methods.
* (feature) Micex TEAP. Stock32, Stock33, Stock34, Currency32, Currency33, Currency34 interfaces added.
* (feature) SecurityId. SecurityType marked as obsolete.
* (feature) QuikLua. Translates T+N money positions.
* (feature) FixServer. SecurityLookupMessage.SecurityTypes supported.
* (bug) FixServer. Fix SecurityStatusRequest handling.
* (feature) FixServer. Sends PortfolioLookupResultMessage.
* (feature) QUIK. 64 bit support.
* (feature) QUIK. Candles BuildFrom mode supported.
* (feature) QUIK. Terminal connection lost notification supported.
* (feature) IMarketDataDrive. Verify method added.
* (feature) DriveCache. Moved from Hydra to Algo.
* (feature) BatchEmulation. Accept storage drive and format.
* (feature) IMarketDataDrive. LookupSecurities method added.
* (feature) ITransactionProvider. MassOrderCanceled2, MassOrderCancelFailed2, OrderStatusFailed2 events added.
* (feature) DriveComboBox, StorageSettingsWindow added.
* (feature) ChartHelper. ExcludeObsolete for IndicatorTypes.
* (feature) QuoteChangeStates added.
* (feature) IMessageAdapter. IsSupportOrderBookIncrements property added.
* (feature) IMessageAdapter. IsSupportOrderBookDepths -> SupportedOrderBookDepths.
* (bug) ITCH, Plaza. Fix OL->OB local time stamp.
* (feature) Remote storage files moved from Algo.History.Hydra to Algo.Storages.Remote namespace.
* (feature) FortsDailyData moved to TraderHelper.
* (feature) Algo. Downgraded to .NET 4.0.
* (feature) Algo. Removed instruments cache.
* (feature) ExcelWorker -> IExcelWorkerProvider.
* (feature) ShrinkPrice. Uses 0.01 as default price step.
* (feature) log4net excluded.
* (feature) MoreLinq merged with Ecng.Collections.
* (feature) Plaza. Excluded ClientGate option.
* (feature) SmartCom. Excluded V3 version.
* (feature) AlfaDirect. Excluded 3.5 version.
* (feature) SecurityTypes. Etf added.
* (feature) OrderConditionalGrid, OrderConditionalWindows marked as obsolete.
* (feature) IMessageAdapter. IsSecurityNewsOnly property added.
* (feature) IMarketDataProvider. RegisterNews accepts Security arg.
* (bug) Fix handling custom data type subscriptions.
* (feature) SmartCOM. Replaced SmartComTimeFrames by TimeSpan.
* (feature) Oanda. Removed News support (deprecated).
* (feature) InteractiveBrokers. Live candles supported.
* (bug) InteractiveBrokers. Fix options calc subscriptions.
* (bug) InteractiveBrokers. Fix handling extended market data types.
* (feature) InteractiveBrokers. Replaced InteractiveBrokersTimeFrames by TimeSpan.
* (bug) FXCM. Fix connection error/drop handling.
* (bug) FXCM. Fix change order's trailing step.
* (bug) FXCM. Fix order state tracking.
* (bug) Connector. Fix ValuesChanged event processing for tick data.
* (feature) Connector. Updated level1 values until order book and tick trades received.
* (feature) Upbit connector.
* (feature) Exchange. EngName and RusName marked as obsolete.
* (feature) SampleHistoryTesting. Using FinamMessageAdapter and YahooMessageAdapter.
* (bug) Coinbase, Digifinex, IEX fixes.
* (feature) Samples. Removed connector specific samples.
* (feature) Samples. Folders reorganization.
* (feature) SampleMultiConnection -> SampleConnectionWithStorage.
* (feature) SampleConnection added.
* (feature) MarketDepth. QuotesChanged marked as obsolete.
* (feature) IConnector. ChangePasswordResult event added.
* (feature) ConnectorWindow. Change password options added.
* (feature) ConnectorWindow. Enabled/disable market-data/transaction messages.
* (feature) StrategiesDashboard. ClosePosition, RevertPosition, RiskRules commands added.
* (feature) CandleSettingsEditor refactoring.
* (feature) QuickOrderPanel added.
* (feature) Charting. Ruler annotation.
* (feature) Charting. Order error messages.
* (feature) Charting. Candle custom drawing.
* (feature) Charting. Orders/trades alternative icons.
* (feature) Charting. Quick orders panel.
* (feature) Charting. Quick time-frame and candle type switch.
* (feature) IMessageAdapter. OrderConditionType property added.
* (feature) CoinEx, FatBTC, LATOKEN connectors.
* (bug) Strategies. Fix stopping with non filled orders https://stocksharp.ru/forum/11068/strategiya-kotirovaniya-ne-ostanavlivaetsya-pri-polucheniya-oshibki-snyatiya-zayavki/
* (feature) Sterling. Instruments lookup supported with stub logic.
* (feature) ContextMenu -> PopupMenu.
* (feature) IMarketDataProvider. RegisterXXX renamed into SubscribeXXX.
* (feature) ISubscriptionProvider interface created.
* (feature) Boards subscription unified with MarketDataMessage.
* (feature) BoardStateStorage added.
* (feature) Connector. SupportSubscriptionTracking enabled by default.
* (feature) IMarketDataProvider. LookupTimeFrames added.
* (feature) IMessageAdapter. SupportedOutMessages property added.
* (feature) IMessageAdapter. IsConnectionAlive removed.
* (feature) Connector. Subscription tracking on normal and error disconnects turned on by default.
* (feature) IConnector. ConnectionLost, ConnectionRestored events added.
* (feature) Gopax, Hotbit, CoinHub connectors.
* (feature) StorageMessageAdapter. Meta-info extracted into StorageMetaInfoMessageAdapter.
* (feature) BasketMessageAdapter. Each connection uses own StorageMessageAdapter.
* (feature) IPositionProvider. SubscribePositions filter by Portfolio added.
* (feature) PortfolioLookupMessage. SecurityId filter added.
* (feature) Connector. MarketDataSubscriptionOnline event added.
* (feature) Algo. Remote types moved to Algo.Server namespace.
* (feature) SecurityId. Money, News, All instances created.
* (feature) Portfolio derived from Position.
* (feature) Connector. All subscriptions and lookups done via Subscription class.
* (feature) Connector. ReConnectionSettings marked as obsolete.
* (feature) BasketMessageAdapter. UseSeparatedChannels property created.
* (feature) MarketRuleHelper. Subscription rules.
* (feature) OKEX. PostOnly order supported.
* (feature) OKEX. MatchPrice supported.
* (feature) OKEX. Futures, Swap close position operation supported.
* (feature) FIX connector. Bovespa FIX and FAST dialects added.
* (feature) PortfolioGrid. Leverage column added.
* (feature) Connector. Support single order status requests.
* (feature) Connector. IsAutoPortfoliosSubscribe added.
* (feature) ITransactionProvider. StopOrder events marked as obsolete. Use events for regular orders.
* (feature) Bitalong connector.
* (feature) IExternalCandleSource removed.
* (feature) ExecutionMessage. IsCancelled -> IsCancellation.
* (feature) SampleHistoryTesting. OwnMessageAdapter added.
* (feature) SampleRandomTesting removed.
* (feature) DevExpress 18.1 -> 19.2
* (feature) Ookii.Dialogs -> DXDialogs.
* (feature) MarketDataFinished -> SubscriptionFinished.
* (feature) MarketDataMessage. Extracted response logic into SubscriptionResponseMessage.
* (feature) Portfolio subscription uses SubscriptionResponseMessage as response.
* (feature) OrderStatus subscription uses SubscriptionResponseMessage as response.
* (feature) SubscriptionResponseMessage. IsNotSupported property removed.
* (feature) Use SubscriptionResponseMessage.Error as response for error lookup messages.
* (feature) Uses SubscriptionOnlineMessage, SubscriptionFinishedMessage instead of SecurityLookupResultMessage, PortfolioLookupResultMessage, OrderStatusMessage.
* (feature) News. Language property added.
* (feature) Quote. OrdersCount added.
* (feature) SecurityTypes. Gdr, MultiLeg, Loan, Spread, Receipt, Indicator, Strategy, Volatility types added.
* (feature) QuoteChangeMessage. Updates by position supported.
* (feature) QuoteChange. Side removed.
* (feature) Removed XXXResultMessage. Uses SubscriptionFinishedMessage.
* (feature) PortfolioChangeMessage removed.
* (feature) Storages. Check version of an app and stored format.
* (bug) Fix snapshot storage fractional values.
* (feature) QuoteCondition added.
* (feature) Level1Fields. Index, Imbalance, UnderlyingPrice.
* (feature) Deribit. Test environment supported.
* (feature) MarketDepthControl. OrdersCount, Condition columns added.
* (feature) QuikLua32 (C# version).
* (bug) QuikLua. Turn off auto logic with client code initialization. Possible fix https://stocksharp.ru/forum/11227/ne-udaetsya-avtomaticheski-podat-zayavku-na-spb/
* (feature) FIX connector. OneZero dialect added.
* (feature) FIX connector. Dialects inherited from IMessageAdapter.
* (feature) Strategy. Connector now is class type.
* (feature) Binance. Futures and Margin supported.
* (feature) Huobi. Removed obsolete HADAX.
* (feature) MarketDataMessage. RefreshSpeed option added.
* (bug) Coinbase and GDAX. Fix historical step.
* (bug) Coincheck. Fix historical ticks parsing.
* (feature) Quoinex. Supports Liquid.
* (bug) Upbit. Fix non minutes candles request.
* (feature) Binance. New position events processing and fast order book subscription.
* (bug) Bittrex. Fix subscription replies send.
* (bug) Bitexbook. Fix market data.
* (feature) Latoken. V2 protocol supported.
* (feature) FIX connector. Dialects made as public.
* (feature) SecurityGrid. ProcessLevel1 method added.
* (bug) OKEX. Fix tick volume for margin section - https://stocksharp.ru/forum/11367/dlya-fyuchersov-okex-ne-prihodit-znachenie-obema-tika/
* (feature) LBank. V2 protocol supported.
* (feature) PrizmBit connector.
* (bug) OKEX. Fix https://stocksharp.ru/forum/11384/oshibki-registratsii-orderov-dlya-okex/
* (feature) IFileService. Compression added.
* (feature) IUpdateService created.
* (feature) Security. MaxVolume property added.
* (feature) Level1. LowBidPrice, HighAskPrice, LastTradeVolumeLow, LastTradeVolumeHigh new fields.
* (feature) News. ExpiryDate property added.
* (feature) Tick storage. Supports string ids.
* (feature) Heikin Ashi candles.
* (feature) DigitexFutures connector.
* (feature) Position. BuyOrdersCount, SellOrdersCount, BuyOrdersMargin, SellOrdersMargin, OrdersMargin, OrdersCount, TradesCount.
* (bug) FileLogListener. Clean out of date writers.
* (feature) INativeIdStorage. Supports variable tuple based ids. Support remove operations.
* (feature) IPortfolioProvider. GetPortfolio -> LookupByPortfolioName.

## v4.4.16:
* (feature) Alerts. Message made optional for sound based events.
* (feature) ISecurityAssociationStorage added.
* (feature) Bithumb. Prime service supported.
* (feature) OKEX. Support turn on/off sections.
* (bug) IQFeed. Candles request fixes.
* (feature) FIX connector. IFixDialect tick as level1 option.
* (feature) Process non persistable basket securities.
* (bug) Storage adapter. Fix boards lookup and update.
* (feature) IPositionStorage added.
* (feature) Reduced IEntityRegistry usage.
* (feature) Diagram. Multiple sockets for logical condition added.
* (feature) TraderHelper. Filter positions by PortfolioLookupMessage.
* (feature) ServicesRegistry added.
* (bug) ExpressionIndexSecurityProcessor fix.
* (feature) FixServer. FixSecurityLegsRequestMessage, FixSecurityLegsResultMessage messages added.
* (bug) Reconnection fix.
* (bug) IMessageAdapter.IsSupportSecuritiesLookupAll overriding fix.
* (feature) Digifinex, Idax, TradeOgre connectors.
* (feature) ITakeProfitOrderCondition, IStopLossOrderCondition, IWithdrawOrderCondition.
* (feature) CsvEntityRegistry. Currency and ExpirationDate added.
* (feature) SubscriptionMessageAdapter. IsRestoreOnNormalReconnect added.
* (feature) News. Priority property added.
* (feature) CoinCap connector.
* (feature) Coinigy connector.
* (feature) FIX connector. IBKR dialect supported algo orders.
* (feature) FixServer. Support SecurityStatusRequest.
* (feature) OKEx. Web sockets v3 supported.
* (feature) Plaza CGate. Spectra 6.3 supported.
* (feature) TWIME. Spectra 6.2 supported.
* (feature) Micex TEAP. Interface 31 supported.
* (feature) Storage lists. WaitFlush added. Removed ReadLasts.
* (bug) Statistics parameters. Fix reset state.
* (bug) MarketDepthControl. Fix processing quote msgs with empty instrument info.
* (bug) RSS fixes.
* (feature) MarketDepthControl. Dynamic change price/volume text formats.
* (feature) UnitEditor refactoring.
* (feature) OpenECry. More order types supported.
* (feature) OKEX. Position and account swap and margin supported.
* (bug) Candles compression. Fix for non TF candles compression.
* (bug) Basket adapter. Fix processing pending connect subscriptions.
* (bug) Connector. Fix subscribers counter for error responses.
* (feature) IConnector. MarketDataUnexpectedCancelled event added.
* (feature) Basket adapter. Support offline mode for dedicated adapters.
* (feature) SampleBitfinex. Candles added.
* (feature) ActionMessage. Message for iterate actions.
* (feature) BoardEditor redesign.
* (bug) MessageAdapter. Fix ReConnectionSettings save/load.
* (feature) PropertyGridEx. WorkingTime supported.
* (feature) OpenECryException removed.
* (bug) Connector. Clear portfolio lookup criteria fix.
* (bug) Simulator. Turn off heartbeat tracking for non owned adapters https://stocksharp.ru/posts/m/46473/
* (feature) Order. IsManual property added.
* (feature) LBank, BitMax, BW, Bibox, CoinBene, BitZ, ZB connectors.
* (bug) Heartbeat. Restore after reconnect.
* (feature) Deribit. Extended orders.
* (feature) Diagram. More options for order registration.
* (feature) CurrencyTypes. MXP added.
* (feature) IConnector. LookupXXXResult2 overloads.
* (feature) AlphaVantage. Lookup instruments supported.
* (bug) Quik. Fix hands while instruments lookup https://stocksharp.ru/forum/9238/zavisaet-quik-pri-podklyuchenii/
* (bug) Fix security lookup.
* (bug) Basket adapter. Fix lookup processing.
* (bug) MessageAdapter. Time out fixes.

## v4.4.15:
* (feature) LiveCoin. Candles support.
* (feature) LiveCoin. Websocket supported.
* (feature) Position. SettlementPrice added.
* (feature) OKEx. V3 protocol supported.
* (feature) Bitmex. Stop orders extended.
* (feature) MatLab. Candles supported.
* (feature) WorkingTime. Set schedules for part-time working days.
* (feature) FixServer. Translates Board info.
* (feature) OpenECry. v3.5.14.53
* (feature) MyTradeGrid. Support PnL column update.
* (bug) PropertyGridEx. Exchange board editor fixes.
* (feature) Diagrams. CandleSourceDiagramElement.AllowBuildFromSmallerTimeFrame added.
* (feature) Diagrams. Support market depth based indicators.
* (feature) FIX. CheckTimeFrameByRequest for default dialect.
* (feature) FIX connector. Support TotalNumSecurities processing.
* (feature) FIX connector. Allow change Encoding in dialect.
* (bug) FIX connector. Fix position processing errors.
* (feature) SecurityMessage.ToString override improved.
* (feature) ToString overrides. Avoid print empty Error tag for successful messages.
* (feature) TimeFrameLookupMessage, TimeFrameLookupResultMessage added.
* (feature) FIX connector, QuikLua. Supported TimeFrameLookupMessage, TimeFrameLookupResultMessage.
* (feature) Strategy messages.
* (feature) FixServer. Strategy messages supported.
* (feature) IConnector. Lookup result events passes lookup request messages.
* (feature) FixServer. SecurityMappingRequest, SecurityMappingResult messages supported.
* (bug) Bitmex. ExecInst fixes.
* (bug) Candles. Fix duplicate candles subscription.
* (bug) Market depth. Fix build depths from OL and L1.
* (feature) MT4, MT5 connectors.

## v4.4.14:
* (feature) Message. IgnoreOffline -> OfflineMode.
* (feature) SampleSmartCandles removed.
* (bug) SmartCOM. Fix candles and historical ticks requests.
* (bug) LocalMarketDataDrive. GetAvailableDataTypes fix.
* (bug) CandleBuilderMessageAdapter. Fix start date for compression subscriptions.
* (feature) IConnector. MarketDataSubscriptionFinished event to notify end of subscription packet.
* (feature) Diagram. Options elements description.
* (feature) Plaza. Spectra 6.1 supported.
* (feature) TWIME. 3.1 supported.
* (feature) Algo.Expressions. Moved to Algo proj.
* (feature) IBasketSecurityProcessorProvider. Basket securities refactoring.
* (feature) BaseGridControl. Autoscroll added.
* (feature) Bitfinex. Market data v2 API.
* (feature) Bithumb. WebSocket support.
* (feature) CandleBuilderProvider. Ability to register own CandleBuilder-s.
* (bug) CandleBuilderMessageAdapter. Fix non time-frame based candles processing.
* (feature) MarketDataGenerator. Set Interval default value.
* (feature) Charting. Move orders with mouse beyond chart range.
* (bug) Charting. Fix exception in BoxVolume chart.
* (bug) Charting. Fix date on x-axis not clipped to control bounds.
* (feature) Charting. Panels resize animation.
* (bug) Charting. Indicators binding with additional axis fix.
* (bug) Charting. Composite indicators properties modification fix.
* (feature) Charting. Volatility smile chart. Line interpolation.
* (feature) Charting. Equity and option charts - change drawing style possibility.
* (feature) Charting. Annotations creates from code.
* (feature) UserInfoMessage. The message provided user's information.
* (feature) ExpirationContinuousSecurity. Moved implementation from ContinuousSecurity. ContinuousSecurity is abstract.
* (bug) FinamHistorySource. Fix stocks lookup.
* (feature) Charting. OptionVolitilitySmileChart. Combine approximated lines + points in legend.
* (feature) CEX. Remove ClientId settings.
* (feature) BasePosition. ExpirationDate property added.
* (bug) FXCM fixes.
* (feature) CsvImporter. SecurityUpdated event added.
* (feature) Positions. ClientCode moved from Position to BasePosition.
* (feature) ImportSettingsPanel. IPersistable implemented.
* (bug) Monitor. Clear sources tree in monitor fixes.
* (bug) Monitor. Do not trim log sources names.
* (feature) CsvEntityRegistry. Support Position.ClientCode save-load.
* (feature) SessionMessage->BoardStateMessage renamed.
* (feature) BoardLookupMessage, BoardRequestMessage, UserRequestMessage added.
* (feature) IConnector. LookupBoardsResult event added.
* (feature) IConnector. LookupBoards method added.
* (feature) IConnector. SubscribeBoard/UnSubscribeBoard methods added.
* (bug) SubscriptionMessageAdapter. Set original trans id while disconnect unsubscribe.
* (feature) Connector. SupportBasketSecurities property added.
* (feature) InteractiveBrokers. MaxVersion settings added.
* (feature) Level1ChangeMessage. CommissionMaker, CommissionTaker fields added.
* (feature) PositionChangeMessage. CommissionMaker, CommissionTaker fields added.
* (feature) Order, MyTrade. CommissionCurrency field added.
* (feature) SecurityGrid, Level1Grid. CommissionMaker, CommissionTaker columns added.
* (feature) PortfolioGrid. Show Board, CommissionMaker, CommissionTaker columns.
* (feature) InteractiveBrokers, Micex, Plaza. Commission translate supported.
* (feature) Crypto connectors. Commission translate supported.
* (feature) FIX server. CommissionCurrency supported.
* (feature) ExchangeBoard. LME, WIKI instances.
* (feature) MfdHistorySource. Filter for options lookup.
* (feature) QuandlHistorySource. Lookup securities. Support newest protocol changes.
* (bug) Plaza. TableEditor fix.

## v4.4.13:
* (bug) Monitor.Clear fix.
* (bug) Candles. Fix process error response in case of multiples connections.
* (bug) PortfolioGrid. Fix State column localization.
* (bug) OrderLogMessageAdapter. Fix multi subscription processing.
* (feature) FAST settings as public.
* (bug) LMAX securities lookup fix.
* (bug) QuikLua. Fix Si and Eu symbols processing.
* (bug) FixServer. QuotesInterval handling fix.
* (feature) FixServer. TransactionId mapping associated with clients session.
* (bug) TraderHelper. IsLookupAll method fix.
* (bug) CandleBuilderMessageAdapter. Fix BuildMode processing.
* (feature) CandleBuilderMessageAdapter. Track From date.
* (bug) Transaq. Fix sec code decoding (&amp;).
* (bug) Bitmex. Process order fix.
* (bug) Chart. Date on x-axis not clipped to control bounds.
* (bug) Bittrex. Market depth fix and nullable order's field fix.
* (bug) Yobit. Empty order book processing fix.
* (feature) Chart. Removed obsolete xml exporting option.
* (bug) CandleBuilderMessageAdapter. Do not switch to smaller tf in case successfully finished original tf series.
* (bug) CandleBuilderMessageAdapter. Fix Load only subscription processing.
* (bug) SecurityNativeIdMessageAdapter. Ignore candles with empty security id.
* (bug) BasketMessageAdapter. Send error response for unhandled security lookup request.
* (bug) Samples. Fix lookup all securities for non supported connections.
* (bug) LMAX. Fix Level1 subscription.
* (feature) LMAX. v1.9.0.2
* (feature) FXCM. REST API support.
* (feature) ExchangeBoard. IEX board.
* (feature) AlphaVantage connector.
* (feature) IEX connector.
* (bug) Deribit. Fix news subscription.
* (bug) Cryptopia. Order book request fix.
* (bug) Fix CandleManager.Stopped event invoke for Connector source https://stocksharp.ru/posts/m/44515/
* (feature) Zaif, Quoinex, Bitbank connectors.
* (bug) Storage. Fix path for security id start from '.'.
* (feature) IConnector. Methods RegisterMarketDepth, RegisterSecurity and RegisterTrades accepts BuildFrom argument.
* (bug) Bitmex. Fix tick subscription https://stocksharp.ru/forum/9741/bitmex-poluchenie-sdelok-api-4412/
* (bug) Bitmex. Fix candles state.
* (bug) Bitmex. OL processing fix.
* (feature) DevExpress v18.1.5
* (bug) Yobit. Orders processing fix.
* (bug) Fix processing non associated with transaction id order's messages.
* (bug) https://stocksharp.ru/forum/9726/instrument-dlya-market-dannyh-s-identifikatorom-zaprosa-71415971-ne-naiden/ fix delete prev subscriptions.
* (feature) FixServer. Auto unsubscribe for disconnected sessions.
* (bug) CommissionRule. Fix rules processing with percentage values.
* (feature) AlfaDirect. v4.0 support (market data only).

## v4.4.12:
* (feature) Crypto connectors. BalanceCheckInterval for refresh an account balances in case of deposit and withdraw operation.
* (feature) SubscriptionMessageAdapter. SupportLookupMessages options to support duplicated subscriptions as unique.
* (feature) Huobi. Support HADAX.
* (feature) CurrencyTypes.ZAC
* (feature) FIX connector. Time format parsing settings as public.
* (bug) Charting. Fix duplicate candle series save/load.
* (feature) MessageAdapter. INotifyPropertyChanged implemented.
* (bug) TransactionSnapshot. Fix trade data save.
* (feature) OrderWindow. SecurityEnabled, PortfolioEnabled for modify orders.
* (bug) OrderGrid. Cancel group orders fix.
* (feature) Message. IgnoreOffline option to prevent buffering in offline mode.
* (feature) OpenECry. v3.5.14.41
* (feature) SampleOEC. Removed obsolete.
* (feature) FAST dialects. Made network settings configurable.
* (feature) TraderHelper. LookupAllCriteria changed from asterisk to empty string.
* (feature) IMessageAdapter. IsSupportSecuritiesLookupAll option indicates adapter able to download all available securities.
* (feature) InteractiveBrokers. v9.73.07
* (feature) InteractiveBrokers. SSL support.
* (bug) BasketMessageAdapter. Fix securities lookup all processing.
* (feature) CurrencyTypes. DEM, LUF.
* (feature) ExchangeBoard. Globex board info added.
* (bug) CandleMessageBuildableStorage. Fix process multiples timeframes.
* (bug) Transactions snapshot storage. Fix orders and trades save.
* (bug) SecurityGrid. Do not show errors in security grid.
* (bug) Level1FieldsComboBox. Text alignment fixes.
* (feature) PropertyGrid. Reset button to exchange board editor added.
* (feature) CsvEntityRegistry. Method GetBoard uses IExchangeInfoProvider service.
* (bug) InteractiveBrokers. Fix expiry time parsing fix.
* (bug) InteractiveBrokers. Market depth fix.
* (feature) Index builder. Ignore errors as parameters.
* (feature) IMesssageAdapterExtension. Extra info about typical order's condition.
* (feature) OrderWindow. Support conditional orders creation. Take-profit and stop-loss shortcuts.
* (feature) WithdrawWindow. Volume and VolumeStep properties.
* (feature) BasketMessageAdapter. Save/load IPortfolioMessageAdapterProvider mapping.
* (bug) TransactionSnapshot. Fix conditional orders save.

## v4.4.11:
* (feature) Connector. ICandleManager implemented.
* (feature) ICandleSourceList removed.
* (bug) BasketMessageAdapter. Fix news ubsubscribe requests.
* (feature) StorageMessageAdapter. Build candles from tick data in case IsCalcVolumeProfile=true
* (feature) Candles building refactored. Supported IsCalcVolumeProfile. Removed IsHistory usage.
* (bug) Grids. Fix filters for enum based fields.
* (bug) BufferMessageAdapter. Fix subscription for IsCalcVolumeProfile and AllowBuildFromSmallerTimeFrame.
* (feature) Managers. Moved from Connector to BasketMessageAdatper.
* (feature) StorageMessageAdapter. Store historical level1.
* (feature) RemoveMessage.
* (bug) Backtesting. Fix candles subscription using external source https://stocksharp.ru/posts/m/43646/
* (bug) Backtesting. Fix candles subscription generators.
* (feature) Security storage. Forced updates for manual modified data only.
* (feature) FIX connector. IssueDate, IssueSize translation support.
* (feature) FixServer. Multi leg securities support.
* (feature) FixServer.NewOutMessage. FixSession as the first parameter.
* (feature) PermissionCredentialsAuthorization.
* (feature) PermissionCredentialsWindow.
* (bug) FIX connector. SpectraFixDialect. Order mass cancel fix.
* (feature) FixMessageAdapter. SupportedMessages from Dialect.
* (bug) OrderGrid. Active filters fix.
* (bug) GatorHistogram fix.
* (feature) Charting. GatorOscillatorPainter, RelativeVigorIndexPainter.
* (feature) MarketEmulator. CheckMoney option.
* (bug) ConnectorWindow. Fix connector description for Quik lua.
* (bug) RiskMessageAdapter fixes.
* (feature) Plaza. v5.3.6
* (feature) Micex. Stock30, Currency28, Currency30 interfaces.
* (bug) Transaq. Fix shared dll initialization https://stocksharp.ru/forum/9421/podklyuchenie-sdata-k-tranzak-/
* (feature) FinamHistorySource. .NET 4.6 minimum required.
* (feature) FIX connector. SSL support extended.
* (feature) FIX connector. QUIK FIX PreRrade support.
* (bug) PortfolioEditor, SecurityEditor. Fix keyboard typing.
* (feature) IMessageAdapter. CheckTimeFrameByRequest property.
* (bug) TimeSpanEditor. Design fix.
* (feature) Backtesting. Dynamic change market time interval.
* (feature) LogControl. Design adapted for Devexpress.
* (feature) LogControl. LayoutChanged event.
* (bug) MarketDataGrid. Fix further refreshes after error request.
* (feature) CandleSeries.IsRegularTradingHours.
* (feature) CandleBuilder. IExchangeInfoProvider as input argument.
* (feature) ComplexCsvSecurityList. Base class for complex security types list.
* (feature) MultiSecurityStorage.
* (feature) ISecurityWindow restored.
* (feature) ThemedIconsExtension. Icons auto coloring.
* (feature) ContinuousSecurityWindow. Design refactoring.
* (feature) CandleSeries. Count property.
* (bug) Grids. Time zone column fix.
* (bug) Chart icons color fixes.
* (bug) Windows title color fix.
* (feature) Plaza. AdjustedFee, Prohibition tables. FeeRate, Dealer columns populated.
* (bug) Binance. Fix https://stocksharp.ru/forum/9354/vyvod-sredstv-iz-binance-/#m43771
* (feature) SecurityPicker. Removed StatusBar.
* (bug) SecurityPicker. Fix filtering securities started by @ char.
* (feature) FIX connector. Try/catch out messages processing.
* (feature) Chart.DisableIndicatorReset
* (bug) Chart. Indicator draw from non-GUI thread fix.
* (bug) BufferMessageAdapter. Fix transactions data save/load.
* (bug) MACD Histogram calc fix https://stocksharp.ru/forum/9487/nekorrektno-risuetsya-macd-histogram/
* (feature) QuotesBinarySnapshotSerializer. Support unlimited depths.
* (feature) Level1BinarySnapshotSerializer. Support more fields.
* (feature) TransactionBinarySnapshotSerializer. Condition orders support.
* (feature) IConnector. LookupOrders methods.
* (feature) StorageMessageAdapter. SupportLookupMessages (method Load is obsolete).
* (feature) IndicatorPickerWindow. PropGrid for indicator settings.
* (feature) Charting. MACD Histogram. Signal and MACD lines added.
* (bug) Bitfinex. Fix position processing https://stocksharp.ru/forum/9498/pri-podklyucheni-vylazit-oshibka-parsinga-konnektor-bitfinex/
* (bug) Bithumb. Ticks subscription fix.
* (bug) QuikLua. Fix send back market data errors subscriptions.
* (feature) Alerts. Schemas panel.
* (bug) Connector. Fix processing order status response with failed orders info.
* (feature) OfflineMessageAdapter. Track disconnect.
* (bug) YahooHistorySource. Fix downloading history for futures.
* (feature) YahooHistorySource. Intraday interval supported.
* (bug) Image publish cancellation processing fix.
* (feature) MarketDepth.MaxDepth marked as obsolete.
* (feature) MarketDepth. Removed thread safety support.
* (feature) QuikLua. Handle From To date range for market data requests. https://stocksharp.ru/forum/9460/korrektnoe-otobrazhenie-svechei/
* (feature) StrategiesDashboard. Set security and portfolio directly. Allow trading and last error columns.
* (bug) Charting. Fix Equity legend color.
* (feature) Diagram. MarketDepthTruncateDiagramElement.
* (bug) TimeSpanEditor. Fix DevExpress themes.
* (feature) Default implementation of IOrderLogMarketDepthBuilder.
* (feature) MarketDataMessage. BuildCandlesModes -> BuildMode, BuildCandlesFrom -> BuildFrom, BuildCandlesField -> BuildField.
* (feature) OrderLogMessageAdapter. Moved depth and tick building from Connector to Adapter.
* (feature) Connector. CreateDepthFromOrdersLog, CreateTradesFromOrdersLog market as obsolete.
* (feature) SampleFix. OrdersLogWindow added.
* (feature) SamplePlaza. OrdersLogWindow uses OrderLogGrid.
* (feature) Message. Method Clone made as abstract.
* (feature) Bittrex. Web sockets supported.
* (bug) Charting. Fix drawing trades with string id.
* (feature) OrderGrid, ExecutionGrid, TradeGrid, MyTradeGrid. Sides coloring.
* (feature) SecurityGrid. BuyBackDate, BuyBackPrice columns.
* (bug) ConnectorWindow. Fix stub connectors check.
* (feature) IConnector. Method RegisterXXX accepts From and To range, build from option.
* (feature) ExecutionGrid. OriginSide coloring.

## v4.4.8:
* (feature) ImportSettingsPanel control.
* (feature) CSV storages. Provide error details as return from Init method.
* (feature) CsvEntityRegistry. Store UnderlyingSecurityType, CfiCode, IssueDate, IssueSize.
* (feature) MarketDepthControl. GetOrders method.
* (feature) MarketDepthControl. CellMouseLeftDoubleClick event.
* (feature) InteractiveBrokers. Min10, Min 20, Hour2, Hour3, Hour4, Hour8 timeframes support.
* (feature) InteractiveBrokers. Historical data types AdjustedLast, RebateRate, FeeRate support.
* (bug) InteractiveBrokers. End date for candles request fix https://stocksharp.ru/posts/m/43390/.
* (bug) InteractiveBrokers. SecurityLookup error response handling fix.
* (bug) InteractiveBrokers. Candles request fix.
* (feature) PortfolioPicker.
* (feature) DukasCopyHistorySource.GetCandles. Sides -> Level1Fields.
* (feature) OrderRegisterMessage. CopyTo method.
* (feature) OfflineMessageAdapter. Support replace for pending orders.
* (bug) Real time candles subscription determine fix.
* (bug) InteractiveBrokers. Native order ids fix.
* (bug) Charting. Fix order creation for non first panel.
* (feature) Charting. Auto select Security and Portfolio.
* (bug) CsvEntityRegistry. Securities IsChanged fix (new empty values).
* (bug) SampleLogging fix. Support Verbose.
* (feature) SampleFix. Market data adapter switch FIX<->FAST.
* (bug) LogControl. Fix themes https://stocksharp.ru/forum/9253/kak-zapretit-izmenenie-svoistv-kontrola/
* (bug) FIX connector. CFI code sending fix.
* (feature) FIX connector. FixTags.Product support.
* (feature) FIX connector. AstsFixDialect splitted on Currency and Equity dialects.
* (feature) NewsPanel. IPersistable support.

## v4.4.7:
* (feature) Connector.SubscribedCandleSeries.
* (feature) CandleSeries.AllowBuildFromSmallerTimeFrame.
* (feature) SmartCOM. V4 as default.
* (bug) Transaq. Fix locked file issue.
* (feature) LocalMarketDataDrive.GetDataType. Return null in case parsing error.
* (feature) BasketMarketDataStorage. Initialize OriginalTransactionId.
* (feature) IMessageAdapter.GetTimeFrames.
* (feature) StorageMessageAdapter.CacheBuildableCandles.
* (bug) CandleArgToFolderName and ToCandleArg fixes.
* (bug) BasketMessageAdapter. Handle multiple subscriptions fix.
* (bug) SubscriptionMessageAdapter. Fix multiple candles subscription handling.
* (bug) CsvImporter. Fix candles processing.
* (feature) DateRangeWindow renamed to CandleSettingsWindow and moved to Xaml.Charting.
* (bug) Binary storage. Fix local time save http://stocksharp.ru/forum/9296/isklyuchenie-pri-sohranenii-executionmessage
* (feature) IMessageAdapter.IsSupportCandlesUpdates

## v4.4.6.2:
* (feature) HeartbeatMessageAdapter.SuppressReconnectingErrors
* (bug) HeartbeatMessageAdapter. Infinitive reconnection attempts fix.
* (bug) HeartbeatMessageAdapter. Infinitive first connection attempts fix.
* (bug) Binance, Coinbase, Bitfinex, Bitstamp, IQFeed fixes.

## v4.4.6.1:
* (bug) InteractiveBrokers. Fix historical ticks request.
* (feature) Charting. Uses DateRangeWindow to set candle series From and To.
* (feature) StorageCandleSource removed as obsolete.
* (bug) Connector. IsBack MD messages for News fix.
* (feature) Connector. Market data events now support News subscriptions.
* (bug) CandleBuilderMessageAdapter. Fix build unsubscribe.
* (feature) IStorageRegistry.DefaultDrive setter added.
* (bug) FileProgressWindow closing fix.
* (bug) ConnectorWindow. Show missed column names.
* (feature) Charting. StochasticOscillatorPainter.
* (bug) Kraken margin position obtain fix.
* (feature) IFileService.GetUploadLimit return value int -> long.
* (feature) Charting. Auto and manual select candles series for indicators.
* (bug) Charting. Fix auto select appropriate candle series.
* (bug) FIX connector. Fix process unknown outgoing messages.
* (bug) AlfaDirect, Transaq. PortfolioMessage processing fixes.
* (bug) Charting. OptionPositionChart. Legend binding fix.
* (bug) Charting. Options charts theme binding fix.
* (bug) HistoryEmulationConnector. Fix external sources processing.

## v4.4.6:
* (feature) Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, Kucoin, BITEXBOOK, CoinExchange stubs.
* (feature) WithdrawWindow.
* (feature) IndexSecurityWindow.
* (feature) Quik DDE turned off.
* (bug) OfflineMessageAdapter. Cancel pending orders fix.
* (feature) OrderGrid. Allow cancel pending orders.
* (feature) FIX connector. Process unknown transactions option.
* (feature) OpenECryStopType -> OpenECryStopTypes.
* (bug) Fix http://stocksharp.ru/forum/9261/isklyuchenie-pri-popytke-podklyucheniya-k-bittrex/
* (feature) Crypto withdraw. Uses Order.Security instead of WithdrawInfo.Currency.
* (feature) Level1Fields. Dividends, AfterSplit, BeforeSplit
* (bug) YahooHistorySource restored.
* (bug) IQFeed. Fix parse fundamental messages with empty exchange code.
* (bug) Xaml.Diagram. Fix drag n drop from palette http://stocksharp.com/forum/9268/Drag-and-Drop-S-Designer-error/

## v4.4.5.4:
* (feature) TraderHelper.TryAdd IsZeroAcceptable.
* (bug) Connector. Lookup messages sending fix.
* (bug) Child strategies. Fix set Connector for child strategies.
* (feature) Charting. Reset Y axis range on double click.
* (feature) Charting. Time zone settings for each axis.
* (feature) Charting. Draw lines on mouse down event.
* (bug) Charting. X axis scaling fix.
* (bug) Charting. Chart annotation editor related fixes.
* (bug) Charting. Tooltip fix for chart line display style.
* (feature) Charting. X0 style support.
* (bug) BasketMessageAdapter. Disconnect message processing fix.
* (bug) CandleHolderMessageAdapter. Unsubscribe fix.
* (feature) HistoryMessageAdapter.CheckTradableDates.
* (feature) Crypto connectors. Withdraw support.
* (feature) CandlesHolder.
* (feature) Order. IsMargin property.
* (feature) Order. Slippage property.
* (feature) FIX connector. SSL support.
* (feature) FIX connector. Dukascopy support.
* (feature) Samples crypto. CandleChart added.
* (bug) MarketEmulator. Emulate price limit from OrderLog trade price.
* (feature) MarketEmulator. Check security and session states.
* (bug) Fix live market data adapter reusing in real time emulation. http://stocksharp.ru/posts/m/42374/
* (feature) FIX connector. FixStopOrderType -> FixStopOrderTypes.
* (feature) ConnectorWindow. Lookup connections.

## v4.4.5.3:
* (feature) OrderLossMoreRule.
* (feature) KrakenMessageAdapter.IsMarginEnabled.
* (feature) LogManager. Save/Load Application settings.
* (bug) Chart. Tooltip format fix http://stocksharp.ru/posts/m/42383/ 
* (feature) Chart. Custom candle colors.
* (bug) Chart. Area style fix.
* (bug) Chart. Indicator is ahead of candles.
* (feature) Chart. Overview mouse scrolling support.
* (feature) MarketRuleHelper. Connector state handling rules.
* (bug) Reconnect adapter fixes.
* (bug) Subscription adapter fixes (reply handling fix, avoid potential deadlocks).
* (feature) BTCE. Turned off book snapshot.
* (feature) Poloniex. Reduce get trades invokes.
* (bug) AlfaDirect. Fix double field parsing.
* (bug) HitBTC. Market order fixes.
* (bug) Bitstamp. Fix transaction reply parsing.
* (bug) SecurityGrid. LastTrade.Volume display as empty when it 0.
* (feature) MyTradeGrid. TransactionId column.
* (bug) Monitor. Fix empty log source processing.
* (feature) OrderLogGrid. Security column added.

## v4.4.5.2:
* (bug) OrderProfitMoreRule fix.
* (bug) Change milliseconds for TimeSpanEditor fixes.
* (feature) SampleGdax. OrderLogWindow.
* (bug) CandleHelper.ToTrades fix.
* (feature) SampleBitfinex. OrderLogWindow.
* (feature) OrderLogGrid. Security column added.
* (feature) Connectors. DefaultHeartbeatInterval
* (bug) Crypto connectors fixes.

## v4.4.5.1:
* (bug) PropertyGrid. Fix SecureString editor.
* (feature) Importing securities. More fields.
* (feature) Importing. Enum fields has default mapping.
* (feature) Importing. Field order.
* (feature) FIX server. Support market data response messages.
* (bug) QuikLua. Market data response support. Fix http://stocksharp.ru/forum/9072/tikovye-svechi-v-mesto-kastomnogo-taimfreima/
* (bug) InteractiveBrokers. Expiry date parse fix.
* (bug) Samples for crypto. Retargeted to 4.6.
* (feature) CsvParser. Support quotes.
* (feature) Configuration. Try/catch missed adapter's files.
* (feature) FillDefaultCryptoFields.
* (bug) Fix Samples.sln
* (bug) Fix missed DevExpress files http://stocksharp.ru/forum/9095/tehpodderzhka-biblioteka-devexpresspdfv172core/
* (feature) BitStamp. ClientId. int->string http://stocksharp.ru/posts/m/42535/
* (bug) BitStamp. Fix http://stocksharp.ru/posts/m/42474/
* (bug) Kraken. FIX NRE, ticks subscription.
* (bug) InteractiveBrokers. Greenwich time zone parsing fix.
* (bug) ProxyEditorWindow. Fix for non BaseApplication apps.

## v4.4.5:
* (feature) Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck connectors.
* (bug) Charting. Envelope indicator rendering fix.
* (feature) Charting. Painters lookup refactoring.
* (feature) Quik. Removed obsolete QuikOrderConditionResults.
* (bug) OrderRegMsg.TillDate usage fix.
* (bug) InteractiveBrokers. Resubscribe fix.
* (feature) Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck connectors.
* (bug) BasketMessageAdapter. Fix subscription for not yet connected adapters.
* (feature) WEX (BTCE). Pusher support.
* (bug) OrderLogGrid. Binding fix for TIF and Expiry date.
* (bug) Bitstamp fixes.
* (bug) MarketDataMessage. Do not set From value for real-time subscriptions.
* (feature) PortfolioMessage. Removed State property.
* (feature) MarketRuleHelper. OrderProfitMoreRule.
* (feature) QuotesBinarySnapshotSerializer. MaxDepth property.

## v4.4.4:
* (feature) IPositionProvider interface added.
* (feature) IPortfolioProvider.PortfolioChanged event added.
* (feature) Strategy. Implemented IPositionProvider interface.
* (feature) Unit. Evaluation with nullable parts return null (was ArgNullExcp).
* (feature) BasketBlackScholes. Uses IPositionProvider.
* (feature) OptionPositionChart. Uses IPositionProvider.
* (bug) OptionDesk, OptionPositionChart, OptionVolatilitySmile fixes.
* (feature) StorageMessageAdapter. Load only curr day for order book and level1 (in case From is null).
* (bug) Finam history. Fix security lookup. http://stocksharp.ru/posts/m/41218/
* (bug) FIX server. Security expiration date fix http://stocksharp.ru/forum/8703/data-ehkspiratsii-optsionov-na-forts/
* (bug) IQFeed. Level1 fix.
* (bug) Oanda. Fix large candle's range request.
* (bug) Alerts fixes.
* (bug) InteractiveBrokers. Market data fixes.
* (bug) CandleMessage.State init fix.
* (bug) MarketDepthControl. Binding fixes.
* (feature) Snapshot storage.
* (feature) Security. IssueDate, IssueSize and UnderlyingSecurityType properties.
* (bug) http://stocksharp.com/forum/8795/Binary-Storage-Corrupted/
* (feature) Backtesting. Performance boost.
* (feature) CurrencyTypes.ETH.
* (bug) QuandlHistorySource. Lookup fix.
* (bug) MfdHistorySource. Fix options lookup.
* (bug) LMAX. Instruments file parsing fix.
* (bug) InteractiveBrokers. Market data fixes.
* (feature) InteractiveBrokers. v9.73.06 support.
* (bug) SmartCOM. Position translation fix.
* (feature) OptionDesk. Show expiration date columns.
* (feature) Security.UnderlyingSecurityType.
* (bug) Options OTM ITM. Fix http://stocksharp.ru/forum/8834/optsiony-itm-otm/
* (feature) Exchange. New info for Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck.
* (feature) QuikLua. AutoFixFutureCodes
* (feature) Binary quotes. Allow save bid > ask.
* (feature) QuikLua. Filter securities by type.
* (bug) FIX server. Fix security lookup. http://stocksharp.ru/forum/8874/problemy-podklyucheniya-k-hydra-cherez-fix-/
* (feature) FIX connector. Spectra dialect. Support nanoseconds.
* (feature) FIX connector. ExecutionReport.LastCapacity
* (feature) FIX connector. Otkritie microseconds support.
* (bug) Level1 binary storage. Fix min max serialization.
* (feature) Level1 binary storage. Use long as step counts.
* (bug) Binary storage. Fix non adjust price steps.
* (bug) MarketDepthGenerator fixes.
* (feature) DevExpress v17.1.7
* (bug) IQFeed. Symbol lookup fixes.
* (bug) Backtesting candles fix http://stocksharp.ru/forum/8816/ne-pravilnoe-ispolnenie-sdelok-pri-testirovanii-na-svechah/ 
* (bug) Tick and quotes binary storage. Fix store highly fractional prices.
* (feature) Order log binary storage. Nullable volumes posibilites.
* (bug) Quotes storage. Fix zero and negative prices store.
* (feature) IMessageAdapter.TimeFrames
* (bug) SampleMultiConnection. Fill time frames http://stocksharp.ru/forum/8849/primer-samplemulticonnection/
* (bug) PositionManager events fix http://stocksharp.ru/forum/8961/sobytiya-positionmanager-newposition-i-positionchanged/
* (bug) Connector. Stop candle series fix http://stocksharp.ru/forum/8919/povtornyi-zapusk-serii-svechek/
* (bug) Connector. Remove RegisteredXXX fix http://stocksharp.ru/forum/8868/problemy-pri-raborte-so-takanami-pri-konnektore-trader-workstation-ot-ib/

## v4.3.28:
* (feature) SecurityGrid. PriceChartEditor. Provider is non mandatory.
* (feature) LMAX, Oanda, IB. Uses MarketDataMessage.BuildCandlesField.
* (bug) BitStamp. Market data fix.
* (bug) FIX protocol. Exante market-data fix.
* (feature) Binary storage. Support non adjust prices for order book and level1.
* (bug) FIX connector. Position average price receive fix.
* (bug) FIX connector. Order book gathering fix.
* (bug) FIX connector. Check input values while logon.
* (bug) QuikLua. Fix CurrentValue for money positions http://stocksharp.ru/posts/m/41082/
* (feature) IQFeed. Security file parsing into separate thread.
* (bug) Alerts fixes.
* (bug) BTCE. Fix security price step.
* (bug) SecurityCreateWindow fix.
* (feature) Storage. Turned off saving active candles.
* (bug) Storage. Fix filter first data.
* (bug) SecurityGrid. Removed obsolete bindings and fix sorting.
* (bug) BTCE. Fix market-data only mode.
* (bug) IQFeed. Connection error handling fix.
* (feature) InteractiveBrokers. Support historical ticks.
* (feature) ChartAnnotation.
* (bug) QuikLua. Fix candle states http://stocksharp.ru/posts/m/41144/

## v4.3.27.2:
* (feature) QuikLua. Support candles.
* (feature) SpbExTrader.IgnoreLimits.
* (bug) Clipboard fix.
* (feature) IPositionManager.SecurityId filter.
* (feature) IPortfolioProvider.LookupByPortfolioName extension method.
* (bug) OrderGrid.OrderCanceling. Fix signature.
* (feature) BuySellPanel, BuySellGrid controls.
* (bug) Alerts fixes.
* (feature) EquityChart, OptionPositionChart, OptionSmileChart. Support IPersistable.
* (bug) PortfolioGrid. Binding State fix.
* (bug) BasketMessageAdapter. Reconnect fix.
* (feature) IIndicator.ResultType.
* (feature) ThreadSafeCol{Portfolio} -> PortfolioDataSource
* (feature) DevExpress v17.1.5
* (feature) Newtonsoft.Json. v10.0.3
* (feature) Strategy. Browsable=false for modified values.
* (bug) Analytic strategies fixes.
* (feature) Heatmap, Bubble, Histogram charts.
* (feature) OfflineMessageAdapter. Remove subscription while disconnected state.
* (feature) SubscriptionMessageAdapter. Set PortfolioMessage.OriginalTransactionId if empty.
* (feature) Security. Initialize Id, Name, Code, Class as null (prev was empty string).
* (feature) MessageAdapter. Init ServerTime (if not set) for position messages.
* (feature) OfflineMessageAdapter. Cancel previously sent orders.
* (feature) Level1 storage. Support IssueSize, Duration, BuyBackPrice, BuyBackDate. http://stocksharp.ru/posts/m/40757/
* (feature) Level1Field.ToType extension method.
* (bug) Level1CsvSerializer fix.
* (feature) Analytics. Reduced UI logic.
* (feature) Chart controls. Track current DevExp theme.
* (feature) FIX server. Accept date bounds for market data requests.
* (bug) FIX server. Fix async sending.
* (feature) QuikLua. Support official prices. http://stocksharp.ru/forum/8534/nelikvidnye-instrumenty---kak-luchshe-organizovat-rabotu-s-nimi/
* (feature) QuikLua. Ignore case for security lookup.
* (feature) Strategy.Parameters. From Set to Dictionary.
* (feature) Connector.AutoPortfoliosSubscribe.
* (feature) MarketDepthControl.IsBidsOnTop is dependency property.
* (feature) MathDiagramElement.
* (feature) Chart. Save/Load refactoring.
* (feature) Micex. Stock28 interface.
* (feature) Plaza. 5.3.1 support.
* (bug) BasketMessageAdapter. Fix disconnect for broken connection.
* (bug) TraderHelper.CancelOrders. Fix canceling failed orders.
* (feature) IConnector. SendInMessage, SendOutMessage.
* (feature) Strategy. Start/Stop processing from connector's thread.
* (bug) PositionBinarySerializer fix.
* (feature) SmartCOM. Removed V2 support.
* (bug) Expression fix http://stocksharp.ru/forum/8586/skleennye-fyuchersy-s-finama/
* (feature) FIX connector. ExecutionReport.LastLiquidityInd
* (feature) FIX connector. TransactTime for OrderCancelReject
* (feature) FIX connector. Handle order register errors.
* (bug) FIX connector. SpectraFixDialect cancel orders fix.
* (feature) IIndicator.InputType
* (bug) FIX server. Fix depths subscription for ALL security.
* (feature) FIX connector. Candle state support.
* (feature) FXCM. Masked as x64 bit only.
* (bug) Storage. Fix data bounds validation.
* (feature) CandleBuilder. Build from level1.
* (feature) FIX connector. ASTS. Microseconds.
* (feature) Level1 binary storage. Support unknown types.
* (feature) ICandleBuilderSourceValue -> ICandleBuilderValueTransform.
* (feature) Algo.Candles.Compression. Removed sources.
* (bug) AlfaDirect. Security lookup fixes.
* (feature) SecurityStorage. Load empty strings as null.
* (feature) CandleBuilder. Support OI.
* (feature) Connector.UpdateSecurityByDefinition.
* (feature) QuoteChangeMessage.GetSpreadMiddle extension.
* (feature) Level1ChangeMessage.GetSpreadMiddle extension.
* (bug) Copy security info fix.
* (feature) IMessageAdapter.IsValid removed.
* (feature) SecurityGrid. Close price mini chart column.
* (feature) StrategiesDashboard. P&L mini chart column.
* (feature) StrategiesStatisticsPanel. P&L mini chart column.
* (feature) BitStamp. V2 protocol.
* (feature) SecurityGrid. Days till expiry column.
* (bug) SecurityGrid. ExpiryDate, Strike columns sorting fix.
* (feature) Interactive Brokers. v9.73.04
* (bug) Interactive Brokers. Fix historical market data request.
* (feature) Transaq. Initial init candle periods.
* (bug) SmartCom. Price step fixes.
* (feature) BTCE. Domain update.
* (bug) Oanda. Fill ExecMsg.TradePrice
* (feature) MarketDepthControl. Process order with specified state and balance (in async mode).

## v4.3.26.2:
* (feature) Position tracking storage.
* (feature) MarketDepthControl. Show histogram for volumes.
* (feature) MarketDepthControl. Single column for volume.
* (feature) MarketDataGrid. Show Transaction data.
* (feature) ConnectorWindow. Enable/disable checkbox moved to grid row.
* (feature) PositionChangeTypesComboBox.
* (feature) PositionChangeGrid.
* (bug) Binary storage. Fix local time saving.
* (feature) SecurityMappingAdapter.
* (bug) BTCE fix.
* (feature) SecurityMappingWindow.
* (feature) FastMessageAdapter. Extracted from FixMessageAdapter.
* (bug) FAST fixes.
* (bug) SpbEx fixes.
* (bug) Level1CsvSerializer fix.
* (bug) Fix index creation http://stocksharp.ru/forum/8409/v-gidre-pri-raschete-indeksa-vydaetsya-soobshshenie-method-must-have-a-return-type/
* (feature) Export. Indicator values.
* (feature) Level1Grid. Duration, BuyBackDate, BuyBackPrice.
* (feature) CSV importing.
* (bug) Fix symbol mapping http://stocksharp.ru/forum/8433/problema-podklyucheniya-k-quik-lua-v-designer-versii-43252/
* (feature) Order.VisibleVolume. Made as optional param.
* (feature) IMessageAdapter. IsFullCandlesOnly, IsSupportSubscriptions.
* (feature) Security.Turnover.
* (feature) SmartCOM 4.
* (feature) SecurityPicker.UnderlyingGrid.
* (feature) TraderHelper.FromMicexCurrencyName. Error handler.
* (feature) Storages (binary and csv) support Order.IsMarketMaker info.
* (feature) OrderGrid, ExecutionGrid, OrderWindow. Show IsMarketMaker property.
* (feature) Extended storages. Delete support.
* (feature) Order.ExpiryDate. GTC mean only nullable value.
* (feature) ExtendedInfoStorageSelectWindow -> ExtendedInfoStorageWindow. Provide manage extended storages.
* (bug) FIX server. Fix crash while client's disconnecting.
* (bug) QuikLua. Fix authorization http://stocksharp.ru/forum/8432/problema-pri-podklyuchenii-k-quik-(lua)/
* (feature) QuikLua. Market maker support.
* (feature) xNet. v3.3.3
* (feature) QuikLua. Index securities support (O H L C values).
* (feature) FinamHistorySource. Ticks origin side as optional.
* (bug) BaseGridControl. Export fixes http://stocksharp.ru/posts/m/40578/
* (feature) Strategy.OrderChanged. Invoke after position calculation.
* (feature) QuantFEED.
* (feature) LogLeves.Verbose.
* (feature) Order.DerivedOrder is deprecated.
* (feature) BufferMessageAdapter. Enabled option.
* (feature) SubscriptionMessageAdapter. Send original subscription id.
* (feature) SubscriptionMessageAdapter. Support non filterable subscriptions.
* (bug) SecurityMappingMessageAdapter, SecurityNativeIdMessageAdapter. Group cancel orders fix.
* (bug) Not found themes. Fix http://stocksharp.ru/forum/8404/ne-rabotayut-primery-stocksharp_4325/

## v4.3.25:
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
* (bug) Alerts. Popup window. Icon quality fix.
* (bug) CQG continuum. Order condition fix.
* (feature) DevExp 17.1 update.
* (feature) FeedbackWindow.
* (feature) FIX server. Performance boost.
* (feature) Oanda 2.0 support.
* (feature) FinamHistorySource. OriginSide support.
* (feature) ExchangeBoard.CmeMini
* (feature) SampleChart. Select input security option.
* (bug) OrderLog -> Ticks fix.
* (feature) ExchangeBoard.Fxcm
* (feature) FxcmHistorySource.
* (feature) History. Fix naming (XXXSource -> XXXHistorySource).
* (feature) BaseGridControl. Interactive export process.
* (feature) Chart. Fix theme styles.
* (feature) Chart. Draw style quick access.
* (feature) Licensing. Strategy subscription support.
* (feature) Connector. Subscribe/unsubscribe methods for candles added.
* (feature) FXCM connector.
* (feature) Xaml.Code moved to Xaml proj.

## v4.3.24:
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

## v4.3.23:
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

## v4.3.22:
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

## v4.3.21:
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

## v4.3.19.5:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/279

## v4.3.19.4:
* (bug) FIX connection establish fix http://stocksharp.ru/posts/m/37571/

## v4.3.19.2:
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

## v4.3.19.1:
* (bug) SmartCOM candles fix.
* (bug) OpenECry candles fix.
* (bug) Backtesting fix.
* (bug) SecurityNativeIdMessageAdapter. Clone fixes.
* (feature) IndexSecurity. IgnoreErrors.

## v4.3.19:
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

## v4.3.18:
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

## v4.3.17:
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

## v4.3.16.1:
* (feature) MicexDownloader + FortsDownloader -> MoexDownloader
* (feature) StockSharp.Xaml.Code

## v4.3.16:
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

## v4.3.15:
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

## v4.3.14.2:
* (feature) Chart. Active orders.
* (bug) https://github.com/StockSharp/StockSharp/issues/222
* (feature) IBTrader -> InteractiveBrokersTrader, OECTrader -> OpenECryTrader
* (bug) TargetPlatformWindow fix.

## v4.3.14.1:
* (bug) Transaq. double <-> decimal conversation fix.
* (feature) Blackwood. Embed zlib into resources.
* (bug) Nuget fixed.

## v4.3.14:
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

## v4.3.13:
* (bug) MessageAdaptersPanel fix.
* (bug) Chart fix (draw values in hidden mode).
* (bug) SmartCOM sec info fix.
* (feature) Connectors. Doc + Icon attributes.

## v4.3.12:
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

## v4.3.11:
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

## v4.3.10
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

## v4.3.9.1:
* (feature) Candle.RelativeVolume is nullable.
* (feature) Candle. Ticks fields are nullable.
* (feature) CandleSerializer. Ticks fields.

## v4.3.9:
* (bug) RecoveryFactorParameter fix.
* (bug) OrderLogMarketDepthBuilder fix.
* (feature) Algo.Storages.Backup - clients for cloud storage backup services.
* (feature) FIX connector. More level1 fields support.
* (feature) LicenseHelper.LicenseError
* (feature) Connector. Not track ticks option.

## v4.3.8:
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

## v4.3.7:
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

## v4.3.6:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/90
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/93
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/92
* (feature) https://github.com/StockSharp/StockSharp/commit/62a19979280ab678679aee7660f73c9b9614de93

## v4.3.5:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/87
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/83
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/70
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/79
* (feature) FIX connector. Check sum is uint.
* (feature) https://github.com/StockSharp/StockSharp/pull/74
* (bug) https://github.com/StockSharp/StockSharp/pull/81
* (bug) Fix http://stocksharp.com/forum/yaf_postsm35263_FixServer-System-ArgumentOutOfRangeException.aspx#post35263

## v4.3.4:
* (bug) Fix https://github.com/StockSharp/StockSharp/issues/46
* (feature) OEC 3.5.14
* (feature) ILogSource.IsRoot
* (bug) Back testings. Generated data fixes.
* (bug) Emulator. Prevent big order book generation.
* (feature) Storage. Replace entity by messages.
* (feature) BitStamp. Level1 refresh interval is 10 sec.
* (feature) FIX. Check sum is uint.
* (bug) FixServer. Sync writers.

## v4.3.3:
* (feature) OverrideDll option.
* (bug) BasketMessageAdapter. Disconnect fix.
* (feature) Transaq. UTC
* (bug) Back testing. Fixes.

## v4.3.2:
* (bug) BTCE. Security decimals fix.

## v4.3.1:
* (feature) BitStamp, IQFeed, ETrade and Oanda source code.
* (bug) Fix http://stocksharp.com/forum/yaf_postst5619_Oshibka-nie-udalos--naiti-chast--puti.aspx

## v4.3.0:
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

## v4.2.75:
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

## v4.2.74:
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

## v4.2.73:
* (feature) FIX connector. Client side no longer use QuickFix
* (bug) LuaFixServer. Level1 thread safety.
* (bug) EntityCache. Fix ExecMsg.OridinTransId == OrderStatusMsg.TransactionId
* (feature) ExecutionMessage. OrderId, Balance, Volume, VisibleVolume, TradeId, TradePrice, TradeStatus is nullable.
* (feature) Security.Strike is nullable
* (feature) QuikOrderCondition. Nullable fields.

## v4.2.72:
* (feature) SecurityMessage.Decimals
* (feature) Security.State nullable
* (feature) IOHelper.ToFullPath
* (bug) FIX connector. Reconnection fix. Lost connection control fix.
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/33
* (bug) Micex. Decimals fix
* (bug) SecurityGrid. Fix PriceStep, Decimals and VolumeStep columns.

## v4.2.71:
* (feature) Strategy.StartedTime is DTO
* (bug) Fix http://stocksharp.com/forum/yaf_postst5556_S--Api.aspx
* (bug) Fix RtsHistorySource

## v4.2.70:
* (feature) SecurityGrid. Add columns.
* (feature) Security. O H L C V fields marked as nullable.
* (bug) FIX connector. Level1 small fix.
* (bug) Quik lua. Level1 value type fix.
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/31

## v4.2.69:
* (feature) LicensePanel. Xaml -> Licensing
* (feature) MarketEmu. Depth fill improve.
* (bug) LicenseTool fixes.

## v4.2.68:
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

## v4.2.67:
* (feature) Ecng update.

## v4.2.66:
* (feature) Quik lua. Support ALL@ALL security for market data subscription.
* (feature) Quik lua. Level1 subscription check optimization.
* (feature) Quik lua. Check Level1 duplicates.
* (feature) FixServer request id mapping refactoring.
* (bug) Plaza. Fix level1 time.
* (feature) OrderStatMsg. Single order details.
* (bug) Emulator small fix.

## v4.2.65:
* (feature) QuandlHistorySource
* (feature) Quoting refactoring
* (bug) Fix Quik LUA. Fix http://stocksharp.com/forum/yaf_postst5525_Oshibka-Lua-podkliuchieniia-pri-rabotie-s-aktsiiami.aspx
* (bug) Fix Quik LUA. Fix exception handling

## v4.2.64:
* (feature) Source codes for Quik and InteractiveBrokers

## v4.2.63:
* (bug) OpenECry. Fix double <-> decimal casting.
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/16

## v4.2.62:
* (bug) Micex. Format price fix
* (feature) Source codes for Messages, BE, Algo, Xaml, Localization, Logging, Community and few connectors (SmartCOM, AlfaDirect, Transaq, BTCE, OpenECry, LMAX, MatLab, CQG, Sterling, RSS, Alor)

## v4.2.61:
* (bug) Quik. Fix https://github.com/stocksharp/stocksharp/issues/13
* (bug) Fusion/Blackwood. Fix http://stocksharp.com/forum/yaf_postst5511_4-2-60---Exception-pri-otpravkie-ordiera.aspx
* (bug) LogManager.Application. Replacing fix
* (bug) Plaza. Level1 ServerTime fill
* (bug) YahooHistorySource. Time fix

## v4.2.60:
* (bug) Fusion/Blackwood. Fix http://stocksharp.com/forum/yaf_postst5498_Probliema-na-rieal-nom-schietie.aspx
* (feature) Quik. Process request performance boost
* (feature) OrderWindow. Disable ByMarket checkbox
* (feature) Plaza. Anonym deals turned on by default
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/11

## v4.2.59:
* (feature) Fusion/Blackwood. 3.1.8
* (bug) Micex. OrderBookDepth fix
* (bug) Micex. RequestAllDepths fix
* (bug) OpenECry. Order processing fix

## v4.2.58:
* (feature) Micex. RequestAllDepths
* (bug) Micex. Tick subscribe fix

## v4.2.57:
* (feature) QuikLua. Removed atomic reregister for micex
* (feature) Micex. Added IFC_Broker24 interface
* (feature) Micex. OrderBookDepth
* (bug) Unit fixes. Fix http://stocksharp.com/forum/yaf_postst5489_Izmieniena-loghika-raboty-s-Unit-v-novykh-viersiiakh.aspx
* (bug) OpenECry. Remoting fix
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/7
* (bug) Fix https://github.com/stocksharp/stocksharp/issues/3

## v4.2.56:
* (bug) AlfaDirect. Fix cadle subscription. Fix http://stocksharp.com/forum/yaf_postst5483_primier-SampleAlfaCandles.aspx
* (bug) OpenECry fixes
* (bug) Localization fixes
* (bug) Chart. Fix indicator adding

## v4.2.55:
* (feature) Export executions
* (bug) Localization fixes

## v4.2.54:
* (bug) Execution storage fix
* (bug) Plaza. Fix handling non Message based transaction
* (bug) Localization fixes

## v4.2.53:
* (bug) Localization fixes

## v4.2.52:
* (bug) Localization fixes

## v4.2.51:
* (bug) Oanda. Security lookup and market data subscription fixes
* (bug) Localization fixes
* (feature) OrderFail. ServerTime and LocalTime fields
* (bug) TrueFX and GainCapital historical sources fixes
* (bug) Unit. Fix serialization

## v4.2.50:
* (bug) Error loading candles from storage
* (bug) FortsDailyData.GetRate fix

## v4.2.49:
* (bug) PriceStep fixes for Oanda
* (bug) Filling empty Arg for Candles from CandleSeries
* (feature) Tick origin side added for FIX (QuikLua) http://stocksharp.com/forum/yaf_postst5476_S--API.aspx
* (feature) TimeZoneComboBox

## v4.2.48:
* (feature) Plaza supports MM and limit transactions
* (bug) Localization fixes for Xaml

## v4.2.47:
* (bug) Localization fixes

## v4.2.46:
* (feature) Filling Security.Status for QuikLua http://stocksharp.com/forum/yaf_postsm34270_Novyi-konniektor-k-Quik.aspx#post34270
* (bug) Building market depth from OL fixes
* (feature) Zero or negative prices for spreads in Storage
* (feature) ExpirationDate added to OrderWindow

## v4.2.0-4.2.43
Available on [forum](http://stocksharp.com/forum/yaf_postst4219_S--API-4-2.aspx)
