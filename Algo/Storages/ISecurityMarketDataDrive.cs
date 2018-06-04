#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ISecurityMarketDataDrive.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages.Csv;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing the storage for instrument.
	/// </summary>
	public interface ISecurityMarketDataDrive
	{
		/// <summary>
		/// Instrument identifier.
		/// </summary>
		SecurityId SecurityId { get; }

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of tick trades.</returns>
		IMarketDataStorage<ExecutionMessage> GetTickStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The order books storage.</returns>
		IMarketDataStorage<QuoteChangeMessage> GetQuoteStorage(IMarketDataSerializer<QuoteChangeMessage> serializer);

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of orders log.</returns>
		IMarketDataStorage<ExecutionMessage> GetOrderLogStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the storage of level1 data.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of level1 data.</returns>
		IMarketDataStorage<Level1ChangeMessage> GetLevel1Storage(IMarketDataSerializer<Level1ChangeMessage> serializer);

		/// <summary>
		/// To get the storage of position changes data.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of position changes data.</returns>
		IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(IMarketDataSerializer<PositionChangeMessage> serializer);

		/// <summary>
		/// To get the candles storage for the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The candles storage.</returns>
		IMarketDataStorage<CandleMessage> GetCandleStorage(Type candleType, object arg, IMarketDataSerializer<CandleMessage> serializer);

		/// <summary>
		/// To get the transactions storage for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The transactions storage.</returns>
		IMarketDataStorage<ExecutionMessage> GetTransactionStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>Market-data storage.</returns>
		IMarketDataStorage GetStorage(Type dataType, object arg, IMarketDataSerializer serializer);

		///// <summary>
		///// To get available candles types with parameters for the instrument.
		///// </summary>
		///// <param name="serializer">The serializer.</param>
		///// <returns>Available candles types with parameters.</returns>
		//IEnumerable<Tuple<Type, object[]>> GetCandleTypes(IMarketDataSerializer<CandleMessage> serializer);
	}

	/// <summary>
	/// The storage for the instrument.
	/// </summary>
	public class SecurityMarketDataDrive : ISecurityMarketDataDrive
	{

		private abstract class ConvertableStorage<TMessage, TEntity, TId> : MarketDataStorage<TMessage, TId>, IMarketDataStorage<TEntity>, IMarketDataStorageInfo<TEntity>
			where TMessage : Message
		{
			private readonly SecurityMarketDataDrive _parent;

			protected ConvertableStorage(SecurityMarketDataDrive parent, Security security, SecurityId securityId, object arg, Func<TMessage, DateTimeOffset> getTime, Func<TMessage, SecurityId> getSecurity, Func<TMessage, TId> getId, IMarketDataSerializer<TMessage> serializer, IMarketDataStorageDrive drive)
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

		private sealed class TradeStorage : ConvertableStorage<ExecutionMessage, Trade, long>
		{
			public TradeStorage(SecurityMarketDataDrive parent, Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
				: base(parent, security, securityId, ExecutionTypes.Tick, trade => trade.ServerTime, trade => trade.SecurityId, trade => trade.TradeId ?? 0, serializer, drive)
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
			public MarketDepthStorage(SecurityMarketDataDrive parent, Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<QuoteChangeMessage> serializer)
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
			public OrderLogStorage(SecurityMarketDataDrive parent, Security security, SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
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
			protected CandleMessageStorage(Security security, SecurityId securityId, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
				: base(security, securityId, arg, candle => candle.OpenTime, candle => candle.SecurityId, candle => candle.OpenTime.StorageTruncate(serializer.TimePrecision), serializer, drive)
			{
			}

			IEnumerable<CandleMessage> IMarketDataStorage<CandleMessage>.Load(DateTime date)
			{
				return Load(date);
			}

			IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => throw new NotSupportedException();

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

				return messages.ToCandles<TCandle>(Security);
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

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMarketDataDrive"/>.
		/// </summary>
		/// <param name="drive">The storage (database, file etc.).</param>
		/// <param name="security">Security.</param>
		public SecurityMarketDataDrive(IMarketDataDrive drive, Security security)
		{
			Drive = drive ?? throw new ArgumentNullException(nameof(drive));
			Security = security ?? throw new ArgumentNullException(nameof(security));
			SecurityId = security.ToSecurityId();
			SecurityId.EnsureHashCode();
		}

		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive { get; }

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; }

		private static StorageFormats ToFormat(IMarketDataSerializer serializer)
		{
			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			return serializer.GetType().GetGenericType(typeof(CsvMarketDataSerializer<>)) != null
				? StorageFormats.Csv
				: StorageFormats.Binary;
		}

		private IMarketDataStorageDrive GetStorageDrive<TMessage>(IMarketDataSerializer<TMessage> serializer, object arg = null)
			where TMessage : Message
		{
			return GetStorageDrive(serializer, typeof(TMessage), arg);
		}

		private IMarketDataStorageDrive GetStorageDrive(IMarketDataSerializer serializer, Type messageType, object arg = null)
		{
			return Drive.GetStorageDrive(SecurityId, messageType, arg, ToFormat(serializer));
		}

		/// <summary>
		/// Instrument identifier.
		/// </summary>
		public SecurityId SecurityId { get; }

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
		public IMarketDataStorage<ExecutionMessage> GetTickStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new TradeStorage(this, Security, SecurityId, GetStorageDrive(serializer, ExecutionTypes.Tick), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteStorage(IMarketDataSerializer<QuoteChangeMessage> serializer)
		{
			return new MarketDepthStorage(this, Security, SecurityId, GetStorageDrive(serializer), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetOrderLogStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new OrderLogStorage(this, Security, SecurityId, GetStorageDrive(serializer, ExecutionTypes.OrderLog), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<Level1ChangeMessage> GetLevel1Storage(IMarketDataSerializer<Level1ChangeMessage> serializer)
		{
			return new Level1Storage(Security, SecurityId, GetStorageDrive(serializer), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<PositionChangeMessage> GetPositionMessageStorage(IMarketDataSerializer<PositionChangeMessage> serializer)
		{
			return new PositionStorage(Security, SecurityId, GetStorageDrive(serializer), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<CandleMessage> GetCandleStorage(Type candleType, object arg, IMarketDataSerializer<CandleMessage> serializer)
		{
			if (candleType == null)
				throw new ArgumentNullException(nameof(candleType));

			if (!candleType.IsCandleMessage())
				throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

			return typeof(CandleStorage<,>).Make(candleType, candleType.ToCandleType()).CreateInstance<IMarketDataStorage<CandleMessage>>(Security, SecurityId, arg, GetStorageDrive(serializer, candleType, arg), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new TransactionStorage(Security, SecurityId, GetStorageDrive(serializer, ExecutionTypes.Transaction), serializer);
		}

		/// <inheritdoc />
		public IMarketDataStorage GetStorage(Type dataType, object arg, IMarketDataSerializer serializer)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (!dataType.IsSubclassOf(typeof(Message)))
				dataType = dataType.ToMessageType(ref arg);

			if (dataType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						return GetTickStorage((IMarketDataSerializer<ExecutionMessage>)serializer);
					case ExecutionTypes.Transaction:
						return GetTransactionStorage((IMarketDataSerializer<ExecutionMessage>)serializer);
					case ExecutionTypes.OrderLog:
						return GetTransactionStorage((IMarketDataSerializer<ExecutionMessage>)serializer);
					default:
						throw new ArgumentOutOfRangeException(nameof(arg), arg, LocalizedStrings.Str1219);
				}
			}
			else if (dataType == typeof(Level1ChangeMessage))
				return GetLevel1Storage((IMarketDataSerializer<Level1ChangeMessage>)serializer);
			else if (dataType == typeof(QuoteChangeMessage))
				return GetQuoteStorage((IMarketDataSerializer<QuoteChangeMessage>)serializer);
			else if (dataType.IsCandleMessage())
				return GetCandleStorage(dataType, arg, (IMarketDataSerializer<CandleMessage>)serializer);
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1018);
		}

		///// <summary>
		///// To get available candles types with parameters for the instrument.
		///// </summary>
		///// <param name="serializer">The serializer.</param>
		///// <returns>Available candles types with parameters.</returns>
		//public IEnumerable<Tuple<Type, object[]>> GetCandleTypes(IMarketDataSerializer<CandleMessage> serializer)
		//{
		//	return Drive.GetCandleTypes(Security.ToSecurityId(), ToFormat(serializer));
		//}
	}
}