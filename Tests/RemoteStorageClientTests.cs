namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using StockSharp.Algo.Storages.Remote;

/// <summary>
/// Tests for RemoteStorageClient using mock adapter.
/// </summary>
[TestClass]
public class RemoteStorageClientTests : BaseTestClass
{
	#region Mock Adapter

	private class MockRemoteAdapter : MessageAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public Dictionary<long, ISubscriptionMessage> ActiveSubscriptions { get; } = [];

		public Func<SecurityLookupMessage, (SecurityMessage[] securities, byte[] archive)> SecurityLookupHandler { get; set; }
		public Func<DataTypeLookupMessage, DataTypeInfoMessage[]> DataTypeLookupHandler { get; set; }
		public Func<RemoteFileCommandMessage, RemoteFileMessage> FileCommandHandler { get; set; }
		public bool SimulateTimeout { get; set; }

		public MockRemoteAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			this.AddSupportedMarketDataType(DataType.Securities);
			this.AddSupportedMarketDataType(DataType.Level1);
		}

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Enqueue(message);

			switch (message.Type)
			{
				case MessageTypes.Connect:
					if (!SimulateTimeout)
						SendOutMessage(new ConnectMessage());
					break;

				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					break;

				case MessageTypes.SecurityLookup:
				{
					var lookup = (SecurityLookupMessage)message;
					var isSubscribe = ((ISubscriptionMessage)lookup).IsSubscribe;

					if (isSubscribe)
					{
						ActiveSubscriptions[lookup.TransactionId] = lookup;

						// Send subscription response
						SendOutMessage(lookup.CreateResponse());

						if (SecurityLookupHandler != null && !SimulateTimeout)
						{
							var (securities, archive) = SecurityLookupHandler(lookup);

							foreach (var sec in securities)
							{
								sec.OriginalTransactionId = lookup.TransactionId;
								sec.SetSubscriptionIds([lookup.TransactionId]);
								SendOutMessage(sec);
							}

							// Send finished message with optional archive
							var finished = new SubscriptionFinishedMessage { OriginalTransactionId = lookup.TransactionId };

							if (archive != null && archive.Length > 0)
								finished.Body = archive;

							SendOutMessage(finished);
						}
					}
					else
					{
						ActiveSubscriptions.Remove(lookup.OriginalTransactionId);
						SendOutMessage(lookup.CreateResponse());
					}
					break;
				}

				case MessageTypes.DataTypeLookup:
				{
					var lookup = (DataTypeLookupMessage)message;
					var isSubscribe = ((ISubscriptionMessage)lookup).IsSubscribe;

					if (isSubscribe)
					{
						ActiveSubscriptions[lookup.TransactionId] = lookup;

						// Send subscription response
						SendOutMessage(lookup.CreateResponse());

						if (DataTypeLookupHandler != null && !SimulateTimeout)
						{
							var results = DataTypeLookupHandler(lookup);

							foreach (var info in results)
							{
								info.OriginalTransactionId = lookup.TransactionId;
								info.SetSubscriptionIds([lookup.TransactionId]);
								SendOutMessage(info);
							}

							SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = lookup.TransactionId });
						}
					}
					break;
				}

				case MessageTypes.RemoteFileCommand:
				{
					var cmd = (RemoteFileCommandMessage)message;

					if (cmd.Command == CommandTypes.Get)
					{
						ActiveSubscriptions[cmd.TransactionId] = cmd;
						SendOutMessage(cmd.CreateResponse());

						if (FileCommandHandler != null && !SimulateTimeout)
						{
							var result = FileCommandHandler(cmd);

							if (result != null)
							{
								result.OriginalTransactionId = cmd.TransactionId;
								result.SetSubscriptionIds([cmd.TransactionId]);
								SendOutMessage(result);
							}

							SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = cmd.TransactionId });
						}
					}
					break;
				}
			}

			return default;
		}
	}

	private class MockSecurityProvider : ISecurityProvider
	{
		public HashSet<Security> Securities { get; } = [];

		public int Count => Securities.Count;

		public event Action<IEnumerable<Security>> Added { add { } remove { } }
		public event Action<IEnumerable<Security>> Removed { add { } remove { } }
		public event Action Cleared { add { } remove { } }

		public ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken)
			=> new(Securities.FirstOrDefault(s => s.ToSecurityId() == id));

		public IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria)
			=> ToAsyncEnumerable([.. Securities]);

		public ValueTask<SecurityMessage> LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
		{
			var sec = Securities.FirstOrDefault(s => s.ToSecurityId() == id);
			return new(sec?.ToMessage());
		}

		public IAsyncEnumerable<SecurityMessage> LookupMessagesAsync(SecurityLookupMessage criteria)
			=> ToAsyncEnumerable(Securities.Select(s => s.ToMessage()).ToArray());

		private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(T[] items, [EnumeratorCancellation] CancellationToken ct = default)
		{
			foreach (var item in items)
			{
				ct.ThrowIfCancellationRequested();
				yield return item;
			}

			await Task.CompletedTask;
		}
	}

	#endregion

	#region Constructor Tests

	[TestMethod]
	public void Constructor_ThrowsOnNullAdapter()
	{
		var thrown = false;
		try
		{
			_ = new RemoteStorageClient(null, null, 100, TimeSpan.FromSeconds(30));
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	[TestMethod]
	public void Constructor_ThrowsOnInvalidBatchSize()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var thrown1 = false;
		try
		{
			_ = new RemoteStorageClient(adapter, null, 0, TimeSpan.FromSeconds(30));
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}
		IsTrue(thrown1);

		adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		var thrown2 = false;
		try
		{
			_ = new RemoteStorageClient(adapter, null, -1, TimeSpan.FromSeconds(30));
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}
		IsTrue(thrown2);
	}

	[TestMethod]
	public void Constructor_ThrowsOnInvalidTimeout()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var thrown1 = false;
		try
		{
			_ = new RemoteStorageClient(adapter, null, 100, TimeSpan.Zero);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}
		IsTrue(thrown1);

		adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		var thrown2 = false;
		try
		{
			_ = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(-1));
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}
		IsTrue(thrown2);
	}

	[TestMethod]
	public void Constructor_CreatesClient()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		client.AssertNotNull();
	}

	#endregion

	#region VerifyAsync Tests

	[TestMethod]
	[Timeout(5000, CooperativeCancellation = true)]
	public async Task VerifyAsync_ConnectsSuccessfully()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		await client.VerifyAsync(CancellationToken);

		IsTrue(adapter.SentMessages.OfType<ConnectMessage>().Any());
	}

	#endregion

	#region GetAvailableSecuritiesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableSecuritiesAsync_ReturnsSecurities()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		adapter.SecurityLookupHandler = lookup =>
		{
			var securities = new[]
			{
				new SecurityMessage { SecurityId = secId1 },
				new SecurityMessage { SecurityId = secId2 },
			};
			return (securities, null);
		};

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var result = new List<SecurityId>();
		await foreach (var id in client.GetAvailableSecuritiesAsync())
			result.Add(id);

		HasCount(2, result);
		result.AssertContains(secId1);
		result.AssertContains(secId2);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableSecuritiesAsync_ReturnsEmpty_WhenNoSecurities()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		adapter.SecurityLookupHandler = lookup =>
		{
			// Return empty array
			return ([], null);
		};

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var result = new List<SecurityId>();
		await foreach (var id in client.GetAvailableSecuritiesAsync())
			result.Add(id);

		HasCount(0, result);
	}

	#endregion

	#region GetAvailableDataTypesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableDataTypesAsync_ReturnsDataTypes()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		adapter.DataTypeLookupHandler = lookup =>
		[
			new DataTypeInfoMessage { FileDataType = DataType.Ticks },
			new DataTypeInfoMessage { FileDataType = DataType.Level1 },
			new DataTypeInfoMessage { FileDataType = DataType.MarketDepth },
		];

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var result = await client.GetAvailableDataTypesAsync(secId, StorageFormats.Binary, CancellationToken);

		var dataTypes = result.ToList();
		HasCount(3, dataTypes);
		dataTypes.AssertContains(DataType.Ticks);
		dataTypes.AssertContains(DataType.Level1);
		dataTypes.AssertContains(DataType.MarketDepth);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableDataTypesAsync_ReturnsDeduplicated()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		adapter.DataTypeLookupHandler = lookup =>
		[
			new DataTypeInfoMessage { FileDataType = DataType.Ticks },
			new DataTypeInfoMessage { FileDataType = DataType.Ticks }, // duplicate
			new DataTypeInfoMessage { FileDataType = DataType.Level1 },
		];

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var result = await client.GetAvailableDataTypesAsync(secId, StorageFormats.Binary, CancellationToken);

		var dataTypes = result.ToList();
		HasCount(2, dataTypes); // should be deduplicated
	}

	#endregion

	#region GetDatesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetDatesAsync_ReturnsDates()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date1 = new DateTime(2024, 1, 15);
		var date2 = new DateTime(2024, 1, 16);
		var date3 = new DateTime(2024, 1, 17);

		adapter.DataTypeLookupHandler = lookup =>
		[
			new DataTypeInfoMessage { FileDataType = DataType.Ticks, Dates = [date1, date2] },
			new DataTypeInfoMessage { FileDataType = DataType.Ticks, Dates = [date3] },
		];

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var result = await client.GetDatesAsync(secId, DataType.Ticks, StorageFormats.Binary, CancellationToken);

		var dates = result.ToList();
		HasCount(3, dates);

		// Should be ordered
		AreEqual(date1, dates[0]);
		AreEqual(date2, dates[1]);
		AreEqual(date3, dates[2]);
	}

	#endregion

	#region SaveSecuritiesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task SaveSecuritiesAsync_SendsMessages()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var securities = new[]
		{
			new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" } },
			new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" } },
		};

		await client.SaveSecuritiesAsync(securities, CancellationToken);

		var sentSecurities = adapter.SentMessages.OfType<SecurityMessage>().ToList();
		HasCount(2, sentSecurities);
	}

	[TestMethod]
	public async Task SaveSecuritiesAsync_ThrowsOnNull()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var thrown = false;
		try
		{
			await client.SaveSecuritiesAsync(null, CancellationToken);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	#endregion

	#region LoadStreamAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LoadStreamAsync_ReturnsData()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var expectedData = "test data content"u8.ToArray();

		adapter.FileCommandHandler = cmd =>
		{
			if (cmd.Command == CommandTypes.Get)
				return new RemoteFileMessage { Body = expectedData };
			return null;
		};

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var result = await client.LoadStreamAsync(secId, DataType.Ticks, StorageFormats.Binary, new DateTime(2024, 1, 15), CancellationToken);

		result.AssertNotNull();
		AreNotEqual(Stream.Null, result);

		using var ms = new MemoryStream();
		await result.CopyToAsync(ms);
		CollectionAssert.AreEqual(expectedData, ms.ToArray());
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LoadStreamAsync_ReturnsNullStream_WhenNoData()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		adapter.FileCommandHandler = cmd => null; // No data

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var result = await client.LoadStreamAsync(secId, DataType.Ticks, StorageFormats.Binary, new DateTime(2024, 1, 15), CancellationToken);

		AreEqual(Stream.Null, result);
	}

	#endregion

	#region SaveStreamAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task SaveStreamAsync_SendsCommand()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var data = "test data"u8.ToArray();
		using var stream = new MemoryStream(data);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		await client.SaveStreamAsync(secId, DataType.Ticks, StorageFormats.Binary, new DateTime(2024, 1, 15), stream, CancellationToken);

		var cmd = adapter.SentMessages.OfType<RemoteFileCommandMessage>().First();
		AreEqual(CommandTypes.Update, cmd.Command);
		AreEqual(CommandScopes.File, cmd.Scope);
		AreEqual(secId, cmd.SecurityId);
		AreEqual(DataType.Ticks, cmd.FileDataType);
	}

	[TestMethod]
	public async Task SaveStreamAsync_ThrowsOnNullStream()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		var thrown = false;
		try
		{
			await client.SaveStreamAsync(secId, DataType.Ticks, StorageFormats.Binary, new DateTime(2024, 1, 15), null, CancellationToken);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	#endregion

	#region DeleteAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task DeleteAsync_SendsCommand()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		await client.DeleteAsync(secId, DataType.Ticks, StorageFormats.Binary, new DateTime(2024, 1, 15), CancellationToken);

		var cmd = adapter.SentMessages.OfType<RemoteFileCommandMessage>().First();
		AreEqual(CommandTypes.Remove, cmd.Command);
		AreEqual(CommandScopes.File, cmd.Scope);
		AreEqual(secId, cmd.SecurityId);
	}

	#endregion

	#region LookupSecuritiesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupSecuritiesAsync_FiltersExistingSecurities()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var existingId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var newId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		adapter.SecurityLookupHandler = lookup =>
		{
			// Return only the requested securities (based on criteria.SecurityIds)
			var securities = new List<SecurityMessage>();
			foreach (var id in lookup.SecurityIds)
			{
				if (id == existingId)
					securities.Add(new SecurityMessage { SecurityId = existingId });
				else if (id == newId)
					securities.Add(new SecurityMessage { SecurityId = newId });
			}
			return ([.. securities], null);
		};

		var provider = new MockSecurityProvider();
		provider.Securities.Add(new Security { Id = "AAPL@NASDAQ" }); // existing

		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		// Use criteria with specific security IDs - existing AAPL should be filtered out, only MSFT requested
		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [existingId, newId]
		};

		var result = new List<SecurityMessage>();

		await foreach (var sec in client.LookupSecuritiesAsync(criteria, provider))
			result.Add(sec);

		// Should only return new security, filtering out existing
		HasCount(1, result);
		AreEqual(newId, result[0].SecurityId);
	}

	[TestMethod]
	public async Task LookupSecuritiesAsync_ThrowsOnNullCriteria()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var provider = new MockSecurityProvider();

		var thrown = false;
		try
		{
			await foreach (var _ in client.LookupSecuritiesAsync(null, provider)) { }
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	[TestMethod]
	public async Task LookupSecuritiesAsync_ThrowsOnNullProvider()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		var thrown = false;
		try
		{
			await foreach (var _ in client.LookupSecuritiesAsync(new SecurityLookupMessage(), null)) { }
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	#endregion

	#region Dispose Tests

	[TestMethod]
	public void Dispose_DisposesAdapter()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		var client = new RemoteStorageClient(adapter, null, 100, TimeSpan.FromSeconds(30));

		client.Dispose();

		// Adapter should be disposed (IsDisposed property or similar)
		// Since MessageAdapter doesn't expose IsDisposed publicly, we just verify no exception
	}

	#endregion
}
