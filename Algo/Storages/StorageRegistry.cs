#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: StorageRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages.Binary;
	using StockSharp.Algo.Storages.Csv;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The storage of market data.
	/// </summary>
	public class StorageRegistry : Disposable, IStorageRegistry
	{
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>> _depthStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>> _level1Storages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<PositionChangeMessage>> _positionStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<PositionChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>> _candleStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>> _executionStorages = new SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>>();
		private readonly SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<NewsMessage>> _newsStorages = new SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<NewsMessage>>();
		private readonly SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<BoardStateMessage>> _boardStateStorages = new SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<BoardStateMessage>>();
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
		public IEntityMarketDataStorage<Trade, ExecutionMessage> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetTickMessageStorage(security.ToSecurityId(), drive, format).ToEntityStorage<ExecutionMessage, Trade>(security, ExchangeInfoProvider);
		}

		/// <inheritdoc />
		public IEntityMarketDataStorage<MarketDepth, QuoteChangeMessage> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetQuoteMessageStorage(security.ToSecurityId(), drive, format).ToEntityStorage<QuoteChangeMessage, MarketDepth>(security, ExchangeInfoProvider);
		}

		/// <inheritdoc />
		public IEntityMarketDataStorage<OrderLogItem, ExecutionMessage> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetOrderLogMessageStorage(security.ToSecurityId(), drive, format).ToEntityStorage<ExecutionMessage, OrderLogItem>(security, ExchangeInfoProvider);
		}

		/// <inheritdoc />
		public IEntityMarketDataStorage<Candle, CandleMessage> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetCandleMessageStorage(candleType.ToCandleMessageType(), security.ToSecurityId(), arg, drive, format).ToEntityStorage<CandleMessage, Candle>(security, ExchangeInfoProvider);
		}

		/// <inheritdoc />
		public IEntityMarketDataStorage<News, NewsMessage> GetNewsStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetNewsMessageStorage(drive, format).ToEntityStorage<NewsMessage, News>(null, ExchangeInfoProvider);
		}

		/// <inheritdoc />
		public IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetStorage(security.ToSecurityId(), dataType, arg, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(securityId, ExecutionTypes.Tick, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(SecurityId securityId, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			return _depthStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.MarketDepth, format)), key =>
			{
				IMarketDataSerializer<QuoteChangeMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new QuoteBinarySerializer(key.Item1, ExchangeInfoProvider);
						break;
					case StorageFormats.Csv:
						serializer = new MarketDepthCsvSerializer(key.Item1);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
				}

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

				IMarketDataSerializer<Level1ChangeMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new Level1BinarySerializer(key.Item1, ExchangeInfoProvider);
						break;
					case StorageFormats.Csv:
						serializer = new Level1CsvSerializer(key.Item1);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
				}

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

				IMarketDataSerializer<PositionChangeMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new PositionBinarySerializer(key.Item1, ExchangeInfoProvider);
						break;
					case StorageFormats.Csv:
						serializer = new PositionCsvSerializer(key.Item1);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
				}

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
				IMarketDataSerializer serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = typeof(CandleBinarySerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(key.Item1, arg, ExchangeInfoProvider);
						break;
					case StorageFormats.Csv:
						serializer = typeof(CandleCsvSerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(key.Item1, arg, null);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
				}

				return typeof(CandleStorage<>).Make(candleMessageType).CreateInstance<IMarketDataStorage<CandleMessage>>(key.Item1, arg, key.Item2, serializer);
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(SecurityId securityId, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			return _executionStorages.SafeAdd(Tuple.Create(securityId, type, (drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.Create(typeof(ExecutionMessage), type), format)), key =>
			{
				var secId = key.Item1;
				var mdDrive = key.Item3;

				switch (type)
				{
					case ExecutionTypes.Tick:
					{
						IMarketDataSerializer<ExecutionMessage> serializer;

						switch (format)
						{
							case StorageFormats.Binary:
								serializer = new TickBinarySerializer(key.Item1, ExchangeInfoProvider);
								break;
							case StorageFormats.Csv:
								serializer = new TickCsvSerializer(key.Item1);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
						}

						return new TradeStorage(securityId, mdDrive, serializer);
					}
					case ExecutionTypes.Transaction:
					{
						IMarketDataSerializer<ExecutionMessage> serializer;

						switch (format)
						{
							case StorageFormats.Binary:
								serializer = new TransactionBinarySerializer(secId, ExchangeInfoProvider);
								break;
							case StorageFormats.Csv:
								serializer = new TransactionCsvSerializer(secId);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
						}

						return new TransactionStorage(securityId, mdDrive, serializer);
					}
					case ExecutionTypes.OrderLog:
					{
						IMarketDataSerializer<ExecutionMessage> serializer;

						switch (format)
						{
							case StorageFormats.Binary:
								serializer = new OrderLogBinarySerializer(secId, ExchangeInfoProvider);
								break;
							case StorageFormats.Csv:
								serializer = new OrderLogCsvSerializer(secId);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
						}

						return new OrderLogStorage(securityId, mdDrive, serializer);
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
				}
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage GetStorage(SecurityId securityId, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (!dataType.IsSubclassOf(typeof(Message)))
				dataType = dataType.ToMessageType(ref arg);

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
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1018);
		}

		/// <inheritdoc />
		public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			var securityId = SecurityId.News;

			return _newsStorages.SafeAdd((drive ?? DefaultDrive).GetStorageDrive(securityId, DataType.News, format), key =>
			{
				IMarketDataSerializer<NewsMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new NewsBinarySerializer(ExchangeInfoProvider);
						break;
					case StorageFormats.Csv:
						serializer = new NewsCsvSerializer();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
				}

				return new NewsStorage(securityId, serializer, key);
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage<BoardStateMessage> GetBoardStateMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return _boardStateStorages.SafeAdd((drive ?? DefaultDrive).GetStorageDrive(default, DataType.BoardState, format), key =>
			{
				IMarketDataSerializer<BoardStateMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new BoardStateBinarySerializer(ExchangeInfoProvider);
						break;
					case StorageFormats.Csv:
						serializer = new BoardStateCsvSerializer();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
				}

				return new BoardStateStorage(default, serializer, key);
			});
		}
	}
}