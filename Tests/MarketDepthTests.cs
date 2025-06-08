namespace StockSharp.Tests;

[TestClass]
public class MarketDepthTests
{
	private static readonly decimal[] _priceRanges = [0.01m, 0.1m, 1m, 10m, 100m, 1000m];
	private const int _maxDepth = int.MaxValue;

	[TestMethod]
	public void Sparse()
	{
		foreach (var step in _priceRanges)
		{
			for (var x = 0; x < 100; x++)
			{
				var security = Helper.CreateSecurity();

				var generator = new TrendMarketDepthGenerator(security.ToSecurityId())
				{
					MaxBidsDepth = RandomGen.GetInt(0, 10),
					MaxAsksDepth = RandomGen.GetInt(0, 10),
					GenerateDepthOnEachTrade = true,
				};
				generator.Init();

				generator.Process(security.ToMessage());
				generator.Process(security.Board.ToMessage());

				var depth = (QuoteChangeMessage)generator.Process(new ExecutionMessage
				{
					DataTypeEx = DataType.Ticks,
					SecurityId = generator.SecurityId,
					TradeVolume = RandomGen.GetInt(1, 10),
					TradePrice = RandomGen.GetInt(100, 200),
				});

				var sparseDepth = depth.Sparse(step, null, _maxDepth);
				sparseDepth.Verify();

				var sparseBids = sparseDepth.Bids.ToDictionary(p => p.Price);
				var sparseAsks = sparseDepth.Asks.ToDictionary(p => p.Price);

				foreach (var bid in depth.Bids)
					sparseBids.ContainsKey(bid.Price).AssertTrue();

				foreach (var ask in depth.Asks)
					sparseAsks.ContainsKey(ask.Price).AssertTrue();

				var bestBid = depth.GetBestBid();
				var bestAsk = depth.GetBestAsk();

				if (bestBid != null && bestAsk != null)
				{
					for (var p = bestBid.Value.Price + step; p < bestAsk.Value.Price; p += step)
					{
						(sparseBids.ContainsKey(p) || sparseAsks.ContainsKey(p)).AssertTrue();
					}
				}

				//if (depth.Bids.Length > 1)
				//	validateMiddle(depth.Bids.First().Price, depth.Bids.Last().Price, -step);

				//if (depth.Asks.Length > 1)
				//	validateMiddle(depth.Asks.First().Price, depth.Asks.Last().Price, step);
			}
		}
	}

	[TestMethod]
	public void Group()
	{
		var security = Helper.CreateSecurity();

		foreach (var priceRange in _priceRanges)
		{
			for (var x = 0; x < 5; x++)
			{
				var generator = new TrendMarketDepthGenerator(security.ToSecurityId())
				{
					MaxBidsDepth = RandomGen.GetInt(0, 10),
					MaxAsksDepth = RandomGen.GetInt(0, 10),
					GenerateDepthOnEachTrade = true,
				};
				generator.Init();

				generator.Process(security.ToMessage());
				generator.Process(security.Board.ToMessage());

				var depth = (QuoteChangeMessage)generator.Process(new ExecutionMessage
				{
					DataTypeEx = DataType.Ticks,
					SecurityId = generator.SecurityId,
					TradeVolume = RandomGen.GetInt(1, 10),
					TradePrice = RandomGen.GetInt(100, 200),
				});

				var grouppedDepth = depth.Group(priceRange);

				foreach (var q in grouppedDepth.Bids.Concat(grouppedDepth.Asks))
				{
					q.InnerQuotes.AssertNotNull();
					(q.InnerQuotes.Length > 0).AssertTrue();
				}

				var grouppedBids = new List<QuoteChange>();

				var bids = depth.Bids;

				if (bids.Length > 1)
				{
					var idx = 0;

					while (idx < bids.Length)
					{
						var currentLevel = bids[idx].Price;

						var gq = new QuoteChange
						{
							Price = currentLevel,
							InnerQuotes = [.. bids.Where(q => currentLevel >= q.Price && q.Price > (currentLevel - priceRange))]
						};

						idx += gq.InnerQuotes.Length;

						gq.Volume.AssertEqual(gq.InnerQuotes.Sum(q => q.Volume));

						if (gq.Volume > 0)
							grouppedBids.Add(gq);
					}
				}
				else if (bids.Length == 1)
				{
					var q = bids[0];
					grouppedBids.Add(new QuoteChange { Price = q.Price, Volume = q.Volume });
				}

				grouppedBids = [.. grouppedBids.OrderByDescending(q => q.Price)];

				grouppedBids.Count.AssertEqual(grouppedDepth.Bids.Length);

				for (var i = 0; i < grouppedBids.Count; i++)
				{
					Helper.CheckEqual(grouppedDepth.Bids[i], grouppedBids[i]);
				}

				var grouppedAsks = new List<QuoteChange>();

				var asks = depth.Asks;

				if (asks.Length > 1)
				{
					var idx = 0;

					while (idx < asks.Length)
					{
						var currentLevel = asks[idx].Price;

						var gq = new QuoteChange
						{
							Price = currentLevel,
							InnerQuotes = [.. asks.Where(q => currentLevel <= q.Price && q.Price < (currentLevel + priceRange))]
						};

						gq.Volume.AssertEqual(gq.InnerQuotes.Sum(q => q.Volume));

						if (gq.Volume > 0)
							grouppedAsks.Add(gq);

						idx += gq.InnerQuotes.Length;
					}
				}
				else if (asks.Length == 1)
				{
					var q = asks[0];
					grouppedAsks.Add(new QuoteChange { Price = q.Price, Volume = q.Volume });
				}

				grouppedAsks = [.. grouppedAsks.OrderBy(q => q.Price)];

				grouppedAsks.Count.AssertEqual(grouppedDepth.Asks.Length);

				for (var i = 0; i < grouppedAsks.Count; i++)
				{
					Helper.CheckEqual(grouppedDepth.Asks[i], grouppedAsks[i]);
				}
			}
		}
	}

	[TestMethod]
	public void GroupUngroup()
	{
		var security = Helper.CreateSecurity();

		foreach (var priceRange in _priceRanges)
		{
			for (var x = 0; x < 100; x++)
			{
				var generator = new TrendMarketDepthGenerator(security.ToSecurityId())
				{
					MaxBidsDepth = RandomGen.GetInt(0, 10),
					MaxAsksDepth = RandomGen.GetInt(0, 10),
					GenerateDepthOnEachTrade = true,
				};
				generator.Init();

				generator.Process(security.ToMessage());
				generator.Process(security.Board.ToMessage());

				var depth = (QuoteChangeMessage)generator.Process(new ExecutionMessage
				{
					DataTypeEx = DataType.Ticks,
					SecurityId = generator.SecurityId,
					TradeVolume = RandomGen.GetInt(1, 10),
					TradePrice = RandomGen.GetInt(100, 200),
				});

				var grouppedDepth = depth.Group(priceRange);
				var originalDepth = grouppedDepth.UnGroup();

				depth.BuildFrom = DataType.MarketDepth;
				Helper.CheckEqual(depth, originalDepth);
			}
		}
	}
}