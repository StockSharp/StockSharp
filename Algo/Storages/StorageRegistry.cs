namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый класс, описывающий хранилище маркет-данных.
	/// </summary>
	public class StorageRegistry : Disposable, IStorageRegistry
	{
		private class MarketDepthCsvSerializer : CsvMarketDataSerializer<QuoteChangeMessage>
		{
			private class QuoteEnumerable : SimpleEnumerable<QuoteChangeMessage>
			{
				private class QuoteEnumerator : SimpleEnumerator<QuoteChangeMessage>
				{
					private readonly IEnumerator<TimeQuoteChange> _enumerator;
					private readonly SecurityId _securityId;

					private bool _resetCurrent = true;
					private bool _needMoveNext = true;

					public QuoteEnumerator(IEnumerator<TimeQuoteChange> enumerator, SecurityId securityId)
					{
						_enumerator = enumerator;
						_securityId = securityId;
					}

					public override bool MoveNext()
					{
						if (_resetCurrent)
						{
							Current = null;

							if (_needMoveNext && !_enumerator.MoveNext())
								return false;
						}

						_needMoveNext = true;

						do
						{
							var quote = _enumerator.Current;

							if (Current == null)
							{
								Current = new QuoteChangeMessage
								{
									SecurityId = _securityId,
									ServerTime = quote.ServerTime,
									LocalTime = quote.LocalTime,
									Bids = new List<QuoteChange>(),
									Asks = new List<QuoteChange>(),
								};
							}
							else if (Current.ServerTime != quote.ServerTime)
							{
								_resetCurrent = true;
								_needMoveNext = false;

								return true;
							}

							var quotes = (List<QuoteChange>)(quote.Side == Sides.Buy ? Current.Bids : Current.Asks);
							quotes.Add(quote);
						}
						while (_enumerator.MoveNext());

						if (Current == null)
							return false;

						_resetCurrent = true;
						_needMoveNext = true;
						return true;
					}

					public override void Reset()
					{
						_enumerator.Reset();

						_resetCurrent = true;
						_needMoveNext = true;

						base.Reset();
					}

					protected override void DisposeManaged()
					{
						_enumerator.Dispose();
						base.DisposeManaged();
					}
				}

				public QuoteEnumerable(IEnumerable<TimeQuoteChange> quotes, SecurityId securityId)
					: base(() => new QuoteEnumerator(quotes.GetEnumerator(), securityId))
				{
					if (quotes == null)
						throw new ArgumentNullException("quotes");
				}
			}

			private readonly CsvMarketDataSerializer<TimeQuoteChange> _quoteSerializer;

			public MarketDepthCsvSerializer(SecurityId securityId)
				: base(securityId, null)
			{
				_quoteSerializer = new CsvMarketDataSerializer<TimeQuoteChange>(securityId, null);
			}

			public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
			{
				return _quoteSerializer.CreateMetaInfo(date);
			}

			public override byte[] Serialize(IEnumerable<QuoteChangeMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var list = data.SelectMany(d =>
				{
					var items = new List<TimeQuoteChange>();

					items.AddRange(d.Bids.OrderBy(q => q.Price).Select(q => new TimeQuoteChange(q, d)));
					items.AddRange(d.Asks.OrderBy(q => q.Price).Select(q => new TimeQuoteChange(q, d)));

					return items;
				});

				return _quoteSerializer.Serialize(list, metaInfo);
			}

			public override IEnumerableEx<QuoteChangeMessage> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
			{
				return new QuoteEnumerable(_quoteSerializer.Deserialize(stream, metaInfo), SecurityId).ToEx(metaInfo.Count);
			}
		}

		private abstract class ConvertableStorage<TMessage, TEntity, TId> : MarketDataStorage<TMessage, TId>, IMarketDataStorage<TEntity>, IMarketDataStorageInfo<TEntity>
			where TMessage : Message
		{
			protected ConvertableStorage(Security security, object arg, Func<TMessage, DateTimeOffset> getTime, Func<TMessage, SecurityId> getSecurity, Func<TMessage, TId> getId, IMarketDataSerializer<TMessage> serializer, IMarketDataStorageDrive drive)
				: base(security, arg, getTime, getSecurity, getId, serializer, drive)
			{
			}

			IMarketDataSerializer<TEntity> IMarketDataStorage<TEntity>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			void IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
			{
				Save(data.Select(ToMessage));
			}

			void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
			{
				Delete(data.Select(ToMessage));
			}

			IEnumerableEx<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
			{
				return Load(date).ToEntities<TMessage, TEntity>(Security);
			}

			IDataStorageReader<TEntity> IMarketDataStorage<TEntity>.GetReader(DateTime date)
			{
				return new ConvertableDataStorageReader<TEntity, TMessage>(GetReader(date),
					messages => messages.ToEntities<TMessage, TEntity>(Security));
			}

			public abstract DateTimeOffset GetTime(TEntity data);

			protected abstract TMessage ToMessage(TEntity entity);
		}

		private sealed class TradeStorage : ConvertableStorage<ExecutionMessage, Trade, long>
		{
			public TradeStorage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(security, ExecutionTypes.Tick, trade => trade.ServerTime, trade => trade.SecurityId, trade => trade.TradeId, serializer, drive)
			{
			}

			protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var tradeInfo = metaInfo as TradeMetaInfo;

				if (tradeInfo == null)
					return base.FilterNewData(data, metaInfo);

				var prevId = tradeInfo.PrevId;
				var prevTime = tradeInfo.LastTime;

				return data.Where(t =>
				{
					if (t.ServerTime > prevTime)
						return true;
					else if (t.ServerTime == prevTime)
						return t.TradeId != prevId; // если разные сделки имеют одинаковое время
					else
						return false;
				});
			}

			public override DateTimeOffset GetTime(Trade data)
			{
				return data.Time;
			}

			protected override ExecutionMessage ToMessage(Trade entity)
			{
				return entity.ToMessage();
			}
		}

		private sealed class MarketDepthStorage : ConvertableStorage<QuoteChangeMessage, MarketDepth, DateTimeOffset>
		{
			public MarketDepthStorage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<QuoteChangeMessage> serializer)
				: base(security, null, depth => depth.ServerTime, depth => depth.SecurityId, depth => depth.ServerTime.Truncate(), serializer, drive)
			{
			}

			protected override IEnumerable<QuoteChangeMessage> FilterNewData(IEnumerable<QuoteChangeMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var mdInfo = metaInfo as QuoteMetaInfo;

				if (mdInfo == null || mdInfo.Version < MarketDataVersions.Version45)
					return base.FilterNewData(data, metaInfo);

				return data.Where(d => d.ServerTime.Truncate() >= mdInfo.LastTime);
			}

			public override DateTimeOffset GetTime(MarketDepth data)
			{
				return data.LastChangeTime;
			}

			protected override QuoteChangeMessage ToMessage(MarketDepth entity)
			{
				return entity.ToMessage();
			}
		}

		private sealed class OrderLogStorage : ConvertableStorage<ExecutionMessage, OrderLogItem, long>
		{
			public OrderLogStorage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(security, ExecutionTypes.OrderLog, item => item.ServerTime, item => item.SecurityId, item => item.TransactionId, serializer, drive)
			{
			}

			protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var olInfo = metaInfo as OrderLogMetaInfo;

				if (olInfo == null || olInfo.Version < MarketDataVersions.Version34)
					return base.FilterNewData(data, metaInfo);

				var prevTransId = olInfo.LastTransactionId;
				return data.Where(i => i.TransactionId > prevTransId);
			}

			public override DateTimeOffset GetTime(OrderLogItem data)
			{
				return data.Order.Time;
			}

			protected override ExecutionMessage ToMessage(OrderLogItem entity)
			{
				return entity.ToMessage();
			}
		}

		// http://stackoverflow.com/a/15316996
		private abstract class CandleMessageStorage<TCandleMessage> :
				MarketDataStorage<TCandleMessage, DateTimeOffset>,
				IMarketDataStorage<CandleMessage>,
				IMarketDataStorageInfo<CandleMessage>
			where TCandleMessage : CandleMessage, new()
		{
			protected CandleMessageStorage(Security security, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, arg, candle => candle.OpenTime, candle => candle.SecurityId, candle => candle.OpenTime.Truncate(), serializer, drive)
			{
			}

			IEnumerableEx<CandleMessage> IMarketDataStorage<CandleMessage>.Load(DateTime date)
			{
				return Load(date);
			}

			IDataStorageReader<CandleMessage> IMarketDataStorage<CandleMessage>.GetReader(DateTime date)
			{
				return new ConvertableDataStorageReader<CandleMessage, TCandleMessage>(GetReader(date),
					messages => messages.Cast<CandleMessage>().ToEx(messages.Count));
			}

			IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			void IMarketDataStorage<CandleMessage>.Save(IEnumerable<CandleMessage> data)
			{
				Save(data.Cast<TCandleMessage>());
			}

			void IMarketDataStorage<CandleMessage>.Delete(IEnumerable<CandleMessage> data)
			{
				Delete(data.Cast<TCandleMessage>());
			}

			DateTimeOffset IMarketDataStorageInfo<CandleMessage>.GetTime(CandleMessage data)
			{
				return data.OpenTime;
			}
		}

		// http://stackoverflow.com/a/15316996
		private abstract class TypedCandleStorage<TCandleMessage, TCandle> :
				CandleMessageStorage<TCandleMessage>,
				IMarketDataStorage<TCandle>
			where TCandleMessage : CandleMessage, new()
			where TCandle : Candle
		{
			protected TypedCandleStorage(Security security, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, arg, drive, serializer)
			{
			}

			void IMarketDataStorage<TCandle>.Save(IEnumerable<TCandle> data)
			{
				Save(data.Select(Convert));
			}

			void IMarketDataStorage<TCandle>.Delete(IEnumerable<TCandle> data)
			{
				Delete(data.Select(Convert));
			}

			IEnumerableEx<TCandle> IMarketDataStorage<TCandle>.Load(DateTime date)
			{
				var messages = Load(date);

				return messages
					.ToCandles<TCandle>(Security)
					.ToEx(messages.Count);
			}

			IDataStorageReader<TCandle> IMarketDataStorage<TCandle>.GetReader(DateTime date)
			{
				throw new NotSupportedException();
			}

			IMarketDataSerializer<TCandle> IMarketDataStorage<TCandle>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			protected TCandleMessage Convert(TCandle candle)
			{
				var arg = candle.Arg;
				var expectedArg = ((IMarketDataStorage)this).Arg;

				if (!arg.Equals(expectedArg))
					throw new ArgumentException(LocalizedStrings.Str1016Params.Put(candle, candle.Arg, expectedArg), "candle");

				return (TCandleMessage)candle.ToMessage();
			}
		}

		private sealed class CandleStorage<TCandleMessage, TCandle> :
				TypedCandleStorage<TCandleMessage, TCandle>,
				IMarketDataStorage<Candle>,
				IMarketDataStorageInfo<Candle>
			where TCandleMessage : CandleMessage, new()
			where TCandle : Candle
		{
			public CandleStorage(Security security, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, arg, drive, serializer)
			{
			}

			IDataStorageReader<Candle> IMarketDataStorage<Candle>.GetReader(DateTime date)
			{
				return new ConvertableDataStorageReader<Candle, TCandleMessage>(GetReader(date),
					messages => messages.ToCandles<Candle>(Security, typeof(TCandleMessage).ToCandleType()).ToEx(messages.Count));
			}

			IMarketDataSerializer<Candle> IMarketDataStorage<Candle>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			void IMarketDataStorage<Candle>.Save(IEnumerable<Candle> data)
			{
				Save(data.Select(c => Convert((TCandle)c)));
			}

			void IMarketDataStorage<Candle>.Delete(IEnumerable<Candle> data)
			{
				Delete(data.Select(c => Convert((TCandle)c)));
			}

			IEnumerableEx<Candle> IMarketDataStorage<Candle>.Load(DateTime date)
			{
				var messages = Load(date);

				return messages
					.ToCandles<Candle>(Security, typeof(TCandleMessage).ToCandleType())
					.ToEx(messages.Count);
			}

			DateTimeOffset IMarketDataStorageInfo<Candle>.GetTime(Candle data)
			{
				return data.OpenTime;
			}
		}

		private sealed class Level1Storage : MarketDataStorage<Level1ChangeMessage, DateTimeOffset>//, IMarketDataStorage<SecurityChange>, IMarketDataStorageInfo<SecurityChange>
		{
			public Level1Storage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<Level1ChangeMessage> serializer)
				: base(security, null, value => value.ServerTime, value => value.SecurityId, value => value.ServerTime.Truncate(), serializer, drive)
			{
			}
		}

		private sealed class ExecutionStorage : MarketDataStorage<ExecutionMessage, long>, IMarketDataStorage<Order>,
			IMarketDataStorageInfo<Order>, IMarketDataStorage<MyTrade>, IMarketDataStorageInfo<MyTrade>
		{
			public ExecutionStorage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(security, ExecutionTypes.Order, msg => msg.ServerTime, msg => msg.SecurityId, msg => msg.TransactionId, serializer, drive)
			{
				AppendOnlyNew = false;
			}

			#region Order

			IMarketDataSerializer<Order> IMarketDataStorage<Order>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			void IMarketDataStorage<Order>.Save(IEnumerable<Order> data)
			{
				Save(data.Select(t => t.ToMessage()));
			}

			void IMarketDataStorage<Order>.Delete(IEnumerable<Order> data)
			{
				throw new NotSupportedException();
			}

			IEnumerableEx<Order> IMarketDataStorage<Order>.Load(DateTime date)
			{
				throw new NotSupportedException();
			}

			IDataStorageReader<Order> IMarketDataStorage<Order>.GetReader(DateTime date)
			{
				throw new NotSupportedException();
			}

			DateTimeOffset IMarketDataStorageInfo<Order>.GetTime(Order data)
			{
				return data.Time;
			}

			#endregion

			#region Trade

			IMarketDataSerializer<MyTrade> IMarketDataStorage<MyTrade>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			void IMarketDataStorage<MyTrade>.Save(IEnumerable<MyTrade> data)
			{
				Save(data.Select(t => t.ToMessage()));
			}

			void IMarketDataStorage<MyTrade>.Delete(IEnumerable<MyTrade> data)
			{
				throw new NotSupportedException();
			}

			IEnumerableEx<MyTrade> IMarketDataStorage<MyTrade>.Load(DateTime date)
			{
				throw new NotSupportedException();
			}

			IDataStorageReader<MyTrade> IMarketDataStorage<MyTrade>.GetReader(DateTime date)
			{
				throw new NotSupportedException();
			}

			DateTimeOffset IMarketDataStorageInfo<MyTrade>.GetTime(MyTrade data)
			{
				return data.Trade.Time;
			}

			#endregion
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>> _depthStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>> _level1Storages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>> _candleStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>>();

		private readonly SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>> _executionStorages = new SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>>();

		/// <summary>
		/// Создать <see cref="StorageRegistry"/>.
		/// </summary>
		public StorageRegistry()
		{
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			DefaultDrive.Dispose();
			base.DisposeManaged();
		}

		private IMarketDataDrive _defaultDrive = new LocalMarketDataDrive();

		/// <summary>
		/// Хранилище, которое используется по-умолчанию.
		/// </summary>
		public virtual IMarketDataDrive DefaultDrive
		{
			get { return _defaultDrive; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value == _defaultDrive)
					return;

				_defaultDrive.Dispose();
				_defaultDrive = value;
			}
		}

		/// <summary>
		/// Добавить хранилище тиковых сделок.
		/// </summary>
		/// <param name="storage">Хранилище тиковых сделок</param>
		public void RegisterTradeStorage(IMarketDataStorage<Trade> storage)
		{
			RegisterTradeStorage((IMarketDataStorage<ExecutionMessage>)storage);
		}

		/// <summary>
		/// Добавить хранилище стаканов.
		/// </summary>
		/// <param name="storage">Хранилище стаканов.</param>
		public void RegisterMarketDepthStorage(IMarketDataStorage<MarketDepth> storage)
		{
			RegisterMarketDepthStorage((IMarketDataStorage<QuoteChangeMessage>)storage);
		}

		/// <summary>
		/// Зарегистрировать хранилище лога заявок.
		/// </summary>
		/// <param name="storage">Хранилище лога заявок.</param>
		public void RegisterOrderLogStorage(IMarketDataStorage<OrderLogItem> storage)
		{
			RegisterOrderLogStorage((IMarketDataStorage<ExecutionMessage>)storage);
		}

		/// <summary>
		/// Добавить хранилище свечек.
		/// </summary>
		/// <param name="storage">Хранилище свечек.</param>
		public void RegisterCandleStorage(IMarketDataStorage<Candle> storage)
		{
			RegisterCandleStorage((IMarketDataStorage<CandleMessage>)storage);
		}

		/// <summary>
		/// Зарегистрировать хранилище тиковых сделок.
		/// </summary>
		/// <param name="storage">Хранилище тиковых сделок</param>
		public void RegisterTradeStorage(IMarketDataStorage<ExecutionMessage> storage)
		{
			RegisterStorage(_executionStorages, ExecutionTypes.Tick, storage);
		}

		/// <summary>
		/// Зарегистрировать хранилище стаканов.
		/// </summary>
		/// <param name="storage">Хранилище стаканов.</param>
		public void RegisterMarketDepthStorage(IMarketDataStorage<QuoteChangeMessage> storage)
		{
			RegisterStorage(_depthStorages, storage);
		}

		/// <summary>
		/// Зарегистрировать хранилище лога заявок.
		/// </summary>
		/// <param name="storage">Хранилище лога заявок.</param>
		public void RegisterOrderLogStorage(IMarketDataStorage<ExecutionMessage> storage)
		{
			RegisterStorage(_executionStorages, ExecutionTypes.OrderLog, storage);
		}

		/// <summary>
		/// Зарегистрировать хранилище изменений инструмента.
		/// </summary>
		/// <param name="storage">Хранилище изменений инструмента.</param>
		public void RegisterLevel1Storage(IMarketDataStorage<Level1ChangeMessage> storage)
		{
			RegisterStorage(_level1Storages, storage);
		}

		private static void RegisterStorage<T>(SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<T>> storages, IMarketDataStorage<T> storage)
		{
			if (storages == null)
				throw new ArgumentNullException("storages");

			if (storage == null)
				throw new ArgumentNullException("storage");

			storages.Add(Tuple.Create(storage.Security.ToSecurityId(), storage.Drive), storage);
		}

		private static void RegisterStorage<T>(SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<T>> storages, ExecutionTypes type, IMarketDataStorage<T> storage)
		{
			if (storages == null)
				throw new ArgumentNullException("storages");

			if (storage == null)
				throw new ArgumentNullException("storage");

			storages.Add(Tuple.Create(storage.Security.ToSecurityId(), type, storage.Drive), storage);
		}

		/// <summary>
		/// Зарегистрировать хранилище свечек.
		/// </summary>
		/// <param name="storage">Хранилище свечек.</param>
		public void RegisterCandleStorage(IMarketDataStorage<CandleMessage> storage)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			_candleStorages.Add(Tuple.Create(storage.Security.ToSecurityId(), storage.Drive), storage);
		}

		/// <summary>
		/// Получить хранилище тиковых сделок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище тиковых сделок.</returns>
		public IMarketDataStorage<Trade> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<Trade>)GetTickMessageStorage(security, drive, format);
		}

		/// <summary>
		/// Получить хранилище стаканов для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище стаканов.</returns>
		public IMarketDataStorage<MarketDepth> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<MarketDepth>)GetQuoteMessageStorage(security, drive, format);
		}

		/// <summary>
		/// Получить хранилище лога заявок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище лога заявок.</returns>
		public IMarketDataStorage<OrderLogItem> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<OrderLogItem>)GetOrderLogMessageStorage(security, drive, format);
		}

		/// <summary>
		/// Получить хранилище свечек для заданного инструмента.
		/// </summary>
		/// <param name="candleType">Тип свечи.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище свечек.</returns>
		public IMarketDataStorage<Candle> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<Candle>)GetCandleMessageStorage(candleType.ToCandleMessageType(), security, arg, drive, format);
		}

		/// <summary>
		/// Получить хранилище тиковых сделок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище тиковых сделок.</returns>
		public IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionStorage(security, ExecutionTypes.Tick, drive, format);
		}

		/// <summary>
		/// Получить хранилище стаканов для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище стаканов.</returns>
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var securityId = security.ToSecurityId();

			return _depthStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(QuoteChangeMessage), null, format)), key =>
			{
				if (security is ContinuousSecurity)
					return new ConvertableContinuousSecurityMarketDataStorage<QuoteChangeMessage, MarketDepth>((ContinuousSecurity)security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), md => md.ToMessage(), md => md.LastChangeTime, (s, d) => GetQuoteMessageStorage(s, d, format), key.Item2);
				else if (security is IndexSecurity)
					return new IndexSecurityMarketDataStorage<QuoteChangeMessage>((IndexSecurity)security, null, d => ToSecurity(d.SecurityId), (s, d) => GetQuoteMessageStorage(s, d, format), key.Item2);
				else if (security.Board == ExchangeBoard.Associated)
					return new ConvertableAllSecurityMarketDataStorage<QuoteChangeMessage, MarketDepth>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), md => md.LastChangeTime, (s, d) => GetQuoteMessageStorage(s, d, format), key.Item2);
				else
				{
					IMarketDataSerializer<QuoteChangeMessage> serializer;

					switch (format)
					{
						case StorageFormats.Binary:
							serializer = new QuoteSerializer(key.Item1);
							break;
						case StorageFormats.Csv:
							serializer = new MarketDepthCsvSerializer(key.Item1);
							break;
						default:
							throw new ArgumentOutOfRangeException("format");
					}

					return new MarketDepthStorage(security, key.Item2, serializer);
				}
			});
		}

		/// <summary>
		/// Получить хранилище лога заявок для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище лога заявок.</returns>
		public IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionStorage(security, ExecutionTypes.OrderLog, drive, format);
		}

		/// <summary>
		/// Получить хранилище изменений инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище изменений инструмента.</returns>
		public IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var securityId = security.ToSecurityId();

			return _level1Storages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(Level1ChangeMessage), null, format)), key =>
			{
				if (security.Board == ExchangeBoard.Associated)
					return new AllSecurityMarketDataStorage<Level1ChangeMessage>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), (s, d) => GetLevel1MessageStorage(s, d, format), key.Item2);

				IMarketDataSerializer<Level1ChangeMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new Level1Serializer(key.Item1);
						break;
					case StorageFormats.Csv:
						serializer = new CsvMarketDataSerializer<Level1ChangeMessage>(key.Item1, null);
						break;
					default:
						throw new ArgumentOutOfRangeException("format");
				}

				return new Level1Storage(security, key.Item2, serializer);
			});
		}

		/// <summary>
		/// Получить хранилище свечек для заданного инструмента.
		/// </summary>
		/// <param name="candleMessageType">Тип сообщения свечи.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="arg">Параметр свечи.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище свечек.</returns>
		public IMarketDataStorage<CandleMessage> GetCandleMessageStorage(Type candleMessageType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (candleMessageType == null)
				throw new ArgumentNullException("candleMessageType");

			if (!candleMessageType.IsSubclassOf(typeof(CandleMessage)))
				throw new ArgumentOutOfRangeException("candleMessageType", candleMessageType, LocalizedStrings.WrongCandleType);

			if (security == null)
				throw new ArgumentNullException("security");

			if (arg == null)
				throw new ArgumentNullException("arg");

			if (arg.IsNull(true))
				throw new ArgumentNullException("arg", LocalizedStrings.EmptyCandleArg);

			var securityId = security.ToSecurityId();

			return _candleStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, candleMessageType, arg, format)), key =>
			{
				if (security is ContinuousSecurity)
				{
					var type = typeof(CandleContinuousSecurityMarketDataStorage<>).Make(candleMessageType);

					Func<CandleMessage, DateTimeOffset> getTime = c => c.OpenTime;
					Func<CandleMessage, Security> getSecurity = c => ToSecurity(c.SecurityId);
					//Func<Candle, CandleMessage> toMessage = c => c.ToMessage();
					//Func<Candle, DateTime> getEntityTime = c => c.OpenTime;
					Func<Security, IMarketDataDrive, IMarketDataStorage<CandleMessage>> getStorage = (s, d) => GetCandleMessageStorage(candleMessageType, s, arg, d, format);

					return type.CreateInstance<IMarketDataStorage<CandleMessage>>((ContinuousSecurity)security, arg, getTime, getSecurity, getStorage, key.Item2);
				}
				else if (security is IndexSecurity)
					return new IndexSecurityMarketDataStorage<CandleMessage>((IndexSecurity)security, arg, c => ToSecurity(c.SecurityId), (s, d) => GetCandleMessageStorage(candleMessageType, s, arg, d, format), key.Item2);
				else
				{
					IMarketDataSerializer serializer;

					switch (format)
					{
						case StorageFormats.Binary:
							serializer = typeof(CandleSerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(security.ToSecurityId(), arg);
							break;
						case StorageFormats.Csv:
							serializer = typeof(CsvMarketDataSerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(security.ToSecurityId(), null);
							break;
						default:
							throw new ArgumentOutOfRangeException("format");
					}

					return typeof(CandleStorage<,>).Make(candleMessageType, candleMessageType.ToCandleType()).CreateInstance<IMarketDataStorage<CandleMessage>>(security, arg, key.Item2, serializer);
				}
			});
		}

		/// <summary>
		/// Получить хранилище транзакций для заданного инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="type">Тип данных, информация о которых содержится <see cref="ExecutionMessage"/>.</param>
		/// <param name="drive">Хранилище. Если значение равно <see langword="null"/>, то будет использоваться <see cref="IStorageRegistry.DefaultDrive"/>.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище транзакций.</returns>
		public IMarketDataStorage<ExecutionMessage> GetExecutionStorage(Security security, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var securityId = security.ToSecurityId();

			return _executionStorages.SafeAdd(Tuple.Create(securityId, type, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(ExecutionMessage), type, format)), key =>
			{
				var secId = key.Item1;
				var mdDrive = key.Item3;

				switch (type)
				{
					case ExecutionTypes.Tick:
					{
						if (security is ContinuousSecurity)
							return new ConvertableContinuousSecurityMarketDataStorage<ExecutionMessage, Trade>((ContinuousSecurity)security, null, t => t.ServerTime, t => ToSecurity(t.SecurityId), t => t.ToMessage(), t => t.Time, (s, d) => GetExecutionStorage(s, type, d, format), mdDrive);
						else if (security is IndexSecurity)
							return new IndexSecurityMarketDataStorage<ExecutionMessage>((IndexSecurity)security, null, d => ToSecurity(d.SecurityId), (s, d) => GetExecutionStorage(s, type, d, format), mdDrive);
						else if (security.Board == ExchangeBoard.Associated)
							return new ConvertableAllSecurityMarketDataStorage<ExecutionMessage, Trade>(security, null, t => t.ServerTime, t => ToSecurity(t.SecurityId), t => t.Time, (s, d) => GetExecutionStorage(s, type, d, format), mdDrive);
						else
						{
							IMarketDataSerializer<ExecutionMessage> serializer;

							switch (format)
							{
								case StorageFormats.Binary:
									serializer = new TradeSerializer(key.Item1);
									break;
								case StorageFormats.Csv:
									serializer = new CsvMarketDataSerializer<ExecutionMessage>(key.Item1, ExecutionTypes.Tick);
									break;
								default:
									throw new ArgumentOutOfRangeException("format");
							}

							return new TradeStorage(security, mdDrive, serializer);
						}
					}
					case ExecutionTypes.Order:
					case ExecutionTypes.Trade:
						return new ExecutionStorage(security, mdDrive, new ExecutionSerializer(secId));
					case ExecutionTypes.OrderLog:
					{
						IMarketDataSerializer<ExecutionMessage> serializer;

						switch (format)
						{
							case StorageFormats.Binary:
								serializer = new OrderLogSerializer(secId);
								break;
							case StorageFormats.Csv:
								serializer = new CsvMarketDataSerializer<ExecutionMessage>(secId, ExecutionTypes.OrderLog);
								break;
							default:
								throw new ArgumentOutOfRangeException("format");
						}

						return new OrderLogStorage(security, mdDrive, serializer);
					}
					default:
						throw new ArgumentOutOfRangeException("type");
				}
			});
		}

		/// <summary>
		/// Получить хранилище маркет-данных.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="drive">Хранилище.</param>
		/// <param name="format">Тип формата. По-умолчанию передается <see cref="StorageFormats.Binary"/>.</param>
		/// <returns>Хранилище маркет-данных.</returns>
		public IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			if (dataType == typeof(Trade))
				return GetTradeStorage(security, drive, format);
			else if (dataType == typeof(MarketDepth))
				return GetMarketDepthStorage(security, drive, format);
			//else if (dataType == typeof(SecurityChange))
			//	return GetSecurityChangeStorage(security, drive, format);
			else if (dataType == typeof(OrderLogItem))
				return GetOrderLogStorage(security, drive, format);
			else if (dataType.IsSubclassOf(typeof(Candle)))
				return GetCandleStorage(dataType, security, arg, drive, format);
			else if (dataType == typeof(Order))
				return GetExecutionStorage(security, ExecutionTypes.Order, drive, format);
			else if (dataType == typeof(ExecutionMessage))
				return GetExecutionStorage(security, (ExecutionTypes)arg, drive, format);
			else if (dataType == typeof(Level1ChangeMessage))
				return GetLevel1MessageStorage(security, drive, format);
			else if (dataType == typeof(QuoteChangeMessage))
				return GetQuoteMessageStorage(security, drive, format);
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				return GetCandleMessageStorage(dataType, security, arg, drive, format);
			else
				throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str1018);
		}

		private static Security ToSecurity(SecurityId securityId)
		{
			return new Security
			{
				Id = securityId.SecurityCode + "@" + securityId.BoardCode,
				Code = securityId.SecurityCode,
				Board = ExchangeBoard.GetOrCreateBoard(securityId.BoardCode)
			};
		}
	}
}