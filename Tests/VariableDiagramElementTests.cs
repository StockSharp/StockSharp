namespace StockSharp.Tests;

using StockSharp.Diagram;
using StockSharp.Diagram.Elements;

[TestClass]
public class VariableDiagramElementTests : BaseTestClass
{
	private static Security AddKnownSecurity()
	{
		var security = new Security { Id = $"{Guid.NewGuid():N}@TEST", PriceStep = 0.5m };
		((CollectionSecurityProvider)ServicesRegistry.SecurityProvider).Add(security);
		return security;
	}

	/// <summary>
	/// A security the provider knows is taken from it when the variable is loaded: the element holds
	/// the provider's own instance, not a placeholder made from the identifier.
	/// </summary>
	[TestMethod]
	public async Task LoadedSecurityIsTheOneTheProviderHolds()
	{
		var security = AddKnownSecurity();

		var source = new VariableDiagramElement { Type = DiagramSocketType.Security, Value = security };
		var storage = await source.SaveAsync(CancellationToken);

		var loaded = new VariableDiagramElement();
		await loaded.LoadAsync(storage, CancellationToken);

		AreSame(security, loaded.Value);
	}

	/// <summary>
	/// A security the provider does not know yet is held as a placeholder with the saved identifier
	/// and is replaced by the provider's instance once the provider gets it.
	/// </summary>
	[TestMethod]
	public async Task LoadedSecurityUnknownToTheProviderIsReplacedWhenItArrives()
	{
		var id = $"{Guid.NewGuid():N}@TEST";

		var source = new VariableDiagramElement { Type = DiagramSocketType.Security, Value = new Security { Id = id } };
		var storage = await source.SaveAsync(CancellationToken);

		var loaded = new VariableDiagramElement();
		await loaded.LoadAsync(storage, CancellationToken);

		AreEqual(id, ((Security)loaded.Value).Id);

		var security = new Security { Id = id, PriceStep = 0.5m };
		((CollectionSecurityProvider)ServicesRegistry.SecurityProvider).Add(security);

		AreSame(security, loaded.Value);
	}
}
