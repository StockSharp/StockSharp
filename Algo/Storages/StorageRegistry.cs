namespace StockSharp.Algo.Storages;

using StockSharp.Algo.Storages.Binary;
using StockSharp.Algo.Storages.Csv;

/// <summary>
/// The storage of market data.
/// </summary>
public class StorageRegistry : Disposable, IStorageRegistry
{
	private static readonly UTF8Encoding _utf8 = new(false);

	private readonly SynchronizedDictionary<(SecurityId secId, IMarketDataStorageDrive drive), IMarketDataStorage<QuoteChangeMessage>> _depthStorages = [];
	private readonly SynchronizedDictionary<(SecurityId secId, IMarketDataStorageDrive drive), IMarketDataStorage<Level1ChangeMessage>> _level1Storages = [];
	private readonly SynchronizedDictionary<(SecurityId secId, IMarketDataStorageDrive drive), IMarketDataStorage<PositionChangeMessage>> _positionStorages = [];
	private readonly SynchronizedDictionary<(SecurityId secId, IMarketDataStorageDrive drive), IMarketDataStorage<CandleMessage>> _candleStorages = [];
	private readonly SynchronizedDictionary<(SecurityId secId, IMarketDataStorageDrive drive, DataType dt), IMarketDataStorage<ExecutionMessage>> _executionStorages = [];
	private readonly SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<NewsMessage>> _newsStorages = [];
	private readonly SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<BoardStateMessage>> _boardStateStorages = [];
	//private readonly SynchronizedDictionary<IMarketDataDrive, ISecurityStorage> _securityStorages = new SynchronizedDictionary<IMarketDataDrive, ISecurityStorage>();

	/// <summary>
	/// Initializes a new instance of the <see cref="StorageRegistry"/>.
	/// </summary>
	public StorageRegistry()
		: this(new InMemoryExchangeInfoProvider())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StorageRegistry"/>.
	/// </summary>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public StorageRegistry(IExchangeInfoProvider exchangeInfoProvider)
	{
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		DefaultDrive.Dispose();
		base.DisposeManaged();
	}

	private IMarketDataDrive _defaultDrive = new LocalMarketDataDrive();

	/// <inheritdoc />
	public virtual IMarketDataDrive DefaultDrive
	{
		get => _defaultDrive;
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value == _defaultDrive)
				return;

			_defaultDrive.Dispose();
			_defaultDrive = value;
		}
	}

	/// <inheritdoc />
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

	/// <inheritdoc />
	public void RegisterTradeStorage(IMarketDataStorage<ExecutionMessage> storage)
	{
		RegisterStorage(_executionStorages, DataType.Ticks, storage);
	}

	/// <inheritdoc />
	public void RegisterMarketDepthStorage(IMarketDataStorage<QuoteChangeMessage> storage)
	{
		RegisterStorage(_depthStorages, storage);
	}

	/// <inheritdoc />
	public void RegisterOrderLogStorage(IMarketDataStorage<ExecutionMessage> storage)
	{
		RegisterStorage(_executionStorages, DataType.OrderLog, storage);
	}

	/// <inheritdoc />
	public void RegisterLevel1Storage(IMarketDataStorage<Level1ChangeMessage> storage)
	{
		RegisterStorage(_level1Storages, storage);
	}

	/// <inheritdoc />
	public void RegisterPositionStorage(IMarketDataStorage<PositionChangeMessage> storage)
	{
		RegisterStorage(_positionStorages, storage);
	}

	/// <inheritdoc />
	public void RegisterCandleStorage(IMarketDataStorage<CandleMessage> storage)
	{
		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		_candleStorages.Add((storage.SecurityId, storage.Drive), storage);
	}

	private static void RegisterStorage<T>(SynchronizedDictionary<(SecurityId, IMarketDataStorageDrive), IMarketDataStorage<T>> storages, IMarketDataStorage<T> storage)
		where T : Message
	{
		if (storages == null)
			throw new ArgumentNullException(nameof(storages));

		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		storages.Add((storage.SecurityId, storage.Drive), storage);
	}

	private static void RegisterStorage<T>(SynchronizedDictionary<(SecurityId, IMarketDataStorageDrive, DataType), IMarketDataStorage<T>> storages, DataType type, IMarketDataStorage<T> storage)
		where T : Message
	{
		if (storages == null)
			throw new ArgumentNullException(nameof(storages));

		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		if (type == null)
			throw new ArgumentNullException(nameof(type));

		storages.Add((storage.SecurityId, storage.Drive, type.Immutable()), storage);
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetExecutionMessageStorage(securityId, DataType.Ticks, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary, bool passThroughOrderBookIncrement = false)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _depthStorages.SafeAdd((secId: securityId, drive: (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.MarketDepth, format)), key =>
		{
			IMarketDataSerializer<QuoteChangeMessage> serializer = format switch
			{
				StorageFormats.Binary => new QuoteBinarySerializer(key.secId, ExchangeInfoProvider) { PassThroughOrderBookIncrement = passThroughOrderBookIncrement },
				StorageFormats.Csv => new MarketDepthCsvSerializer(key.secId, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new MarketDepthStorage(securityId, key.drive, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetExecutionMessageStorage(securityId, DataType.OrderLog, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetExecutionMessageStorage(securityId, DataType.Transactions, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _level1Storages.SafeAdd((secId: securityId, drive: (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.Level1, format)), key =>
		{
			//if (security.Board == ExchangeBoard.Associated)
			//	return new AllSecurityMarketDataStorage<Level1ChangeMessage>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), (s, d) => GetLevel1MessageStorage(s, d, format), key.Item2, ExchangeInfoProvider);

			IMarketDataSerializer<Level1ChangeMessage> serializer = format switch
			{
				StorageFormats.Binary => new Level1BinarySerializer(key.secId, ExchangeInfoProvider),
				StorageFormats.Csv => new Level1CsvSerializer(key.secId, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new Level1Storage(securityId, key.drive, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _positionStorages.SafeAdd((secId: securityId, drive: (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.PositionChanges, format)), key =>
		{
			//if (security.Board == ExchangeBoard.Associated)
			//	return new AllSecurityMarketDataStorage<Level1ChangeMessage>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), (s, d) => GetLevel1MessageStorage(s, d, format), key.Item2, ExchangeInfoProvider);

			IMarketDataSerializer<PositionChangeMessage> serializer = format switch
			{
				StorageFormats.Binary => new PositionBinarySerializer(key.secId, ExchangeInfoProvider),
				StorageFormats.Csv => new PositionCsvSerializer(key.secId, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new PositionChangeStorage(securityId, key.drive, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<CandleMessage> GetCandleMessageStorage(SecurityId securityId, DataType type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		if (!type.IsCandles)
			throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.WrongCandleType);

		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		if (type.Arg.IsNull(true))
			throw new ArgumentNullException(nameof(type), LocalizedStrings.EmptyCandleArg);

		type = type.Immutable();
		var candleMessageType = type.MessageType;

		return _candleStorages.SafeAdd((secId: securityId, drive: (drive ?? DefaultDrive).GetStorageDrive(securityId, type, format)), key =>
		{
			var serializer = format switch
			{
				StorageFormats.Binary => typeof(CandleBinarySerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(key.secId, type, ExchangeInfoProvider),
				StorageFormats.Csv => typeof(CandleCsvSerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(key.secId, type, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return typeof(CandleStorage<>).Make(candleMessageType).CreateInstance<IMarketDataStorage<CandleMessage>>(key.secId, type, key.drive, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(SecurityId securityId, DataType type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		ArgumentNullException.ThrowIfNull(type);

		type = type.Immutable();

		return _executionStorages.SafeAdd((secId: securityId, drive: (drive ?? DefaultDrive).GetStorageDrive(securityId, type, format), type), key =>
		{
			if (type == DataType.Ticks)
			{
				IMarketDataSerializer<ExecutionMessage> serializer = format switch
				{
					StorageFormats.Binary => new TickBinarySerializer(key.secId, ExchangeInfoProvider),
					StorageFormats.Csv => new TickCsvSerializer(key.secId, _utf8),
					_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
				};

				return new TradeStorage(securityId, key.drive, serializer);
			}
			else if (type == DataType.Transactions)
			{
				IMarketDataSerializer<ExecutionMessage> serializer = format switch
				{
					StorageFormats.Binary => new TransactionBinarySerializer(key.secId, ExchangeInfoProvider),
					StorageFormats.Csv => new TransactionCsvSerializer(key.secId, _utf8),
					_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
				};

				return new TransactionStorage(securityId, key.drive, serializer);
			}
			else if (type == DataType.OrderLog)
			{
				IMarketDataSerializer<ExecutionMessage> serializer = format switch
				{
					StorageFormats.Binary => new OrderLogBinarySerializer(key.secId, ExchangeInfoProvider),
					StorageFormats.Csv => new OrderLogCsvSerializer(key.secId, _utf8),
					_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
				};

				return new OrderLogStorage(securityId, key.drive, serializer);
			}
			else
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage GetStorage(SecurityId securityId, DataType dataType, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (dataType == null)
			throw new ArgumentNullException(nameof(dataType));

		var messageType = dataType.MessageType;

		if (!messageType.IsSubclassOf(typeof(Message)))
			throw new ArgumentException(LocalizedStrings.TypeNotImplemented.Put(messageType.Name, typeof(Message).Name), nameof(dataType));

		if (messageType == typeof(ExecutionMessage))
			return GetExecutionMessageStorage(securityId, dataType, drive, format);
		else if (dataType == DataType.Level1)
			return GetLevel1MessageStorage(securityId, drive, format);
		else if (dataType == DataType.PositionChanges)
			return GetPositionMessageStorage(securityId, drive, format);
		else if (dataType == DataType.MarketDepth)
			return GetQuoteMessageStorage(securityId, drive, format);
		else if (dataType == DataType.News)
			return GetNewsMessageStorage(drive, format);
		else if (dataType == DataType.BoardState)
			return GetBoardStateMessageStorage(drive, format);
		else if (dataType.IsCandles)
			return GetCandleMessageStorage(securityId, dataType, drive, format);
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
	}

	/// <inheritdoc />
	public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		var securityId = SecurityId.News;

		return _newsStorages.SafeAdd((drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.News, format), key =>
		{
			IMarketDataSerializer<NewsMessage> serializer = format switch
			{
				StorageFormats.Binary => new NewsBinarySerializer(ExchangeInfoProvider),
				StorageFormats.Csv => new NewsCsvSerializer(_utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new NewsStorage(securityId, serializer, key);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<BoardStateMessage> GetBoardStateMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return _boardStateStorages.SafeAdd((drive ?? DefaultDrive).GetStorageDrive(default, DataType.BoardState, format), key =>
		{
			IMarketDataSerializer<BoardStateMessage> serializer = format switch
			{
				StorageFormats.Binary => new BoardStateBinarySerializer(ExchangeInfoProvider),
				StorageFormats.Csv => new BoardStateCsvSerializer(_utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new BoardStateStorage(default, serializer, key);
		});
	}
}