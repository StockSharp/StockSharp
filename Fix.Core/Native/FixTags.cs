// ReSharper disable InconsistentNaming
namespace StockSharp.Fix.Native;

/// <summary>
/// FIX protocol tags.
/// </summary>
public enum FixTags
{
	/// <summary>
	/// </summary>
	Account = 1,

	/// <summary>
	/// </summary>
	AvgPx = 6,

	/// <summary>
	/// </summary>
	BeginSeqNo = 7,

	/// <summary>
	/// </summary>
	BeginString = 8,

	/// <summary>
	/// </summary>
	BodyLength = 9,

	/// <summary>
	/// </summary>
	CheckSum = 10,

	/// <summary>
	/// </summary>
	ClOrdID = 11,

	/// <summary>
	/// </summary>
	Commission = 12,

	/// <summary>
	/// </summary>
	CommType = 13,

	/// <summary>
	/// </summary>
	CumQty = 14,

	/// <summary>
	/// </summary>
	Currency = 15,

	/// <summary>
	/// </summary>
	EndSeqNo = 16,

	/// <summary>
	/// </summary>
	ExecID = 17,

	/// <summary>
	/// </summary>
	ExecInst = 18,

	/// <summary>
	/// </summary>
	ExecRefId = 19,

	/// <summary>
	/// </summary>
	HandlInst = 21,

	/// <summary>
	/// </summary>
	IDSource = 22,

	/// <summary>
	/// </summary>
	LastCapacity = 29,

	/// <summary>
	/// </summary>
	LastPx = 31,

	/// <summary>
	/// </summary>
	LastQty = 32,

	/// <summary>
	/// </summary>
	MsgSeqNum = 34,

	/// <summary>
	/// </summary>
	MsgType = 35,

	/// <summary>
	/// </summary>
	NewSeqNo = 36,

	/// <summary>
	/// </summary>
	OrderID = 37,

	/// <summary>
	/// </summary>
	OrderQty = 38,

	/// <summary>
	/// </summary>
	OrdStatus = 39,

	/// <summary>
	/// </summary>
	OrdType = 40,

	/// <summary>
	/// </summary>
	OrigClOrdID = 41,

	/// <summary>
	/// </summary>
	OrigTime = 42,

	/// <summary>
	/// Indicates possible retransmission of message with this sequence number.
	/// </summary>
	PossDupFlag = 43,

	/// <summary>
	/// </summary>
	Price = 44,

	/// <summary>
	/// </summary>
	RefSeqNum = 45,

	/// <summary>
	/// </summary>
	SecurityID = 48,

	/// <summary>
	/// </summary>
	SenderCompID = 49,

	/// <summary>
	/// </summary>
	SenderSubID = 50,

	/// <summary>
	/// </summary>
	SendingTime = 52,

	/// <summary>
	/// </summary>
	Side = 54,

	/// <summary>
	/// </summary>
	Symbol = 55,

	/// <summary>
	/// </summary>
	TargetCompID = 56,

	/// <summary>
	/// </summary>
	Text = 58,

	/// <summary>
	/// </summary>
	TimeInForce = 59,

	/// <summary>
	/// </summary>
	TransactTime = 60,

	/// <summary>
	/// </summary>
	Urgency = 61,

	/// <summary>
	/// </summary>
	SettlType = 63,

	/// <summary>
	/// </summary>
	SettlDate = 64,

	/// <summary>
	/// </summary>
	SymbolSfx = 65,

	/// <summary>
	/// </summary>
	TradeDate = 75,

	/// <summary>
	/// </summary>
	ExecBroker = 76,

	/// <summary>
	/// </summary>
	PositionEffect = 77,

	/// <summary>
	/// </summary>
	RptSeq = 83,

	/// <summary>
	/// </summary>
	RawDataLength = 95,

	/// <summary>
	/// </summary>
	RawData = 96,

	/// <summary>
	/// Indicates that message may contain information that has been sent under another sequence number.
	/// </summary>
	PossResend = 97,

	/// <summary>
	/// </summary>
	EncryptMethod = 98,

	/// <summary>
	/// </summary>
	StopPx = 99,

	/// <summary>
	/// </summary>
	ExDestination = 100,

	/// <summary>
	/// </summary>
	CxlRejReason = 102,

	/// <summary>
	/// </summary>
	SecurityDesc = 107,

	/// <summary>
	/// </summary>
	HeartBtInt = 108,

	/// <summary>
	/// </summary>
	ClientID = 109,

	/// <summary>
	/// </summary>
	MinQty = 110,

	/// <summary>
	/// </summary>
	MaxFloor = 111,

	/// <summary>
	/// </summary>
	TestReqID = 112,

	/// <summary>
	/// </summary>
	QuoteID = 117,

	/// <summary>
	/// </summary>
	SettlCurrency = 120,

	/// <summary>
	/// Original time of message transmission (always expressed in UTC) when transmitting orders as the result of a resend request.
	/// </summary>
	OrigSendingTime = 122,

	/// <summary>
	/// </summary>
	GapFillFlag = 123,

	/// <summary>
	/// </summary>
	ExpireTime = 126,

	/// <summary>
	/// </summary>
	QuoteReqID = 131,

	/// <summary>
	/// </summary>
	BidPx = 132,

	/// <summary>
	/// </summary>
	OfferPx = 133,

	/// <summary>
	/// </summary>
	BidSize = 134,

	/// <summary>
	/// </summary>
	OfferSize = 135,

	/// <summary>
	/// </summary>
	PrevClosePx = 140,

	/// <summary>
	/// </summary>
	ResetSeqNumFlag = 141,

	/// <summary>
	/// </summary>
	NoRelatedSym = 146,

	/// <summary>
	/// </summary>
	Headline = 148,

	/// <summary>
	/// </summary>
	URLLink = 149,

	/// <summary>
	/// </summary>
	ExecType = 150,

	/// <summary>
	/// </summary>
	LeavesQty = 151,

	/// <summary>
	/// </summary>
	SecurityType = 167,

	/// <summary>
	/// </summary>
	EffectiveTime = 168,

	/// <summary>
	/// </summary>
	SecondaryOrderID = 198,

	/// <summary>
	/// </summary>
	MaturityMonthYear = 200,

	/// <summary>
	/// </summary>
	PutOrCall = 201,

	/// <summary>
	/// </summary>
	StrikePrice = 202,

	/// <summary>
	/// </summary>
	MaturityDay = 205,

	/// <summary>
	/// </summary>
	SecurityExchange = 207,

	/// <summary>
	/// </summary>
	PegOffsetValue = 211,

	/// <summary>
	/// </summary>
	XmlDataLen = 212,

	/// <summary>
	/// </summary>
	XmlData = 213,

	/// <summary>
	/// </summary>
	IssueDate = 225,

	/// <summary>
	/// </summary>
	Factor = 228,

	/// <summary>
	/// </summary>
	ContractMultiplier = 231,

	/// <summary>
	/// </summary>
	Yield = 236,

	/// <summary>
	/// </summary>
	RedemptionDate = 240,

	/// <summary>
	/// </summary>
	LegIssueDate = 249,

	/// <summary>
	/// </summary>
	MDReqID = 262,

	/// <summary>
	/// </summary>
	SubscriptionRequestType = 263,

	/// <summary>
	/// </summary>
	MarketDepth = 264,

	/// <summary>
	/// </summary>
	MDUpdateType = 265,

	/// <summary>
	/// </summary>
	NoMDEntryTypes = 267,

	/// <summary>
	/// </summary>
	NoMDEntries = 268,

	/// <summary>
	/// </summary>
	MDEntryType = 269,

	/// <summary>
	/// </summary>
	MDEntryPx = 270,

	/// <summary>
	/// </summary>
	MDEntrySize = 271,

	/// <summary>
	/// </summary>
	MDEntryDate = 272,

	/// <summary>
	/// </summary>
	MDEntryTime = 273,

	/// <summary>
	/// </summary>
	TickDirection = 274,

	/// <summary>
	/// </summary>
	QuoteCondition = 276,

	/// <summary>
	/// </summary>
	MDEntryId = 278,

	/// <summary>
	/// </summary>
	MDUpdateAction = 279,

	/// <summary>
	/// </summary>
	MDReqRejReason = 281,

	/// <summary>
	/// </summary>
	MDEntryOriginator = 282,

	/// <summary>
	/// </summary>
	OpenCloseSettleFlag = 286,

	/// <summary>
	/// </summary>
	MDEntryBuyer = 288,

	/// <summary>
	/// </summary>
	MDEntrySeller = 289,

	/// <summary>
	/// </summary>
	MDEntryPositionNo = 290,

	/// <summary>
	/// </summary>
	NoQuoteEntries = 295,

	/// <summary>
	/// </summary>
	NoQuoteSets = 296,

	/// <summary>
	/// </summary>
	QuoteCancelType = 298,

	/// <summary>
	/// </summary>
	QuoteEntryID = 299,

	/// <summary>
	/// </summary>
	QuoteSetID = 302,

	/// <summary>
	/// </summary>
	TotNoQuoteEntries = 304,

	/// <summary>
	/// </summary>
	UnderlyingIDSource = 305,

	/// <summary>
	/// </summary>
	UnderlyingSecurityDesc = 307,

	/// <summary>
	/// </summary>
	UnderlyingSecurityExchange = 308,

	/// <summary>
	/// </summary>
	UnderlyingSecurityID = 309,

	/// <summary>
	/// </summary>
	UnderlyingSecurityType = 310,

	/// <summary>
	/// </summary>
	UnderlyingSymbol = 311,

	/// <summary>
	/// </summary>
	UnderlyingSymbolSfx = 312,

	/// <summary>
	/// </summary>
	UnderlyingMaturityMonthYear = 313,

	/// <summary>
	/// </summary>
	UnderlyingPutOrCall = 315,

	/// <summary>
	/// </summary>
	UnderlyingStrikePrice = 316,

	/// <summary>
	/// </summary>
	UnderlyingCurrency = 318,

	/// <summary>
	/// </summary>
	RatioQty = 319,

	/// <summary>
	/// </summary>
	SecurityReqID = 320,

	/// <summary>
	/// </summary>
	SecurityRequestType = 321,

	/// <summary>
	/// </summary>
	SecurityResponseID = 322,

	/// <summary>
	/// </summary>
	SecurityResponseType = 323,

	/// <summary>
	/// </summary>
	SecurityStatusReqID = 324,

	/// <summary>
	/// </summary>
	UnsolicitedIndicator = 325,

	/// <summary>
	/// </summary>
	SecurityTradingStatus = 326,

	/// <summary>
	/// </summary>
	HighPx = 332,

	/// <summary>
	/// </summary>
	LowPx = 333,

	/// <summary>
	/// </summary>
	TradSesReqID = 335,

	/// <summary>
	/// </summary>
	TradingSessionID = 336,

	/// <summary>
	/// </summary>
	TradSesStatus = 340,

	/// <summary>
	/// </summary>
	NumberOfOrders = 346,

	/// <summary>
	/// </summary>
	LastMsgSeqNumProcessed = 369,

	/// <summary>
	/// </summary>
	RefTagID = 371,

	/// <summary>
	/// </summary>
	RefMsgType = 372,

	/// <summary>
	/// </summary>
	SessionRejectReason = 373,

	/// <summary>
	/// </summary>
	ContraBroker = 375,

	/// <summary>
	/// </summary>
	ExecRestatementReason = 378,

	/// <summary>
	/// </summary>
	BusinessRejectReason = 380,

	/// <summary>
	/// </summary>
	GrossTradeAmt = 381,

	/// <summary>
	/// </summary>
	NoTradingSessions = 386,

	/// <summary>
	/// </summary>
	TotalVolumeTraded = 387,

	/// <summary>
	/// </summary>
	TotNoRelatedSym = 393,

	/// <summary>
	/// </summary>
	PriceType = 423,

	/// <summary>
	/// </summary>
	ExpireDate = 432,

	/// <summary>
	/// </summary>
	CxlRejResponseTo = 434,

	/// <summary>
	/// </summary>
	PartyIDSource = 447,

	/// <summary>
	/// </summary>
	PartyID = 448,

	/// <summary>
	/// </summary>
	PartyRole = 452,

	/// <summary>
	/// </summary>
	NoPartyIDs = 453,

	/// <summary>
	/// </summary>
	NoSecurityAltID = 454,

	/// <summary>
	/// </summary>
	SecurityAltID = 455,

	/// <summary>
	/// </summary>
	SecurityAltIDSource = 456,

	/// <summary>
	/// </summary>
	Product = 460,

	/// <summary>
	/// </summary>
	CFICode = 461,

	/// <summary>
	/// </summary>
	CommCurrency = 479,

	/// <summary>
	/// </summary>
	NestedPartyID = 524,

	/// <summary>
	/// </summary>
	NestedPartyIDSource = 525,

	/// <summary>
	/// </summary>
	SecondaryClOrdID = 526,

	/// <summary>
	/// </summary>
	OrderCapacity = 528,

	/// <summary>
	/// </summary>
	OrderRestrictions = 529,

	/// <summary>
	/// </summary>
	MassCancelRequestType = 530,

	/// <summary>
	/// </summary>
	MassCancelResponse = 531,

	/// <summary>
	/// </summary>
	MassCancelRejectReason = 532,

	/// <summary>
	/// </summary>
	QuoteType = 537,

	/// <summary>
	/// </summary>
	NestedPartyRole = 538,

	/// <summary>
	/// </summary>
	NoNestedPartyIDs = 539,

	/// <summary>
	/// </summary>
	CashMargin = 544,

	/// <summary>
	/// </summary>
	Scope = 546,

	/// <summary>
	/// </summary>
	NoSides = 552,

	/// <summary>
	/// </summary>
	Username = 553,

	/// <summary>
	/// </summary>
	Password = 554,

	/// <summary>
	/// </summary>
	NoLegs = 555,

	/// <summary>
	/// </summary>
	LegCurrency = 556,

	/// <summary>
	/// </summary>
	NoSecurityTypes = 558,

	/// <summary>
	/// </summary>
	SecurityListRequestType = 559,

	/// <summary>
	/// </summary>
	SecurityRequestResult = 560,

	/// <summary>
	/// </summary>
	RoundLot = 561,

	/// <summary>
	/// </summary>
	MinTradeVol = 562,

	/// <summary>
	/// </summary>
	TradSesStatusRejReason = 567,

	/// <summary>
	/// </summary>
	AccountType = 581,

	/// <summary>
	/// </summary>
	CustOrderCapacity = 582,

	/// <summary>
	/// </summary>
	MassStatusReqID = 584,

	/// <summary>
	/// </summary>
	MassStatusReqType = 585,

	/// <summary>
	/// </summary>
	LegSymbol = 600,

	/// <summary>
	/// </summary>
	LegSymbolSfx = 601,

	/// <summary>
	/// </summary>
	LegSecurityID = 602,

	/// <summary>
	/// </summary>
	LegSecurityIDSource = 603,

	/// <summary>
	/// </summary>
	NoLegSecurityAltID = 604,

	/// <summary>
	/// </summary>
	LegSecurityAltID = 605,

	/// <summary>
	/// </summary>
	LegSecurityAltIDSource = 606,

	/// <summary>
	/// </summary>
	LegProduct = 607,

	/// <summary>
	/// </summary>
	LegCFICode = 608,

	/// <summary>
	/// </summary>
	LegSecurityType = 609,

	/// <summary>
	/// </summary>
	LegMaturityMonthYear = 610,

	/// <summary>
	/// </summary>
	LegMaturityDate = 611,

	/// <summary>
	/// </summary>
	LegStrikePrice = 612,

	/// <summary>
	/// </summary>
	LegOptAttribute = 613,

	/// <summary>
	/// </summary>
	LegContractMultiplier = 614,

	/// <summary>
	/// </summary>
	LegCouponRate = 615,

	/// <summary>
	/// </summary>
	LegSecurityExchange = 616,

	/// <summary>
	/// </summary>
	LegIssuer = 617,

	/// <summary>
	/// </summary>
	EncodedLegIssuerLen = 618,

	/// <summary>
	/// </summary>
	EncodedLegIssuer = 619,

	/// <summary>
	/// </summary>
	LegSecurityDesc = 620,

	/// <summary>
	/// </summary>
	EncodedLegSecurityDescLen = 621,

	/// <summary>
	/// </summary>
	EncodedLegSecurityDesc = 622,

	/// <summary>
	/// </summary>
	LegRatioQty = 623,

	/// <summary>
	/// </summary>
	LegSide = 624,

	/// <summary>
	/// </summary>
	TradingSessionSubID = 625,

	/// <summary>
	/// </summary>
	Price2 = 640,

	/// <summary>
	/// </summary>
	QuoteStatusReqID = 649,

	/// <summary>
	/// </summary>
	QuoteRequestRejectReason = 658,

	/// <summary>
	/// </summary>
	AcctIDSource = 660,

	/// <summary>
	/// </summary>
	ContractSettlMonth = 667,

	/// <summary>
	/// </summary>
	NoPositions = 702,

	/// <summary>
	/// </summary>
	PosType = 703,

	/// <summary>
	/// </summary>
	LongQty = 704,

	/// <summary>
	/// </summary>
	ShortQty = 705,

	/// <summary>
	/// </summary>
	PosQtyStatus = 706,

	/// <summary>
	/// </summary>
	PosAmtType = 707,

	/// <summary>
	/// </summary>
	PosAmt = 708,

	/// <summary>
	/// </summary>
	PosReqID = 710,

	/// <summary>
	/// </summary>
	NoUnderlyings = 711,

	/// <summary>
	/// </summary>
	ClearingBusinessDate = 715,

	/// <summary>
	/// </summary>
	PosMaintRptID = 721,

	/// <summary>
	/// </summary>
	PosReqType = 724,

	/// <summary>
	/// </summary>
	PosReqResult = 728,

	/// <summary>
	/// </summary>
	PosReqStatus = 729,

	/// <summary>
	/// </summary>
	NoPosAmt = 753,

	/// <summary>
	/// </summary>
	SecuritySubType = 762,

	/// <summary>
	/// </summary>
	NoTrdRegTimestamps = 768,

	/// <summary>
	/// </summary>
	TrdRegTimestamp = 769,

	/// <summary>
	/// </summary>
	TrdRegTimestampType = 770,

	/// <summary>
	/// </summary>
	NextExpectedMsgSeqNum = 789,

	/// <summary>
	/// </summary>
	OrdStatusReqID = 790,

	/// <summary>
	/// </summary>
	ApplQueueDepth = 813,

	/// <summary>
	/// </summary>
	ApplQueueResolution = 814,

	/// <summary>
	/// </summary>
	TrdType = 828,

	/// <summary>
	/// </summary>
	PegOffsetType = 836,

	/// <summary>
	/// </summary>
	LastLiquidityInd = 851,

	/// <summary>
	/// </summary>
	PublishTrdIndicator = 852,

	/// <summary>
	/// </summary>
	NoInstrAttrib = 870,

	/// <summary>
	/// </summary>
	InstrAttribType = 871,

	/// <summary>
	/// </summary>
	InstrAttribValue = 872,

	/// <summary>
	/// </summary>
	LastFragment = 893,

	/// <summary>
	/// </summary>
	TotNumReports = 911,

	/// <summary>
	/// </summary>
	AgreementID = 914,

	/// <summary>
	/// </summary>
	StartDate = 916,

	/// <summary>
	/// </summary>
	EndDate = 917,

	/// <summary>
	/// </summary>
	UserRequestID = 923,

	/// <summary>
	/// </summary>
	UserRequestType = 924,

	/// <summary>
	/// </summary>
	NewPassword = 925,

	/// <summary>
	/// </summary>
	UserStatus = 926,

	/// <summary>
	/// </summary>
	UserStatusText = 927,

	/// <summary>
	/// </summary>
	NoStrategyParameters = 957,

	/// <summary>
	/// </summary>
	StrategyParameterName = 958,

	/// <summary>
	/// </summary>
	StrategyParameterType = 959,

	/// <summary>
	/// </summary>
	StrategyParameterValue = 960,

	/// <summary>
	/// </summary>
	SecurityStatus = 965,

	/// <summary>
	/// </summary>
	MinPriceIncrement = 969,

	/// <summary>
	/// </summary>
	TradeVolume = 1020,

	/// <summary>
	/// </summary>
	MatchIncrement = 1089,

	/// <summary>
	/// </summary>
	MDEntryForwardPoints = 1027,

	/// <summary>
	/// </summary>
	ManualOrderIndicator = 1028,

	/// <summary>
	/// </summary>
	AggressorIndicator = 1057,

	/// <summary>
	/// </summary>
	OrderCategory = 1115,

	/// <summary>
	/// </summary>
	ApplVerId = 1128,

	/// <summary>
	/// </summary>
	CstmApplVerID = 1129,

	/// <summary>
	/// </summary>
	DefaultApplVerID = 1137,

	/// <summary>
	/// </summary>
	DisplayQty = 1138,

	/// <summary>
	/// </summary>
	MaxTradeVol = 1140,

	/// <summary>
	/// </summary>
	SecurityGroup = 1151,

	/// <summary>
	/// </summary>
	ApplId = 1180,

	/// <summary>
	/// </summary>
	ApplSeqNum = 1181,

	/// <summary>
	/// </summary>
	ApplBegSeqNum = 1182,

	/// <summary>
	/// </summary>
	ApplEndSeqNum = 1183,

	/// <summary>
	/// </summary>
	ApplReqID = 1346,

	/// <summary>
	/// </summary>
	ApplReqType = 1347,

	/// <summary>
	/// </summary>
	ApplRespType = 1348,

	/// <summary>
	/// </summary>
	ApplLastSeqNum = 1350,

	/// <summary>
	/// </summary>
	ApplRespID = 1353,

	/// <summary>
	/// </summary>
	ApplReportID = 1356,

	/// <summary>
	/// </summary>
	RefApplLastSeqNum = 1357,

	/// <summary>
	/// </summary>
	ApplReportType = 1426,

	/// <summary>
	/// </summary>
	MDStreamID = 1500,

	/// <summary>
	/// </summary>
	NoApplIDs = 1351,

	/// <summary>
	/// </summary>
	ApplResendFlag = 1352,

	/// <summary>
	/// </summary>
	ApplRespError = 1354,

	/// <summary>
	/// </summary>
	RefApplID = 1355,

	/// <summary>
	/// </summary>
	MDEntryArg = 10000,

	/// <summary>
	/// </summary>
	MDOtherValue = 15000,

	/// <summary>
	/// </summary>
	ExtendedOrderStatus = 17000,

	/// <summary>
	/// </summary>
	ExtendedTradeStatus = 17001,

	///// <summary>
	///// </summary>
	//FromMarketData = 20000,

	///// <summary>
	///// </summary>
	//ToMarketData = 20001,

	/// <summary>
	/// </summary>
	CandleState = 20002,

	/// <summary>
	/// </summary>
	IssueSize = 20003,

	/// <summary>
	/// </summary>
	Formula = 20004,

	/// <summary>
	/// </summary>
	LegRoundLot = 20005,

	/// <summary>
	/// </summary>
	LegMinTradeVol = 20006,

	/// <summary>
	/// </summary>
	LegNoInstrAttrib = 20007,

	/// <summary>
	/// </summary>
	LegInstrAttribType = 20008,

	/// <summary>
	/// </summary>
	LegInstrAttribValue = 20009,

	/// <summary>
	/// </summary>
	LegEndDate = 20010,

	/// <summary>
	/// </summary>
	LegIssueSize = 20011,

	/// <summary>
	/// </summary>
	SessionPeriods = 20012,

	/// <summary>
	/// </summary>
	SessionSpecialDays = 20013,

	/// <summary>
	/// </summary>
	Obsolete = 20014,

	/// <summary>
	/// </summary>
	Name = 20015,

	/// <summary>
	/// </summary>
	StrategyTypeId = 20016,

	/// <summary>
	/// </summary>
	Id = 20017,

	/// <summary>
	/// </summary>
	GroupId = 20018,

	///// <summary>
	///// </summary>
	//ParameterName = 20019,

	///// <summary>
	///// </summary>
	//ParameterType = 20020,

	///// <summary>
	///// </summary>
	//ParameterValue = 20021,

	/// <summary>
	/// </summary>
	Command = 20022,

	///// <summary>
	///// </summary>
	//MinVolume = 20023,

	/// <summary>
	/// </summary>
	Shortable = 20024,

	///// <summary>
	///// </summary>
	//StrategyTypeAssembly = 20025,

	/// <summary>
	/// </summary>
	AllowBuildFromSmallerTimeFrame = 20026,

	/// <summary>
	/// </summary>
	CalcVolumeProfile = 20027,

	/// <summary>
	/// </summary>
	FinishedCandles = 20028,

	/// <summary>
	/// </summary>
	MarketDataBuildMode = 20029,

	/// <summary>
	/// </summary>
	MarketDataBuildFrom = 20030,

	/// <summary>
	/// </summary>
	MarketDataBuildField = 20031,

	/// <summary>
	/// </summary>
	RegularTradingHours = 20032,

	/// <summary>
	/// </summary>
	MarketDataCount = 20033,

	/// <summary>
	/// </summary>
	NoIpRestrictions = 20034,

	/// <summary>
	/// </summary>
	IpRestrictions = 20035,

	/// <summary>
	/// </summary>
	NoPermissions = 20036,

	/// <summary>
	/// </summary>
	Permissions = 20037,

	/// <summary>
	/// </summary>
	NoPermissionsValues = 20038,

	/// <summary>
	/// </summary>
	FaceValue = 20039,

	/// <summary>
	/// </summary>
	MDResponseID = 20040,

	/// <summary>
	/// </summary>
	Language = 20041,

	/// <summary>
	/// </summary>
	Topic = 20042,

	/// <summary>
	/// </summary>
	ContentType = 20043,

	/// <summary>
	/// </summary>
	Content = 20044,

	/// <summary>
	/// </summary>
	Owner = 20045,

	/// <summary>
	/// </summary>
	Picture = 20046,

	/// <summary>
	/// </summary>
	Revision = 20047,

	/// <summary>
	/// </summary>
	Private = 20048,

	/// <summary>
	/// </summary>
	Colocation = 20049,

	/// <summary>
	/// </summary>
	PromoPrice = 20050,

	/// <summary>
	/// </summary>
	PromoEnd = 20051,

	/// <summary>
	/// </summary>
	DisplayName = 20052,

	/// <summary>
	/// </summary>
	Phone = 20053,

	/// <summary>
	/// </summary>
	Homepage = 20054,

	/// <summary>
	/// </summary>
	Skype = 20055,

	/// <summary>
	/// </summary>
	City = 20056,

	/// <summary>
	/// </summary>
	Gender = 20057,

	/// <summary>
	/// </summary>
	Subscription = 20058,

	/// <summary>
	/// </summary>
	Token = 20059,

	/// <summary>
	/// </summary>
	PrimaryCode = 20060,

	/// <summary>
	/// </summary>
	PrimaryBoard = 20061,

	/// <summary>
	/// </summary>
	Format = 20062,

	/// <summary>
	/// </summary>
	Hash = 20063,

	/// <summary>
	/// </summary>
	DownloadCount = 20064,

	/// <summary>
	/// </summary>
	Rating = 20065,

	/// <summary>
	/// </summary>
	DocUrl = 20066,

	/// <summary>
	/// </summary>
	SupportedPlugins = 20067,

	/// <summary>
	/// </summary>
	Platform = 20068,

	/// <summary>
	/// </summary>
	HardwareId = 20069,

	/// <summary>
	/// </summary>
	OnlyId = 20070,

	/// <summary>
	/// </summary>
	UpTicks = 20071,

	/// <summary>
	/// </summary>
	DownTicks = 20072,

	/// <summary>
	/// </summary>
	TotalTicks = 20073,

	/// <summary>
	/// </summary>
	LevelPrice = 20074,

	/// <summary>
	/// </summary>
	LevelType = 20075,

	/// <summary>
	/// </summary>
	PostOnly = 20076,

	/// <summary>
	/// </summary>
	Slippage = 20077,

	/// <summary>
	/// </summary>
	MDEntryPositionExtra = 20078,

	/// <summary>
	/// </summary>
	LevelPriceBuyCount = 20079,

	/// <summary>
	/// </summary>
	LevelPriceSellCount = 20080,

	/// <summary>
	/// </summary>
	LevelPriceBuyVolume = 20081,

	/// <summary>
	/// </summary>
	LevelPriceSellVolume = 20082,

	/// <summary>
	/// </summary>
	LevelPriceTotalVolume = 20083,

	/// <summary>
	/// </summary>
	BuildFromType = 20084,

	/// <summary>
	/// </summary>
	BuildFromArg = 20085,

	/// <summary>
	/// </summary>
	OldPrice = 20086,

	/// <summary>
	/// </summary>
	OldVolume = 20087,

	/// <summary>
	/// </summary>
	Leverage = 20088,

	/// <summary>
	/// </summary>
	MarketDataSkip = 20089,

	/// <summary>
	/// </summary>
	Repository = 20090,

	/// <summary>
	/// </summary>
	Currency2 = 20091,

	/// <summary>
	/// </summary>
	MonthlyPrice = 20092,

	/// <summary>
	/// </summary>
	MonthlyPriceCurrency = 20093,

	/// <summary>
	/// </summary>
	LifetimePrice = 20094,

	/// <summary>
	/// </summary>
	LifetimePriceCurrency = 20095,

	/// <summary>
	/// </summary>
	NoStubCount = 20096,

	/// <summary>
	/// </summary>
	RealVersion = 20097,

	/// <summary>
	/// </summary>
	StubVersion = 20098,

	/// <summary>
	/// </summary>
	DiscountMonthlyPrice = 20099,

	/// <summary>
	/// </summary>
	DiscountMonthlyPriceCurrency = 20100,

	/// <summary>
	/// </summary>
	DiscountAnnualPrice = 20101,

	/// <summary>
	/// </summary>
	DiscountAnnualPriceCurrency = 20102,

	/// <summary>
	/// </summary>
	DiscountLifetimePrice = 20103,

	/// <summary>
	/// </summary>
	DiscountLifetimePriceCurrency = 20104,

	/// <summary>
	/// </summary>
	RenewMonthlyPrice = 20105,

	/// <summary>
	/// </summary>
	RenewMonthlyPriceCurrency = 20106,

	/// <summary>
	/// </summary>
	TrialAllow = 20107,

	/// <summary>
	/// </summary>
	TrialRequested = 20108,

	/// <summary>
	/// </summary>
	Text2 = 20109,

	/// <summary>
	/// </summary>
	ProductCategories = 20110,

	/// <summary>
	/// </summary>
	RenewAnnualPrice = 20111,

	/// <summary>
	/// </summary>
	RenewAnnualPriceCurrency = 20112,

	/// <summary>
	/// </summary>
	Description = 20115,

	/// <summary>
	/// </summary>
	Description2 = 20116,

	/// <summary>
	/// </summary>
	OptionStyle = 20117,

	/// <summary>
	/// </summary>
	NoMDFields = 20118,

	/// <summary>
	/// </summary>
	MDField = 20119,

	/// <summary>
	/// </summary>
	LicenseFeatureId = 20120,

	/// <summary>
	/// </summary>
	ComponentTimestamp = 20121,

	/// <summary>
	/// </summary>
	SupportLicensing = 20122,

	/// <summary>
	/// </summary>
	DemoOnly = 20123,

	/// <summary>
	/// </summary>
	IncludeDates = 20124,

	/// <summary>
	/// </summary>
	TakeProfit = 20125,

	/// <summary>
	/// </summary>
	DisableArchive = 20126,

	/// <summary>
	/// </summary>
	CancelMode = 20127,

	/// <summary>
	/// Request incremental updates instead of full snapshots.
	/// </summary>
	IsIncremental = 20128,

	/// <summary>
	/// User identifier for filtering data.
	/// </summary>
	RequestUserId = 20129,

	/// <summary>
	/// Market price at the moment of order registration.
	/// </summary>
	MarketPrice = 20130,
}
