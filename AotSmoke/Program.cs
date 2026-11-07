namespace StockSharp.AotSmoke;

using System;

using StockSharp.MatchingEngine;
using StockSharp.Messages;

/// <summary>
/// Minimal, statically reachable entry point for the linker/AOT compiler.
/// This is an audit harness, not a production application or an AOT compatibility claim.
/// </summary>
internal static class Program
{
	private static void Main()
	{
		var bids = new[] { new QuoteChange(100m, 2m) };
		var asks = new[] { new QuoteChange(101m, 3m) };
		var book = new OrderBook(default);
		book.SetSnapshot(bids, asks);

		var now = DateTime.UtcNow;
		var snapshot = book.ToMessage(now, now);
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(snapshot);
		var rebuilt = builder.GetSnapshot(now);

		if (rebuilt.Bids.Length != 1 || rebuilt.Asks.Length != 1)
			throw new InvalidOperationException("AOT smoke test failed: unexpected order book.");

		Console.WriteLine("Core AOT smoke test passed.");
	}
}
