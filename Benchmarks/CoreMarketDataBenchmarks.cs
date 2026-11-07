namespace StockSharp.Benchmarks;

using System;

using BenchmarkDotNet.Attributes;
using Ecng.Common;

using StockSharp.MatchingEngine;
using StockSharp.Messages;

/// <summary>
/// Compares the allocations and throughput of existing public order-book paths.
/// These operations do different amounts of work and should not be treated as equivalent algorithms.
/// </summary>
[MemoryDiagnoser]
public class CoreMarketDataBenchmarks
{
	private QuoteChangeMessage _source;
	private IOrderLogMarketDepthBuilder _orderLogBuilder;
	private OrderBook _matchingBook;

	private static readonly DateTime _time = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	[Params(10, 100, 1000)]
	public int Levels { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		var bids = new QuoteChange[Levels];
		var asks = new QuoteChange[Levels];

		for (var i = 0; i < Levels; i++)
		{
			bids[i] = new QuoteChange(10_000m - i, 1m + i);
			asks[i] = new QuoteChange(10_001m + i, 1m + i);
		}

		_source = new QuoteChangeMessage { Bids = bids, Asks = asks };
		_orderLogBuilder = new OrderLogMarketDepthBuilder(_source.TypedClone());

		_matchingBook = new OrderBook(default);
		_matchingBook.SetSnapshot(bids, asks);
	}

	[Benchmark]
	public QuoteChangeMessage CloneDepth()
		=> _source.TypedClone();

	[Benchmark]
	public QuoteChangeMessage OrderLogSnapshot()
		=> _orderLogBuilder.GetSnapshot(_time);

	[Benchmark]
	public QuoteChangeMessage MatchingEngineSnapshot()
		=> _matchingBook.ToMessage(_time, _time);
}
