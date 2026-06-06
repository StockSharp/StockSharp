namespace StockSharp.Tests;

using StockSharp.Diagram;

[TestClass]
public class InMemoryCompositionModelBehaviorTests : BaseTestClass
{
	private static InMemoryCompositionModelBehavior CreateWithNode(out InMemoryCompositionModelNode node)
	{
		var behavior = new InMemoryCompositionModelBehavior();
		node = new InMemoryCompositionModelNode();
		behavior.AddNode(node);
		return behavior;
	}

	[TestMethod]
	public void RaiseSocketAdded_DoesNotThrow_AndNotifiesInvalidateRelationships()
	{
		var behavior = CreateWithNode(out var node);

		(ModelChange change, object data) captured = default;
		behavior.BehaviorChanged += t => captured = (t.change, t.data);

		behavior.RaiseSocketAdded(node);

		captured.change.AssertEqual(ModelChange.InvalidateRelationships);
		captured.data.AssertSame(node);
	}

	[TestMethod]
	public void RaiseLinksRemoved_DoesNotThrow_AndNotifiesInvalidateRelationships()
	{
		var behavior = CreateWithNode(out var node);

		(ModelChange change, object data) captured = default;
		behavior.BehaviorChanged += t => captured = (t.change, t.data);

		behavior.RaiseLinksRemoved(node);

		captured.change.AssertEqual(ModelChange.InvalidateRelationships);
		captured.data.AssertSame(node);
	}

	[TestMethod]
	public void RaiseCommited_DoesNotThrow_AndNotifiesProperty()
	{
		var behavior = CreateWithNode(out var node);

		(ModelChange change, object data, string propName) captured = default;
		behavior.BehaviorChanged += t => captured = (t.change, t.data, t.propName);

		behavior.RaiseCommited("op", node, null);

		captured.change.AssertEqual(ModelChange.Property);
		captured.data.AssertSame(node);
		captured.propName.AssertEqual("op");
	}
}
