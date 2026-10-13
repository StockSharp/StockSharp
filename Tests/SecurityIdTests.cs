namespace StockSharp.Tests;

using System.ComponentModel;

[TestClass]
public class SecurityIdTests : BaseTestClass
{
	[TestMethod]
	public void EqualityMatchesOrdinalCaseInsensitiveHashing()
	{
		var lower = new SecurityId
		{
			SecurityCode = "gazp",
			BoardCode = "tqbr",
		};
		var upper = new SecurityId
		{
			SecurityCode = "GAZP",
			BoardCode = "TQBR",
		};

		lower.AssertEqual(upper);
		lower.GetHashCode().AssertEqual(upper.GetHashCode());

		var ligature = new SecurityId
		{
			SecurityCode = "encyclop\u00e6dia",
			BoardCode = "test",
		};
		var expanded = new SecurityId
		{
			SecurityCode = "encyclopaedia",
			BoardCode = "test",
		};

		ligature.AssertNotEqual(expanded);
	}

	/// <summary>
	/// The native id is the venue's own handle for the instrument, and it is the truth when it is
	/// present: codes are how humans spell an instrument and the same instrument is spelled
	/// differently by different feeds. Comparing by code once a native id is known would split one
	/// instrument into two, or merge two into one, in every dictionary keyed by this id.
	/// </summary>
	[TestMethod]
	public void NativeIdWins_OverCodesInEquality()
	{
		var byOneFeed = new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
			Native = "N1",
		};

		var byAnotherFeed = new SecurityId
		{
			SecurityCode = "AAPL.US",
			BoardCode = "NSDQ",
			Native = "N1",
		};

		byOneFeed.AssertEqual(byAnotherFeed, "the same native id is the same instrument however it is spelled");
		byOneFeed.GetHashCode().AssertEqual(byAnotherFeed.GetHashCode(), "equal ids must hash alike or a dictionary never finds them");

		var otherInstrument = new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
			Native = "N2",
		};

		byOneFeed.AssertNotEqual(otherInstrument, "matching codes must not merge two different native ids");
	}

	/// <summary>
	/// The hash is cached, so an id whose code is corrected after it was first hashed must compare
	/// by the new code. A stale hash makes an id unequal to its own twin, and the entry already put
	/// into a dictionary under the old hash can never be looked up again.
	/// </summary>
	[TestMethod]
	public void ChangingACodeAfterHashingStillComparesByTheNewCode()
	{
		var id = new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
		};

		var staleHash = id.GetHashCode();

		id.SecurityCode = "MSFT";

		id.AssertEqual(new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" }, "the id must compare by the code it now carries");
		id.AssertNotEqual(new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" }, "the id must no longer answer to the code it dropped");
		id.GetHashCode().AssertNotEqual(staleHash, "the cached hash must be dropped when the code changes");
	}

	/// <summary>
	/// <see cref="SecurityId.Money"/> is a single shared value that the whole library compares
	/// against. Because an id is a struct, handing it out means handing out copies - so a copy has
	/// to refuse modification too, otherwise one caller renaming its copy would be free to do so
	/// while everything else still expects MONEY.
	/// </summary>
	[TestMethod]
	public void FrozenIdRefusesModificationThroughACopy()
	{
		var copy = SecurityId.Money;

		Throws<InvalidOperationException>(() => copy.SecurityCode = "GAZP", "a frozen id must refuse a new code");
		Throws<InvalidOperationException>(() => copy.BoardCode = "TQBR", "a frozen id must refuse a new board");
		Throws<InvalidOperationException>(() => copy.Native = 1L, "a frozen id must refuse a native id");

		SecurityId.Money.SecurityCode.AssertEqual("MONEY");
		SecurityId.Money.BoardCode.AssertEqual(SecurityId.AssociatedBoardCode);
	}

	/// <summary>
	/// The text form of an id is what ends up in logs and error messages, and it is the only
	/// description of an instrument the reader of a log has. It must name the code and the board,
	/// and it must show every foreign identifier the id carries, so that two ids that differ only
	/// by a native or ISIN id do not print identically.
	/// </summary>
	[TestMethod]
	public void ToStringNamesTheCodeBoardAndEveryForeignIdCarried()
	{
		new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
		}.ToString().AssertEqual("AAPL@NASDAQ");

		new SecurityId
		{
			SecurityCode = "AAPL",
			BoardCode = "NASDAQ",
			Native = 42L,
			Isin = "US0378331005",
			IQFeed = "AAPL.X",
			InteractiveBrokers = 7,
		}.ToString().AssertEqual("AAPL@NASDAQ,Native:42,ISIN:US0378331005,IQFeed:AAPL.X,IB:7");
	}

	/// <summary>
	/// The converter is how a security id typed into a settings file or a property grid becomes an
	/// id. It has to split the text on the board separator, because an id parsed into the code
	/// alone silently addresses a different instrument than the one that was written down.
	/// </summary>
	[TestMethod]
	public void ConverterSplitsAStringIdIntoCodeAndBoard()
	{
		var converter = new StringToSecurityIdTypeConverter();
		var ctx = new Mock<ITypeDescriptorContext>().Object;

		converter.CanConvertFrom(ctx, typeof(string)).AssertTrue();

		var id = (SecurityId)converter.ConvertFrom(ctx, CultureInfo.InvariantCulture, "AAPL@NASDAQ");

		id.SecurityCode.AssertEqual("AAPL");
		id.BoardCode.AssertEqual("NASDAQ");
	}

	/// <summary>
	/// Text with no board separator does not name an instrument, so the converter must not build an
	/// id out of half of it: an id with a code and no board matches nothing and would be taken for a
	/// real instrument everywhere it is passed on.
	/// </summary>
	[TestMethod]
	public void ConverterBuildsNoIdFromTextWithoutABoard()
	{
		var converter = new StringToSecurityIdTypeConverter();
		var ctx = new Mock<ITypeDescriptorContext>().Object;

		var id = (SecurityId)converter.ConvertFrom(ctx, CultureInfo.InvariantCulture, "AAPL");

		id.SecurityCode.IsEmpty().AssertTrue("no board means no instrument, so no code either");
		id.BoardCode.IsEmpty().AssertTrue("no board means no instrument");
	}

	/// <summary>
	/// A type converter is normally reached through <see cref="TypeDescriptor"/>, which passes no
	/// design-time context at all. Converting an id from a config file or from code must therefore
	/// work without one - a converter that only answers a property grid cannot be used to read
	/// settings.
	/// </summary>
	[TestMethod]
	public void ConverterParsesAStringIdWithoutADesignTimeContext()
	{
		var converter = new StringToSecurityIdTypeConverter();

		var id = (SecurityId)converter.ConvertFrom("AAPL@NASDAQ");

		id.SecurityCode.AssertEqual("AAPL");
		id.BoardCode.AssertEqual("NASDAQ");
	}
}
