namespace StockSharp.Tests;

/// <summary>
/// Level1, news and board state storage.
/// </summary>
partial class StorageTests
{
	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task News(StorageFormats format)
	{
		var newsStorage = GetStorageRegistry().GetNewsMessageStorage(null, format);
		var token = CancellationToken;

		var news = Helper.RandomNews();

		await newsStorage.SaveAsync(news, token);

		var loaded = await newsStorage.LoadAsync(news.First().ServerTime, news.Last().ServerTime).ToArrayAsync(token);

		loaded.CompareMessages(news);

		await newsStorage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task BoardState(StorageFormats format)
	{
		var storage = GetStorageRegistry().GetBoardStateMessageStorage(null, format);
		var token = CancellationToken;

		var data = Helper.RandomBoardStates();

		await storage.SaveAsync(data, token);

		var loaded = await storage.LoadAsync(data.First().ServerTime, data.Last().ServerTime).ToArrayAsync(token);

		loaded.CompareMessages(data);

		await storage.DeleteWithCheckAsync(token);
	}

	private async Task Level1(StorageFormats format, bool isFractional, bool diffDays = false)
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var token = CancellationToken;

		var testValues = security.RandomLevel1(isFractional, diffDays, _level1Count);

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		await l1Storage.SaveAsync(testValues, token);
		var loaded = await l1Storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loaded.CompareMessages(testValues, skipLocalTime: format == StorageFormats.Csv);

		var loadedItems = await l1Storage.LoadAsync(testValues.First().ServerTime, testValues.Last().ServerTime).ToArrayAsync(token);
		loadedItems.CompareMessages(testValues, skipLocalTime: format == StorageFormats.Csv);

		await l1Storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task Level1(StorageFormats format)
	{
		return Level1(format, false);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1Empty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var token = CancellationToken;

		var testValues = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = DateTime.UtcNow,
			}
		};

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		await l1Storage.SaveAsync(testValues, token);
		var loaded = await l1Storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.Count().AssertEqual(0);

		await l1Storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	//[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task Level1DiffDays(StorageFormats format)
	{
		return Level1(format, false, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public Task Level1Fractional(StorageFormats format)
	{
		return Level1(format, true);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1MinMax(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var token = CancellationToken;

#pragma warning disable CS0618 // Type or member is obsolete
		security.PriceStep = security.MinPrice = 0.0000001m;
		security.MaxPrice = 100000000m;

		var securityId = security.ToSecurityId();
		var serverTime = DateTime.UtcNow;

		var testValues = new List<Level1ChangeMessage>
		{
			new Level1ChangeMessage
			{
				SecurityId = securityId,
				ServerTime = serverTime,
			}.TryAdd(Level1Fields.MinPrice, security.MinPrice)
		};

		serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));

		testValues.Add(new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
		}.TryAdd(Level1Fields.MaxPrice, security.MaxPrice));
#pragma warning restore CS0618 // Type or member is obsolete

		var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

		await l1Storage.SaveAsync(testValues, token);
		var loaded = await l1Storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loaded.CompareMessages(testValues, skipLocalTime: format == StorageFormats.Csv);

		var loadedItems = await l1Storage.LoadAsync(testValues.First().ServerTime, testValues.Last().ServerTime).ToArrayAsync(token);
		loadedItems.CompareMessages(testValues, skipLocalTime: format == StorageFormats.Csv);

		await l1Storage.DeleteWithCheckAsync(token);
	}

	//[DataTestMethod]
	//[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	//public void Level1Duplicates(StorageFormats format)
	//{
	//	var security = Helper.CreateSecurity();

	//	var securityId = security.ToSecurityId();
	//	var serverTime = DateTime.UtcNow;

	//	var testValues = new List<Level1ChangeMessage>();

	//	testValues.Add(new Level1ChangeMessage
	//	{
	//		SecurityId = securityId,
	//		ServerTime = serverTime,
	//	}.TryAdd(Level1Fields.LastTradePrice, 1000m));

	//	serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));

	//	testValues.Add(new Level1ChangeMessage
	//	{
	//		SecurityId = securityId,
	//		ServerTime = serverTime,
	//	}.TryAdd(Level1Fields.LastTradePrice, 1000m));

	//	serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));

	//	testValues.Add(new Level1ChangeMessage
	//	{
	//		SecurityId = securityId,
	//		ServerTime = serverTime,
	//	}
	//	.TryAdd(Level1Fields.LastTradePrice, 1000m)
	//	.TryAdd(Level1Fields.BestBidPrice, 999m)
	//	);

	//	var l1Storage = GetStorageRegistry().GetLevel1MessageStorage(securityId, null, format);

	//	await l1Storage.SaveAsync(testValues, token);
	//	var loaded = await l1Storage.LoadAsync(DateTime.MinValue, token).ToArrayAsync(token);

	//	testValues.RemoveAt(0);
	//	testValues[1].Changes.Remove(Level1Fields.LastTradePrice);

	//	var loadedItems = l1Storage.Load(testValues.First().ServerTime, testValues.Last().ServerTime).ToArray();
	//	loaded.CompareMessages(testValues);

	//	await l1Storage.DeleteWithCheckAsync(token);
	//}
}
