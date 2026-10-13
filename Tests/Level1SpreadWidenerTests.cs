namespace StockSharp.Tests;

[TestClass]
public class Level1SpreadWidenerTests : BaseTestClass
{
	[TestMethod]
	public void ZeroPercent_IsDisabled()
	{
		var w = new Level1SpreadWidener(0m);
		w.IsEnabled.AreEqual(false);
		w.Percent.AreEqual(0m);
	}

	[TestMethod]
	public void NegativePercent_IsDisabled()
	{
		new Level1SpreadWidener(-5m).IsEnabled.AreEqual(false);
	}

	[TestMethod]
	public void PositivePercent_IsEnabled()
	{
		var w = new Level1SpreadWidener(2m);
		w.IsEnabled.AreEqual(true);
		w.Percent.AreEqual(2m);
	}

	[TestMethod]
	public void ZeroPercent_ReturnsOriginalUntouched()
	{
		var msg = new Level1ChangeMessage();
		msg.Add(Level1Fields.BestBidPrice, 100m);
		msg.Add(Level1Fields.BestAskPrice, 101m);

		var result = new Level1SpreadWidener(0m).Apply(msg);

		ReferenceEquals(result, msg).AreEqual(true);
		((decimal)msg.Changes[Level1Fields.BestBidPrice]).AreEqual(100m);
	}

	[TestMethod]
	public void PositivePercent_ReturnsCloneWithShiftedPrices_OriginalIntact()
	{
		var msg = new Level1ChangeMessage();
		msg.Add(Level1Fields.BestBidPrice, 100m);
		msg.Add(Level1Fields.BestAskPrice, 100m);

		var result = new Level1SpreadWidener(2m).Apply(msg);

		ReferenceEquals(result, msg).AreEqual(false);
		((decimal)result.Changes[Level1Fields.BestBidPrice]).AreEqual(98m);
		((decimal)result.Changes[Level1Fields.BestAskPrice]).AreEqual(102m);

		// Original untouched.
		((decimal)msg.Changes[Level1Fields.BestBidPrice]).AreEqual(100m);
		((decimal)msg.Changes[Level1Fields.BestAskPrice]).AreEqual(100m);
	}

	[TestMethod]
	public void OnlyBid_LeavesAskAlone()
	{
		var msg = new Level1ChangeMessage();
		msg.Add(Level1Fields.BestBidPrice, 100m);
		msg.Add(Level1Fields.LastTradePrice, 99m);

		var result = new Level1SpreadWidener(1m).Apply(msg);

		((decimal)result.Changes[Level1Fields.BestBidPrice]).AreEqual(99m);
		((decimal)result.Changes[Level1Fields.LastTradePrice]).AreEqual(99m);
		result.Changes.ContainsKey(Level1Fields.BestAskPrice).AreEqual(false);
	}

	[TestMethod]
	public void ZeroPrice_SkipsThatSide()
	{
		var msg = new Level1ChangeMessage();
		msg.Add(Level1Fields.BestBidPrice, 0m);
		msg.Add(Level1Fields.BestAskPrice, 100m);

		var result = new Level1SpreadWidener(5m).Apply(msg);

		((decimal)result.Changes[Level1Fields.BestBidPrice]).AreEqual(0m);
		((decimal)result.Changes[Level1Fields.BestAskPrice]).AreEqual(105m);
	}

	[TestMethod]
	public void NullMessage_ReturnsNull()
	{
		new Level1SpreadWidener(1m).Apply(null).AreEqual(null);
	}
}
