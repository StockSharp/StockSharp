namespace StockSharp.Tests;

[TestClass]
public class InMemoryPositionStorageTests : BaseTestClass
{
	[TestMethod]
	public void LookupByPortfolioName_NameInAnotherCase_FindsThePortfolio()
	{
		var storage = new InMemoryPositionStorage();
		var portfolio = new Portfolio { Name = "Main" };
		storage.Save(portfolio);

		AreSame(portfolio, storage.LookupByPortfolioName("MAIN"));
	}

	[TestMethod]
	public void LookupByPortfolioName_NameWithAnInvisibleCharacter_FindsNothing()
	{
		var storage = new InMemoryPositionStorage();
		storage.Save(new Portfolio { Name = "Main" });

		IsNull(storage.LookupByPortfolioName("Ma\u00ADin"));
	}
}
