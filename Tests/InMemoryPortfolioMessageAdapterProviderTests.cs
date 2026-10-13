namespace StockSharp.Tests;

[TestClass]
public class InMemoryPortfolioMessageAdapterProviderTests : BaseTestClass
{
	[TestMethod]
	public void TryGetAdapter_NameInAnotherCase_FindsTheAdapter()
	{
		var provider = new InMemoryPortfolioMessageAdapterProvider();
		var adapterId = Guid.NewGuid();
		provider.SetAdapter("Main", adapterId);

		AreEqual<Guid?>(adapterId, provider.TryGetAdapter("MAIN"));
	}

	[TestMethod]
	public void TryGetAdapter_NameWithAnInvisibleCharacter_FindsNothing()
	{
		var provider = new InMemoryPortfolioMessageAdapterProvider();
		provider.SetAdapter("Main", Guid.NewGuid());

		IsNull(provider.TryGetAdapter("Ma\u00ADin"));
	}
}
