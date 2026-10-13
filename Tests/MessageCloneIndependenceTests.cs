namespace StockSharp.Tests;

/// <summary>
/// Independence guarantees of mutable message payloads handed to two consumers.
/// </summary>
[TestClass]
public class MessageCloneIndependenceTests : BaseTestClass
{
	/// <summary>
	/// A grouped book keeps the quotes a level was built from in <see cref="QuoteChange.InnerQuotes"/>.
	/// Cloning the book copies the Bids and Asks arrays, so the second consumer owns its levels; the
	/// quotes inside them are part of the same payload and must be its own too.
	/// </summary>
	[TestMethod]
	public void QuoteChangeMessageCloneOwnsItsInnerQuotes()
	{
		var bid = new QuoteChange(100m, 0m)
		{
			InnerQuotes = [new QuoteChange(100m, 5m), new QuoteChange(100m, 7m)],
		};

		var original = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc),
			Bids = [bid],
		};

		var clone = (QuoteChangeMessage)original.Clone();

		clone.Bids[0].InnerQuotes[0].Volume = 500m;

		AreNotSame(original.Bids[0].InnerQuotes, clone.Bids[0].InnerQuotes);

		// the first consumer asked for a book with 5 and 7 behind the level, and 5 + 7 = 12 at it
		AreEqual(5m, original.Bids[0].InnerQuotes[0].Volume);
		AreEqual(7m, original.Bids[0].InnerQuotes[1].Volume);
		AreEqual(12m, original.Bids[0].Volume);
	}

	/// <summary>
	/// The body is the payload of a remote file message, so a clone that shares it hands two consumers
	/// one buffer: whichever writes first rewrites what the other is reading.
	/// </summary>
	[TestMethod]
	public void RemoteFileMessageCloneOwnsItsBody()
	{
		var original = new RemoteFileMessage
		{
			TransactionId = 42,
			SecurityId = Helper.CreateSecurityId(),
			FileDataType = DataType.Ticks,
			Date = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc),
			Body = [1, 2, 3],
		};

		var clone = (RemoteFileMessage)original.Clone();

		clone.Body[0] = 9;

		AreNotSame(original.Body, clone.Body);
		AreEqual((byte)1, original.Body[0]);
		AreEqual((byte)2, clone.Body[1]);
	}
}
