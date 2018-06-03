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
	using System.Collections;
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
		private abstract class ConvertableStorage<TMessage, TEntity, TId> : MarketDataStorage<TMessage, TId>, IMarketDataStorage<TEntity>, IMarketDataStorageInfo<TEntity>
			where TMessage : Message
		{
			private readonly StorageRegistry _parent;

			protected ConvertableStorage(StorageRegistry parent, Security security, SecurityId securityId, object arg, Func<TMessage, DateTimeOffset> getTime, Func<TMessage, SecurityId> getSecurity, Func<TMessage, TId> getId, IMarketDataSerializer<TMessage> serializer, IMarketDataStorageDrive drive)
				: base(security, securityId, arg, getTime, getSecurity, getId, serializer, drive)
			{
				_parent = parent;
			}

			IMarketDataSerializer<TEntity> IMarketDataStorage<TEntity>.Serializer => throw new NotSupportedException();

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
				return Load(date).ToEntities<TMessage, TEntity>(Security, _parent.ExchangeInfoProvider);
			}

			public abstract DateTimeOffset GetTime(TEntity data);

			protected abstract TMessage ToMessage(TEntity entity);
		}

		private sealed class TradeStorage : ConvertableStorage<ExecutionMessage, Trade, DateTimeOffset>
		{
			public TradeStorage(StorageRegistry parent, Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(parent, security, securityId, ExecutionTypes.Tick, trade => trade.ServerTime, trade => trade.SecurityId, trade => trade.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive)
			{
			}

			protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var prevId = (long?)metaInfo.LastId ?? 0;
				var prevTime = metaInfo.LastTime.ApplyTimeZone(TimeZoneInfo.Utc);

				foreach (var msg in data)
				{
					if (msg.ServerTime > prevTime)
					{
						prevId = msg.TradeId ?? 0;
						prevTime = msg.ServerTime;

						yield return msg;
					}
					else if (msg.ServerTime == prevTime)
					{
						// если разные сделки имеют одинаковое время
						if (prevId != 0 && msg.TradeId != null && msg.TradeId != prevId)
						{
							prevId = msg.TradeId ?? 0;
							prevTime = msg.ServerTime;

							yield return msg;
						}
					}
				}
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
			public MarketDepthStorage(StorageRegistry parent, Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<QuoteChangeMessage> serializer)
				: base(parent, security, securityId, null, depth => depth.ServerTime, depth => depth.SecurityId, depth => depth.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive)
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
			public OrderLogStorage(StorageRegistry parent, Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(parent, security, securityId, ExecutionTypes.OrderLog, item => item.ServerTime, item => item.SecurityId, item => item.TransactionId, serializer, drive)
			{
			}

			protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var prevTransId = (long?)metaInfo.LastId ?? 0;

				foreach (var msg in data)
				{
					if (msg.TransactionId != 0 && msg.TransactionId <= prevTransId)
						continue;

					prevTransId = msg.TransactionId;
					yield return msg;
				}
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
			private class CandleSerializer : IMarketDataSerializer<CandleMessage>
			{
				private readonly IMarketDataSerializer<TCandleMessage> _serializer;

				public CandleSerializer(IMarketDataSerializer<TCandleMessage> serializer)
				{
					_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
				}

				StorageFormats IMarketDataSerializer.Format => _serializer.Format;

				TimeSpan IMarketDataSerializer.TimePrecision => _serializer.TimePrecision;

				IMarketDataMetaInfo IMarketDataSerializer.CreateMetaInfo(DateTime date) => _serializer.CreateMetaInfo(date);

				void IMarketDataSerializer.Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo)
					=> _serializer.Serialize(stream, data, metaInfo);

				IEnumerable<CandleMessage> IMarketDataSerializer<CandleMessage>.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
					=> _serializer.Deserialize(stream, metaInfo);

				void IMarketDataSerializer<CandleMessage>.Serialize(Stream stream, IEnumerable<CandleMessage> data, IMarketDataMetaInfo metaInfo)
					=> _serializer.Serialize(stream, data, metaInfo);

				IEnumerable IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
					=> _serializer.Deserialize(stream, metaInfo);
			}

			private readonly CandleSerializer _serializer;

			protected CandleMessageStorage(Security security, SecurityId securityId, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, securityId, arg, candle => candle.OpenTime, candle => candle.SecurityId, candle => candle.OpenTime.StorageTruncate(serializer.TimePrecision), serializer, drive)
			{
				_serializer = new CandleSerializer(Serializer);
			}

			protected override IEnumerable<TCandleMessage> FilterNewData(IEnumerable<TCandleMessage> data, IMarketDataMetaInfo metaInfo)
			{
				var lastTime = metaInfo.LastTime.ApplyTimeZone(TimeZoneInfo.Utc);

				foreach (var msg in data)
				{
					if (msg.State == CandleStates.Active)
						continue;

					var time = GetTruncatedTime(msg).ApplyTimeZone(TimeZoneInfo.Utc);

					if ((msg is TimeFrameCandleMessage && time <= lastTime) || time < lastTime)
						continue;

					lastTime = time;
					yield return msg;
				}
			}

			IEnumerable<CandleMessage> IMarketDataStorage<CandleMessage>.Load(DateTime date)
			{
				return Load(date);
			}

			IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => _serializer;

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
			protected TypedCandleStorage(Security security, SecurityId securityId, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, securityId, arg, drive, serializer)
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

			IMarketDataSerializer<TCandle> IMarketDataStorage<TCandle>.Serializer => throw new NotSupportedException();

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
			public CandleStorage(Security security, SecurityId securityId, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, securityId, arg, drive, serializer)
			{
			}

			IMarketDataSerializer<Candle> IMarketDataStorage<Candle>.Serializer => throw new NotSupportedException();

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

		private sealed class Level1Storage : MarketDataStorage<Level1ChangeMessage, DateTimeOffset>
		{
			public Level1Storage(Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<Level1ChangeMessage> serializer)
				: base(security, securityId, null, value => value.ServerTime, value => value.SecurityId, value => value.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive)
			{
			}
		}

		private sealed class PositionStorage : MarketDataStorage<PositionChangeMessage, DateTimeOffset>
		{
			public PositionStorage(Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<PositionChangeMessage> serializer)
				: base(security, securityId, null, value => value.ServerTime, value => value.SecurityId, value => value.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive)
			{
			}
		}

		private sealed class TransactionStorage : MarketDataStorage<ExecutionMessage, long>, IMarketDataStorage<Order>,
			IMarketDataStorageInfo<Order>, IMarketDataStorage<MyTrade>, IMarketDataStorageInfo<MyTrade>
		{
			public TransactionStorage(Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(security, securityId, ExecutionTypes.Transaction, msg => msg.ServerTime, msg => msg.SecurityId, msg => msg.TransactionId, serializer, drive)
			{
				AppendOnlyNew = false;
			}

			#region Order

			IMarketDataSerializer<Order> IMarketDataStorage<Order>.Serializer => throw new NotSupportedException();

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

			IMarketDataSerializer<MyTrade> IMarketDataStorage<MyTrade>.Serializer => throw new NotSupportedException();

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
			public NewsStorage(StorageRegistry parent, Security security, SecurityId securityId, IMarketDataSerializer<NewsMessage> serializer, IMarketDataStorageDrive drive)
				: base(parent, security, securityId, null, m => m.ServerTime, m => default(SecurityId), m => null, serializer, drive)
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
		private readonly SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<PositionChangeMessage>> _positionStorages = new SynchronizedDictionary<Tuple<SecurityId, IMarketDataStorageDrive>, IMarketDataStorage<PositionChangeMessage>>();
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

		private IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		/// <summary>
		/// Exchanges and trading boards provider.
		/// </summary>
		public IExchangeInfoProvider ExchangeInfoProvider
		{
			get => _exchangeInfoProvider;
			set => _exchangeInfoProvider = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public void RegisterTradeStorage(IMarketDataStorage<Trade> storage)
		{
			RegisterTradeStorage((IMarketDataStorage<ExecutionMessage>)storage);
		}

		/// <inheritdoc />
		public void RegisterMarketDepthStorage(IMarketDataStorage<MarketDepth> storage)
		{
			RegisterMarketDepthStorage((IMarketDataStorage<QuoteChangeMessage>)storage);
		}

		/// <inheritdoc />
		public void RegisterOrderLogStorage(IMarketDataStorage<OrderLogItem> storage)
		{
			RegisterOrderLogStorage((IMarketDataStorage<ExecutionMessage>)storage);
		}

		/// <inheritdoc />
		public void RegisterCandleStorage(IMarketDataStorage<Candle> storage)
		{
			RegisterCandleStorage((IMarketDataStorage<CandleMessage>)storage);
		}

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

			_candleStorages.Add(Tuple.Create(storage.Security.ToSecurityId(), storage.Drive), storage);
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


		/// <inheritdoc />
		public IMarketDataStorage<Trade> GetTradeStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<Trade>)GetTickMessageStorage(security, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<MarketDepth> GetMarketDepthStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<MarketDepth>)GetQuoteMessageStorage(security, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<OrderLogItem> GetOrderLogStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<OrderLogItem>)GetOrderLogMessageStorage(security, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<Candle> GetCandleStorage(Type candleType, Security security, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<Candle>)GetCandleMessageStorage(candleType.ToCandleMessageType(), security, arg, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetTickMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(security, ExecutionTypes.Tick, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var securityId = GetSecurityId(security);

			return _depthStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(QuoteChangeMessage), null, format)), key =>
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

				return new MarketDepthStorage(this, security, securityId, key.Item2, serializer);
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetOrderLogMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(security, ExecutionTypes.OrderLog, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return GetExecutionMessageStorage(security, ExecutionTypes.Transaction, drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<Level1ChangeMessage> GetLevel1MessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var securityId = GetSecurityId(security);

			return _level1Storages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(Level1ChangeMessage), null, format)), key =>
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

				return new Level1Storage(security, securityId, key.Item2, serializer);
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(Security security, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var securityId = GetSecurityId(security);

			return _positionStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(PositionChangeMessage), null, format)), key =>
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

				return new PositionStorage(security, securityId, key.Item2, serializer);
			});
		}

		/// <inheritdoc />
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

			var securityId = GetSecurityId(security);

			return _candleStorages.SafeAdd(Tuple.Create(securityId, (drive ?? DefaultDrive).GetStorageDrive(securityId, candleMessageType, arg, format)), key =>
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

				return typeof(CandleStorage<,>).Make(candleMessageType, candleMessageType.ToCandleType()).CreateInstance<IMarketDataStorage<CandleMessage>>(security, key.Item1, arg, key.Item2, serializer);
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetExecutionMessageStorage(Security security, ExecutionTypes type, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var securityId = GetSecurityId(security);

			return _executionStorages.SafeAdd(Tuple.Create(securityId, type, (drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(ExecutionMessage), type, format)), key =>
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

						return new TradeStorage(this, security, securityId, mdDrive, serializer);
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

						return new TransactionStorage(security, securityId, mdDrive, serializer);
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

						return new OrderLogStorage(this, security, securityId, mdDrive, serializer);
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
				}
			});
		}

		/// <inheritdoc />
		public IMarketDataStorage GetStorage(Security security, Type dataType, object arg, IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (!dataType.IsSubclassOf(typeof(Message)))
				dataType = dataType.ToMessageType(ref arg);

			if (dataType == typeof(ExecutionMessage))
			{
				if (arg == null)
					throw new ArgumentNullException(nameof(arg));

				return GetExecutionMessageStorage(security, (ExecutionTypes)arg, drive, format);
			}
			else if (dataType == typeof(Level1ChangeMessage))
				return GetLevel1MessageStorage(security, drive, format);
			else if (dataType == typeof(PositionChangeMessage))
				return GetPositionMessageStorage(security, drive, format);
			else if (dataType == typeof(QuoteChangeMessage))
				return GetQuoteMessageStorage(security, drive, format);
			else if (dataType == typeof(NewsMessage))
				return GetNewsMessageStorage(drive, format);
			else if (dataType.IsCandleMessage())
				return GetCandleMessageStorage(dataType, security, arg, drive, format);
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1018);
		}

		//private Security ToSecurity(SecurityId securityId)
		//{
		//	return new Security
		//	{
		//		Id = securityId.ToStringId(),
		//		Code = securityId.SecurityCode,
		//		Board = ExchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode)
		//	};
		//}

		/// <inheritdoc />
		public IMarketDataStorage<News> GetNewsStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return (IMarketDataStorage<News>)GetNewsMessageStorage(drive, format);
		}

		/// <inheritdoc />
		public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			var securityId = GetSecurityId(TraderHelper.NewsSecurity);

			return _newsStorages.SafeAdd((drive ?? DefaultDrive).GetStorageDrive(securityId, typeof(NewsMessage), null, format), key =>
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

				return new NewsStorage(this, TraderHelper.NewsSecurity, securityId, serializer, key);
			});
		}

		private static SecurityId GetSecurityId(Security security)
		{
			var id = security.ToSecurityId();
			id.EnsureHashCode();
			return id;
		}

		private class SecurityStorage : Disposable, ISecurityStorage
		{
			private readonly StorageRegistry _parent;
			private const string _format = "{Id};{Type};{Decimals};{PriceStep};{VolumeStep};{Multiplier};{Name};{ShortName};{UnderlyingSecurityId};{Class};{Currency};{OptionType};{Strike};{BinaryOptionType}";
			private readonly string _file;
			private readonly CachedSynchronizedSet<Security> _securities = new CachedSynchronizedSet<Security>();

			public SecurityStorage(StorageRegistry parent, IMarketDataDrive drive)
			{
				if (drive == null)
					throw new ArgumentNullException(nameof(drive));

				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
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
					foreach (var line in File.ReadAllLines(_file))
					{
						var security = new Security();

						var cells = line.Split(';');

						for (var i = 0; i < proxySet.Length; i++)
						{
							var proxy = proxySet[i];
							var cell = cells[i];

							if (cell.Length == 0)
								cell = null;

							var value = (object)cell;

							if (proxy.ReturnType != typeof(string))
								value = value.To(proxy.ReturnType);

							proxy.SetValue(security, value);
						}

						var id = security.Id.ToSecurityId();
						security.Code = id.SecurityCode;
						security.Board = _parent.ExchangeInfoProvider.GetOrCreateBoard(id.BoardCode);

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

			void ISecurityStorage.Save(Security security, bool forced)
			{
				if (!_securities.TryAdd(security))
					return;

				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					using (var file = File.AppendText(_file))
						file.WriteLine(_format.PutEx(security));
				});

				Added?.Invoke(new[] { security });
			}

			void ISecurityStorage.Delete(Security security)
			{
				if (!_securities.Remove(security))
					return;

				Save();
				Removed?.Invoke(new[] { security });
			}

			void ISecurityStorage.DeleteBy(Security criteria)
			{
				var removed = new List<Security>();

				foreach (var security in _securities.Cache.Filter(criteria))
				{
					if (_securities.Remove(security))
						removed.Add(security);
				}

				Removed?.Invoke(removed);
				
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

			//IEnumerable<string> ISecurityStorage.GetSecurityIds()
			//{
			//	return _securities.Cache.Select(s => s.Id);
			//}
		}

		/// <summary>
		/// To get the instruments storage.
		/// </summary>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="StorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="format">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		/// <returns>The instruments storage.</returns>
		public ISecurityStorage GetSecurityStorage(IMarketDataDrive drive = null, StorageFormats format = StorageFormats.Binary)
		{
			return _securityStorages.SafeAdd(drive ?? DefaultDrive, key => new SecurityStorage(this, key));
		}
	}
}