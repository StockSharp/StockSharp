namespace StockSharp.Fix.Native;

/// <summary>
/// Encryption methods.
/// </summary>
public enum EncryptMethod
{
	/// <summary>
	/// None.
	/// </summary>
	None,

	/// <summary>
	/// PKCS method.
	/// </summary>
	PKCS,

	/// <summary>
	/// DES method.
	/// </summary>
	DES,

	/// <summary>
	/// PKCS/DES method.
	/// </summary>
	PKCS_DES,

	/// <summary>
	/// PGP/DES method.
	/// </summary>
	PGP_DES,

	/// <summary>
	/// PGP/DES MD5 method.
	/// </summary>
	PGP_DES_MD5,

	/// <summary>
	/// PEM/DES MD5 method.
	/// </summary>
	PEM_DES_MD5
}

/// <summary>
/// Session states.
/// </summary>
public enum TradSesStatus
{
	/// <summary>
	/// Unknown.
	/// </summary>
	Unknown,

	/// <summary>
	/// Suspended.
	/// </summary>
	Halted,

	/// <summary>
	/// Opened.
	/// </summary>
	Open,

	/// <summary>
	/// Closed.
	/// </summary>
	Closed,

	/// <summary>
	/// Open pending.
	/// </summary>
	PreOpen,

	/// <summary>
	/// Close pending.
	/// </summary>
	PreClose,

	/// <summary>
	/// Session state request error.
	/// </summary>
	RequestRejected
}

/// <summary>
/// Session state request error message.
/// </summary>
public enum TradSesStatusRejReason
{
	/// <summary>
	/// Unknown session.
	/// </summary>
	InvalidSession = 1,

	/// <summary>
	/// Other.
	/// </summary>
	Other = 99,
}

/// <summary>
/// The user request states.
/// </summary>
public enum UserStatus
{
	/// <summary>
	/// Logged in.
	/// </summary>
	LoggedIn = 1,

	/// <summary>
	/// Not logged in.
	/// </summary>
	NotLoggedIn = 2,

	/// <summary>
	/// User not found.
	/// </summary>
	UserNotRecognised = 3,

	/// <summary>
	/// Incorrect password.
	/// </summary>
	PasswordIncorrect = 4,

	/// <summary>
	/// The password was changed.
	/// </summary>
	PasswordChanged = 5,

	/// <summary>
	/// </summary>
	Other = 6,

	/// <summary>
	/// </summary>
	ForcedUserLogoutByExchange = 7,

	/// <summary>
	/// </summary>
	SessionShutdownWarning = 8,
}

/// <summary>
/// </summary>
public enum QuoteType
{
	/// <summary>
	/// </summary>
	Indicative,

	/// <summary>
	/// </summary>
	Tradeable,

	/// <summary>
	/// </summary>
	RestrictedTradeable,

	/// <summary>
	/// </summary>
	Counter,
}

/// <summary>
/// </summary>
public static class Side
{
	/// <summary>
	/// </summary>
	public const char Buy = '1';

	/// <summary>
	/// </summary>
	public const char Sell = '2';

	/// <summary>
	/// </summary>
	public const char BuyMinus = '3';

	/// <summary>
	/// </summary>
	public const char SellPlus = '4';

	/// <summary>
	/// </summary>
	public const char SellShort = '5';

	/// <summary>
	/// </summary>
	public const char SellShortExempt = '6';

	/// <summary>
	/// </summary>
	public const char Undisclosed = '7';

	/// <summary>
	/// </summary>
	public const char Cross = '8';

	/// <summary>
	/// </summary>
	public const char CrossShort = '9';

	/// <summary>
	/// </summary>
	public const char CrossShortExempt = 'A';

	/// <summary>
	/// </summary>
	public const char AsDefined = 'B';

	/// <summary>
	/// </summary>
	public const char Opposite = 'C';

	/// <summary>
	/// </summary>
	public const char Subscribe = 'D';

	/// <summary>
	/// </summary>
	public const char Redeem = 'E';

	/// <summary>
	/// </summary>
	public const char Lend = 'F';

	/// <summary>
	/// </summary>
	public const char Borrow = 'G';
}

/// <summary>
/// </summary>
public static class MDEntryType
{
	/// <summary>
	/// </summary>
	public const char Bid = '0';

	/// <summary>
	/// </summary>
	public const char Offer = '1';

	/// <summary>
	/// </summary>
	public const char Trade = '2';

	/// <summary>
	/// </summary>
	public const char IndexValue = '3';

	/// <summary>
	/// </summary>
	public const char OpeningPrice = '4';

	/// <summary>
	/// </summary>
	public const char ClosingPrice = '5';

	/// <summary>
	/// </summary>
	public const char SettlementPrice = '6';

	/// <summary>
	/// </summary>
	public const char TradingSessionHighPrice = '7';

	/// <summary>
	/// </summary>
	public const char TradingSessionLowPrice = '8';

	/// <summary>
	/// </summary>
	public const char TradingSessionVWAPPrice = '9';

	/// <summary>
	/// </summary>
	public const char Imbalance = 'A';

	/// <summary>
	/// </summary>
	public const char TradeVolume = 'B';

	/// <summary>
	/// </summary>
	public const char OpenInterest = 'C';

	/// <summary>
	/// </summary>
	public const char CompositeUnderlyingPrice = 'D';

	/// <summary>
	/// </summary>
	public const char SimulatedSellPrice = 'E';

	/// <summary>
	/// </summary>
	public const char SimulatedBuyPrice = 'F';

	/// <summary>
	/// </summary>
	public const char MarginRate = 'G';

	/// <summary>
	/// </summary>
	public const char MidPrice = 'H';

	/// <summary>
	/// </summary>
	public const char EmptyBook = 'J';

	/// <summary>
	/// </summary>
	public const char SettleHighPrice = 'K';

	/// <summary>
	/// </summary>
	public const char SettleLowPrice = 'L';

	/// <summary>
	/// </summary>
	public const char PriorSettlePrice = 'M';

	/// <summary>
	/// </summary>
	public const char SessionHighBid = 'N';

	/// <summary>
	/// </summary>
	public const char SessionLowOffer = 'O';

	/// <summary>
	/// </summary>
	public const char EarlyPrices = 'P';

	/// <summary>
	/// </summary>
	public const char AuctionClearingPrice = 'Q';

	/// <summary>
	/// </summary>
	public const char SwapValueFactor = 'S';

	/// <summary>
	/// </summary>
	public const char DailyValueAdjustmentForLongPositions = 'R';

	/// <summary>
	/// </summary>
	public const char CumulativeValueAdjustmentForLongPositions = 'T';

	/// <summary>
	/// </summary>
	public const char DailyValueAdjustmentForShortPositions = 'U';

	/// <summary>
	/// </summary>
	public const char CumulativeValueAdjustmentForShortPositions = 'V';

	/// <summary>
	/// </summary>
	public const char RecoveryRate = 'Y';

	/// <summary>
	/// </summary>
	public const char RecoveryRateForLong = 'Z';

	/// <summary>
	/// </summary>
	public const char RecoveryRateForShort = 'a';

	/// <summary>
	/// </summary>
	public const char FixingPrice = 'W';

	/// <summary>
	/// </summary>
	public const char CashRate = 'X';
}

/// <summary>
/// Actions.
/// </summary>
public static class MDUpdateAction
{
	/// <summary>
	/// Create.
	/// </summary>
	public const char New = '0';

	/// <summary>
	/// Change.
	/// </summary>
	public const char Change = '1';

	/// <summary>
	/// Delete.
	/// </summary>
	public const char Delete = '2';

	/// <summary>
	/// Delete thru.
	/// </summary>
	public const char DeleteThru = '3';

	/// <summary>
	/// Delete from.
	/// </summary>
	public const char DeleteFrom = '4';

	/// <summary>
	/// Overlay.
	/// </summary>
	public const char Overlay = '5';
}

/// <summary>
/// Option types.
/// </summary>
public enum PutOrCall
{
	/// <summary>
	/// Put.
	/// </summary>
	Put,

	/// <summary>
	/// Call.
	/// </summary>
	Call
}

/// <summary>
/// </summary>
public enum SecurityResponseType
{
	/// <summary>
	/// </summary>
	AcceptSecurityProposalAsIs = 1,

	/// <summary>
	/// </summary>
	AcceptSecurityProposalWithRevisionsAsIndicatedInTheMessage = 2,

	/// <summary>
	/// </summary>
	ListOfSecurityTypesReturnedPerRequest = 3,

	/// <summary>
	/// </summary>
	ListOfSecuritiesReturnedPerRequest = 4,

	/// <summary>
	/// </summary>
	RejectSecurityProposal = 5,

	/// <summary>
	/// </summary>
	CannotMatchSelectionCriteria = 6,
}

/// <summary>
/// </summary>
public enum SecurityRequestResult
{
	/// <summary>
	/// </summary>
	ValidRequest = 0,

	/// <summary>
	/// </summary>
	InvalidOrUnsupportedRequest = 1,

	/// <summary>
	/// </summary>
	NoInstrumentsFoundThatMatchSelectionCriteria = 2,

	/// <summary>
	/// </summary>
	NotAuthorizedToRetrieveInstrumentData = 3,

	/// <summary>
	/// </summary>
	InstrumentDataTemporarilyUnavailable = 4,

	/// <summary>
	/// </summary>
	RequestForInstrumentDataNotSupported = 5,
}

/// <summary>
/// Ticks side.
/// </summary>
public static class TickDirection
{
	/// <summary>
	/// </summary>
	public const char PlusTick = '0';

	/// <summary>
	/// </summary>
	public const char ZeroPlusTick = '1';

	/// <summary>
	/// </summary>
	public const char MinusTick = '2';

	/// <summary>
	/// </summary>
	public const char ZeroMinusTick = '3';
}

/// <summary>
/// Quotes cancel types.
/// </summary>
public enum QuoteCancelType
{
	/// <summary>
	/// </summary>
	CancelForOneOrMoreSecurities = 1,

	/// <summary>
	/// </summary>
	CancelForSecurityType = 2,

	/// <summary>
	/// </summary>
	CancelForUnderlyingSecurity = 3,

	/// <summary>
	/// </summary>
	CancelAllQuotes = 4,

	/// <summary>
	/// </summary>
	CancelQuoteSpecifiedInQuoteid = 5,

	/// <summary>
	/// </summary>
	CancelByQuotetype = 6,

	/// <summary>
	/// </summary>
	CancelForSecurityIssuer = 7,

	/// <summary>
	/// </summary>
	CancelForIssuerOfUnderlyingSecurity = 8,
}

/// <summary>
/// </summary>
public static class SubscriptionRequestType
{
	/// <summary>
	/// </summary>
	public const char Snapshot = '0';

	/// <summary>
	/// </summary>
	public const char SnapshotPlusUpdates = '1';

	/// <summary>
	/// </summary>
	public const char DisablePreviousSnapshotPlusUpdateRequest = '2';

	/// <summary>
	/// </summary>
	public const char DisablePrevious = '2';
}

/// <summary>
/// </summary>
public enum MDUpdateType
{
	/// <summary>
	/// </summary>
	FullRefresh = 0,

	/// <summary>
	/// </summary>
	IncrementalRefresh = 1,
}

/// <summary>
/// </summary>
public enum SecurityListRequestType
{
	/// <summary>
	/// </summary>
	Symbol = 0,

	/// <summary>
	/// </summary>
	SecurityTypeAndOrCficode = 1,

	/// <summary>
	/// </summary>
	Product = 2,

	/// <summary>
	/// </summary>
	TradingSessionId = 3,

	/// <summary>
	/// </summary>
	AllSecurities = 4,

	/// <summary>
	/// </summary>
	MarketidOrMarketidPlusMarketSegmentId = 5,
}

/// <summary>
/// </summary>
public enum SecurityRequestType
{
	/// <summary>
	/// </summary>
	RequestSecurityIdentityAndSpecifications = 0,

	/// <summary>
	/// </summary>
	RequestSecurityIdentityForTheSpecificationsProvided = 1,

	/// <summary>
	/// </summary>
	RequestListSecurityTypes = 2,

	/// <summary>
	/// </summary>
	RequestListSecurities = 3,

	/// <summary>
	/// </summary>
	Symbol = 4,

	/// <summary>
	/// </summary>
	SecurityTypeAndOrCfiCode = 5,

	/// <summary>
	/// </summary>
	Product = 6,

	/// <summary>
	/// </summary>
	TradingSessionId = 7,

	/// <summary>
	/// </summary>
	AllSecurities = 8,

	/// <summary>
	/// </summary>
	MarketId = 9,
}

/// <summary>
/// </summary>
public static class SecurityType
{
	/// <summary>
	/// </summary>
	public const string AssetBackedSecurities = "ABS";

	/// <summary>
	/// </summary>
	public const string AmendedRestated = "AMENDED";

	/// <summary>
	/// </summary>
	public const string OtherAnticipationNotes = "AN";

	/// <summary>
	/// </summary>
	public const string BankersAcceptance = "BA";

	/// <summary>
	/// </summary>
	public const string BankNotes = "BN";

	/// <summary>
	/// </summary>
	public const string BillOfExchanges = "BOX";

	/// <summary>
	/// </summary>
	public const string BradyBond = "BRADY";

	/// <summary>
	/// </summary>
	public const string BridgeLoan = "BRIDGE";

	/// <summary>
	/// </summary>
	public const string BuySellback = "BUYSELL";

	/// <summary>
	/// </summary>
	public const string ConvertibleBond = "CB";

	/// <summary>
	/// </summary>
	public const string CertificateOfDeposit = "CD";

	/// <summary>
	/// </summary>
	public const string CallLoans = "CL";

	/// <summary>
	/// </summary>
	public const string CorpMortgageBackedSecurities = "CMBS";

	/// <summary>
	/// </summary>
	public const string CollateralizedMortgageObligation = "CMO";

	/// <summary>
	/// </summary>
	public const string CertificateOfObligation = "COFO";

	/// <summary>
	/// </summary>
	public const string CertificateOfParticipation = "COFP";

	/// <summary>
	/// </summary>
	public const string CorporateBond = "CORP";

	/// <summary>
	/// </summary>
	public const string CommercialPaper = "CP";

	/// <summary>
	/// </summary>
	public const string CorporatePrivatePlacement = "CPP";

	/// <summary>
	/// </summary>
	public const string CommonStock = "CS";

	/// <summary>
	/// </summary>
	public const string Defaulted = "DEFLTED";

	/// <summary>
	/// </summary>
	public const string DebtorInPossession = "DINP";

	/// <summary>
	/// </summary>
	public const string DepositNotes = "DN";

	/// <summary>
	/// </summary>
	public const string DualCurrency = "DUAL";

	/// <summary>
	/// </summary>
	public const string EuroCertificateOfDeposit = "EUCD";

	/// <summary>
	/// </summary>
	public const string EuroCorporateBond = "EUCORP";

	/// <summary>
	/// </summary>
	public const string EuroCommercialPaper = "EUCP";

	/// <summary>
	/// </summary>
	public const string EuroSovereigns = "EUSOV";

	/// <summary>
	/// </summary>
	public const string EuroSupranationalCoupons = "EUSUPRA";

	/// <summary>
	/// </summary>
	public const string FederalAgencyCoupon = "FAC";

	/// <summary>
	/// </summary>
	public const string FederalAgencyDiscountNote = "FADN";

	/// <summary>
	/// </summary>
	public const string ForeignExchangeContract = "FOR";

	/// <summary>
	/// </summary>
	public const string Forward = "FORWARD";

	/// <summary>
	/// </summary>
	public const string Future = "FUT";

	/// <summary>
	/// </summary>
	public const string GeneralObligationBonds = "GO";

	/// <summary>
	/// </summary>
	public const string IoetteMortgage = "IET";

	/// <summary>
	/// </summary>
	public const string LetterOfCredit = "LOFC";

	/// <summary>
	/// </summary>
	public const string LiquidityNote = "LQN";

	/// <summary>
	/// </summary>
	public const string Matured = "MATURED";

	/// <summary>
	/// </summary>
	public const string MortgageBackedSecurities = "MBS";

	/// <summary>
	/// </summary>
	public const string MutualFund = "MF";

	/// <summary>
	/// </summary>
	public const string MortgageInterestOnly = "MIO";

	/// <summary>
	/// </summary>
	public const string MultilegInstrument = "MLEG";

	/// <summary>
	/// </summary>
	public const string MortgagePrincipalOnly = "MPO";

	/// <summary>
	/// </summary>
	public const string MortgagePrivatePlacement = "MPP";

	/// <summary>
	/// </summary>
	public const string MiscellaneousPassThrough = "MPT";

	/// <summary>
	/// </summary>
	public const string MandatoryTender = "MT";

	/// <summary>
	/// </summary>
	public const string MediumTermNotes = "MTN";

	/// <summary>
	/// </summary>
	public const string NoSecurityType = "NONE";

	/// <summary>
	/// </summary>
	public const string Overnight = "ONITE";

	/// <summary>
	/// </summary>
	public const string Option = "OPT";

	/// <summary>
	/// </summary>
	public const string PrivateExportFunding = "PEF";

	/// <summary>
	/// </summary>
	public const string Pfandbriefe = "PFAND";

	/// <summary>
	/// </summary>
	public const string PromissoryNote = "PN";

	/// <summary>
	/// </summary>
	public const string PreferredStock = "PS";

	/// <summary>
	/// </summary>
	public const string PlazosFijos = "PZFJ";

	/// <summary>
	/// </summary>
	public const string RevenueAnticipationNote = "RAN";

	/// <summary>
	/// </summary>
	public const string Replaced = "REPLACD";

	/// <summary>
	/// </summary>
	public const string Repurchase = "REPO";

	/// <summary>
	/// </summary>
	public const string Retired = "RETIRED";

	/// <summary>
	/// </summary>
	public const string RevenueBonds = "REV";

	/// <summary>
	/// </summary>
	public const string RevolverLoan = "RVLV";

	/// <summary>
	/// </summary>
	public const string RevolverTermLoan = "RVLVTRM";

	/// <summary>
	/// </summary>
	public const string SecuritiesLoan = "SECLOAN";

	/// <summary>
	/// </summary>
	public const string SecuritiesPledge = "SECPLEDGE";

	/// <summary>
	/// </summary>
	public const string SpecialAssessment = "SPCLA";

	/// <summary>
	/// </summary>
	public const string SpecialObligation = "SPCLO";

	/// <summary>
	/// </summary>
	public const string SpecialTax = "SPCLT";

	/// <summary>
	/// </summary>
	public const string ShortTermLoanNote = "STN";

	/// <summary>
	/// </summary>
	public const string StructuredNotes = "STRUCT";

	/// <summary>
	/// </summary>
	public const string UsdSupranationalCoupons = "SUPRA";

	/// <summary>
	/// </summary>
	public const string SwingLineFacility = "SWING";

	/// <summary>
	/// </summary>
	public const string TaxAnticipationNote = "TAN";

	/// <summary>
	/// </summary>
	public const string TaxAllocation = "TAXA";

	/// <summary>
	/// </summary>
	public const string ToBeAnnounced = "TBA";

	/// <summary>
	/// </summary>
	public const string UsTreasuryBillTbill = "TBILL";

	/// <summary>
	/// </summary>
	public const string UsTreasuryBond = "TBOND";

	/// <summary>
	/// </summary>
	public const string PrincipalStripOfACallableBondOrNote = "TCAL";

	/// <summary>
	/// </summary>
	public const string TimeDeposit = "TD";

	/// <summary>
	/// </summary>
	public const string TaxExemptCommercialPaper = "TECP";

	/// <summary>
	/// </summary>
	public const string TermLoan = "TERM";

	/// <summary>
	/// </summary>
	public const string InterestStripFromAnyBondOrNote = "TINT";

	/// <summary>
	/// </summary>
	public const string TreasuryInflationProtectedSecurities = "TIPS";

	/// <summary>
	/// </summary>
	public const string UsTreasuryNoteTnote = "TNOTE";

	/// <summary>
	/// </summary>
	public const string PrincipalStripFromANonCallableBondOrNote = "TPRN";

	/// <summary>
	/// </summary>
	public const string TaxRevenueAnticipationNote = "TRAN";

	/// <summary>
	/// </summary>
	public const string UsTreasuryNoteUst = "UST";

	/// <summary>
	/// </summary>
	public const string UsTreasuryBillUstb = "USTB";

	/// <summary>
	/// </summary>
	public const string VariableRateDemandNote = "VRDN";

	/// <summary>
	/// </summary>
	public const string Warrant = "WAR";

	/// <summary>
	/// </summary>
	public const string Withdrawn = "WITHDRN";

	/// <summary>
	/// </summary>
	public const string WildcardEntryForUseOnSecurityDefinitionRequest = "?";

	/// <summary>
	/// </summary>
	public const string ExtendedCommNote = "XCN";

	/// <summary>
	/// </summary>
	public const string IndexedLinked = "XLINKD";

	/// <summary>
	/// </summary>
	public const string YankeeCorporateBond = "YANK";

	/// <summary>
	/// </summary>
	public const string YankeeCertificateOfDeposit = "YCD";

	/// <summary>
	/// </summary>
	public const string OptionsOnPhysical = "OOP";

	/// <summary>
	/// </summary>
	public const string OptionsOnFutures = "OOF";

	/// <summary>
	/// </summary>
	public const string Cash = "CASH";

	/// <summary>
	/// </summary>
	public const string OptionsOnCombo = "OOC";

	/// <summary>
	/// </summary>
	public const string InterestRateSwap = "IRS";

	/// <summary>
	/// </summary>
	public const string BankDepositoryNote = "BDN";

	/// <summary>
	/// </summary>
	public const string CanadianMoneyMarkets = "CAMM";

	/// <summary>
	/// </summary>
	public const string CanadianTreasuryNotes = "CAN";

	/// <summary>
	/// </summary>
	public const string CanadianTreasuryBills = "CTB";

	/// <summary>
	/// </summary>
	public const string CreditDefaultSwap = "CDS";

	/// <summary>
	/// </summary>
	public const string CanadianMortgageBonds = "CMB";

	/// <summary>
	/// </summary>
	public const string EuroCorporateFloatingRateNotes = "EUFRN";

	/// <summary>
	/// </summary>
	public const string UsCorporateFloatingRateNotes = "FRN";

	/// <summary>
	/// </summary>
	public const string CanadianProvincialBonds = "PROV";

	/// <summary>
	/// </summary>
	public const string SecuredLiquidityNote = "SLQN";

	/// <summary>
	/// </summary>
	public const string TreasuryBill = "TB";

	/// <summary>
	/// </summary>
	public const string TermLiquidityNote = "TLQN";

	/// <summary>
	/// </summary>
	public const string TaxableMunicipalCp = "TMCP";

	/// <summary>
	/// </summary>
	public const string NonDeliverableForward = "FXNDF";

	/// <summary>
	/// </summary>
	public const string FXSpot = "FXSPOT";

	/// <summary>
	/// </summary>
	public const string FXForward = "FXFWD";

	/// <summary>
	/// </summary>
	public const string FXSwap = "FXSWAP";

	/// <summary>
	/// </summary>
	public const string WildcardEntry = "WLD";

	/// <summary>
	/// </summary>
	public const string UsTreasuryNote = "TNOTE";

	/// <summary>
	/// </summary>
	public const string UsTreasuryBill = "TBILL";

	/// <summary>
	/// </summary>
	public const string AmendedAndRestated = "AMENDED";

	/// <summary>
	/// </summary>
	public const string TaxAndRevenueAnticipationNote = "TRAN";

	/// <summary>
	/// </summary>
	public const string Wildcard = "?";

	/// <summary>
	/// </summary>
	public const string ConvertableBond = "CB";

	/// <summary>
	/// </summary>
	public const string IndexLinked = "XLINKD";

	/// <summary>
	/// </summary>
	public const string PreferedStock = "PS";

	/// <summary>
	/// </summary>
	public const string UsTreasuryNoteBond = "UST";

	/// <summary>
	/// </summary>
	public const string LiquidityNotes = "LQN";

	/// <summary>
	/// </summary>
	public const string Overnite = "ONITE";

	/// <summary>
	/// </summary>
	public const string PromissoryNotes = "PN";

	/// <summary>
	/// </summary>
	public const string RepurchaseAgreement = "RP";

	/// <summary>
	/// </summary>
	public const string ReverseRepurchaseAgreement = "RVRP";

	/// <summary>
	/// </summary>
	public const string AgencyPools = "POOL";

	/// <summary>
	/// </summary>
	public const string CollateralizeMortgageObligation = "CMO";

	/// <summary>
	/// </summary>
	public const string FederalHousingAuthority = "FHA";

	/// <summary>
	/// </summary>
	public const string FederalHomeLoan = "FHL";

	/// <summary>
	/// </summary>
	public const string FederalNationalMortgageAssociation = "FN";

	/// <summary>
	/// </summary>
	public const string GovernmentNationalMortgageAssociation = "GN";

	/// <summary>
	/// </summary>
	public const string TreasuriesPlusAgencyDebenture = "GOVT";

	/// <summary>
	/// </summary>
	public const string MiscellaneousPassthru = "MPT";

	/// <summary>
	/// </summary>
	public const string MunicipalBond = "MUNI";

	/// <summary>
	/// </summary>
	public const string NoIsitcSecurityType = "NONE";

	/// <summary>
	/// </summary>
	public const string StudentLoanMarketingAssociation = "SL";

	/// <summary>
	/// </summary>
	public const string CatsTigers = "ZOO";

	/// <summary>
	/// </summary>
	public const string MortgagePrincipleOnly = "MPO";
}

/// <summary>
/// </summary>
public static class OrdType
{
	/// <summary>
	/// </summary>
	public const char Market = '1';

	/// <summary>
	/// </summary>
	public const char Limit = '2';

	/// <summary>
	/// </summary>
	public const char Stop = '3';

	/// <summary>
	/// </summary>
	public const char StopLimit = '4';

	/// <summary>
	/// </summary>
	public const char MarketOnClose = '5';

	/// <summary>
	/// </summary>
	public const char WithOrWithout = '6';

	/// <summary>
	/// </summary>
	public const char LimitOrBetter = '7';

	/// <summary>
	/// </summary>
	public const char LimitWithOrWithout = '8';

	/// <summary>
	/// </summary>
	public const char OnBasis = '9';

	/// <summary>
	/// </summary>
	public const char OnClose = 'A';

	/// <summary>
	/// </summary>
	public const char LimitOnClose = 'B';

	/// <summary>
	/// </summary>
	public const char ForexMarket = 'C';

	/// <summary>
	/// </summary>
	public const char PreviouslyQuoted = 'D';

	/// <summary>
	/// </summary>
	public const char PreviouslyIndicated = 'E';

	/// <summary>
	/// </summary>
	public const char ForexLimit = 'F';

	/// <summary>
	/// </summary>
	public const char ForexSwap = 'G';

	/// <summary>
	/// </summary>
	public const char ForexPreviouslyQuoted = 'H';

	/// <summary>
	/// </summary>
	public const char Funari = 'I';

	/// <summary>
	/// </summary>
	public const char MarketIfTouched = 'J';

	/// <summary>
	/// </summary>
	public const char MarketWithLeftOverAsLimit = 'K';

	/// <summary>
	/// </summary>
	public const char PreviousFundValuationPoint = 'L';

	/// <summary>
	/// </summary>
	public const char NextFundValuationPoint = 'M';

	/// <summary>
	/// </summary>
	public const char Pegged = 'P';

	/// <summary>
	/// </summary>
	public const char CounterOrderSelection = 'Q';
}

/// <summary>
/// </summary>
public static class OrdStatus
{
	/// <summary>
	/// </summary>
	public const char New = '0';

	/// <summary>
	/// </summary>
	public const char PartiallyFilled = '1';

	/// <summary>
	/// </summary>
	public const char Filled = '2';

	/// <summary>
	/// </summary>
	public const char DoneForDay = '3';

	/// <summary>
	/// </summary>
	public const char Canceled = '4';

	/// <summary>
	/// </summary>
	public const char Replaced = '5';

	/// <summary>
	/// </summary>
	public const char PendingCancel = '6';

	/// <summary>
	/// </summary>
	public const char Stopped = '7';

	/// <summary>
	/// </summary>
	public const char Rejected = '8';

	/// <summary>
	/// </summary>
	public const char Suspended = '9';

	/// <summary>
	/// </summary>
	public const char PendingNew = 'A';

	/// <summary>
	/// </summary>
	public const char Calculated = 'B';

	/// <summary>
	/// </summary>
	public const char Expired = 'C';

	/// <summary>
	/// </summary>
	public const char AcceptedForBidding = 'D';

	/// <summary>
	/// </summary>
	public const char PendingReplace = 'E';
}

/// <summary>
/// </summary>
public static class ExecType
{
	/// <summary>
	/// </summary>
	public const char New = '0';

	/// <summary>
	/// </summary>
	public const char PartialFill = '1';

	/// <summary>
	/// </summary>
	public const char Fill = '2';

	/// <summary>
	/// </summary>
	public const char DoneForDay = '3';

	/// <summary>
	/// </summary>
	public const char Canceled = '4';

	/// <summary>
	/// </summary>
	public const char Replaced = '5';

	/// <summary>
	/// </summary>
	public const char PendingCancel = '6';

	/// <summary>
	/// </summary>
	public const char Stopped = '7';

	/// <summary>
	/// </summary>
	public const char Rejected = '8';

	/// <summary>
	/// </summary>
	public const char Suspended = '9';

	/// <summary>
	/// </summary>
	public const char PendingNew = 'A';

	/// <summary>
	/// </summary>
	public const char Calculated = 'B';

	/// <summary>
	/// </summary>
	public const char Expired = 'C';

	/// <summary>
	/// </summary>
	public const char Restated = 'D';

	/// <summary>
	/// </summary>
	public const char PendingReplace = 'E';

	/// <summary>
	/// </summary>
	public const char Trade = 'F';

	/// <summary>
	/// </summary>
	public const char TradeCorrect = 'G';

	/// <summary>
	/// </summary>
	public const char TradeCancel = 'H';

	/// <summary>
	/// </summary>
	public const char OrderStatus = 'I';

	/// <summary>
	/// </summary>
	public const char TradeInAClearingHold = 'J';

	/// <summary>
	/// </summary>
	public const char TradeHasBeenReleasedToClearing = 'K';

	/// <summary>
	/// </summary>
	public const char TriggeredOrActivatedBySystem = 'L';
}

/// <summary>
/// </summary>
public static class FixTimeInForce
{
	/// <summary>
	/// </summary>
	public const char Day = '0';

	/// <summary>
	/// </summary>
	public const char GoodTillCancel = '1';

	/// <summary>
	/// </summary>
	public const char AtTheOpening = '2';

	/// <summary>
	/// </summary>
	public const char ImmediateOrCancel = '3';

	/// <summary>
	/// </summary>
	public const char FillOrKill = '4';

	/// <summary>
	/// </summary>
	public const char GoodTillCrossing = '5';

	/// <summary>
	/// </summary>
	public const char GoodTillDate = '6';

	/// <summary>
	/// </summary>
	public const char AtTheClose = '7';

	/// <summary>
	/// </summary>
	public const char GoodThroughCrossing = '8';

	/// <summary>
	/// </summary>
	public const char AtCrossing = '9';
}

/// <summary>
/// </summary>
public static class MDReqRejReason
{
	/// <summary>
	/// </summary>
	public const char UnknownSymbol = '0';

	/// <summary>
	/// </summary>
	public const char DuplicateMdReqId = '1';

	/// <summary>
	/// </summary>
	public const char InsufficientBandwidth = '2';

	/// <summary>
	/// </summary>
	public const char InsufficientPermissions = '3';

	/// <summary>
	/// </summary>
	public const char UnsupportedSubscriptionRequestType = '4';

	/// <summary>
	/// </summary>
	public const char UnsupportedMarketDepth = '5';

	/// <summary>
	/// </summary>
	public const char UnsupportedMdUpdateType = '6';

	/// <summary>
	/// </summary>
	public const char UnsupportedAggregatedBook = '7';

	/// <summary>
	/// </summary>
	public const char UnsupportedMdEntryType = '8';

	/// <summary>
	/// </summary>
	public const char UnsupportedTradingSessionId = '9';

	/// <summary>
	/// </summary>
	public const char UnsupportedScope = 'A';

	/// <summary>
	/// </summary>
	public const char UnsupportedOpenCloseSettleFlag = 'B';

	/// <summary>
	/// </summary>
	public const char UnsupportedMdImplicitDelete = 'C';

	/// <summary>
	/// </summary>
	public const char InsufficientCredit = 'D';
}

/// <summary>
/// </summary>
public static class HandlInst
{
	/// <summary>
	/// </summary>
	public const char AutomatedExecutionOrderPrivate = '1';

	/// <summary>
	/// </summary>
	public const char AutomatedExecutionOrderPublic = '2';

	/// <summary>
	/// </summary>
	public const char ManualOrder = '3';
}

/// <summary>
/// </summary>
public static class MassCancelRequestType
{
	/// <summary>
	/// </summary>
	public const char CancelOrdersForSecurity = '1';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForUnderlyingSecurity = '2';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForProduct = '3';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForCfiCode = '4';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForSecurityType = '5';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForTradingSession = '6';

	/// <summary>
	/// </summary>
	public const char CancelAllOrders = '7';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForMarket = '8';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForMarketSegment = '9';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForSecurityGroup = 'A';

	/// <summary>
	/// </summary>
	public const char CancelForSecurityIssuer = 'B';

	/// <summary>
	/// </summary>
	public const char CancelForIssuerOfUnderlyingSecurity = 'C';
}

/// <summary>
/// </summary>
public static class MassCancelResponse
{
	/// <summary>
	/// </summary>
	public const char CancelRequestRejected = '0';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForASecurity = '1';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForAnUnderlyingSecurity = '2';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForAProduct = '3';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForACficode = '4';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForASecuritytype = '5';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForATradingSession = '6';

	/// <summary>
	/// </summary>
	public const char CancelAllOrders = '7';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForAMarket = '8';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForAMarketSegment = '9';

	/// <summary>
	/// </summary>
	public const char CancelOrdersForASecurityGroup = 'A';
}

/// <summary>
/// </summary>
public enum MassCancelRejectReason
{
	/// <summary>
	/// </summary>
	MassCancelNotSupported = 0,

	/// <summary>
	/// </summary>
	InvalidOrUnknownSecurity = 1,

	/// <summary>
	/// </summary>
	InvalidOrUnkownUnderlyingSecurity = 2,

	/// <summary>
	/// </summary>
	InvalidOrUnknownProduct = 3,

	/// <summary>
	/// </summary>
	InvalidOrUnknownCficode = 4,

	/// <summary>
	/// </summary>
	InvalidOrUnknownSecurityType = 5,

	/// <summary>
	/// </summary>
	InvalidOrUnknownTradingSession = 6,

	/// <summary>
	/// </summary>
	Other = 99,

	/// <summary>
	/// </summary>
	InvalidOrUnknownMarket = 7,

	/// <summary>
	/// </summary>
	InvalidOrUnkownMarketSegment = 8,

	/// <summary>
	/// </summary>
	InvalidOrUnknownSecurityGroup = 9,

	/// <summary>
	/// </summary>
	InvalidOrUnknownSecurityIssuer = 10,

	/// <summary>
	/// </summary>
	InvalidOrUnknownIssuerOfUnderlyingSecurity = 11,

	/// <summary>
	/// </summary>
	InvalidOrUnknownUnderlying = 2,
}

/// <summary>
/// </summary>
public enum PosReqType
{
	/// <summary>
	/// </summary>
	Positions = 0,

	/// <summary>
	/// </summary>
	Trades = 1,

	/// <summary>
	/// </summary>
	Exercises = 2,

	/// <summary>
	/// </summary>
	Assignments = 3,

	/// <summary>
	/// </summary>
	SettlementActivity = 4,

	/// <summary>
	/// </summary>
	BackoutMessage = 5,

	/// <summary>
	/// </summary>
	DeltaPositions = 6,
}

/// <summary>
/// </summary>
public enum AccountType
{
	/// <summary>
	/// </summary>
	AccountIsCarriedOnCustomerSideBooks = 1,

	/// <summary>
	/// </summary>
	AccountIsCarriedOnNonCustomerSideBooks = 2,

	/// <summary>
	/// </summary>
	HouseTrader = 3,

	/// <summary>
	/// </summary>
	FloorTrader = 4,

	/// <summary>
	/// </summary>
	AccountIsCarriedOnNonCustomerSideBooksAndIsCrossMargined = 6,

	/// <summary>
	/// </summary>
	AccountIsHouseTraderAndIsCrossMargined = 7,

	/// <summary>
	/// </summary>
	JointBackOfficeAccount = 8,
}

/// <summary>
/// </summary>
public enum MassStatusReqType
{
	/// <summary>
	/// </summary>
	StatusForOrdersForASecurity = 1,

	/// <summary>
	/// </summary>
	StatusForOrdersForAnUnderlyingSecurity = 2,

	/// <summary>
	/// </summary>
	StatusForOrdersForAProduct = 3,

	/// <summary>
	/// </summary>
	StatusForOrdersForACfiCode = 4,

	/// <summary>
	/// </summary>
	StatusForOrdersForASecurityType = 5,

	/// <summary>
	/// </summary>
	StatusForOrdersForATradingSession = 6,

	/// <summary>
	/// </summary>
	StatusForAllOrders = 7,

	/// <summary>
	/// </summary>
	StatusForOrdersForAPartyid = 8,

	/// <summary>
	/// </summary>
	StatusForSecurityIssuer = 9,

	/// <summary>
	/// </summary>
	StatusForIssuerOfUnderlyingSecurity = 10,
}

/// <summary>
/// </summary>
public enum CxlRejReason
{
	/// <summary>
	/// </summary>
	TooLateToCancel = 0,
	
	/// <summary>
	/// </summary>
	UnknownOrder = 1,
	
	/// <summary>
	/// </summary>
	Broker = 2,
	
	/// <summary>
	/// </summary>
	OrderAlreadyInPendingCancelOrPendingReplaceStatus = 3,
	
	/// <summary>
	/// </summary>
	UnableToProcessOrderMassCancelRequest = 4,
	
	/// <summary>
	/// </summary>
	OrigOrdModTime = 5,
	
	/// <summary>
	/// </summary>
	DuplicateClOrdId = 6,
	
	/// <summary>
	/// </summary>
	Other = 99,
	
	/// <summary>
	/// </summary>
	InvalidPriceIncrement = 18,
	
	/// <summary>
	/// </summary>
	PriceExceedsCurrentPrice = 7,
	
	/// <summary>
	/// </summary>
	PriceExceedsCurrentPriceBand = 8,
}

/// <summary>
/// </summary>
public static class CxlRejResponseTo
{
	/// <summary>
	/// </summary>
	public const char OrderCancelRequest = '1';
	
	/// <summary>
	/// </summary>
	public const char OrderCancelReplaceRequest = '2';
}

/// <summary>
/// </summary>
public enum PosReqResult
{
	/// <summary>
	/// </summary>
	ValidRequest = 0,
	
	/// <summary>
	/// </summary>
	InvalidOrUnsupportedRequest = 1,
	
	/// <summary>
	/// </summary>
	NoPositionsFoundThatMatchCriteria = 2,
	
	/// <summary>
	/// </summary>
	NotAuthorizedToRequestPositions = 3,
	
	/// <summary>
	/// </summary>
	RequestForPositionNotSupported = 4,
	
	/// <summary>
	/// </summary>
	Other = 99,
}

/// <summary>
/// </summary>
public static class PosType
{
	/// <summary>
	/// </summary>
	public const string AllocationTradeQty = "ALC";
	
	/// <summary>
	/// </summary>
	public const string OptionAssignment = "AS";
	
	/// <summary>
	/// </summary>
	public const string AsOfTradeQty = "ASF";
	
	/// <summary>
	/// </summary>
	public const string DeliveryQty = "DLV";
	
	/// <summary>
	/// </summary>
	public const string ElectronicTradeQty = "ETR";
	
	/// <summary>
	/// </summary>
	public const string OptionExerciseQty = "EX";
	
	/// <summary>
	/// </summary>
	public const string EndOfDayQty = "FIN";
	
	/// <summary>
	/// </summary>
	public const string IntraSpreadQty = "IAS";
	
	/// <summary>
	/// </summary>
	public const string InterSpreadQty = "IES";
	
	/// <summary>
	/// </summary>
	public const string AdjustmentQty = "PA";
	
	/// <summary>
	/// </summary>
	public const string PitTradeQty = "PIT";
	
	/// <summary>
	/// </summary>
	public const string StartOfDayQty = "SOD";
	
	/// <summary>
	/// </summary>
	public const string IntegralSplit = "SPL";
	
	/// <summary>
	/// </summary>
	public const string TransactionFromAssignment = "TA";
	
	/// <summary>
	/// </summary>
	public const string TotalTransactionQty = "TOT";
	
	/// <summary>
	/// </summary>
	public const string TransactionQuantity = "TQ";
	
	/// <summary>
	/// </summary>
	public const string TransferTradeQty = "TRF";
	
	/// <summary>
	/// </summary>
	public const string TransactionFromExercise = "TX";
	
	/// <summary>
	/// </summary>
	public const string CrossMarginQty = "XM";
	
	/// <summary>
	/// </summary>
	public const string ReceiveQuantity = "RCV";
	
	/// <summary>
	/// </summary>
	public const string CorporateActionAdjustment = "CAA";
	
	/// <summary>
	/// </summary>
	public const string DeliveryNoticeQty = "DN";
	
	/// <summary>
	/// </summary>
	public const string ExchangeForPhysicalQty = "EP";
	
	/// <summary>
	/// </summary>
	public const string PrivatelyNegotiatedTradeQty = "PNTN";
	
	/// <summary>
	/// </summary>
	public const string NetDeltaQty = "DLT";
	
	/// <summary>
	/// </summary>
	public const string CreditEventAdjustment = "CEA";
	
	/// <summary>
	/// </summary>
	public const string SuccessionEventAdjustment = "SEA";
}

/// <summary>
/// </summary>
public enum PosQtyStatus
{
	/// <summary>
	/// </summary>
	Submitted = 0,

	/// <summary>
	/// </summary>
	Accepted = 1,
	
	/// <summary>
	/// </summary>
	Rejected = 2,
}

/// <summary>
/// </summary>
public static class PosAmtType
{
	/// <summary>
	/// </summary>
	public const string CashAmount = "CASH";
	
	/// <summary>
	/// </summary>
	public const string CashResidualAmount = "CRES";
	
	/// <summary>
	/// </summary>
	public const string FinalMarkToMarketAmount = "FMTM";
	
	/// <summary>
	/// </summary>
	public const string IncrementalMarkToMarketAmount = "IMTM";
	
	/// <summary>
	/// </summary>
	public const string PremiumAmount = "PREM";
	
	/// <summary>
	/// </summary>
	public const string StartOfDayMarkToMarketAmount = "SMTM";
	
	/// <summary>
	/// </summary>
	public const string TradeVariationAmount = "TVAR";
	
	/// <summary>
	/// </summary>
	public const string ValueAdjustedAmount = "VADJ";
	
	/// <summary>
	/// </summary>
	public const string SettlementValue = "SETL";
	
	/// <summary>
	/// </summary>
	public const string InitialTradeCouponAmount = "ICPN";
	
	/// <summary>
	/// </summary>
	public const string AccruedCouponAmount = "ACPN";
	
	/// <summary>
	/// </summary>
	public const string CouponAmount = "CPN";
	
	/// <summary>
	/// </summary>
	public const string IncrementalAccruedCoupon = "IACPN";
	
	/// <summary>
	/// </summary>
	public const string CollateralizedMarkToMarket = "CMTM";
	
	/// <summary>
	/// </summary>
	public const string IncrementalCollateralizedMarkToMarket = "ICMTM";
	
	/// <summary>
	/// </summary>
	public const string CompensationAmount = "DLV";
	
	/// <summary>
	/// </summary>
	public const string TotalBankedAmount = "BANK";
	
	/// <summary>
	/// </summary>
	public const string TotalCollateralizedAmount = "COLAT";
}

/// <summary>
/// </summary>
public static class TimeInForce
{
	/// <summary>
	/// </summary>
	public const char Day = '0';

	/// <summary>
	/// </summary>
	public const char GoodTillCancel = '1';

	/// <summary>
	/// </summary>
	public const char AtTheOpening = '2';

	/// <summary>
	/// </summary>
	public const char ImmediateOrCancel = '3';

	/// <summary>
	/// </summary>
	public const char FillOrKill = '4';

	/// <summary>
	/// </summary>
	public const char GoodTillCrossing = '5';

	/// <summary>
	/// </summary>
	public const char GoodTillDate = '6';

	/// <summary>
	/// </summary>
	public const char AtTheClose = '7';

	/// <summary>
	/// </summary>
	public const char GoodThroughCrossing = '8';

	/// <summary>
	/// </summary>
	public const char AtCrossing = '9';
}

/// <summary>
/// </summary>
public enum UserRequestType
{
	/// <summary>
	/// Broker SBE extension: user lookup. The FIX-standard UserRequestType (tag 924)
	/// starts at 1, so 0 is unused there; our SBE UserRequest reuses it for a lookup.
	/// </summary>
	Lookup = 0,

	/// <summary>
	/// </summary>
	LogOnUser = 1,

	/// <summary>
	/// </summary>
	LogOffUser = 2,

	/// <summary>
	/// </summary>
	ChangePasswordForUser = 3,

	/// <summary>
	/// </summary>
	RequestIndividualUserStatus = 4,
}

/// <summary>
/// </summary>
public static class CommType
{
	/// <summary>
	/// </summary>
	public const char PerUnit = '1';

	/// <summary>
	/// </summary>
	public const char Percent = '2';

	/// <summary>
	/// </summary>
	public const char Absolute = '3';

	/// <summary>
	/// </summary>
	public const char PointsPerBondOrContract = '6';

	/// <summary>
	/// </summary>
	public const char PercentageWaivedCashDiscount = '4';

	/// <summary>
	/// </summary>
	public const char PercentageWaivedEnhancedUnits = '5';
}

/// <summary>
/// </summary>
public enum PartyRole
{
	/// <summary>
	/// </summary>
	ExecutingFirm = 1,

	/// <summary>
	/// </summary>
	SettlementLocation = 10,

	/// <summary>
	/// </summary>
	OrderOriginationTrader = 11,

	/// <summary>
	/// </summary>
	ExecutingTrader = 12,

	/// <summary>
	/// </summary>
	OrderOriginationFirm = 13,

	/// <summary>
	/// </summary>
	GiveupClearingFirm = 14,

	/// <summary>
	/// </summary>
	CorrespondantClearingFirm = 15,

	/// <summary>
	/// </summary>
	ExecutingSystem = 16,

	/// <summary>
	/// </summary>
	ContraFirm = 17,

	/// <summary>
	/// </summary>
	ContraClearingFirm = 18,

	/// <summary>
	/// </summary>
	SponsoringFirm = 19,

	/// <summary>
	/// </summary>
	BrokerOfCredit = 2,

	/// <summary>
	/// </summary>
	UnderlyingContraFirm = 20,

	/// <summary>
	/// </summary>
	ClearingOrganization = 21,

	/// <summary>
	/// </summary>
	Exchange = 22,

	/// <summary>
	/// </summary>
	CustomerAccount = 24,

	/// <summary>
	/// </summary>
	CorrespondentClearingOrganization = 25,

	/// <summary>
	/// </summary>
	CorrespondentBroker = 26,

	/// <summary>
	/// </summary>
	BuyerSeller = 27,

	/// <summary>
	/// </summary>
	Custodian = 28,

	/// <summary>
	/// </summary>
	Intermediary = 29,

	/// <summary>
	/// </summary>
	ClientId = 3,

	/// <summary>
	/// </summary>
	Agent = 30,

	/// <summary>
	/// </summary>
	SubCustodian = 31,

	/// <summary>
	/// </summary>
	Beneficiary = 32,

	/// <summary>
	/// </summary>
	InterestedParty = 33,

	/// <summary>
	/// </summary>
	RegulatoryBody = 34,

	/// <summary>
	/// </summary>
	LiquidityProvider = 35,

	/// <summary>
	/// </summary>
	EnteringTrader = 36,

	/// <summary>
	/// </summary>
	ContraTrader = 37,

	/// <summary>
	/// </summary>
	PositionAccount = 38,

	/// <summary>
	/// </summary>
	ClearingFirm = 4,

	/// <summary>
	/// </summary>
	InvestorId = 5,

	/// <summary>
	/// </summary>
	IntroducingFirm = 6,

	/// <summary>
	/// </summary>
	EnteringFirm = 7,

	/// <summary>
	/// </summary>
	Locate = 8,

	/// <summary>
	/// </summary>
	FundManagerClientId = 9,

	/// <summary>
	/// </summary>
	IntroducingBroker = 60,

	/// <summary>
	/// </summary>
	ContraPositionAccount = 41,

	/// <summary>
	/// </summary>
	ContraExchange = 42,

	/// <summary>
	/// </summary>
	InternalCarryAccount = 43,

	/// <summary>
	/// </summary>
	OrderEntryOperatorId = 44,

	/// <summary>
	/// </summary>
	SecondaryAccountNumber = 45,

	/// <summary>
	/// </summary>
	ForeignFirm = 46,

	/// <summary>
	/// </summary>
	ThirdPartyAllocationFirm = 47,

	/// <summary>
	/// </summary>
	ClaimingAccount = 48,

	/// <summary>
	/// </summary>
	AssetManager = 49,

	/// <summary>
	/// </summary>
	PledgorAccount = 50,

	/// <summary>
	/// </summary>
	PledgeeAccount = 51,

	/// <summary>
	/// </summary>
	LargeTraderReportableAccount = 52,

	/// <summary>
	/// </summary>
	TraderMnemonic = 53,

	/// <summary>
	/// </summary>
	SenderLocation = 54,

	/// <summary>
	/// </summary>
	SessionId = 55,

	/// <summary>
	/// </summary>
	AcceptableCounterparty = 56,

	/// <summary>
	/// </summary>
	UnacceptableCounterparty = 57,

	/// <summary>
	/// </summary>
	EnteringUnit = 58,

	/// <summary>
	/// </summary>
	ExecutingUnit = 59,

	/// <summary>
	/// </summary>
	ContraInvestorId = 39,

	/// <summary>
	/// </summary>
	TransferToFirm = 40,

	/// <summary>
	/// </summary>
	QuoteOriginator = 61,

	/// <summary>
	/// </summary>
	ReportOriginator = 62,

	/// <summary>
	/// </summary>
	SystematicInternaliser = 63,

	/// <summary>
	/// </summary>
	MultilateralTradingFacility = 64,

	/// <summary>
	/// </summary>
	RegulatedMarket = 65,

	/// <summary>
	/// </summary>
	MarketMaker = 66,

	/// <summary>
	/// </summary>
	InvestmentFirm = 67,

	/// <summary>
	/// </summary>
	HostCompetentAuthority = 68,

	/// <summary>
	/// </summary>
	HomeCompetentAuthority = 69,

	/// <summary>
	/// </summary>
	CompetentAuthorityOfTheMostRelevantMarketInTermsOfLiquidity = 70,

	/// <summary>
	/// </summary>
	CompetentAuthorityOfTheTransaction = 71,

	/// <summary>
	/// </summary>
	ReportingIntermediary = 72,

	/// <summary>
	/// </summary>
	ExecutionVenue = 73,

	/// <summary>
	/// </summary>
	MarketDataEntryOriginator = 74,

	/// <summary>
	/// </summary>
	LocationId = 75,

	/// <summary>
	/// </summary>
	DeskId = 76,

	/// <summary>
	/// </summary>
	MarketDataMarket = 77,

	/// <summary>
	/// </summary>
	AllocationEntity = 78,

	/// <summary>
	/// </summary>
	PrimeBrokerProvidingGeneralTradeServices = 79,

	/// <summary>
	/// </summary>
	StepOutFirm = 80,

	/// <summary>
	/// </summary>
	Brokerclearingid = 81,

	/// <summary>
	/// </summary>
	CentralRegistrationDepository = 82,

	/// <summary>
	/// </summary>
	ClearingAccount = 83,

	/// <summary>
	/// </summary>
	AcceptableSettlingCounterparty = 84,

	/// <summary>
	/// </summary>
	UnacceptableSettlingCounterparty = 85,

	/// <summary>
	/// </summary>
	ForiegnFirm = 46,

	/// <summary>
	/// </summary>
	LocateLendingFirm = 8,
}

/// <summary>
/// </summary>
public static class PartyIDSource
{
	/// <summary>
	/// </summary>
	public const char KoreanInvestorId = '1';
	/// <summary>
	/// </summary>
	public const char TaiwaneseQualifiedForeignInvestorIdQfiiFid = '2';
	/// <summary>
	/// </summary>
	public const char TaiwaneseTradingAcct = '3';
	/// <summary>
	/// </summary>
	public const char MalaysianCentralDepository = '4';
	/// <summary>
	/// </summary>
	public const char ChineseInvestorId = '5';
	/// <summary>
	/// </summary>
	public const char UkNationalInsuranceOrPensionNumber = '6';
	/// <summary>
	/// </summary>
	public const char UsSocialSecurityNumber = '7';
	/// <summary>
	/// </summary>
	public const char UsEmployerOrTaxIdNumber = '8';
	/// <summary>
	/// </summary>
	public const char AustralianBusinessNumber = '9';
	/// <summary>
	/// </summary>
	public const char AustralianTaxFileNumber = 'A';
	/// <summary>
	/// </summary>
	public const char Bic = 'B';
	/// <summary>
	/// </summary>
	public const char GenerallyAcceptedMarketParticipantIdentifier = 'C';
	/// <summary>
	/// </summary>
	public const char Proprietary = 'D';
	/// <summary>
	/// </summary>
	public const char IsoCountryCode = 'E';
	/// <summary>
	/// </summary>
	public const char SettlementEntityLocation = 'F';
	/// <summary>
	/// </summary>
	public const char Mic = 'G';
	/// <summary>
	/// </summary>
	public const char CsdParticipantMemberCode = 'H';
	/// <summary>
	/// </summary>
	public const char DirectedBrokerThreeCharacterAcronymAsDefinedInIsitcEtcBestPracticeGuidelinesDocument = 'I';
}

/// <summary>
/// </summary>
public enum TrdRegTimestampType
{
	/// <summary>
	/// </summary>
	ExecutionTime = 1,
	/// <summary>
	/// </summary>
	TimeIn = 2,
	/// <summary>
	/// </summary>
	TimeOut = 3,
	/// <summary>
	/// </summary>
	BrokerReceipt = 4,
	/// <summary>
	/// </summary>
	BrokerExecution = 5,
	/// <summary>
	/// </summary>
	DeskReceipt = 6,
}

/// <summary>
/// </summary>
public static class SecurityIDSource
{
	/// <summary>
	/// </summary>
	public const string Cusip = "1";
	/// <summary>
	/// </summary>
	public const string Sedol = "2";
	/// <summary>
	/// </summary>
	public const string Quik = "3";
	/// <summary>
	/// </summary>
	public const string IsinNumber = "4";
	/// <summary>
	/// </summary>
	public const string RicCode = "5";
	/// <summary>
	/// </summary>
	public const string IsoCurrencyCode = "6";
	/// <summary>
	/// </summary>
	public const string IsoCountryCode = "7";
	/// <summary>
	/// </summary>
	public const string ExchangeSymbol = "8";
	/// <summary>
	/// </summary>
	public const string ConsolidatedTapeAssociation = "9";
	/// <summary>
	/// </summary>
	public const string BloombergSymbol = "A";
	/// <summary>
	/// </summary>
	public const string Wertpapier = "B";
	/// <summary>
	/// </summary>
	public const string Dutch = "C";
	/// <summary>
	/// </summary>
	public const string Valoren = "D";
	/// <summary>
	/// </summary>
	public const string Sicovam = "E";
	/// <summary>
	/// </summary>
	public const string Belgian = "F";
	/// <summary>
	/// </summary>
	public const string Common = "G";
	/// <summary>
	/// </summary>
	public const string ClearingHouse = "H";
	/// <summary>
	/// </summary>
	public const string IsdaFpmlProductSpecification = "I";
	/// <summary>
	/// </summary>
	public const string OptionPriceReportingAuthority = "J";
	/// <summary>
	/// </summary>
	public const string LetterOfCredit = "L";
	/// <summary>
	/// </summary>
	public const string IsdaFpmlProductUrl = "K";
	/// <summary>
	/// </summary>
	public const string MarketplaceAssignedIdentifier = "M";
}

/// <summary>
/// Identifies whether an order is a margin order or a non-margin order.
/// </summary>
public static class CashMargin
{
	/// <summary>
	/// Cash.
	/// </summary>
	public const char Cash = (char)1;

	/// <summary>
	/// Margin open.
	/// </summary>
	public const char MarginOpen = (char)2;

	/// <summary>
	/// Margin close.
	/// </summary>
	public const char MarginClose = (char)3;
}

/// <summary>
/// Indicates the type of product the security is associated with.
/// </summary>
public enum Product
{
	/// <summary>
	/// </summary>
	Agency = 1,
	/// <summary>
	/// </summary>
	Commodity = 2,
	/// <summary>
	/// </summary>
	Corporate = 3,
	/// <summary>
	/// </summary>
	Currency = 4,
	/// <summary>
	/// </summary>
	Equity = 5,
	/// <summary>
	/// </summary>
	Government = 6,
	/// <summary>
	/// </summary>
	Index = 7,
	/// <summary>
	/// </summary>
	Loan = 8,
	/// <summary>
	/// </summary>
	MoneyMarket = 9,
	/// <summary>
	/// </summary>
	Mortgage = 10,
	/// <summary>
	/// </summary>
	Municipal = 11,
	/// <summary>
	/// </summary>
	Other = 12,
	/// <summary>
	/// </summary>
	Financing = 13,
}

/// <summary>
/// Urgency.
/// </summary>
public static class Urgency
{
	/// <summary>
	/// Normal.
	/// </summary>
	public const char Normal = (char)0;

	/// <summary>
	/// Flash.
	/// </summary>
	public const char Flash = (char)1;

	/// <summary>
	/// Background.
	/// </summary>
	public const char Background = (char)2;
}

/// <summary>
/// </summary>
public enum PosReqStatus
{
	/// <summary>
	/// </summary>
	Completed = 0,

	/// <summary>
	/// </summary>
	CompletedWarnings = 1,

	/// <summary>
	/// </summary>
	Rejected = 2,
}

/// <summary>
/// </summary>
public enum ApplReqType
{
	/// <summary>
	/// </summary>
	RetransmissionOfApplicationMessagesForTheSpecifiedApplications = 0,
	
	/// <summary>
	/// </summary>
	SubscriptionToTheSpecifiedApplications = 1,
	
	/// <summary>
	/// </summary>
	RequestForTheLastAppllastseqnumPublishedForTheSpecifiedApplications = 2,
	
	/// <summary>
	/// </summary>
	RequestValidSetOfApplications = 3,
	
	/// <summary>
	/// </summary>
	UnsubscribeToTheSpecifiedApplications = 4,
	
	/// <summary>
	/// </summary>
	CancelRetransmission = 5,
	
	/// <summary>
	/// </summary>
	CancelRetransmissionAndUnsubscribeToTheSpecifiedApplications = 6,
}

/// <summary>
/// </summary>
public enum ApplVerId
{
	/// <summary>
	/// </summary>
	Fix27,
	
	/// <summary>
	/// </summary>
	Fix30,
	
	/// <summary>
	/// </summary>
	Fix40,
	
	/// <summary>
	/// </summary>
	Fix41,
	
	/// <summary>
	/// </summary>
	Fix42,
	
	/// <summary>
	/// </summary>
	Fix43,
	
	/// <summary>
	/// </summary>
	Fix44,
	
	/// <summary>
	/// </summary>
	Fix50,
	
	/// <summary>
	/// </summary>
	Fix50SP1,
	
	/// <summary>
	/// </summary>
	Fix50SP2,
}

/// <summary>
/// Indicates whether the resulting position after a trade should be an opening position or closing position.
/// </summary>
public static class PositionEffect
{
	/// <summary>
	/// Close.
	/// </summary>
	public const char Close = 'C';

	/// <summary>
	/// Default.
	/// </summary>
	public const char Default = 'D';

	/// <summary>
	/// FIFO.
	/// </summary>
	public const char FIFO = 'F';

	/// <summary>
	/// Close but notify on open.
	/// </summary>
	public const char CloseNotify = 'N';

	/// <summary>
	/// Open.
	/// </summary>
	public const char Open = 'O';

	/// <summary>
	/// Rolled.
	/// </summary>
	public const char Rolled = 'R';
}

/// <summary>
/// Space-delimited list of conditions describing a quote.
/// </summary>
public static class QuoteCondition
{
	/// <summary>
	/// Active.
	/// </summary>
	public const char Active = 'A';

	/// <summary>
	/// Indicative.
	/// </summary>
	public const char Indicative = 'I';
}

/// <summary>
/// Code to identify reason for an <see cref="ExecutionReport"/> message sent with <see cref="ExecType"/>=<see cref="ExecType.Restated"/> or used when communicating an unsolicited cancel.
/// </summary>
public enum ExecRestatementReason
{
	/// <summary>
	/// GT corporate action.
	/// </summary>
	CorporateAction,

	/// <summary>
	/// GT renewal / restatement (no corporate action).
	/// </summary>
	RenewalRestatement,

	/// <summary>
	/// Verbal change.
	/// </summary>
	VerbalChange,

	/// <summary>
	/// Repricing of order.
	/// </summary>
	RepricingOrder,

	/// <summary>
	/// Broker option.
	/// </summary>
	BrokerOption,

	/// <summary>
	/// Partial decline of <see cref="FixTags.OrderQty"/> (e.g. exchange initiated partial cancel).
	/// </summary>
	PartialDecline,

	/// <summary>
	/// Cancel on Trading Halt.
	/// </summary>
	CancelTradingHalt,

	/// <summary>
	/// Cancel on System Failure.
	/// </summary>
	CancelSystemFailure,

	/// <summary>
	/// Market (Exchange) option.
	/// </summary>
	MarketOption,

	/// <summary>
	/// Canceled, not best.
	/// </summary>
	CanceledNotBest,

	/// <summary>
	/// Warehouse Recap.
	/// </summary>
	WarehouseRecap,

	/// <summary>
	/// Peg Refresh.
	/// </summary>
	PegRefresh,

	/// <summary>
	/// Other.
	/// </summary>
	Other = 99,
}

/// <summary>
/// Security status.
/// </summary>
public static class SecurityStatus
{
	/// <summary>
	/// Active.
	/// </summary>
	public const string Active = "1";

	/// <summary>
	/// Inactive.
	/// </summary>
	public const string Inactive = "2";
}