namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection;

	using StockSharp.Messages;

	class TradeStorage : MarketDataStorage<ExecutionMessage, DateTimeOffset>
	{
		public TradeStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
			: base(securityId, ExecutionTypes.Tick, trade => trade.ServerTime, trade => trade.SecurityId, trade => trade.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => true)
		{
		}

		protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
		{
			var prevId = (long?)metaInfo.LastId ?? 0;
			var prevTime = metaInfo.LastTime.ApplyUtc();

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
	}

	class MarketDepthStorage : MarketDataStorage<QuoteChangeMessage, DateTimeOffset>
	{
		public MarketDepthStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<QuoteChangeMessage> serializer)
			: base(securityId, null, depth => depth.ServerTime, depth => depth.SecurityId, depth => depth.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => true)
		{
		}
	}

	class OrderLogStorage : MarketDataStorage<ExecutionMessage, long>
	{
		public OrderLogStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
			: base(securityId, ExecutionTypes.OrderLog, item => item.ServerTime, item => item.SecurityId, item => item.TransactionId, serializer, drive, m => true)
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
	}

	// http://stackoverflow.com/a/15316996
	class CandleStorage<TCandleMessage> :
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

		public CandleStorage(SecurityId securityId, object arg, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
			: base(securityId, arg, candle => candle.OpenTime, candle => candle.SecurityId, candle => candle.OpenTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => true)
		{
			_serializer = new CandleSerializer(Serializer);
		}

		protected override IEnumerable<TCandleMessage> FilterNewData(IEnumerable<TCandleMessage> data, IMarketDataMetaInfo metaInfo)
		{
			var lastTime = metaInfo.LastTime.ApplyUtc();

			foreach (var msg in data)
			{
				if (msg.State == CandleStates.Active)
					continue;

				var time = GetTruncatedTime(msg).ApplyUtc();

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

	class Level1Storage : MarketDataStorage<Level1ChangeMessage, DateTimeOffset>
	{
		public Level1Storage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<Level1ChangeMessage> serializer)
			: base(securityId, null, value => value.ServerTime, value => value.SecurityId, value => value.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => m.Changes.Count > 0)
		{
		}
	}

	class PositionChangeStorage : MarketDataStorage<PositionChangeMessage, DateTimeOffset>
	{
		public PositionChangeStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<PositionChangeMessage> serializer)
			: base(securityId, null, value => value.ServerTime, value => value.SecurityId, value => value.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => m.Changes.Count > 0)
		{
		}
	}

	class TransactionStorage : MarketDataStorage<ExecutionMessage, long>
	{
		public TransactionStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
			: base(securityId, ExecutionTypes.Transaction, msg => msg.ServerTime, msg => msg.SecurityId, msg => msg.TransactionId, serializer, drive, m => true)
		{
			AppendOnlyNew = false;
		}
	}

	class NewsStorage : MarketDataStorage<NewsMessage, VoidType>
	{
		public NewsStorage(SecurityId securityId, IMarketDataSerializer<NewsMessage> serializer, IMarketDataStorageDrive drive)
			: base(securityId, null, m => m.ServerTime, m => default, m => null, serializer, drive, m => true)
		{
		}
	}

	class BoardStateStorage : MarketDataStorage<BoardStateMessage, VoidType>
	{
		public BoardStateStorage(SecurityId securityId, IMarketDataSerializer<BoardStateMessage> serializer, IMarketDataStorageDrive drive)
			: base(securityId, null, m => m.ServerTime, m => default, m => null, serializer, drive, m => true)
		{
		}
	}
}