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
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
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
		/// To get the candles storage the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The candles storage.</returns>
		IMarketDataStorage<CandleMessage> GetCandleStorage(Type candleType, object arg, IMarketDataSerializer<CandleMessage> serializer);

		/// <summary>
		/// To get the transactions storage the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The transactions storage.</returns>
		IMarketDataStorage<ExecutionMessage> GetTransactionStorage(IMarketDataSerializer<ExecutionMessage> serializer);

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="Candle.Arg"/>.</param>
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

			protected override void Write(TextWriter writer, QuoteChangeMessage data)
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
				var prevId = (long)metaInfo.LastId;
				var prevTime = metaInfo.LastTime.ApplyTimeZone(TimeZoneInfo.Utc);

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

				return messages.ToCandles<TCandle>(Security);
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

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMarketDataDrive"/>.
		/// </summary>
		/// <param name="drive">The storage (database, file etc.).</param>
		/// <param name="security">Security.</param>
		public SecurityMarketDataDrive(IMarketDataDrive drive, Security security)
		{
			if (drive == null)
				throw new ArgumentNullException(nameof(drive));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Drive = drive;
			Security = security;
			SecurityId = security.ToSecurityId();
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
			return Drive.GetStorageDrive(Security.ToSecurityId(), messageType, arg, ToFormat(serializer));
		}

		/// <summary>
		/// Instrument identifier.
		/// </summary>
		public SecurityId SecurityId { get; }

		/// <summary>
		/// To get the storage of tick trades for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of tick trades.</returns>
		public IMarketDataStorage<ExecutionMessage> GetTickStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new TradeStorage(Security, GetStorageDrive(serializer, ExecutionTypes.Tick), serializer);
		}

		/// <summary>
		/// To get the storage of order books for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The order books storage.</returns>
		public IMarketDataStorage<QuoteChangeMessage> GetQuoteStorage(IMarketDataSerializer<QuoteChangeMessage> serializer)
		{
			return new MarketDepthStorage(Security, GetStorageDrive(serializer), serializer);
		}

		/// <summary>
		/// To get the storage of orders log for the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of orders log.</returns>
		public IMarketDataStorage<ExecutionMessage> GetOrderLogStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new OrderLogStorage(Security, GetStorageDrive(serializer, ExecutionTypes.OrderLog), serializer);
		}

		/// <summary>
		/// To get the storage of level1 data.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The storage of level1 data.</returns>
		public IMarketDataStorage<Level1ChangeMessage> GetLevel1Storage(IMarketDataSerializer<Level1ChangeMessage> serializer)
		{
			return new Level1Storage(Security, GetStorageDrive(serializer), serializer);
		}

		/// <summary>
		/// To get the candles storage the specified instrument.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="arg">Candle arg.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The candles storage.</returns>
		public IMarketDataStorage<CandleMessage> GetCandleStorage(Type candleType, object arg, IMarketDataSerializer<CandleMessage> serializer)
		{
			if (candleType == null)
				throw new ArgumentNullException(nameof(candleType));

			if (!candleType.IsCandleMessage())
				throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

			return typeof(CandleStorage<,>).Make(candleType, candleType.ToCandleType()).CreateInstance<IMarketDataStorage<CandleMessage>>(Security, arg, GetStorageDrive(serializer, candleType, arg), serializer);
		}

		/// <summary>
		/// To get the transactions storage the specified instrument.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The transactions storage.</returns>
		public IMarketDataStorage<ExecutionMessage> GetTransactionStorage(IMarketDataSerializer<ExecutionMessage> serializer)
		{
			return new TransactionStorage(Security, GetStorageDrive(serializer, ExecutionTypes.Transaction), serializer);
		}

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="Candle.Arg"/>.</param>
		/// <param name="serializer">The serializer.</param>
		/// <returns>Market-data storage.</returns>
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
						throw new ArgumentOutOfRangeException(nameof(arg));
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