namespace StockSharp.Tests;

/// <summary>
/// Comprehensive tests for SecurityMessage.IsMatch extension method.
/// Tests all parameters in SecurityLookupMessage criteria.
/// </summary>
[TestClass]
public class IsMatchTests : BaseTestClass
{
	#region SecurityId Fields Tests

	[TestMethod]
	public void IsMatch_SecurityCode_ContainsMatch()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AA" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "APL" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "aapl" } }).AssertTrue(); // case-insensitive
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "MSFT" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_BoardCode_ExactMatchCaseInsensitive()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { BoardCode = "NASDAQ" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { BoardCode = "nasdaq" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { BoardCode = "NAS" } }).AssertFalse(); // NOT contains, exact match
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { BoardCode = "NYSE" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityCodeAndBoardCode_ExactIdMatch()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		// When both code and board specified, it's an exact ID match
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AAPL", BoardCode = "NASDAQ" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AAPL", BoardCode = "NYSE" } }).AssertFalse();
		// Contains check still passes but exact ID check fails
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AA", BoardCode = "NASDAQ" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Bloomberg_ContainsMatch()
	{
		var security = CreateSecurity();
		security.SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ", Bloomberg = "AAPL US Equity" };

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Bloomberg = "AAPL" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Bloomberg = "Equity" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Bloomberg = "aapl us" } }).AssertTrue(); // case-insensitive
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Bloomberg = "MSFT" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Cusip_ContainsMatch()
	{
		var security = CreateSecurity();
		security.SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ", Cusip = "037833100" };

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Cusip = "037833" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Cusip = "100" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Cusip = "999999" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Isin_ContainsMatch()
	{
		var security = CreateSecurity();
		security.SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ", Isin = "US0378331005" };

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Isin = "US037" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Isin = "1005" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Isin = "DE" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Ric_ContainsMatch()
	{
		var security = CreateSecurity();
		security.SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ", Ric = "AAPL.O" };

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Ric = "AAPL" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Ric = ".O" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Ric = "MSFT" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Sedol_ContainsMatch()
	{
		var security = CreateSecurity();
		security.SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ", Sedol = "2046251" };

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Sedol = "2046" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Sedol = "251" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Sedol = "999" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_IQFeed_ContainsMatch()
	{
		var security = CreateSecurity();
		security.SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ", IQFeed = "AAPL.NASDAQ" };

		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { IQFeed = "AAPL" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { IQFeed = "NASDAQ" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { IQFeed = "NYSE" } }).AssertFalse();
	}

	#endregion

	#region SecurityIds Array Tests

	[TestMethod]
	public void IsMatch_SecurityIds_MatchesExactId()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [
				new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" },
				new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			]
		};

		security.IsMatch(criteria).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_SecurityIds_NoMatchReturnsFlase()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [
				new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" },
				new SecurityId { SecurityCode = "GOOGL", BoardCode = "NASDAQ" },
			]
		};

		security.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityIds_PartialCodeMatch()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		// Only security code specified - should match if contains
		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [
				new SecurityId { SecurityCode = "AA" }, // partial match
			]
		};

		// NOTE: Current behavior - partial code without board returns FALSE
		// This is because the code only returns true when BOTH code AND board match exactly
		// When only code is specified, the loop continues without returning true
		// This might be by design or a bug depending on intended behavior
		var result = security.IsMatch(criteria);
		result.AssertFalse(); // Documenting current behavior
	}

	[TestMethod]
	public void IsMatch_SecurityIds_BoardOnlyMatch_CurrentBehavior()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		// Only board code specified
		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [
				new SecurityId { BoardCode = "NASDAQ" },
			]
		};

		// NOTE: Current behavior - board only without code returns FALSE
		// The loop checks pass, but the return true only happens when BOTH are specified
		// This might be intentional - SecurityIds is meant for exact ID lookup
		var result = security.IsMatch(criteria);
		result.AssertFalse(); // Documenting current behavior
	}

	[TestMethod]
	public void IsMatch_SecurityIds_ExactMatchRequired()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");

		// Full ID match works
		var fullMatch = new SecurityLookupMessage
		{
			SecurityIds = [new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" }]
		};
		security.IsMatch(fullMatch).AssertTrue();

		// Partial matches don't work with SecurityIds array
		var partialCode = new SecurityLookupMessage
		{
			SecurityIds = [new SecurityId { SecurityCode = "AAPL" }] // no board
		};
		security.IsMatch(partialCode).AssertFalse();

		var partialBoard = new SecurityLookupMessage
		{
			SecurityIds = [new SecurityId { BoardCode = "NASDAQ" }] // no code
		};
		security.IsMatch(partialBoard).AssertFalse();
	}

	#endregion

	#region SecurityType Tests

	[TestMethod]
	public void IsMatch_SecurityType_SingleType()
	{
		var stock = CreateSecurity(type: SecurityTypes.Stock);
		var option = CreateSecurity(type: SecurityTypes.Option);
		var future = CreateSecurity(type: SecurityTypes.Future);

		var stockCriteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };

		stock.IsMatch(stockCriteria).AssertTrue();
		option.IsMatch(stockCriteria).AssertFalse();
		future.IsMatch(stockCriteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityType_NullSecurityTypeNotMatched()
	{
		var security = CreateSecurity(); // no type set

		var criteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };

		// Security without type set should not match type filter
		security.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityTypes_Multiple()
	{
		var stock = CreateSecurity(type: SecurityTypes.Stock);
		var option = CreateSecurity(type: SecurityTypes.Option);
		var future = CreateSecurity(type: SecurityTypes.Future);
		var bond = CreateSecurity(type: SecurityTypes.Bond);

		var criteria = new SecurityLookupMessage();
		criteria.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Future });

		stock.IsMatch(criteria).AssertTrue();
		future.IsMatch(criteria).AssertTrue();
		option.IsMatch(criteria).AssertFalse();
		bond.IsMatch(criteria).AssertFalse();
	}

	#endregion

	#region String Fields Tests (Contains Match)

	[TestMethod]
	public void IsMatch_Name_ContainsMatch()
	{
		var security = CreateSecurity();
		security.Name = "Apple Inc. Common Stock";

		security.IsMatch(new SecurityLookupMessage { Name = "Apple" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Name = "APPLE" }).AssertTrue(); // case-insensitive
		security.IsMatch(new SecurityLookupMessage { Name = "Common Stock" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Name = "Microsoft" }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_ShortName_ContainsMatch()
	{
		var security = CreateSecurity();
		security.ShortName = "AAPL Inc";

		security.IsMatch(new SecurityLookupMessage { ShortName = "AAPL" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { ShortName = "Inc" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { ShortName = "MSFT" }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_CfiCode_ContainsMatch()
	{
		var security = CreateSecurity();
		security.CfiCode = "ESXXXX";

		security.IsMatch(new SecurityLookupMessage { CfiCode = "ES" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { CfiCode = "XXXX" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { CfiCode = "OP" }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Class_ContainsMatch()
	{
		var security = CreateSecurity();
		security.Class = "TQBR";

		security.IsMatch(new SecurityLookupMessage { Class = "TQ" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Class = "BR" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Class = "FORTS" }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_BinaryOptionType_ContainsMatch()
	{
		var security = CreateSecurity();
		security.BinaryOptionType = "HighLow";

		security.IsMatch(new SecurityLookupMessage { BinaryOptionType = "High" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { BinaryOptionType = "Low" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { BinaryOptionType = "Touch" }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_BasketCode_ContainsMatch()
	{
		var security = CreateSecurity();
		security.BasketCode = "WI";

		security.IsMatch(new SecurityLookupMessage { BasketCode = "W" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { BasketCode = "WI" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { BasketCode = "EI" }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_BasketExpression_ExactMatch()
	{
		var security = CreateSecurity();
		security.BasketExpression = "AAPL@NASDAQ + MSFT@NASDAQ";

		// BasketExpression uses EqualsIgnoreCase, not Contains
		security.IsMatch(new SecurityLookupMessage { BasketExpression = "AAPL@NASDAQ + MSFT@NASDAQ" }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { BasketExpression = "aapl@nasdaq + msft@nasdaq" }).AssertTrue(); // case-insensitive
		security.IsMatch(new SecurityLookupMessage { BasketExpression = "AAPL" }).AssertFalse(); // not contains
	}

	#endregion

	#region Numeric Fields Tests (Exact Match)

	[TestMethod]
	public void IsMatch_VolumeStep_ExactMatch()
	{
		var security = CreateSecurity();
		security.VolumeStep = 10m;

		security.IsMatch(new SecurityLookupMessage { VolumeStep = 10m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { VolumeStep = 1m }).AssertFalse();
		security.IsMatch(new SecurityLookupMessage { VolumeStep = null }).AssertTrue(); // null criteria = any
	}

	[TestMethod]
	public void IsMatch_MinVolume_ExactMatch()
	{
		var security = CreateSecurity();
		security.MinVolume = 100m;

		security.IsMatch(new SecurityLookupMessage { MinVolume = 100m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { MinVolume = 50m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_MaxVolume_ExactMatch()
	{
		var security = CreateSecurity();
		security.MaxVolume = 10000m;

		security.IsMatch(new SecurityLookupMessage { MaxVolume = 10000m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { MaxVolume = 5000m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Multiplier_ExactMatch()
	{
		var security = CreateSecurity();
		security.Multiplier = 100m;

		security.IsMatch(new SecurityLookupMessage { Multiplier = 100m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Multiplier = 10m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Decimals_ExactMatch()
	{
		var security = CreateSecurity();
		security.Decimals = 2;

		security.IsMatch(new SecurityLookupMessage { Decimals = 2 }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Decimals = 4 }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_PriceStep_ExactMatch()
	{
		var security = CreateSecurity();
		security.PriceStep = 0.01m;

		security.IsMatch(new SecurityLookupMessage { PriceStep = 0.01m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { PriceStep = 0.001m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Strike_ExactMatch()
	{
		var security = CreateSecurity(type: SecurityTypes.Option);
		security.Strike = 150m;

		security.IsMatch(new SecurityLookupMessage { Strike = 150m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Strike = 160m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_UnderlyingSecurityMinVolume_ExactMatch()
	{
		var security = CreateSecurity();
		security.UnderlyingSecurityMinVolume = 100m;

		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityMinVolume = 100m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityMinVolume = 50m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_IssueSize_ExactMatch()
	{
		var security = CreateSecurity();
		security.IssueSize = 1000000m;

		security.IsMatch(new SecurityLookupMessage { IssueSize = 1000000m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { IssueSize = 500000m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_FaceValue_ExactMatch()
	{
		var security = CreateSecurity();
		security.FaceValue = 1000m;

		security.IsMatch(new SecurityLookupMessage { FaceValue = 1000m }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { FaceValue = 100m }).AssertFalse();
	}

	#endregion

	#region Date Fields Tests (Date Part Only)

	[TestMethod]
	public void IsMatch_ExpiryDate_DatePartMatch()
	{
		var security = CreateSecurity();
		security.ExpiryDate = new DateTime(2024, 12, 20, 14, 30, 0);

		// Only date part matters, time ignored
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2024, 12, 20) }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2024, 12, 20, 9, 0, 0) }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2024, 12, 21) }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SettlementDate_DatePartMatch()
	{
		var security = CreateSecurity();
		security.SettlementDate = new DateTime(2024, 12, 22, 16, 0, 0);

		security.IsMatch(new SecurityLookupMessage { SettlementDate = new DateTime(2024, 12, 22) }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SettlementDate = new DateTime(2024, 12, 23) }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_IssueDate_DatePartMatch()
	{
		var security = CreateSecurity();
		security.IssueDate = new DateTime(2020, 1, 15, 10, 0, 0);

		security.IsMatch(new SecurityLookupMessage { IssueDate = new DateTime(2020, 1, 15) }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { IssueDate = new DateTime(2020, 1, 16) }).AssertFalse();
	}

	#endregion

	#region Enum Fields Tests (Exact Match)

	[TestMethod]
	public void IsMatch_OptionType_ExactMatch()
	{
		var callOption = CreateSecurity(type: SecurityTypes.Option);
		callOption.OptionType = OptionTypes.Call;

		var putOption = CreateSecurity(type: SecurityTypes.Option);
		putOption.OptionType = OptionTypes.Put;

		callOption.IsMatch(new SecurityLookupMessage { OptionType = OptionTypes.Call }).AssertTrue();
		callOption.IsMatch(new SecurityLookupMessage { OptionType = OptionTypes.Put }).AssertFalse();
		putOption.IsMatch(new SecurityLookupMessage { OptionType = OptionTypes.Put }).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_Currency_ExactMatch()
	{
		var security = CreateSecurity();
		security.Currency = CurrencyTypes.USD;

		security.IsMatch(new SecurityLookupMessage { Currency = CurrencyTypes.USD }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Currency = CurrencyTypes.EUR }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_UnderlyingSecurityType_ExactMatch()
	{
		var security = CreateSecurity(type: SecurityTypes.Option);
		security.UnderlyingSecurityType = SecurityTypes.Stock;

		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityType = SecurityTypes.Stock }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityType = SecurityTypes.Future }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Shortable_ExactMatch()
	{
		var security = CreateSecurity();
		security.Shortable = true;

		security.IsMatch(new SecurityLookupMessage { Shortable = true }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Shortable = false }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SettlementType_ExactMatch()
	{
		var security = CreateSecurity();
		security.SettlementType = SettlementTypes.Cash;

		security.IsMatch(new SecurityLookupMessage { SettlementType = SettlementTypes.Cash }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SettlementType = SettlementTypes.Delivery }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_OptionStyle_ExactMatch()
	{
		var security = CreateSecurity(type: SecurityTypes.Option);
		security.OptionStyle = OptionStyles.American;

		security.IsMatch(new SecurityLookupMessage { OptionStyle = OptionStyles.American }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { OptionStyle = OptionStyles.European }).AssertFalse();
	}

	#endregion

	#region Underlying Security Tests

	[TestMethod]
	public void IsMatch_UnderlyingSecurityId_ContainsMatch()
	{
		var security = CreateSecurity(type: SecurityTypes.Option);
		security.UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		// GetUnderlyingCode() returns the security code
		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityId = new SecurityId { SecurityCode = "AA" } }).AssertTrue(); // contains
		security.IsMatch(new SecurityLookupMessage { UnderlyingSecurityId = new SecurityId { SecurityCode = "MSFT" } }).AssertFalse();
	}

	#endregion

	#region PrimaryId Tests

	[TestMethod]
	public void IsMatch_PrimaryId_ExactMatch()
	{
		var security = CreateSecurity();
		security.PrimaryId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		security.IsMatch(new SecurityLookupMessage { PrimaryId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { PrimaryId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" } }).AssertFalse();
	}

	#endregion

	#region Combined Criteria Tests

	[TestMethod]
	public void IsMatch_MultipleCriteria_AllMustMatch()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ", type: SecurityTypes.Stock);
		security.Currency = CurrencyTypes.USD;
		security.PriceStep = 0.01m;

		var matchingCriteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL" },
			SecurityType = SecurityTypes.Stock,
			Currency = CurrencyTypes.USD,
			PriceStep = 0.01m,
		};

		var nonMatchingCriteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL" },
			SecurityType = SecurityTypes.Stock,
			Currency = CurrencyTypes.EUR, // mismatch
		};

		security.IsMatch(matchingCriteria).AssertTrue();
		security.IsMatch(nonMatchingCriteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_EmptyCriteria_MatchesAll()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ", type: SecurityTypes.Stock);
		security.Currency = CurrencyTypes.USD;
		security.Name = "Apple Inc.";

		var emptyCriteria = new SecurityLookupMessage();

		security.IsMatch(emptyCriteria).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_NullValuesInSecurity_NotMatchedByNonNullCriteria()
	{
		var security = CreateSecurity();
		security.Currency = null;
		security.VolumeStep = null;

		security.IsMatch(new SecurityLookupMessage { Currency = CurrencyTypes.USD }).AssertFalse();
		security.IsMatch(new SecurityLookupMessage { VolumeStep = 1m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_OptionFilter_AllOptionCriteria()
	{
		var option = CreateSecurity(code: "AAPL240120C00150000", board: "OPRA", type: SecurityTypes.Option);
		option.UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		option.ExpiryDate = new DateTime(2024, 1, 20);
		option.Strike = 150m;
		option.OptionType = OptionTypes.Call;
		option.OptionStyle = OptionStyles.American;
		option.Currency = CurrencyTypes.USD;
		option.Multiplier = 100m;

		// All criteria match
		option.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" },
			ExpiryDate = new DateTime(2024, 1, 20),
			Strike = 150m,
			OptionType = OptionTypes.Call,
			OptionStyle = OptionStyles.American,
			Currency = CurrencyTypes.USD,
			Multiplier = 100m,
		}).AssertTrue();

		// Wrong strike
		option.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" },
			Strike = 160m,
		}).AssertFalse();

		// Wrong option type
		option.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			OptionType = OptionTypes.Put,
		}).AssertFalse();

		// Wrong expiry
		option.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			ExpiryDate = new DateTime(2024, 2, 20),
		}).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_FutureFilter_AllFutureCriteria()
	{
		var future = CreateSecurity(code: "ESH24", board: "CME", type: SecurityTypes.Future);
		future.ExpiryDate = new DateTime(2024, 3, 15);
		future.SettlementType = SettlementTypes.Cash;
		future.Currency = CurrencyTypes.USD;
		future.PriceStep = 0.25m;
		future.Multiplier = 50m;
		future.UnderlyingSecurityId = new SecurityId { SecurityCode = "SPX", BoardCode = "INDEX" };

		// All criteria match
		future.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Future,
			ExpiryDate = new DateTime(2024, 3, 15),
			SettlementType = SettlementTypes.Cash,
			Currency = CurrencyTypes.USD,
			PriceStep = 0.25m,
			Multiplier = 50m,
		}).AssertTrue();

		// Wrong settlement type
		future.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Future,
			SettlementType = SettlementTypes.Delivery,
		}).AssertFalse();

		// Only underlying matches
		future.IsMatch(new SecurityLookupMessage
		{
			UnderlyingSecurityId = new SecurityId { SecurityCode = "SPX" },
		}).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_BondFilter_AllBondCriteria()
	{
		var bond = CreateSecurity(code: "US912828ZT64", board: "BOND", type: SecurityTypes.Bond);
		bond.IssueDate = new DateTime(2020, 5, 15);
		bond.ExpiryDate = new DateTime(2030, 5, 15);
		bond.FaceValue = 1000m;
		bond.Currency = CurrencyTypes.USD;
		bond.IssueSize = 50000000000m;

		// All criteria match
		bond.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Bond,
			IssueDate = new DateTime(2020, 5, 15),
			ExpiryDate = new DateTime(2030, 5, 15),
			FaceValue = 1000m,
			Currency = CurrencyTypes.USD,
		}).AssertTrue();

		// Wrong face value
		bond.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Bond,
			FaceValue = 100m,
		}).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_MultipleIdentifiers_AnyIdentifierMatches()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");
		security.SecurityId = new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
			Bloomberg = "AAPL US Equity",
			Isin = "US0378331005",
			Cusip = "037833100",
			Ric = "AAPL.O",
			Sedol = "2046251",
		};

		// Match by Bloomberg
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Bloomberg = "AAPL US" } }).AssertTrue();

		// Match by ISIN
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Isin = "US037833" } }).AssertTrue();

		// Match by CUSIP
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Cusip = "037833" } }).AssertTrue();

		// Match by RIC
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Ric = "AAPL" } }).AssertTrue();

		// Match by SEDOL
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Sedol = "2046" } }).AssertTrue();

		// No identifier matches
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { Bloomberg = "MSFT" } }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_CodeAndIdentifier_BothMustMatch()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");
		security.SecurityId = new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
			Isin = "US0378331005",
		};

		// Code matches, ISIN matches
		security.IsMatch(new SecurityLookupMessage
		{
			SecurityId = new() { SecurityCode = "AAPL", Isin = "US037833" }
		}).AssertTrue();

		// Code matches, ISIN doesn't match
		security.IsMatch(new SecurityLookupMessage
		{
			SecurityId = new() { SecurityCode = "AAPL", Isin = "DE" }
		}).AssertFalse();

		// Code doesn't match, ISIN matches
		security.IsMatch(new SecurityLookupMessage
		{
			SecurityId = new() { SecurityCode = "MSFT", Isin = "US037833" }
		}).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityTypesArray_WithOtherCriteria()
	{
		var stock = CreateSecurity(code: "AAPL", board: "NASDAQ", type: SecurityTypes.Stock);
		stock.Currency = CurrencyTypes.USD;

		var etf = CreateSecurity(code: "SPY", board: "NYSE", type: SecurityTypes.Etf);
		etf.Currency = CurrencyTypes.USD;

		var bond = CreateSecurity(code: "UST10Y", board: "BOND", type: SecurityTypes.Bond);
		bond.Currency = CurrencyTypes.USD;

		// Type in array + currency matches
		var criteria = new SecurityLookupMessage { Currency = CurrencyTypes.USD };
		criteria.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Etf });

		stock.IsMatch(criteria).AssertTrue();
		etf.IsMatch(criteria).AssertTrue();
		bond.IsMatch(criteria).AssertFalse(); // Bond not in types array

		// Type in array but wrong currency
		var criteriaEur = new SecurityLookupMessage { Currency = CurrencyTypes.EUR };
		criteriaEur.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Etf });

		stock.IsMatch(criteriaEur).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_StockFilter_RealWorldScenario()
	{
		var appleStock = CreateSecurity(code: "AAPL", board: "NASDAQ", type: SecurityTypes.Stock);
		appleStock.Name = "Apple Inc.";
		appleStock.Currency = CurrencyTypes.USD;
		appleStock.PriceStep = 0.01m;
		appleStock.VolumeStep = 1m;
		appleStock.Shortable = true;
		appleStock.Decimals = 2;

		var msftStock = CreateSecurity(code: "MSFT", board: "NASDAQ", type: SecurityTypes.Stock);
		msftStock.Name = "Microsoft Corporation";
		msftStock.Currency = CurrencyTypes.USD;
		msftStock.PriceStep = 0.01m;
		msftStock.VolumeStep = 1m;
		msftStock.Shortable = true;

		// Find all US stocks with name containing "Apple"
		var appleCriteria = new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Stock,
			Currency = CurrencyTypes.USD,
			Name = "Apple",
		};
		appleStock.IsMatch(appleCriteria).AssertTrue();
		msftStock.IsMatch(appleCriteria).AssertFalse();

		// Find all NASDAQ stocks that are shortable
		var shortableCriteria = new SecurityLookupMessage
		{
			SecurityId = new() { BoardCode = "NASDAQ" },
			Shortable = true,
		};
		appleStock.IsMatch(shortableCriteria).AssertTrue();
		msftStock.IsMatch(shortableCriteria).AssertTrue();

		// Find specific stock by code on specific board
		var specificCriteria = new SecurityLookupMessage
		{
			SecurityId = new() { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
		};
		appleStock.IsMatch(specificCriteria).AssertTrue();
		msftStock.IsMatch(specificCriteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_OptionChainFilter_FindAllOptionsForUnderlying()
	{
		var call150 = CreateSecurity(code: "AAPL240120C150", board: "OPRA", type: SecurityTypes.Option);
		call150.UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		call150.ExpiryDate = new DateTime(2024, 1, 20);
		call150.Strike = 150m;
		call150.OptionType = OptionTypes.Call;

		var call160 = CreateSecurity(code: "AAPL240120C160", board: "OPRA", type: SecurityTypes.Option);
		call160.UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		call160.ExpiryDate = new DateTime(2024, 1, 20);
		call160.Strike = 160m;
		call160.OptionType = OptionTypes.Call;

		var put150 = CreateSecurity(code: "AAPL240120P150", board: "OPRA", type: SecurityTypes.Option);
		put150.UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		put150.ExpiryDate = new DateTime(2024, 1, 20);
		put150.Strike = 150m;
		put150.OptionType = OptionTypes.Put;

		var msftCall = CreateSecurity(code: "MSFT240120C300", board: "OPRA", type: SecurityTypes.Option);
		msftCall.UnderlyingSecurityId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };
		msftCall.ExpiryDate = new DateTime(2024, 1, 20);
		msftCall.Strike = 300m;
		msftCall.OptionType = OptionTypes.Call;

		// Find all AAPL options for specific expiry
		var aaplOptionsCriteria = new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" },
			ExpiryDate = new DateTime(2024, 1, 20),
		};
		call150.IsMatch(aaplOptionsCriteria).AssertTrue();
		call160.IsMatch(aaplOptionsCriteria).AssertTrue();
		put150.IsMatch(aaplOptionsCriteria).AssertTrue();
		msftCall.IsMatch(aaplOptionsCriteria).AssertFalse();

		// Find only AAPL calls
		var aaplCallsCriteria = new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" },
			OptionType = OptionTypes.Call,
		};
		call150.IsMatch(aaplCallsCriteria).AssertTrue();
		call160.IsMatch(aaplCallsCriteria).AssertTrue();
		put150.IsMatch(aaplCallsCriteria).AssertFalse();

		// Find specific strike
		var strike150Criteria = new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Option,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" },
			Strike = 150m,
		};
		call150.IsMatch(strike150Criteria).AssertTrue();
		put150.IsMatch(strike150Criteria).AssertTrue();
		call160.IsMatch(strike150Criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_FuturesChain_FindAllFuturesForUnderlying()
	{
		var esH24 = CreateSecurity(code: "ESH24", board: "CME", type: SecurityTypes.Future);
		esH24.UnderlyingSecurityId = new SecurityId { SecurityCode = "ES", BoardCode = "CME" };
		esH24.ExpiryDate = new DateTime(2024, 3, 15);

		var esM24 = CreateSecurity(code: "ESM24", board: "CME", type: SecurityTypes.Future);
		esM24.UnderlyingSecurityId = new SecurityId { SecurityCode = "ES", BoardCode = "CME" };
		esM24.ExpiryDate = new DateTime(2024, 6, 21);

		var nqH24 = CreateSecurity(code: "NQH24", board: "CME", type: SecurityTypes.Future);
		nqH24.UnderlyingSecurityId = new SecurityId { SecurityCode = "NQ", BoardCode = "CME" };
		nqH24.ExpiryDate = new DateTime(2024, 3, 15);

		// Find all ES futures
		var esFuturesCriteria = new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Future,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "ES" },
		};
		esH24.IsMatch(esFuturesCriteria).AssertTrue();
		esM24.IsMatch(esFuturesCriteria).AssertTrue();
		nqH24.IsMatch(esFuturesCriteria).AssertFalse();

		// Find March 2024 futures on CME
		var march2024Criteria = new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Future,
			SecurityId = new() { BoardCode = "CME" },
			ExpiryDate = new DateTime(2024, 3, 15),
		};
		esH24.IsMatch(march2024Criteria).AssertTrue();
		nqH24.IsMatch(march2024Criteria).AssertTrue();
		esM24.IsMatch(march2024Criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_PartialCriteria_OnlySpecifiedFieldsChecked()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ", type: SecurityTypes.Stock);
		security.Name = "Apple Inc.";
		security.Currency = CurrencyTypes.USD;
		security.PriceStep = 0.01m;
		security.VolumeStep = 1m;
		security.Decimals = 2;
		security.Shortable = true;
		security.ExpiryDate = null; // No expiry for stocks

		// Only check code - other fields don't matter
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AAPL" } }).AssertTrue();

		// Only check currency - other fields don't matter
		security.IsMatch(new SecurityLookupMessage { Currency = CurrencyTypes.USD }).AssertTrue();

		// Only check type - other fields don't matter
		security.IsMatch(new SecurityLookupMessage { SecurityType = SecurityTypes.Stock }).AssertTrue();

		// Check two fields
		security.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Stock,
			Currency = CurrencyTypes.USD,
		}).AssertTrue();

		// One match, one mismatch = no match
		security.IsMatch(new SecurityLookupMessage
		{
			SecurityType = SecurityTypes.Stock,
			Currency = CurrencyTypes.EUR, // Wrong!
		}).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_AllFieldsSet_StressTest()
	{
		var security = CreateSecurity(code: "AAPL240120C00150000", board: "OPRA", type: SecurityTypes.Option);
		security.Name = "AAPL Jan 2024 150 Call";
		security.ShortName = "AAPL C150";
		security.Currency = CurrencyTypes.USD;
		security.PriceStep = 0.01m;
		security.VolumeStep = 1m;
		security.Decimals = 2;
		security.Multiplier = 100m;
		security.Strike = 150m;
		security.OptionType = OptionTypes.Call;
		security.OptionStyle = OptionStyles.American;
		security.ExpiryDate = new DateTime(2024, 1, 20);
		security.SettlementDate = new DateTime(2024, 1, 22);
		security.SettlementType = SettlementTypes.Delivery;
		security.UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		security.UnderlyingSecurityType = SecurityTypes.Stock;
		security.UnderlyingSecurityMinVolume = 100m;
		security.Shortable = true;
		security.SecurityId = new SecurityId
		{
			SecurityCode = "AAPL240120C00150000",
			BoardCode = "OPRA",
			Isin = "US0378331005OPT",
		};

		// Match with many criteria
		var fullCriteria = new SecurityLookupMessage
		{
			SecurityId = new() { SecurityCode = "AAPL240120C00150000", BoardCode = "OPRA" },
			SecurityType = SecurityTypes.Option,
			Name = "AAPL Jan 2024",
			Currency = CurrencyTypes.USD,
			PriceStep = 0.01m,
			VolumeStep = 1m,
			Decimals = 2,
			Multiplier = 100m,
			Strike = 150m,
			OptionType = OptionTypes.Call,
			OptionStyle = OptionStyles.American,
			ExpiryDate = new DateTime(2024, 1, 20),
			SettlementDate = new DateTime(2024, 1, 22),
			SettlementType = SettlementTypes.Delivery,
			UnderlyingSecurityId = new SecurityId { SecurityCode = "AAPL" },
			UnderlyingSecurityType = SecurityTypes.Stock,
			Shortable = true,
		};

		security.IsMatch(fullCriteria).AssertTrue();

		// Change just one field to fail
		var failCriteria = new SecurityLookupMessage
		{
			SecurityId = new() { SecurityCode = "AAPL240120C00150000", BoardCode = "OPRA" },
			SecurityType = SecurityTypes.Option,
			Strike = 160m, // Wrong strike!
		};
		security.IsMatch(failCriteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_CaseSensitivity_CodeAndNameCaseInsensitive()
	{
		var security = CreateSecurity(code: "AAPL", board: "NASDAQ");
		security.Name = "Apple Inc.";

		// Code is case-insensitive (contains match)
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "aapl" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AAPL" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { SecurityCode = "Aapl" } }).AssertTrue();

		// Name is case-insensitive (contains match)
		security.IsMatch(new SecurityLookupMessage { Name = "apple" } ).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Name = "APPLE" } ).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { Name = "Apple" } ).AssertTrue();

		// Board is case-insensitive (exact match)
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { BoardCode = "nasdaq" } }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { SecurityId = new() { BoardCode = "NASDAQ" } }).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_SecurityIdsArray_WithMultipleIds()
	{
		var aapl = CreateSecurity(code: "AAPL", board: "NASDAQ");
		var msft = CreateSecurity(code: "MSFT", board: "NASDAQ");
		var goog = CreateSecurity(code: "GOOG", board: "NASDAQ");
		var tsla = CreateSecurity(code: "TSLA", board: "NYSE");

		var criteria = new SecurityLookupMessage();
		criteria.SecurityIds =
		[
			new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" },
		];

		// Exact matches
		aapl.IsMatch(criteria).AssertTrue();
		msft.IsMatch(criteria).AssertTrue();

		// Not in array
		goog.IsMatch(criteria).AssertFalse();

		// Wrong board
		tsla.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityIdsArray_OnlyCodeSpecified()
	{
		var aapl = CreateSecurity(code: "AAPL", board: "NASDAQ");
		var aaplNyse = CreateSecurity(code: "AAPL", board: "NYSE");

		// When only SecurityCode is set in SecurityIds, it requires exact code+board match
		// This is current behavior - SecurityIds is for exact ID matching
		var criteria = new SecurityLookupMessage();
		criteria.SecurityIds =
		[
			new SecurityId { SecurityCode = "AAPL" }, // No board
		];

		// Current behavior: when board is empty in criteria, it doesn't match
		// because GetHash returns 0 for empty IDs
		aapl.IsMatch(criteria).AssertFalse();
		aaplNyse.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_CrossBoardMatching()
	{
		var aaplNasdaq = CreateSecurity(code: "AAPL", board: "NASDAQ");
		var aaplBats = CreateSecurity(code: "AAPL", board: "BATS");
		var aaplArca = CreateSecurity(code: "AAPL", board: "ARCA");

		// Only code, matches all boards
		var codeOnlyCriteria = new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AAPL" } };
		aaplNasdaq.IsMatch(codeOnlyCriteria).AssertTrue();
		aaplBats.IsMatch(codeOnlyCriteria).AssertTrue();
		aaplArca.IsMatch(codeOnlyCriteria).AssertTrue();

		// Specific board
		var nasdaqCriteria = new SecurityLookupMessage { SecurityId = new() { SecurityCode = "AAPL", BoardCode = "NASDAQ" } };
		aaplNasdaq.IsMatch(nasdaqCriteria).AssertTrue();
		aaplBats.IsMatch(nasdaqCriteria).AssertFalse();
		aaplArca.IsMatch(nasdaqCriteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_NumericRangeEdgeCases()
	{
		var option = CreateSecurity(code: "TEST", board: "TEST", type: SecurityTypes.Option);
		option.Strike = 100m;
		option.Multiplier = 100m;
		option.PriceStep = 0.01m;

		// Exact matches
		option.IsMatch(new SecurityLookupMessage { Strike = 100m }).AssertTrue();
		option.IsMatch(new SecurityLookupMessage { Multiplier = 100m }).AssertTrue();
		option.IsMatch(new SecurityLookupMessage { PriceStep = 0.01m }).AssertTrue();

		// Very close but not exact
		option.IsMatch(new SecurityLookupMessage { Strike = 100.00001m }).AssertFalse();
		option.IsMatch(new SecurityLookupMessage { PriceStep = 0.010001m }).AssertFalse();

		// Zero values
		var zeroMultiplier = CreateSecurity(code: "TEST2", board: "TEST");
		zeroMultiplier.Multiplier = 0m;

		zeroMultiplier.IsMatch(new SecurityLookupMessage { Multiplier = 0m }).AssertTrue();
		zeroMultiplier.IsMatch(new SecurityLookupMessage { Multiplier = 1m }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_DateEdgeCases()
	{
		var security = CreateSecurity(code: "TEST", board: "TEST");
		security.ExpiryDate = new DateTime(2024, 12, 31, 23, 59, 59);

		// Same date, different time
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2024, 12, 31, 0, 0, 0) }).AssertTrue();
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2024, 12, 31, 12, 0, 0) }).AssertTrue();

		// Different date
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2025, 1, 1) }).AssertFalse();
		security.IsMatch(new SecurityLookupMessage { ExpiryDate = new DateTime(2024, 12, 30) }).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_CombinedTypeAndBoardFiltering()
	{
		var nasdaqStock = CreateSecurity(code: "AAPL", board: "NASDAQ", type: SecurityTypes.Stock);
		var nasdaqEtf = CreateSecurity(code: "QQQ", board: "NASDAQ", type: SecurityTypes.Etf);
		var nyseStock = CreateSecurity(code: "IBM", board: "NYSE", type: SecurityTypes.Stock);
		var cmeOption = CreateSecurity(code: "ESZ24C5000", board: "CME", type: SecurityTypes.Option);

		// Find all stocks on NASDAQ
		var nasdaqStocksCriteria = new SecurityLookupMessage
		{
			SecurityId = new() { BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Stock,
		};
		nasdaqStock.IsMatch(nasdaqStocksCriteria).AssertTrue();
		nasdaqEtf.IsMatch(nasdaqStocksCriteria).AssertFalse();
		nyseStock.IsMatch(nasdaqStocksCriteria).AssertFalse();
		cmeOption.IsMatch(nasdaqStocksCriteria).AssertFalse();

		// Find all equity-like instruments (stocks + ETFs)
		var equityLikeCriteria = new SecurityLookupMessage();
		equityLikeCriteria.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Etf });

		nasdaqStock.IsMatch(equityLikeCriteria).AssertTrue();
		nasdaqEtf.IsMatch(equityLikeCriteria).AssertTrue();
		nyseStock.IsMatch(equityLikeCriteria).AssertTrue();
		cmeOption.IsMatch(equityLikeCriteria).AssertFalse();
	}

	#endregion

	#region Helper Methods

	private static SecurityMessage CreateSecurity(
		string code = "TEST",
		string board = "TEST",
		SecurityTypes? type = null)
	{
		return new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = code, BoardCode = board },
			SecurityType = type,
		};
	}

	#endregion
}
