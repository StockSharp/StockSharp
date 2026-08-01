namespace StockSharp.Tests;

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
}
