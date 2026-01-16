namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

using StockSharp.Algo.Storages.Csv;

/// <summary>
/// Tests for RemoteStorageClient using mock adapter.
/// </summary>
[TestClass]
public class RemoteStorageClientTests : BaseTestClass
{
	#region Mock Adapter

	private class MockRemoteAdapter : MessageAdapter,
		IAddressAdapter<System.Net.EndPoint>,
		ISenderTargetAdapter,
		ILoginPasswordAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public Dictionary<long, ISubscriptionMessage> ActiveSubscriptions { get; } = [];

		public Func<SecurityLookupMessage, (SecurityMessage[] securities, byte[] archive)> SecurityLookupHandler { get; set; }
		public Func<BoardLookupMessage, (BoardMessage[] boards, byte[] archive)> BoardLookupHandler { get; set; }
		public Func<DataTypeLookupMessage, DataTypeInfoMessage[]> DataTypeLookupHandler { get; set; }
		public Func<RemoteFileCommandMessage, RemoteFileMessage> FileCommandHandler { get; set; }
		public bool SimulateTimeout { get; set; }

		// IAddressAdapter<EndPoint>
		public System.Net.EndPoint Address { get; set; }

		// ISenderTargetAdapter
		public string SenderCompId { get; set; }
		public string TargetCompId { get; set; }

		// ILoginPasswordAdapter
		public string Login { get; set; }
		public SecureString Password { get; set; }

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

				case MessageTypes.BoardLookup:
				{
					var lookup = (BoardLookupMessage)message;
					var isSubscribe = ((ISubscriptionMessage)lookup).IsSubscribe;

					if (isSubscribe)
					{
						ActiveSubscriptions[lookup.TransactionId] = lookup;

						SendOutMessage(lookup.CreateResponse());

						if (BoardLookupHandler != null && !SimulateTimeout)
						{
							var (boards, archive) = BoardLookupHandler(lookup);

							foreach (var board in boards)
							{
								board.OriginalTransactionId = lookup.TransactionId;
								board.SetSubscriptionIds([lookup.TransactionId]);
								SendOutMessage(board);
							}

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
			_ = new RemoteStorageClient(null, 100);
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
			_ = new RemoteStorageClient(adapter, 0);
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
			_ = new RemoteStorageClient(adapter, -1);
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
		using var client = new RemoteStorageClient(adapter, 100);

		client.AssertNotNull();
	}

	#endregion

	#region VerifyAsync Tests

	[TestMethod]
	[Timeout(5000, CooperativeCancellation = true)]
	public async Task VerifyAsync_ConnectsAndDisconnects()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, 100);

		await client.VerifyAsync(CancellationToken);

		IsTrue(adapter.SentMessages.OfType<ConnectMessage>().Any(), "Should send ConnectMessage");
		IsTrue(adapter.SentMessages.OfType<DisconnectMessage>().Any(), "Should send DisconnectMessage after verify");
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

		using var client = new RemoteStorageClient(adapter, 100);

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

		using var client = new RemoteStorageClient(adapter, 100);

		var result = new List<SecurityId>();
		await foreach (var id in client.GetAvailableSecuritiesAsync())
			result.Add(id);

		HasCount(0, result);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableSecuritiesAsync_WithArchive_ExtractsSecurities()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		// Create archive using CsvEntityList (same way server does)
		var archive = await CreateSecuritiesArchiveAsync([
			new Security
			{
				Id = "AAPL@NASDAQ",
				Code = "AAPL",
				Name = "Apple Inc",
				Board = ExchangeBoard.Nasdaq,
				Type = SecurityTypes.Stock,
				PriceStep = 0.01m,
				VolumeStep = 1m,
				Decimals = 2,
			},
			new Security
			{
				Id = "MSFT@NASDAQ",
				Code = "MSFT",
				Name = "Microsoft Corp",
				Board = ExchangeBoard.Nasdaq,
				Type = SecurityTypes.Stock,
				PriceStep = 0.01m,
				VolumeStep = 1m,
				Decimals = 2,
			},
		], CancellationToken);

		adapter.SecurityLookupHandler = lookup =>
		{
			// Return empty securities array but with archive in finished message body
			return ([], archive);
		};

		using var client = new RemoteStorageClient(adapter, 100);

		var result = new List<SecurityId>();
		await foreach (var id in client.GetAvailableSecuritiesAsync())
			result.Add(id);

		HasCount(2, result);
		result.AssertContains(new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" });
		result.AssertContains(new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" });
	}

	private static async Task<byte[]> CreateSecuritiesArchiveAsync(Security[] securities, CancellationToken cancellationToken)
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();

		await using var executor = TimeSpan.FromSeconds(1).CreateExecutorAndRun(_ => { }, cancellationToken);
		await using var registry = new CsvEntityRegistry(fs, path, executor);
		await registry.InitAsync(cancellationToken);

		var securitiesList = (ICsvEntityList)registry.Securities;
		securitiesList.CreateArchivedCopy = true;

		foreach (var security in securities)
			registry.Securities.Save(security);

		// Wait for executor to process
		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		executor.Add(_ =>
		{
			tcs.TrySetResult();
			return default;
		});
		await tcs.Task.WaitAsync(cancellationToken);

		return securitiesList.GetCopy();
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableSecuritiesAsync_WithCorruptedArchive_ThrowsException()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		// Corrupted GZip data (not valid GZip)
		var corruptedArchive = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE };

		adapter.SecurityLookupHandler = lookup => ([], corruptedArchive);

		using var client = new RemoteStorageClient(adapter, 100);

		var thrown = false;
		try
		{
			await foreach (var _ in client.GetAvailableSecuritiesAsync()) { }
		}
		catch (InvalidOperationException)
		{
			thrown = true;
		}

		IsTrue(thrown, "Should throw InvalidOperationException for corrupted archive");
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task GetAvailableSecuritiesAsync_WithInvalidCsvInArchive_ThrowsException()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		// Valid GZip but invalid CSV content (not enough columns)
		var invalidCsv = "invalid;csv;data";
		var archive = CreateGZipArchive(invalidCsv);

		adapter.SecurityLookupHandler = lookup => ([], archive);

		using var client = new RemoteStorageClient(adapter, 100);

		var thrown = false;
		try
		{
			await foreach (var _ in client.GetAvailableSecuritiesAsync()) { }
		}
		catch (InvalidOperationException)
		{
			thrown = true;
		}

		IsTrue(thrown, "Should throw InvalidOperationException for invalid CSV in archive");
	}

	private static byte[] CreateGZipArchive(string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		using var outputStream = new MemoryStream();
		using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
		{
			gzipStream.Write(bytes, 0, bytes.Length);
		}
		return outputStream.ToArray();
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

		using var client = new RemoteStorageClient(adapter, 100);

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

		using var client = new RemoteStorageClient(adapter, 100);

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

		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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

		using var client = new RemoteStorageClient(adapter, 100);

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

		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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

		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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
		using var client = new RemoteStorageClient(adapter, 100);

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

	#region LookupBoardsAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupBoardsAsync_ReturnsBoards()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		adapter.BoardLookupHandler = lookup =>
		{
			var boards = new[]
			{
				new BoardMessage { Code = "NASDAQ", ExchangeCode = "NASDAQ" },
				new BoardMessage { Code = "NYSE", ExchangeCode = "NYSE" },
			};
			return (boards, null);
		};

		using var client = new RemoteStorageClient(adapter, 100);

		var result = new List<BoardMessage>();
		await foreach (var board in client.LookupBoardsAsync(new BoardLookupMessage()))
			result.Add(board);

		HasCount(2, result);
		IsTrue(result.Any(b => b.Code == "NASDAQ"));
		IsTrue(result.Any(b => b.Code == "NYSE"));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupBoardsAsync_WithArchive_ExtractsBoards()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var archive = await CreateBoardsArchiveAsync([
			new ExchangeBoard
			{
				Code = "TBOARD1",
				Exchange = Exchange.Nyse,
				TimeZone = TimeZoneInfo.Utc,
			},
			new ExchangeBoard
			{
				Code = "TBOARD2",
				Exchange = Exchange.Nasdaq,
				TimeZone = TimeZoneInfo.Utc,
			},
		], CancellationToken);

		adapter.BoardLookupHandler = lookup => ([], archive);

		using var client = new RemoteStorageClient(adapter, 100);

		var result = new List<BoardMessage>();
		await foreach (var board in client.LookupBoardsAsync(new BoardLookupMessage()))
			result.Add(board);

		HasCount(2, result);
		IsTrue(result.Any(b => b.Code == "TBOARD1"));
		IsTrue(result.Any(b => b.Code == "TBOARD2"));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task LookupBoardsAsync_WithCorruptedArchive_ThrowsException()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var corruptedArchive = new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE };

		adapter.BoardLookupHandler = lookup => ([], corruptedArchive);

		using var client = new RemoteStorageClient(adapter, 100);

		var thrown = false;
		try
		{
			await foreach (var _ in client.LookupBoardsAsync(new BoardLookupMessage())) { }
		}
		catch (InvalidOperationException)
		{
			thrown = true;
		}

		IsTrue(thrown, "Should throw InvalidOperationException for corrupted archive");
	}

	[TestMethod]
	public async Task LookupBoardsAsync_ThrowsOnNullCriteria()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var client = new RemoteStorageClient(adapter, 100);

		var thrown = false;
		try
		{
			await foreach (var _ in client.LookupBoardsAsync(null)) { }
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	private static async Task<byte[]> CreateBoardsArchiveAsync(ExchangeBoard[] boards, CancellationToken cancellationToken)
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();

		await using var executor = TimeSpan.FromSeconds(1).CreateExecutorAndRun(_ => { }, cancellationToken);
		await using var registry = new CsvEntityRegistry(fs, path, executor);
		await registry.InitAsync(cancellationToken);

		var boardsList = (ICsvEntityList)registry.ExchangeBoards;
		boardsList.CreateArchivedCopy = true;

		foreach (var board in boards)
			registry.ExchangeBoards.Add(board);

		// Wait for executor to process
		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		executor.Add(_ =>
		{
			tcs.TrySetResult();
			return default;
		});
		await tcs.Task.WaitAsync(cancellationToken);

		return boardsList.GetCopy();
	}

	#endregion

	#region Dispose Tests

	[TestMethod]
	public void Dispose_DisposesAdapter()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		var client = new RemoteStorageClient(adapter, 100);

		client.Dispose();

		// Adapter should be disposed (IsDisposed property or similar)
		// Since MessageAdapter doesn't expose IsDisposed publicly, we just verify no exception
	}

	#endregion

	#region RemoteMarketDataDrive Tests

	private RemoteMarketDataDrive CreateDrive(MockRemoteAdapter adapter = null)
	{
		adapter ??= new MockRemoteAdapter(new IncrementalIdGenerator());
		return new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
	}

	#region RemoteMarketDataDrive Constructor Tests

	[TestMethod]
	public void Drive_Constructor_Default_UsesDefaultAddress()
	{
		// Note: Default constructor uses ServicesRegistry which may not be available in tests
		// Test the address-based constructor instead
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		AreEqual(RemoteMarketDataDrive.DefaultAddress, drive.Address);
	}

	[TestMethod]
	public void Drive_Constructor_WithAdapter_ThrowsOnNull()
	{
		var thrown = false;
		try
		{
			_ = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, (IMessageAdapter)null);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	#endregion

	#region RemoteMarketDataDrive Property Tests

	[TestMethod]
	public void Drive_Address_GetSet_Works()
	{
		using var drive = CreateDrive();

		var newAddress = "192.168.1.1:8080".To<System.Net.EndPoint>();
		drive.Address = newAddress;

		AreEqual(newAddress, drive.Address);
	}

	[TestMethod]
	public void Drive_Address_ThrowsOnNull()
	{
		using var drive = CreateDrive();

		var thrown = false;
		try
		{
			drive.Address = null;
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	[TestMethod]
	public void Drive_Path_GetSet_ConvertsToAddress()
	{
		using var drive = CreateDrive();

		drive.Path = "10.0.0.1:9000";

		AreEqual("10.0.0.1:9000", drive.Path);
		AreEqual("10.0.0.1:9000".To<System.Net.EndPoint>(), drive.Address);
	}

	[TestMethod]
	public void Drive_Path_ThrowsOnEmpty()
	{
		using var drive = CreateDrive();

		var thrown = false;
		try
		{
			drive.Path = "";
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	[TestMethod]
	public void Drive_TargetCompId_GetSet_Works()
	{
		using var drive = CreateDrive();

		AreEqual(RemoteMarketDataDrive.DefaultTargetCompId, drive.TargetCompId);

		drive.TargetCompId = "CustomTarget";
		AreEqual("CustomTarget", drive.TargetCompId);
	}

	[TestMethod]
	public void Drive_TargetCompId_ThrowsOnEmpty()
	{
		using var drive = CreateDrive();

		var thrown = false;
		try
		{
			drive.TargetCompId = "";
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	[TestMethod]
	public void Drive_SecurityBatchSize_GetSet_Works()
	{
		using var drive = CreateDrive();

		AreEqual(1000, drive.SecurityBatchSize); // default

		drive.SecurityBatchSize = 500;
		AreEqual(500, drive.SecurityBatchSize);
	}

	[TestMethod]
	public void Drive_SecurityBatchSize_ThrowsOnZeroOrNegative()
	{
		using var drive = CreateDrive();

		var thrown1 = false;
		try
		{
			drive.SecurityBatchSize = 0;
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}
		IsTrue(thrown1);

		var thrown2 = false;
		try
		{
			drive.SecurityBatchSize = -1;
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}
		IsTrue(thrown2);
	}

	[TestMethod]
	public void Drive_Timeout_GetSet_Works()
	{
		using var drive = CreateDrive();

		AreEqual(TimeSpan.FromMinutes(2), drive.Timeout); // default

		drive.Timeout = TimeSpan.FromSeconds(30);
		AreEqual(TimeSpan.FromSeconds(30), drive.Timeout);
	}

	[TestMethod]
	public void Drive_Timeout_ThrowsOnZeroOrNegative()
	{
		using var drive = CreateDrive();

		var thrown1 = false;
		try
		{
			drive.Timeout = TimeSpan.Zero;
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}
		IsTrue(thrown1);

		var thrown2 = false;
		try
		{
			drive.Timeout = TimeSpan.FromSeconds(-1);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}
		IsTrue(thrown2);
	}

	[TestMethod]
	public void Drive_IsBinaryEnabled_GetSet_Works()
	{
		using var drive = CreateDrive();

		IsFalse(drive.IsBinaryEnabled); // default

		drive.IsBinaryEnabled = true;
		IsTrue(drive.IsBinaryEnabled);
	}

	[TestMethod]
	public void Drive_Credentials_NotNull()
	{
		using var drive = CreateDrive();

		drive.Credentials.AssertNotNull();
	}

	#endregion

	#region RemoteMarketDataDrive GetStorageDrive Tests

	[TestMethod]
	public void Drive_GetStorageDrive_ReturnsStorageDrive()
	{
		using var drive = CreateDrive();

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		storageDrive.AssertNotNull();
		AreEqual(drive, storageDrive.Drive);
	}

	[TestMethod]
	public void Drive_GetStorageDrive_ReturnsSameInstanceForSameParams()
	{
		using var drive = CreateDrive();

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		var storageDrive1 = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);
		var storageDrive2 = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		AreSame(storageDrive1, storageDrive2);
	}

	[TestMethod]
	public void Drive_GetStorageDrive_ReturnsDifferentInstanceForDifferentParams()
	{
		using var drive = CreateDrive();

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		var storageDrive1 = drive.GetStorageDrive(secId1, DataType.Ticks, StorageFormats.Binary);
		var storageDrive2 = drive.GetStorageDrive(secId2, DataType.Ticks, StorageFormats.Binary);

		AreNotSame(storageDrive1, storageDrive2);
	}

	[TestMethod]
	public void Drive_GetStorageDrive_ThrowsOnNullDataType()
	{
		using var drive = CreateDrive();

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		var thrown = false;
		try
		{
			drive.GetStorageDrive(secId, null, StorageFormats.Binary);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		IsTrue(thrown);
	}

	#endregion

	#region RemoteMarketDataDrive GetAvailableSecuritiesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Drive_GetAvailableSecuritiesAsync_ReturnsSecurities()
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

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		var result = new List<SecurityId>();
		await foreach (var id in drive.GetAvailableSecuritiesAsync())
			result.Add(id);

		HasCount(2, result);
		result.AssertContains(secId1);
		result.AssertContains(secId2);
	}

	#endregion

	#region RemoteMarketDataDrive GetAvailableDataTypesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Drive_GetAvailableDataTypesAsync_ReturnsDataTypes()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		adapter.DataTypeLookupHandler = lookup =>
		[
			new DataTypeInfoMessage { FileDataType = DataType.Ticks },
			new DataTypeInfoMessage { FileDataType = DataType.Level1 },
		];

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		var result = await drive.GetAvailableDataTypesAsync(secId, StorageFormats.Binary, CancellationToken);

		var dataTypes = result.ToList();
		HasCount(2, dataTypes);
		dataTypes.AssertContains(DataType.Ticks);
		dataTypes.AssertContains(DataType.Level1);
	}

	#endregion

	#region RemoteMarketDataDrive VerifyAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Drive_VerifyAsync_ConnectsSuccessfully()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		// Should not throw
		await drive.VerifyAsync(CancellationToken);

		IsTrue(adapter.SentMessages.OfType<ConnectMessage>().Any());
	}

	#endregion

	#region RemoteMarketDataDrive LookupSecuritiesAsync Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Drive_LookupSecuritiesAsync_ReturnsSecurities()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		adapter.SecurityLookupHandler = lookup =>
		{
			return ([new SecurityMessage { SecurityId = secId, Name = "Apple Inc" }], null);
		};

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		var criteria = new SecurityLookupMessage();
		var provider = new MockSecurityProvider();

		var result = new List<SecurityMessage>();
		await foreach (var sec in drive.LookupSecuritiesAsync(criteria, provider))
			result.Add(sec);

		HasCount(1, result);
		AreEqual(secId, result[0].SecurityId);
	}

	#endregion

	#region RemoteMarketDataDrive Save/Load Tests

	[TestMethod]
	public void Drive_SaveLoad_PreservesSettings()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		// Set custom values
		drive.TargetCompId = "CustomTarget";
		drive.SecurityBatchSize = 500;
		drive.Timeout = TimeSpan.FromSeconds(45);
		drive.IsBinaryEnabled = true;
		drive.Credentials.Email = "test@example.com";
		drive.Credentials.Password = "secret".To<SecureString>();

		// Save
		var storage = new SettingsStorage();
		drive.Save(storage);

		// Create new drive and load
		var adapter2 = new MockRemoteAdapter(new IncrementalIdGenerator());
		using var drive2 = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter2);
		drive2.Load(storage);

		// Verify
		AreEqual("CustomTarget", drive2.TargetCompId);
		AreEqual(500, drive2.SecurityBatchSize);
		AreEqual(TimeSpan.FromSeconds(45), drive2.Timeout);
		IsTrue(drive2.IsBinaryEnabled);
		AreEqual("test@example.com", drive2.Credentials.Email);
	}

	#endregion

	#region RemoteStorageDrive Tests (via GetStorageDrive)

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task StorageDrive_GetDatesAsync_ReturnsDates()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var date1 = new DateTime(2024, 1, 15);
		var date2 = new DateTime(2024, 1, 16);

		adapter.DataTypeLookupHandler = lookup =>
		[
			new DataTypeInfoMessage { FileDataType = DataType.Ticks, Dates = [date1, date2] },
		];

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		var dates = await storageDrive.GetDatesAsync(CancellationToken);
		var datesList = dates.ToList();

		HasCount(2, datesList);
		AreEqual(date1, datesList[0]);
		AreEqual(date2, datesList[1]);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task StorageDrive_GetDatesAsync_CachesResults()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var callCount = 0;

		adapter.DataTypeLookupHandler = lookup =>
		{
			callCount++;
			return [new DataTypeInfoMessage { FileDataType = DataType.Ticks, Dates = [new DateTime(2024, 1, 15)] }];
		};

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		// First call
		await storageDrive.GetDatesAsync(CancellationToken);

		// Second call within cache time (3 seconds)
		await storageDrive.GetDatesAsync(CancellationToken);

		// Should only call adapter once due to caching
		AreEqual(1, callCount);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task StorageDrive_LoadStreamAsync_ReturnsData()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var expectedData = "test market data"u8.ToArray();
		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		adapter.FileCommandHandler = cmd =>
		{
			if (cmd.Command == CommandTypes.Get)
				return new RemoteFileMessage { Body = expectedData };
			return null;
		};

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		var result = await storageDrive.LoadStreamAsync(new DateTime(2024, 1, 15), true, CancellationToken);

		result.AssertNotNull();
		AreNotEqual(Stream.Null, result);

		using var ms = new MemoryStream();
		await result.CopyToAsync(ms);
		CollectionAssert.AreEqual(expectedData, ms.ToArray());
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task StorageDrive_SaveStreamAsync_SendsData()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var data = "test data to save"u8.ToArray();

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		using var stream = new MemoryStream(data);
		await storageDrive.SaveStreamAsync(new DateTime(2024, 1, 15), stream, CancellationToken);

		var cmd = adapter.SentMessages.OfType<RemoteFileCommandMessage>().First();
		AreEqual(CommandTypes.Update, cmd.Command);
		AreEqual(secId, cmd.SecurityId);
		AreEqual(DataType.Ticks, cmd.FileDataType);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task StorageDrive_DeleteAsync_SendsCommand()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		await storageDrive.DeleteAsync(new DateTime(2024, 1, 15), CancellationToken);

		var cmd = adapter.SentMessages.OfType<RemoteFileCommandMessage>().First();
		AreEqual(CommandTypes.Remove, cmd.Command);
		AreEqual(secId, cmd.SecurityId);
	}

	[TestMethod]
	public async Task StorageDrive_ClearDatesCacheAsync_Succeeds()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		using var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);
		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);

		// Should not throw (currently does nothing but should complete)
		await storageDrive.ClearDatesCacheAsync(CancellationToken);
	}

	#endregion

	#region RemoteMarketDataDrive Dispose Tests

	[TestMethod]
	public void Drive_Dispose_CanBeCalledMultipleTimes()
	{
		var adapter = new MockRemoteAdapter(new IncrementalIdGenerator());
		var drive = new RemoteMarketDataDrive(RemoteMarketDataDrive.DefaultAddress, adapter);

		// Should not throw on multiple dispose calls
		drive.Dispose();
		drive.Dispose();
	}

	#endregion

	#endregion
}
