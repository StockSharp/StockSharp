namespace StockSharp.Tests;

/// <summary>
/// Order log storage: round-trip, extreme and fractional values, non-system entries.
/// </summary>
partial class StorageTests
{
	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogRandom(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _orderLogCount);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogFractionalVolume(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _orderLogCount, items =>
		{
			var volumeStep = /*items.First().Order.Security.VolumeStep = */0.00001m;

			foreach (var item in items)
			{
				item.OrderVolume *= volumeStep;

				if (item.TradeVolume is not null)
					item.TradeVolume *= volumeStep;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogFractionalVolume2(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _orderLogCount, items =>
		{
			var volumeStep = /*items.First().Order.Security.VolumeStep = */0.00001m;

			foreach (var item in items)
			{
				item.OrderVolume *= volumeStep * 0.1m;

				if (item.TradeVolume is not null)
					item.TradeVolume *= volumeStep * 0.1m;
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task OrderLogExtreme(StorageFormats format)
	{
		return OrderLogRandomSaveLoad(format, _orderLogCount, items =>
		{
			foreach (var item in items)
			{
				item.OrderPrice = RandomGen.GetDecimal();
				item.OrderVolume = RandomGen.GetBool() || item.OrderState == OrderStates.Active ? decimal.MaxValue : 0;

				if (item.TradePrice is not null)
				{
					item.TradePrice = RandomGen.GetDecimal();
					//item.TradeVolume = RandomGen.GetBool() ? decimal.MaxValue : null;
				}
			}
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task OrderLogNonSystem(StorageFormats format)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var quotes = security.RandomOrderLog(_orderLogCount);

		for (var i = 0; i < quotes.Length; i++)
		{
			if (i > 0 && RandomGen.GetInt(1000) % 10 == 0)
			{
				var item = quotes[i];

				item.IsSystem = false;
				item.OrderPrice = (item.OrderPrice / i).Round(10);

				if (item.TradePrice is not null)
				{
					item.IsSystem = false;

					if (RandomGen.GetInt(1000) % 20 == 0)
						item.TradePrice = (item.TradePrice.Value / i).Round(10);
				}
			}
		}

		var storage = GetStorageRegistry();

		var logStorage = storage.GetOrderLogMessageStorage(secId, null, format);
		await logStorage.SaveAsync(quotes, token);
		await LoadOrderLogAndCompare(logStorage, quotes, format);
		await logStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task OrderLogSameTime(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var dt = DateTime.UtcNow;
		var token = CancellationToken;

		var olStorage = GetStorageRegistry().GetOrderLogMessageStorage(secId, null, format);

		var ol = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				OrderId = 1,
				OrderPrice = 100,
				OrderState = OrderStates.Active,
				SecurityId = secId,
				LocalTime = dt.AddMilliseconds(10),
				ServerTime = dt,
				OrderVolume = 1,
				Side = Sides.Buy,
				TransactionId = 1,
				PortfolioName = Messages.Extensions.AnonymousPortfolioName,
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.OrderLog,
				OrderId = 1,
				OrderPrice = 100,
				OrderState = OrderStates.Done,
				SecurityId = secId,
				LocalTime = dt.AddMilliseconds(10),
				ServerTime = dt,
				OrderVolume = 1,
				Side = Sides.Sell,
				TransactionId = 2,
				PortfolioName = Messages.Extensions.AnonymousPortfolioName,
			},
		}.ToArray();

		await olStorage.SaveAsync([ol[0]], token);
		await olStorage.SaveAsync([ol[1]], token);

		await LoadOrderLogAndCompare(olStorage, ol, format);
		await olStorage.DeleteWithCheckAsync(token);
	}

	private async Task OrderLogRandomSaveLoad(StorageFormats format, int count, Action<IEnumerable<ExecutionMessage>> modify = null)
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var items = security.RandomOrderLog(count);

		modify?.Invoke(items);

		var storage = GetStorageRegistry();

		var logStorage = storage.GetOrderLogMessageStorage(secId, null, format);
		await logStorage.SaveAsync(items, token);
		await LoadOrderLogAndCompare(logStorage, items, format);
		await logStorage.DeleteWithCheckAsync(token);
	}

	private async Task LoadOrderLogAndCompare(IMarketDataStorage<ExecutionMessage> storage, IList<ExecutionMessage> items, StorageFormats format)
	{
		var token = CancellationToken;
		var loadedItems = await storage.LoadAsync(items.First().ServerTime, items.Last().ServerTime).ToArrayAsync(token);
		// OrderLog binary serializer does not save LocalTime
		loadedItems.CompareMessages(items, skipLocalTime: true);
	}
}
