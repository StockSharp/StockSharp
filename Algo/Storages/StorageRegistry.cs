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
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Reflection.Path;

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

						Sides? side = null;

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
									IsSorted = true,
								};
							}
							else if (Current.ServerTime != quote.ServerTime || (side == Sides.Sell && quote.Side == Sides.Buy))
							{
								_resetCurrent = true;
								_needMoveNext = false;

								return true;
							}

							side = quote.Side;

							if (quote.Price != 0)
							{
								var quotes = (List<QuoteChange>)(quote.Side == Sides.Buy ? Current.Bids : Current.Asks);
								quotes.Add(quote);
							}
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
						throw new ArgumentNullException(nameof(quotes));
				}
			}

			private readonly CsvMarketDataSerializer<TimeQuoteChange> _quoteSerializer;

			public MarketDepthCsvSerializer(SecurityId securityId)
				: base(securityId)
			{
				_quoteSerializer = new QuoteCsvSerializer(securityId);
			}

			public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
			{
				return _quoteSerializer.CreateMetaInfo(date);
			}

			public override void Serialize(Stream stream, IEnumerable<QuoteChangeMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var list = data.SelectMany(d =>
				{
					var items = new List<TimeQuoteChange>();

					items.AddRange(d.Bids.OrderByDescending(q => q.Price).Select(q => new TimeQuoteChange(q, d)));

					if (items.Count == 0)
						items.Add(new TimeQuoteChange { Side = Sides.Buy, ServerTime = d.ServerTime });

					var bidsCount = items.Count;

					items.AddRange(d.Asks.OrderBy(q => q.Price).Select(q => new TimeQuoteChange(q, d)));

					if (items.Count == bidsCount)
						items.Add(new TimeQuoteChange { Side = Sides.Sell, ServerTime = d.ServerTime });

					return items;
				});

				_quoteSerializer.Serialize(stream, list, metaInfo);
			}

			public override IEnumerable<QuoteChangeMessage> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
			{
				return new QuoteEnumerable(_quoteSerializer.Deserialize(stream, metaInfo), SecurityId);
			}

			protected override void Write(CsvFileWriter writer, QuoteChangeMessage data)
			{
				throw new NotSupportedException();
			}

			protected override QuoteChangeMessage Read(FastCsvReader reader, DateTime date)
			{
				throw new NotSupportedException();
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

			int IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
			{
				return Save(data.Select(ToMessage));
			}

			void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
			{
				Delete(data.Select(ToMessage));
			}

			IEnumerable<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
			{
				return Load(date).ToEntities<TMessage, TEntity>(Security);
			}

			public abstract DateTimeOffset GetTime(TEntity data);

			protected abstract TMessage ToMessage(TEntity entity);
		}

		private sealed class TradeStorage : ConvertableStorage<ExecutionMessage, Trade, long>
		{
			public TradeStorage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(security, ExecutionTypes.Tick, trade => trade.ServerTime, trade => trade.SecurityId, trade => trade.TradeId ?? 0, serializer, drive)
			{
			}

			protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var prevId = (long?)metaInfo.LastId;
				var prevTime = metaInfo.LastTime.ApplyTimeZone(TimeZoneInfo.Utc);

				return data.Where(t =>
				{
					if (t.ServerTime > prevTime)
						return true;
					else if (t.ServerTime == prevTime && prevId != null)
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
				var prevTransId = (long)metaInfo.LastId;
				return prevTransId == 0 ? data : data.Where(i => i.TransactionId > prevTransId);
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

			IEnumerable<CandleMessage> IMarketDataStorage<CandleMessage>.Load(DateTime date)
			{
				return Load(date);
			}

			IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			int IMarketDataStorage<CandleMessage>.Save(IEnumerable<CandleMessage> data)
			{
				return Save(data.Cast<TCandleMessage>());
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

			int IMarketDataStorage<TCandle>.Save(IEnumerable<TCandle> data)
			{
				return Save(data.Select(Convert));
			}

			void IMarketDataStorage<TCandle>.Delete(IEnumerable<TCandle> data)
			{
				Delete(data.Select(Convert));
			}

			IEnumerable<TCandle> IMarketDataStorage<TCandle>.Load(DateTime date)
			{
				var messages = Load(date);

				return messages
					.ToCandles<TCandle>(Security);
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
					throw new ArgumentException(LocalizedStrings.Str1016Params.Put(candle, candle.Arg, expectedArg), nameof(candle));

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

			IMarketDataSerializer<Candle> IMarketDataStorage<Candle>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			int IMarketDataStorage<Candle>.Save(IEnumerable<Candle> data)
			{
				return Save(data.Select(c => Convert((TCandle)c)));
			}

			void IMarketDataStorage<Candle>.Delete(IEnumerable<Candle> data)
			{
				Delete(data.Select(c => Convert((TCandle)c)));
			}

			IEnumerable<Candle> IMarketDataStorage<Candle>.Load(DateTime date)
			{
				var messages = Load(date);

				return messages
					.ToCandles<Candle>(Security, typeof(TCandleMessage).ToCandleType());
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

		private sealed class TransactionStorage : MarketDataStorage<ExecutionMessage, long>, IMarketDataStorage<Order>,
			IMarketDataStorageInfo<Order>, IMarketDataStorage<MyTrade>, IMarketDataStorageInfo<MyTrade>
		{
			public TransactionStorage(Security security, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(security, ExecutionTypes.Transaction, msg => msg.ServerTime, msg => msg.SecurityId, msg => msg.TransactionId, serializer, drive)
			{
				AppendOnlyNew = false;
			}

			#region Order

			IMarketDataSerializer<Order> IMarketDataStorage<Order>.Serializer
			{
				get { throw new NotSupportedException(); }
			}

			int IMarketDataStorage<Order>.Save(IEnumerable<Order> data)
			{
				return Save(data.Select(t => t.ToMessage()));
			}

			void IMarketDataStorage<Order>.Delete(IEnumerable<Order> data)
			{
				throw new NotSupportedException();
			}

			IEnumerable<Order> IMarketDataStorage<Order>.Load(DateTime date)
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

			int IMarketDataStorage<MyTrade>.Save(IEnumerable<MyTrade> data)
			{
				return Save(data.Select(t => t.ToMessage()));
			}

			void IMarketDataStorage<MyTrade>.Delete(IEnumerable<MyTrade> data)
			{
				throw new NotSupportedException();
			}

			IEnumerable<MyTrade> IMarketDataStorage<MyTrade>.Load(DateTime date)
			{
				throw new NotSupportedException();
			}

			DateTimeOffset IMarketDataStorageInfo<MyTrade>.GetTime(MyTrade data)
			{
				return data.Trade.Time;
			}

			#endregion
		}

		private sealed class NewsStorage : ConvertableStorage<NewsMessage, News, VoidType>
		{
			public NewsStorage(Security security, IMarketDataSerializer<NewsMessage> serializer, IMarketDataStorageDrive drive)
				: base(security, null, m => m.ServerTime, m => default(SecurityId), m => null, serializer, drive)
			{
			}

			public override DateTimeOffset GetTime(News data)
			{
				return data.ServerTime;
			}

			protected override NewsMessage ToMessage(News entity)
			{
				return entity.ToMessage();
			}
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>> _depthStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<QuoteChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>> _level1Storages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<Level1ChangeMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>> _candleStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<CandleMessage>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>> _executionStorages = new SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<ExecutionMessage>>();
		private readonly SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<NewsMessage>> _newsStorages = new SynchronizedDictionary<IMarketDataStorageDrive, IMarketDataStorage<NewsMessage>>();
		private readonly SynchronizedDictionary<IMarketDataDrive, ISecurityStorage> _securityStorages = new SynchronizedDictionary<IMarketDataDrive, ISecurityStorage>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageRegistry"/>.
		/// </summary>
		public StorageRegistry()
		{
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

		/// <summary>
		/// The storage used by default.
		/// </summary>
		public virtual IMarketDataDrive DefaultDrive
		{
			get { return _defaultDrive; }
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

		/// <summary>
		/// To add the tick trades storage.
		/// </summary>
		/// <param name="storage">The storage of tick trades.</param>
		public void RegisterTradeStorage(IMarketDataStorage<Trade> storage)
		{
			RegisterTradeStorage((IMarketDataStorage<ExecutionMessage>)storage);
		}

		/// <summary>
		/// To add the order books storage.
		/// </summary>
		/// <param name="storage">The order books storage.</param>
		public void RegisterMarketDepthStorage(IMarketDataStorage<MarketDepth> storage)
		{
			RegisterMarketDepthStorage((IMarketDataStorage<QuoteChangeMessage>)storage);
		}

		/// <summary>
		/// To register the order log storage.
		/// </summary>
		/// <param name="storage">The storage of orders log.</param>
		public void RegisterOrderLogStorage(IMarketDataStorage<OrderLogItem> storage)
		{
			RegisterOrderLogStorage((IMarketDataStorage<ExecutionMessage>)storage);
		}

		/// <summary>
		/// To add the candles storage.
		/// </summary>
		/// <param name="storage">The candles storage.</param>
		public void RegisterCandleStorage(IMarketDataStorage<Candle> storage)
		{
			RegisterCandleStorage((IMarketDataStorage<CandleMessage>)storage);
		}

		/// <summary>
		/// To register tick trades storage.
		/// </summary>
		/// <param name="storage">The storage of tick trades.</param>
		public void RegisterTradeStorage(IMarketDataStorage<ExecutionMessage> storage)
		{
			RegisterStorage(_executionStorages, ExecutionTypes.Tick, storage);
		}

		/// <summary>
		/// To register the order books storage.
		/// </summary>
		/// <param name="storage">The order books storage.</param>
		public void RegisterMarketDepthStorage(IMarketDataStorage<QuoteChangeMessage> storage)
		{
			RegisterStorage(_depthStorages, storage);
		}

		/// <summary>
		/// To register the order log storage.
		/// </summary>
		/// <param name="storage">The storage of orders log.</param>
		public void RegisterOrderLogStorage(IMarketDataStorage<ExecutionMessage> storage)
		{
			RegisterStorage(_executionStorages, ExecutionTypes.OrderLog, storage);
		}

		/// <summary>
		/// To register storage of instrument changes.
		/// </summary>
		/// <param name="storage">The storage of instrument changes.</param>
		public void RegisterLevel1Storage(IMarketDataStorage<Level1ChangeMessage> storage)
		{
			RegisterStorage(_level1Storages, storage);
		}

		private static void RegisterStorage<T>(SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<T>> storages, IMarketDataStorage<T> storage)
		{
			if (storages == null)
				throw new ArgumentNullException(nameof(storages));

			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storages.Add(Tuple.Create(storage.Security.ToSecurityId(), storage.Drive), storage);
		}

		private static void RegisterStorage<T>(SynchronizedDictionary<Tuple<SecurityId, ExecutionTypes, IMarketDataStorageDrive>, IMarketDataStorage<T>> storages, ExecutionTypes type, IMarketDataStorage<T> storage)
		{
			if (storages == null)
				throw new ArgumentNullException(nameof(storages));

			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storages.Add(Tuple.Create(storage.Security.ToSecurityId(), type, storage.Drive), storage);
		}

		/// <summary>
		/// To register the candles storage.
		/// </summary>
		/// <param name="storage">The candles storage.</param>
		public void RegisterCandleStorage(IMarketDataStorage<CandleMessage> storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_candleStorages.Add(Tuple.Create(storage.Security.ToSecurityId(), storage.Drive), storage);
		}

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of tick trades.</returns>
		public IMarketDataStorage<Trade> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<Trade>)GetTickMessageStorage(security, drive, format);
		}

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The order books storage.</returns>
		public IMarketDataStorage<MarketDepth> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<MarketDepth>)GetQuoteMessageStorage(security, drive, format);
		}

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of orders log.</returns>
		public IMarketDataStorage<OrderLogItem> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<OrderLogItem>)GetOrderLogMessageStorage(security, drive, format);
		}

		/// <summary>
		/// To get the candles storage the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="security">Security.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		public IMarketDataStorage<Candle> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<Candle>)GetCandleMessageStorage(candleType.ToCandleMessageType(), security, arg, drive, format);
		}

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of tick trades.</returns>
		public IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(security, ExecutionTypes.Tick, drive, format);
		}

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The order books storage.</returns>
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

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
							serializer = new QuoteBinarySerializer(key.Item1);
							break;
						case StorageFormats.Csv:
							serializer = new MarketDepthCsvSerializer(key.Item1);
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(format));
					}

					return new MarketDepthStorage(security, key.Item2, serializer);
				}
			});
		}

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of orders log.</returns>
		public IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(security, ExecutionTypes.OrderLog, drive, format);
		}

		/// <summary>
		/// To get the transactions storage the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The transactions storage.</returns>
		public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(security, ExecutionTypes.Transaction, drive, format);
		}

		/// <summary>
		/// To get the storage of instrument changes.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The storage of instrument changes.</returns>
		public IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var securityId = security.ToSecurityId();

			return _level1Storages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(Level1ChangeMessage), null, format)), key =>
			{
				if (security.Board == ExchangeBoard.Associated)
					return new AllSecurityMarketDataStorage<Level1ChangeMessage>(security, null, md => md.ServerTime, md => ToSecurity(md.SecurityId), (s, d) => GetLevel1MessageStorage(s, d, format), key.Item2);

				IMarketDataSerializer<Level1ChangeMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new Level1BinarySerializer(key.Item1);
						break;
					case StorageFormats.Csv:
						serializer = new Level1CsvSerializer(key.Item1);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format));
				}

				return new Level1Storage(security, key.Item2, serializer);
			});
		}

		/// <summary>
		/// To get the candles storage the specified instrument.
		/// </summary>
		/// <param name="candleMessageType">The type of candle message.</param>
		/// <param name="security">Security.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The candles storage.</returns>
		public IMarketDataStorage<CandleMessage> GetCandleMessageStorage(Type candleMessageType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (candleMessageType == null)
				throw new ArgumentNullException(nameof(candleMessageType));

			if (!candleMessageType.IsCandleMessage())
				throw new ArgumentOutOfRangeException(nameof(candleMessageType), candleMessageType, LocalizedStrings.WrongCandleType);

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (arg.IsNull(true))
				throw new ArgumentNullException(nameof(arg), LocalizedStrings.EmptyCandleArg);

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
							serializer = typeof(CandleBinarySerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(security.ToSecurityId(), arg);
							break;
						case StorageFormats.Csv:
							serializer = typeof(CandleCsvSerializer<>).Make(candleMessageType).CreateInstance<IMarketDataSerializer>(security.ToSecurityId(), arg, null);
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(format));
					}

					return typeof(CandleStorage<,>).Make(candleMessageType, candleMessageType.ToCandleType()).CreateInstance<IMarketDataStorage<CandleMessage>>(security, arg, key.Item2, serializer);
				}
			});
		}

		/// <summary>
		/// To get the <see cref="ExecutionMessage"/> storage the specified instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="type">Data type, information about which is contained in the <see cref="ExecutionMessage"/>.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The <see cref="ExecutionMessage"/> storage.</returns>
		public IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(Security security, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

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
							return new ConvertableContinuousSecurityMarketDataStorage<ExecutionMessage, Trade>((ContinuousSecurity)security, null, t => t.ServerTime, t => ToSecurity(t.SecurityId), t => t.ToMessage(), t => t.Time, (s, d) => GetExecutionMessageStorage(s, type, d, format), mdDrive);
						else if (security is IndexSecurity)
							return new IndexSecurityMarketDataStorage<ExecutionMessage>((IndexSecurity)security, null, d => ToSecurity(d.SecurityId), (s, d) => GetExecutionMessageStorage(s, type, d, format), mdDrive);
						else if (security.Board == ExchangeBoard.Associated)
							return new ConvertableAllSecurityMarketDataStorage<ExecutionMessage, Trade>(security, null, t => t.ServerTime, t => ToSecurity(t.SecurityId), t => t.Time, (s, d) => GetExecutionMessageStorage(s, type, d, format), mdDrive);
						else
						{
							IMarketDataSerializer<ExecutionMessage> serializer;

							switch (format)
							{
								case StorageFormats.Binary:
									serializer = new TickBinarySerializer(key.Item1);
									break;
								case StorageFormats.Csv:
									serializer = new TickCsvSerializer(key.Item1);
									break;
								default:
									throw new ArgumentOutOfRangeException(nameof(format));
							}

							return new TradeStorage(security, mdDrive, serializer);
						}
					}
					case ExecutionTypes.Transaction:
					{
						IMarketDataSerializer<ExecutionMessage> serializer;

						switch (format)
						{
							case StorageFormats.Binary:
								serializer = new TransactionBinarySerializer(secId);
								break;
							case StorageFormats.Csv:
								serializer = new TransactionCsvSerializer(secId);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(format));
						}

						return new TransactionStorage(security, mdDrive, serializer);
					}
					case ExecutionTypes.OrderLog:
					{
						IMarketDataSerializer<ExecutionMessage> serializer;

						switch (format)
						{
							case StorageFormats.Binary:
								serializer = new OrderLogBinarySerializer(secId);
								break;
							case StorageFormats.Csv:
								serializer = new OrderLogCsvSerializer(secId);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(format));
						}

						return new OrderLogStorage(security, mdDrive, serializer);
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(type));
				}
			});
		}

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="Candle.Arg"/>.</param>
		/// <param name="drive">Storage.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>Market-data storage.</returns>
		public IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (!dataType.IsSubclassOf(typeof(Message)))
				dataType = dataType.ToMessageType(ref arg);

			if (dataType == typeof(ExecutionMessage))
				return GetExecutionMessageStorage(security, (ExecutionTypes)arg, drive, format);
			else if (dataType == typeof(Level1ChangeMessage))
				return GetLevel1MessageStorage(security, drive, format);
			else if (dataType == typeof(QuoteChangeMessage))
				return GetQuoteMessageStorage(security, drive, format);
			else if (dataType == typeof(NewsMessage))
				return GetNewsMessageStorage(drive, format);
			else if (dataType.IsCandleMessage())
				return GetCandleMessageStorage(dataType, security, arg, drive, format);
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1018);
		}

		private static Security ToSecurity(SecurityId securityId)
		{
			return new Security
			{
				Id = securityId.ToStringId(),
				Code = securityId.SecurityCode,
				Board = ExchangeBoard.GetOrCreateBoard(securityId.BoardCode)
			};
		}

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="StorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The news storage.</returns>
		public IMarketDataStorage<News> GetNewsStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<News>)GetNewsMessageStorage(drive, format);
		}

		private static readonly Security _newsSecurity = new Security { Id = "NEWS@NEWS" };

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="StorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The news storage.</returns>
		public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return _newsStorages.SafeAdd((drive ?? DefaultDrive).GetStorageDrive(_newsSecurity.ToSecurityId(), typeof(NewsMessage), null, format), key =>
			{
				IMarketDataSerializer<NewsMessage> serializer;

				switch (format)
				{
					case StorageFormats.Binary:
						serializer = new NewsBinarySerializer();
						break;
					case StorageFormats.Csv:
						serializer = new NewsCsvSerializer();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(format));
				}

				return new NewsStorage(_newsSecurity, serializer, key);
			});
		}

		private class SecurityStorage : Disposable, ISecurityStorage
		{
			private const string _format = "{Id};{Type};{Decimals};{PriceStep};{VolumeStep};{Multiplier};{Name};{ShortName};{UnderlyingSecurityId};{Class};{Currency};{OptionType};{Strike};{BinaryOptionType}";
			private readonly string _file;
			private readonly CachedSynchronizedSet<Security> _securities = new CachedSynchronizedSet<Security>();

			public SecurityStorage(IMarketDataDrive drive)
			{
				if (drive == null)
					throw new ArgumentNullException(nameof(drive));

				_file = Path.Combine(drive.Path, "instruments.csv");
				Load();
			}

			private void Load()
			{
				if (!File.Exists(_file))
					return;

				var proxySet = _format
					.Split(';')
					.Select(s => MemberProxy.Create(typeof(Security), s.Substring(1, s.Length - 2)))
					.ToArray();

				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var idGen = new SecurityIdGenerator();

					foreach (var line in File.ReadAllLines(_file))
					{
						var security = new Security();

						var cells = line.Split(';');

						for (var i = 0; i < proxySet.Length; i++)
						{
							proxySet[i].SetValue(security, cells[i].To(proxySet[i].ReturnType));
						}

						var id = idGen.Split(security.Id);
						security.Code = id.SecurityCode;
						security.Board = ExchangeBoard.GetOrCreateBoard(id.BoardCode);

						_securities.Add(security);
					}
				});
			}

			int ISecurityProvider.Count => _securities.Count;

			public event Action<IEnumerable<Security>> Added;
			public event Action<IEnumerable<Security>> Removed;

			public event Action Cleared
			{
				add { }
				remove { }
			}

			IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
			{
				return _securities.Cache.Filter(criteria);
			}

			object ISecurityProvider.GetNativeId(Security security)
			{
				return null;
			}

			void ISecurityStorage.Save(Security security)
			{
				if (!_securities.TryAdd(security))
					return;

				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					using (var file = File.AppendText(_file))
						file.WriteLine(_format.PutEx(security));
				});

				Added.SafeInvoke(new[] { security });
			}

			void ISecurityStorage.Delete(Security security)
			{
				if (!_securities.Remove(security))
					return;

				Save();
				Removed.SafeInvoke(new[] { security });
			}

			void ISecurityStorage.DeleteBy(Security criteria)
			{
				var removed = new List<Security>();

				foreach (var security in _securities.Cache.Filter(criteria))
				{
					if (_securities.Remove(security))
						removed.Add(security);
				}

				Removed.SafeInvoke(removed);
				
				Save();
			}

			private void Save()
			{
				var securities = _securities.Cache;

				if (securities.Length == 0)
					File.Delete(_file);

				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					File.WriteAllLines(_file, securities.Select(s => _format.PutEx(s)));
				});
			}

			IEnumerable<string> ISecurityStorage.GetSecurityIds()
			{
				return _securities.Cache.Select(s => s.Id);
			}
		}

		/// <summary>
		/// To get the instruments storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="StorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The instruments storage.</returns>
		public ISecurityStorage GetSecurityStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return _securityStorages.SafeAdd(drive ?? DefaultDrive, key => new SecurityStorage(key));
		}
	}
}