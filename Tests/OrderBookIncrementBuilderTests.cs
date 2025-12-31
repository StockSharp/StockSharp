namespace StockSharp.Tests;

[TestClass]
public class OrderBookIncrementBuilderTests : BaseTestClass
{
	private static SecurityId CreateSec() => new() { SecurityCode = "TEST", BoardCode = "TEST" };

	[TestMethod]
	public void New_InsertAtEnd_Works()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		var change = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 }],
			Asks = []
        };

		var full = builder.TryApply(change);

		IsNotNull(full);
		AreEqual(1, full.Bids.Length);
		AreEqual(100m, full.Bids[0].Price);
		AreEqual(10m, full.Bids[0].Volume);
	}

	[TestMethod]
	public void Update_StartPosEqualsCount_ReturnsNullWhenInvalid()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		// initial snapshot with one quote
		var snapshot = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 }],
			Asks = []
        };

		builder.TryApply(snapshot);

		// Update with startPos == count (1) -> should return null
		var update = new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 100m, Volume = 15m, Action = QuoteChangeActions.Update, StartPosition = 1 }],
			Asks = []
        };

		IsNull(builder.TryApply(update));
	}

	[TestMethod]
	public void Delete_EndPositionLessThanStart_ReturnsNullWhenInvalid()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		// add two quotes
		var add = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [
				new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99m, Volume = 20m, Action = QuoteChangeActions.New, StartPosition = 1 }
			],
			Asks = []
        };

		builder.TryApply(add);

		var del = new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Action = QuoteChangeActions.Delete, StartPosition = 1, EndPosition = 0 }],
			Asks = []
        };

		IsNull(builder.TryApply(del));
	}

	[TestMethod]
	public void Delete_RemoveSingleAtPosition_Works()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		// add two quotes
		var add = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [
				new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99m, Volume = 20m, Action = QuoteChangeActions.New, StartPosition = 1 }
			],
			Asks = []
        };

		var full = builder.TryApply(add);
		IsNotNull(full);
		AreEqual(2, full.Bids.Length);

		// delete the first (startPos=0) with no EndPosition -> remove single
		var del = new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Action = QuoteChangeActions.Delete, StartPosition = 0 }],
			Asks = []
        };

		var after = builder.TryApply(del);
		IsNotNull(after);
		AreEqual(1, after.Bids.Length);
		AreEqual(99m, after.Bids[0].Price);
	}

	[TestMethod]
	public void NullChange_Throws()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		ThrowsExactly<ArgumentNullException>(() => builder.TryApply(null));
	}

	[TestMethod]
	public void NullState_Throws()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var msg = new QuoteChangeMessage { State = null };
		ThrowsExactly<ArgumentException>(() => builder.TryApply(msg));
	}

	[TestMethod]
	public void Update_NoBoundsCheck_ReturnsNull()
	{
		var builder = new OrderBookIncrementBuilder(
			new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" });

		// Create a message that triggers Update action with invalid position
		var change = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids =
			[
				new QuoteChange
				{
					Price = 100,
					Volume = 10,
					Action = QuoteChangeActions.Update,
					StartPosition = 0 // Position that doesn't exist in empty book
				}
			],
			Asks = []
		};

		IsNull(builder.TryApply(change));
	}

	[TestMethod]
	public void Delete_InvalidRange_ReturnsNull()
	{
		var builder = new OrderBookIncrementBuilder(
			new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" });

		// First add some quotes
		var addChange = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids =
			[
				new QuoteChange { Price = 100, Volume = 10, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99, Volume = 20, Action = QuoteChangeActions.New, StartPosition = 1 }
			],
			Asks = []
		};
		builder.TryApply(addChange);

		// Now try to delete with EndPosition < StartPosition (invalid)
		var deleteChange = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids =
			[
				new QuoteChange
				{
					Action = QuoteChangeActions.Delete,
					StartPosition = 1,
					EndPosition = 0 // Bug: EndPosition < StartPosition
				}
			],
			Asks = []
		};

		IsNull(builder.TryApply(deleteChange));
	}
}
