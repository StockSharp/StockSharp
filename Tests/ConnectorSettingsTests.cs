namespace StockSharp.Tests;

[TestClass]
public class ConnectorSettingsTests : BaseTestClass
{
	private static readonly Type[] _managedWrapperOrder =
	[
		typeof(FilteredMarketDepthAdapter),
		typeof(AssociatedSecurityAdapter),
		typeof(SnapshotHolderMessageAdapter),
		typeof(BasketSecurityMessageAdapter),
	];

	private static void DisableManagedWrappers(Connector connector)
	{
		connector.SupportFilteredMarketDepth = false;
		connector.SupportAssociatedSecurity = false;
		connector.SupportSnapshots = false;
		connector.SupportBasketSecurities = false;
	}

	private static Type[] GetManagedWrapperOrder(Connector connector)
	{
		var result = new List<Type>();
		IMessageAdapter current = connector.InnerAdapter;

		while (current is IMessageAdapterWrapper wrapper)
		{
			var type = current.GetType();

			if (_managedWrapperOrder.Contains(type))
				result.Add(type);

			current = wrapper.InnerAdapter;
		}

		return [.. result];
	}

	[TestMethod]
	public void ManagedWrappers_HaveCanonicalOrderRegardlessOfSetterOrder()
	{
		using var insideOut = new Connector();
		DisableManagedWrappers(insideOut);
		insideOut.SupportBasketSecurities = true;
		insideOut.SupportSnapshots = true;
		insideOut.SupportAssociatedSecurity = true;
		insideOut.SupportFilteredMarketDepth = true;

		using var outsideIn = new Connector();
		DisableManagedWrappers(outsideIn);
		outsideIn.SupportFilteredMarketDepth = true;
		outsideIn.SupportAssociatedSecurity = true;
		outsideIn.SupportSnapshots = true;
		outsideIn.SupportBasketSecurities = true;

		GetManagedWrapperOrder(insideOut).AssertEqual(_managedWrapperOrder);
		GetManagedWrapperOrder(outsideIn).AssertEqual(_managedWrapperOrder);
	}

	[TestMethod]
	public void SaveLoad_PreservesProcessingFlagsAndWrapperOrder()
	{
		using var source = new Connector();
		DisableManagedWrappers(source);

		// Deliberately enable these outside-in. Loading must produce the same canonical chain as
		// direct setters even though SettingsStorage has no ordering semantics.
		source.SupportFilteredMarketDepth = true;
		source.SupportAssociatedSecurity = true;
		source.SupportSnapshots = true;
		source.SupportBasketSecurities = true;
		source.TimeChange = false;

		var storage = new SettingsStorage();
		source.Save(storage);

		using var restored = new Connector();
		restored.Load(storage);

		restored.SupportBasketSecurities.AssertTrue();
		restored.SupportSnapshots.AssertTrue();
		restored.SupportAssociatedSecurity.AssertTrue();
		restored.SupportFilteredMarketDepth.AssertTrue();
		restored.TimeChange.AssertFalse();
		GetManagedWrapperOrder(restored).AssertEqual(GetManagedWrapperOrder(source));
	}
}
