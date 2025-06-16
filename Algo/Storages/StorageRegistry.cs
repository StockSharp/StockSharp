namespace StockSharp.Algo.Storages;

using StockSharp.Algo.Storages.Binary;
using StockSharp.Algo.Storages.Csv;

/// <summary>
/// The storage of market data.
/// </summary>
public class StorageRegistry : Disposable, IStorageRegistry
{
	private static readonly UTF8Encoding _utf8 = new(false);

	private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>> _depthStorages = [];
	private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>> _level1Storages = [];
	private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<PositionChangeMessage>> _positionStorages = [];
	private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>> _candleStorages = [];
	private readonly SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>> _executionStorages = [];
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
		RegisterStorage(_executionStorages, ExecutionTypes.Tick, storage);
	}

	/// <inheritdoc />
	public void RegisterMarketDepthStorage(IMarketDataStorage<QuoteChangeMessage> storage)
	{
		RegisterStorage(_depthStorages, storage);
	}

	/// <inheritdoc />
	public void RegisterOrderLogStorage(IMarketDataStorage<ExecutionMessage> storage)
	{
		RegisterStorage(_executionStorages, ExecutionTypes.OrderLog, storage);
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

		_candleStorages.Add(Tuple.Create(storage.SecurityId, storage.Drive), storage);
	}

	private static void RegisterStorage<T>(SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<T>> storages, IMarketDataStorage<T> storage)
		where T : Message
	{
		if (storages == null)
			throw new ArgumentNullException(nameof(storages));

		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		storages.Add(Tuple.Create(storage.SecurityId, storage.Drive), storage);
	}

	private static void RegisterStorage<T>(SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<T>> storages, ExecutionTypes type, IMarketDataStorage<T> storage)
		where T : Message
	{
		if (storages == null)
			throw new ArgumentNullException(nameof(storages));

		if (storage == null)
			throw new ArgumentNullException(nameof(storage));

		storages.Add(Tuple.Create(storage.SecurityId, type, storage.Drive), storage);
	}

	/// <inheritdoc />
	public IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetStorage(security?.ToSecurityId() ?? default, dataType, arg, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetExecutionMessageStorage(securityId, ExecutionTypes.Tick, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary, bool passThroughOrderBookIncrement = false)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _depthStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.MarketDepth, format)), key =>
		{
			IMarketDataSerializer<QuoteChangeMessage> serializer = format switch
			{
				StorageFormats.Binary => new QuoteBinarySerializer(key.Item1, ExchangeInfoProvider) { PassThroughOrderBookIncrement = passThroughOrderBookIncrement },
				StorageFormats.Csv => new MarketDepthCsvSerializer(key.Item1, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new MarketDepthStorage(securityId, key.Item2, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetExecutionMessageStorage(securityId, ExecutionTypes.OrderLog, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		return GetExecutionMessageStorage(securityId, ExecutionTypes.Transaction, drive, format);
	}

	/// <inheritdoc />
	public IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _level1Storages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.Level1, format)), key =>
		{
			//if (security.Board == ExchangeBoard.Associated)
			//	return new AllSecurityMarketDataStorage<Level1ChangeMessage>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), (s, d) => GetLevel1MessageStorage(s, d, format), key.Item2, ExchangeInfoProvider);

			IMarketDataSerializer<Level1ChangeMessage> serializer = format switch
			{
				StorageFormats.Binary => new Level1BinarySerializer(key.Item1, ExchangeInfoProvider),
				StorageFormats.Csv => new Level1CsvSerializer(key.Item1, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new Level1Storage(securityId, key.Item2, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _positionStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.PositionChanges, format)), key =>
		{
			//if (security.Board == ExchangeBoard.Associated)
			//	return new AllSecurityMarketDataStorage<Level1ChangeMessage>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), (s, d) => GetLevel1MessageStorage(s, d, format), key.Item2, ExchangeInfoProvider);

			IMarketDataSerializer<PositionChangeMessage> serializer = format switch
			{
				StorageFormats.Binary => new PositionBinarySerializer(key.Item1, ExchangeInfoProvider),
				StorageFormats.Csv => new PositionCsvSerializer(key.Item1, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return new PositionChangeStorage(securityId, key.Item2, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<CandleMessage> GetCandleMessageStorage(Type candleMessageType, SecurityId securityId, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (candleMessageType == null)
			throw new ArgumentNullException(nameof(candleMessageType));

		if (!candleMessageType.IsCandleMessage())
			throw new ArgumentOutOfRangeException(nameof(candleMessageType), candleMessageType, LocalizedStrings.WrongCandleType);

		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		if (arg.IsNull(true))
			throw new ArgumentNullException(nameof(arg), LocalizedStrings.EmptyCandleArg);

		return _candleStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.Create(candleMessageType, arg), format)), key =>
		{
			var dataType = DataType.Create(candleMessageType, arg).Immutable();

			var serializer = format switch
			{
				StorageFormats.Binary => typeof(CandleBinarySerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(key.Item1, dataType, ExchangeInfoProvider),
				StorageFormats.Csv => typeof(CandleCsvSerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(key.Item1, dataType, _utf8),
				_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
			};

			return typeof(CandleStorage<>).Make(candleMessageType).CreateInstance<IMarketDataStorage<CandleMessage>>(key.Item1, arg, key.Item2, serializer);
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(SecurityId securityId, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _executionStorages.SafeAdd(Tuple.Create(securityId, type, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.Create<ExecutionMessage>(type), format)), key =>
		{
			var secId = key.Item1;
			var mdDrive = key.Item3;

			switch (type)
			{
				case ExecutionTypes.Tick:
				{
					IMarketDataSerializer<ExecutionMessage> serializer = format switch
					{
						StorageFormats.Binary => new TickBinarySerializer(key.Item1, ExchangeInfoProvider),
						StorageFormats.Csv => new TickCsvSerializer(key.Item1, _utf8),
						_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
					};

					return new TradeStorage(securityId, mdDrive, serializer);
				}
				case ExecutionTypes.Transaction:
				{
					IMarketDataSerializer<ExecutionMessage> serializer = format switch
					{
						StorageFormats.Binary => new TransactionBinarySerializer(secId, ExchangeInfoProvider),
						StorageFormats.Csv => new TransactionCsvSerializer(secId, _utf8),
						_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
					};

					return new TransactionStorage(securityId, mdDrive, serializer);
				}
				case ExecutionTypes.OrderLog:
				{
					IMarketDataSerializer<ExecutionMessage> serializer = format switch
					{
						StorageFormats.Binary => new OrderLogBinarySerializer(secId, ExchangeInfoProvider),
						StorageFormats.Csv => new OrderLogCsvSerializer(secId, _utf8),
						_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
					};

					return new OrderLogStorage(securityId, mdDrive, serializer);
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}
		});
	}

	/// <inheritdoc />
	public IMarketDataStorage GetStorage(SecurityId securityId, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
	{
		if (dataType == null)
			throw new ArgumentNullException(nameof(dataType));

		if (!dataType.IsSubclassOf(typeof(Message)))
		{
#pragma warning disable CS0618 // Type or member is obsolete
			dataType = dataType.ToMessageType(ref arg);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		if (dataType == typeof(ExecutionMessage))
		{
			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			return GetExecutionMessageStorage(securityId, (ExecutionTypes)arg, drive, format);
		}
		else if (dataType == typeof(Level1ChangeMessage))
			return GetLevel1MessageStorage(securityId, drive, format);
		else if (dataType == typeof(PositionChangeMessage))
			return GetPositionMessageStorage(securityId, drive, format);
		else if (dataType == typeof(QuoteChangeMessage))
			return GetQuoteMessageStorage(securityId, drive, format);
		else if (dataType == typeof(NewsMessage))
			return GetNewsMessageStorage(drive, format);
		else if (dataType == typeof(BoardStateMessage))
			return GetBoardStateMessageStorage(drive, format);
		else if (dataType.IsCandleMessage())
			return GetCandleMessageStorage(dataType, securityId, arg, drive, format);
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