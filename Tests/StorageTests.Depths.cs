namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Generation;

/// <summary>
/// Order book storage: snapshots, increments, extreme values, depth limits and delete.
/// </summary>
partial class StorageTests
{
	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthAdaptivePriceStep(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		security.PriceStep = 0.0001m;

		var depths = security.RandomDepths(_depthCount2);

		security.PriceStep = 0.1m;

		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format);

		var token = CancellationToken;

		await storage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(storage, depths);

		await storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthLowPriceStep(StorageFormats format)
	{
		var token = CancellationToken;
		
		var security = Helper.CreateSecurity();
		security.PriceStep = 0.00000001m;

		var secId = security.ToSecurityId();

		var depths = security.RandomDepths(_depthCount2);

		var storage = GetStorageRegistry().GetQuoteMessageStorage(secId, null, format: format);

		await storage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(storage, depths);

		await storage.DeleteWithCheckAsync(token);
	}

	private static IMarketDataStorage<QuoteChangeMessage> GetDepthStorage(SecurityId security, StorageFormats format)
	{
		return GetStorageRegistry().GetQuoteMessageStorage(security, null, format);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthInvalidVolume(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.UtcNow,
			SecurityId = secId,
			Bids = [new(1, -1)],
		};

		var storage = GetDepthStorage(secId, format);
		return ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () => { await storage.SaveAsync([depth], token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthInvalidSecurity(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth = new QuoteChangeMessage
		{
			SecurityId = new() { SecurityCode = "another", BoardCode = BoardCodes.Ux },
			Bids = [new(1, 1)],
		};

		var storage = GetDepthStorage(secId, format);
		return ThrowsExactlyAsync<ArgumentException>(async () => { await storage.SaveAsync([depth], token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public async Task DepthOutOfOrder_AppendOnlyDisabled_RoundTrips(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var storage = GetDepthStorage(secId, format);
		storage.AppendOnlyNew = false;
		await storage.SaveAsync([depth2], token);
		await storage.SaveAsync([depth1], token);

		var loaded = await storage.LoadAsync(depth1.ServerTime, depth2.ServerTime).ToArrayAsync(token);
		loaded.Length.AssertEqual(2);
		loaded.Select(d => d.ServerTime).OrderBy(t => t).SequenceEqual([depth1.ServerTime, depth2.ServerTime]).AssertTrue();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public async Task DepthInvalidOrder2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth2], token);
		await LoadDepthsAndCompare(depthStorage, [depth2]);

		await depthStorage.SaveAsync([depth1], token);
		await LoadDepthsAndCompare(depthStorage, [depth2]);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Попытка записать неупорядоченные стаканы.")]
	public async Task DepthInvalidOrder3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		depthStorage.AppendOnlyNew = false;
		await depthStorage.SaveAsync([depth2], token);
		await LoadDepthsAndCompare(depthStorage, [depth2]);

		await depthStorage.SaveAsync([depth1], token);

		var loaded = await depthStorage.LoadAsync(depth1.ServerTime, depth2.ServerTime).ToArrayAsync(token);
		loaded.Length.AssertEqual(2);
		loaded.Select(d => d.ServerTime).OrderBy(t => t).SequenceEqual([depth1.ServerTime, depth2.ServerTime]).AssertTrue();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Все переданные стаканы является пустыми.")]
	public async Task DepthInvalidEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.UtcNow,
			SecurityId = secId,
		};

		var depths = new[] { depth };

		var storage = GetDepthStorage(secId, format);
		await storage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(storage, depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	//[ExpectedException(typeof(ArgumentException), "Переданный стакан является пустым.")]
	public async Task DepthEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
			Bids = [new(2, 2)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthNegativePrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
			Bids = [new(-10, 1)],
			Asks = [new(-0.1m, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Asks = [new(-0.1m, 1), new(1, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
			Bids = [new(-10, 1)],
			Asks = [new(-0.1m, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthZeroPrices(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			Bids = [new(-0.1m, 1)],
			Asks = [new(0, 1)],
		};

		var depth2 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			Asks = [new(0, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			Bids = [new(-10, 1)],
			Asks = [new(0, 1)],
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthEmpty2(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0),
			SecurityId = secId,
		};

		var depth2 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1),
			SecurityId = secId,
			Bids = [new(1, 1)],
		};

		var depth3 = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2),
			SecurityId = secId,
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthEmpty3(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depth1 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 0) };
		var depth2 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 1) };
		var depth3 = new QuoteChangeMessage { SecurityId = secId, ServerTime = new DateTime(2005, 1, 1, 0, 0, 0, 2) };

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync([depth1, depth2, depth3], token);
		await LoadDepthsAndCompare(depthStorage, [depth1, depth2, depth3]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthPartSave(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount2);

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths.Take(500), token);
		await LoadDepthsAndCompare(depthStorage, [.. depths.Take(500)]);

		await depthStorage.SaveAsync([.. depths.Skip(500)], token);
		await LoadDepthsAndCompare(depthStorage, [.. depths.Skip(500)]);

		await LoadDepthsAndCompare(depthStorage, depths);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task DepthHalfFilled(StorageFormats format, int count)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var depthStorage = GetDepthStorage(secId, format);

		var generator = new TrendMarketDepthGenerator(secId);
		generator.Init();

		var secMsg = security.ToMessage();

		generator.Process(secMsg);
		generator.Process(security.Board.ToMessage());

		var time = DateTime.UtcNow;

		var depths = new List<QuoteChangeMessage>();

		var token = CancellationToken;

		for (var x = 0; x < count; x++)
		{
			var isBids = RandomGen.GetBool();
			var maxDepth = RandomGen.GetInt(1, 5);

			generator.MaxBidsDepth = isBids ? maxDepth : 0;
			generator.MaxAsksDepth = isBids ? 0 : maxDepth;

			if (generator.MaxBidsDepth == 0 && generator.MaxAsksDepth == 0)
				continue;

			depths.Add((QuoteChangeMessage)generator.Process(new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = RandomGen.GetInt(100, 120),
				TradeVolume = RandomGen.GetInt(1, 20),
				ServerTime = time.AddDays(x),
				SecurityId = secId,
			}));
		}

		await depthStorage.SaveAsync(depths, token);

		await LoadDepthsAndCompare(depthStorage, depths);

		var from = time;
		var to = from.AddDays(count + 1);

		await depthStorage.DeleteAsync(from, to, token);

		var loadedDepths = await depthStorage.LoadAsync(from, to).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthHalfFilled(StorageFormats format)
	{
		await DepthHalfFilled(format, _depthCount1);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandom(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomOrdersCount(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, ordersCount: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomConditions(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, conditions: true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthExtremePrice(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			//depths.First().Security.PriceStep = 0.0001m;

			foreach (var depth in depths)
			{
				var prices = Enumerable
					.Repeat(0, RandomGen.GetInt(20))
					.Select(i => RandomGen.GetDecimal())
					.OrderBy(v => v)
					.Distinct()
					.ToArray();

				var bidCount = RandomGen.GetInt(prices.Length);

				var bids = Enumerable
					.Repeat(0, bidCount)
					.Select((i, ind) => new QuoteChange(prices[ind], RandomGen.GetInt()))
					.OrderBy(q => 0 - q.Price)
					.ToArray();

				var asks = Enumerable
					.Repeat(0, prices.Length - bidCount)
					.Select((i, ind) => new QuoteChange(prices[bidCount + ind], RandomGen.GetInt()))
					.OrderBy(q => q.Price)
					.ToArray();

				depth.Bids = bids;
				depth.Asks = asks;
				//depth.ServerTime;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthExtremeVolume(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			//depths.First().Security.VolumeStep = 0.0001m;

			foreach (var depth in depths)
			{
				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume = RandomGen.GetDecimal();

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume = RandomGen.GetDecimal();
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthExtremeVolume2(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			//depths.First().Security.VolumeStep = 0.0001m;

			foreach (var depth in depths)
			{
				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume = RandomGen.GetBool() ? 1 : decimal.MaxValue;

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume = RandomGen.GetBool() ? 1 : decimal.MaxValue;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomNanosec(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, interval: TimeSpan.FromTicks(14465));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthFractionalVolume(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			var volumeStep = /*depths.First().Security.VolumeStep = */0.00001m;

			foreach (var depth in depths)
			{
				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume *= volumeStep;

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume *= volumeStep;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthFractionalVolume2(StorageFormats format)
	{
		await DepthRandom(format, _depthCount3, depths =>
		{
			var volumeStep = /*depths.First().Security.VolumeStep = */0.00001m;

			foreach (var depth in depths)
			{
				var volume = volumeStep * 0.1m;

				for (var i = 0; i < depth.Bids.Length; i++)
					depth.Bids[i].Volume *= volume;

				for (var i = 0; i < depth.Asks.Length; i++)
					depth.Asks[i].Volume *= volume;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var dt = DateTime.UtcNow;

		var depthStorage = GetDepthStorage(secId, format);

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				ServerTime = dt,
				SecurityId = secId,
				Bids = [new(10, 1)],
			},
			new QuoteChangeMessage
			{
				ServerTime = dt,
				SecurityId = secId,
				Bids = [new(11, 1)],
			},
		};

		await depthStorage.SaveAsync([depths[0]], token);
		await depthStorage.SaveAsync([depths[1]], token);

		await LoadDepthsAndCompare(depthStorage, depths);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task DepthRandom(StorageFormats format, int count, Action<QuoteChangeMessage[]> modify = null, TimeSpan? interval = null, bool ordersCount = false, bool conditions = false)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(count, interval, null, ordersCount);

		if (conditions)
		{
			foreach (var depth in depths)
			{
				if (!RandomGen.GetBool())
					continue;

				if (depth.Bids.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Bids.Length - 1);
					var q = depth.Bids[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Bids[idx] = q;
				}

				if (depth.Asks.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Asks.Length - 1);
					var q = depth.Asks[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Asks[idx] = q;
				}
			}
		}

		modify?.Invoke(depths);

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync(depths, token);
		await LoadDepthsAndCompare(depthStorage, depths);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3, TimeSpan.FromSeconds(2));

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);

		var randomDeleteDepths = depths.Select(d => RandomGen.GetInt(5) == 2 ? null : d).WhereNotNull().ToList();
		await depthStorage.DeleteAsync(randomDeleteDepths, token);

		await LoadDepthsAndCompare(depthStorage, [.. depths.Except(randomDeleteDepths).OrderBy(d => d.ServerTime)]);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthFullDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3);

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);

		await LoadDepthsAndCompare(depthStorage, depths);

		await depthStorage.DeleteAsync(depths.First().ServerTime, depths.Last().ServerTime, token);

		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		await depthStorage.SaveAsync(depths, token);

		await LoadDepthsAndCompare(depthStorage, depths);

		await depthStorage.DeleteAsync(depths, token);

		loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		loadedDepths = await depthStorage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomDateDelete(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3);

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);

		// The delete range must be derived from the generated books. RandomDepths advances time
		// by less than the generator interval per book, so the whole set spans minutes, and any
		// range built as an absolute offset from UtcNow lands entirely past the data - nothing
		// would be deleted and the comparison below would trivially pass.
		var minTime = DateTime.MaxValue;
		var maxTime = DateTime.MinValue;

		foreach (var d in depths)
		{
			minTime = d.ServerTime < minTime ? d.ServerTime : minTime;
			maxTime = d.ServerTime > maxTime ? d.ServerTime : maxTime;
		}

		var diff = maxTime - minTime;
		var third = TimeSpan.FromTicks(diff.Ticks / 3);

		var from = minTime + third;
		var to = maxTime - third;
		await depthStorage.DeleteAsync(from, to, token);

		var survived = depths.Where(d => d.ServerTime < from || d.ServerTime > to).OrderBy(d => d.ServerTime).ToArray();

		// The middle third must really have been removed while the outer thirds stay - otherwise
		// the range did not intersect the data and the round-trip check means nothing.
		(survived.Length > 0).AssertTrue();
		(survived.Length < depths.Length).AssertTrue();

		await LoadDepthsAndCompare(depthStorage, survived);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadDepthsAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var token = CancellationToken;
		
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomLessMaxDepth(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		const int depthSize = 20;

		var depths = security.RandomDepths(_depthCount3, new TrendMarketDepthGenerator(secId)
		{
			MaxBidsDepth = depthSize,
			MaxAsksDepth = depthSize,
		});

		//storage.MarketDepthMaxDepth = depthSize / 2;

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);

		await depthStorage.DeleteAsync(depths.First().ServerTime, depths.Last().ServerTime, token);
		loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomMoreMaxDepth(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount3);

		//storage.MarketDepthMaxDepth = 20;

		var depthStorage = GetDepthStorage(secId, format);

		await depthStorage.SaveAsync(depths, token);
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);

		await depthStorage.DeleteAsync(depths.First().ServerTime, depths.Last().ServerTime, token);
		loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);
		loadedDepths.Length.AssertEqual(0);

		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task DepthRandomIncrement(StorageFormats format, bool ordersCount, bool conditions)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = security.RandomDepths(_depthCount2, null, null, ordersCount);

		if (conditions)
		{
			foreach (var depth in depths)
			{
				if (!RandomGen.GetBool())
					continue;

				if (depth.Bids.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Bids.Length - 1);
					var q = depth.Bids[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Bids[idx] = q;
				}

				if (depth.Asks.Length > 0 && RandomGen.GetBool())
				{
					var idx = RandomGen.GetInt(depth.Asks.Length - 1);
					var q = depth.Asks[idx];
					q.Condition = QuoteConditions.Indicative;
					depth.Asks[idx] = q;
				}
			}
		}

		var diffQuotes = new List<QuoteChangeMessage>();

		for (var i = depths.Length - 1; i > 0; i--)
		{
			diffQuotes.Add(depths[i - 1].GetDelta(depths[i]));
		}

		diffQuotes.Reverse();

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync(diffQuotes, token);
		await LoadQuotesAndCompare(depthStorage, diffQuotes);
		await depthStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadQuotesAndCompare(IMarketDataStorage<QuoteChangeMessage> depthStorage, IList<QuoteChangeMessage> depths)
	{
		var token = CancellationToken;
		
		var loadedDepths = await depthStorage.LoadAsync(depths.First().ServerTime, depths.Last().ServerTime).ToArrayAsync(token);

		loadedDepths.CompareMessages(depths);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrement(StorageFormats format)
	{
		return DepthRandomIncrement(format, false, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementOrders(StorageFormats format)
	{
		return DepthRandomIncrement(format, true, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementOrdersConditions(StorageFormats format)
	{
		return DepthRandomIncrement(format, true, true);
	}

	private Task DepthRandomIncrementNonIncrement(StorageFormats format, bool isStateFirst)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = isStateFirst ? QuoteChangeStates.SnapshotComplete : null,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = isStateFirst ? null : QuoteChangeStates.SnapshotComplete,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		return ThrowsExactlyAsync<InvalidOperationException>(async () => { await depthStorage.SaveAsync(depths, token); });
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementNonIncrement(StorageFormats format)
	{
		return DepthRandomIncrementNonIncrement(format, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task DepthRandomIncrementNonIncrement2(StorageFormats format)
	{
		return DepthRandomIncrementNonIncrement(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task DepthRandomIncrementNonIncrement3(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var depths = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = QuoteChangeStates.SnapshotComplete,
			},

			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				Bids = [new(101, 1)],
				Asks = [new(102, 2)],
				State = null,
			},
		};

		var depthStorage = GetDepthStorage(secId, format);
		await depthStorage.SaveAsync(depths.Take(1), token);
		await ThrowsExactlyAsync<ArgumentException>(async () => await depthStorage.SaveAsync(depths.Skip(1), token));
	}
}
