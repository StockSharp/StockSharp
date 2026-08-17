namespace StockSharp.Tests;

/// <summary>
/// What the bookkeeping behind a shared subscription owes its caller: one upstream per key, opened
/// by the first holder and given up after the last, and everything a vanished holder held released
/// in one go.
/// </summary>
[TestClass]
public class SharedSubscriptionRegistryTests : BaseTestClass
{
	private static SharedSubscriptionRegistry<string, string, string, long> CreateRegistry() => new();

	[TestMethod]
	public void FirstHolderOpens_AndTheRestJoin()
	{
		var registry = CreateRegistry();

		var first = registry.Add("ticks", "a", "a", _ => 1L, out var firstOpens);
		var second = registry.Add("ticks", "b", "b", _ => 2L, out var secondOpens);

		IsTrue(firstOpens);
		IsFalse(secondOpens);
		AreSame(first, second);
		AreEqual(1L, second.Payload, "the second holder joined what the first opened, so the first payload stands");
		AreEqual(2, second.Holders.Count);
	}

	[TestMethod]
	public void DifferentKeys_AreDifferentSubscriptions()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "a", _ => 1L, out var ticksOpens);
		registry.Add("depth", "b", "b", _ => 2L, out var depthOpens);

		IsTrue(ticksOpens);
		IsTrue(depthOpens);
	}

	[TestMethod]
	public void OneHolderAskingTwice_HoldsItOnce()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "a", _ => 1L, out _);
		var again = registry.Add("ticks", "a", "a", _ => 9L, out var opensAgain);

		IsNull(again);
		IsFalse(opensAgain);
		IsTrue(registry.TryGetByKey("ticks", out var entry));
		AreEqual(1, entry.Holders.Count);
	}

	[TestMethod]
	public void OnlyTheLastHolderLeaving_GivesTheSubscriptionUp()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "a", _ => 1L, out _);
		registry.Add("ticks", "b", "b", _ => 1L, out _);

		IsFalse(registry.Remove("a", out var afterFirst), "one of two holders left, so the subscription stays");
		AreEqual(1L, afterFirst.Payload);
		IsTrue(registry.TryGetByKey("ticks", out _));

		IsTrue(registry.Remove("b", out var afterLast), "the last holder left, so the subscription is given up");
		AreEqual(1L, afterLast.Payload);
		IsFalse(registry.TryGetByKey("ticks", out _));
	}

	[TestMethod]
	public void RemovingAHolderThatHoldsNothing_ChangesNothing()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "a", _ => 1L, out _);

		IsFalse(registry.Remove("nobody", out var entry));
		IsNull(entry);
		AreEqual(1, registry.Entries().Count);
	}

	[TestMethod]
	public void AHolderCanBeFoundByWhatItHolds()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "a", _ => 7L, out _);

		IsTrue(registry.TryGetByHolder("a", out var entry));
		AreEqual(7L, entry.Payload);

		IsFalse(registry.TryGetByHolder("b", out var missing));
		IsNull(missing);
	}

	[TestMethod]
	public void EverythingAVanishedHolderHeld_IsReleasedAtOnce()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "gone:1", "gone:1", _ => 1L, out _);
		registry.Add("depth", "gone:2", "gone:2", _ => 2L, out _);
		registry.Add("news", "stays:1", "stays:1", _ => 3L, out _);
		registry.Add("ticks", "stays:2", "stays:2", _ => 1L, out _);

		var released = registry.RemoveWhere(holder => holder.StartsWith("gone:"));

		AreEqual(1, released.Count, "ticks kept a holder, so only depth was given up");
		AreEqual(2L, released.First().Payload);

		IsTrue(registry.TryGetByKey("ticks", out var ticks));
		AreEqual(1, ticks.Holders.Count);
		IsTrue(registry.TryGetByKey("news", out _));
		IsFalse(registry.TryGetByKey("depth", out _));
	}

	[TestMethod]
	public void DroppingASubscription_TakesItsHoldersWithIt()
	{
		var registry = CreateRegistry();

		var dropped = registry.Add("ticks", "a", "a", _ => 1L, out _);
		registry.Add("ticks", "b", "b", _ => 1L, out _);

		registry.Drop(dropped);
		AreEqual(1L, dropped.Payload);

		IsFalse(registry.TryGetByKey("ticks", out _));
		IsFalse(registry.TryGetByHolder("a", out _), "a holder of a dropped subscription holds nothing");
		IsFalse(registry.TryGetByHolder("b", out _));

		registry.Add("ticks", "a", "a", _ => 5L, out var opensAgain);
		IsTrue(opensAgain, "the subscription was given up whole, so the next holder opens it again");
	}

	[TestMethod]
	public void WhatEachHolderAskedFor_IsKeptWithIt()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "full book", _ => 1L, out _);
		var entry = registry.Add("ticks", "b", "top of book", _ => 1L, out _);

		AreEqual("full book", entry.Holders["a"]);
		AreEqual("top of book", entry.Holders["b"], "holders of one subscription need not have asked for the same thing");

		registry.Remove("a", out _);

		IsFalse(entry.Holders.ContainsKey("a"), "a holder that let go leaves nothing behind");
		AreEqual("top of book", entry.Holders["b"]);
	}

	[TestMethod]
	public void AnUnkeyedSubscription_StillAnswersItsHolders()
	{
		var registry = CreateRegistry();

		var entry = registry.Add("ticks", "a", "a", _ => 1L, out _);

		registry.Unkey(entry);

		IsFalse(registry.TryGetByKey("ticks", out _), "it stopped, so nobody joins it any more");
		IsTrue(registry.TryGetByHolder("a", out var held), "but whoever held it is still being answered");
		AreSame(entry, held);

		registry.Add("ticks", "b", "b", _ => 2L, out var opensAgain);
		IsTrue(opensAgain, "the next holder opens a new one rather than joining the stopped one");

		IsTrue(registry.Remove("a", out _), "the last holder of the stopped one still lets go of it");
	}

	[TestMethod]
	public void ClearForgetsEverything()
	{
		var registry = CreateRegistry();

		registry.Add("ticks", "a", "a", _ => 1L, out _);
		registry.Clear();

		AreEqual(0, registry.Entries().Count);
		IsFalse(registry.TryGetByHolder("a", out _));
	}
}
