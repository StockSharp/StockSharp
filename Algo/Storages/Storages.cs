namespace StockSharp.Algo.Storages;

using Ecng.Reflection;

class TradeStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
	: MarketDataStorage<ExecutionMessage, DateTime>(securityId, DataType.Ticks, trade => trade.SecurityId, trade => trade.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => true)
{
	protected override IEnumerable<ExecutionMessage> FilterNewData(IEnumerable<ExecutionMessage> data, IMarketDataMetaInfo metaInfo)
	{
		var prevId = (long?)metaInfo.LastId ?? 0;
		var prevTime = metaInfo.LastTime.UtcKind();

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

class MarketDepthStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<QuoteChangeMessage> serializer)
	: MarketDataStorage<QuoteChangeMessage, DateTime>(securityId, DataType.MarketDepth, depth => depth.SecurityId, depth => depth.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => true)
{
}

class OrderLogStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
	: MarketDataStorage<ExecutionMessage, long>(securityId, DataType.OrderLog, item => item.SecurityId, item => item.TransactionId, serializer, drive, m => true)
{
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

class CandleStorage<TCandleMessage> :
	MarketDataStorage<TCandleMessage, DateTime>,
	IMarketDataStorage<CandleMessage>
	where TCandleMessage : CandleMessage, new()
{
	private class CandleSerializer(IMarketDataSerializer<TCandleMessage> serializer) : IMarketDataSerializer<CandleMessage>
	{
		private readonly IMarketDataSerializer<TCandleMessage> _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

		StorageFormats IMarketDataSerializer.Format => _serializer.Format;

		TimeSpan IMarketDataSerializer.TimePrecision => _serializer.TimePrecision;

		IMarketDataMetaInfo IMarketDataSerializer.CreateMetaInfo(DateTime date) => _serializer.CreateMetaInfo(date);

		ValueTask IMarketDataSerializer.SerializeAsync(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
			=> _serializer.SerializeAsync(stream, data, metaInfo, cancellationToken);

		IAsyncEnumerable<CandleMessage> IMarketDataSerializer<CandleMessage>.DeserializeAsync(Stream stream, IMarketDataMetaInfo metaInfo)
			=> _serializer.DeserializeAsync(stream, metaInfo);

		ValueTask IMarketDataSerializer<CandleMessage>.SerializeAsync(Stream stream, IEnumerable<CandleMessage> data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
			=> _serializer.SerializeAsync(stream, data, metaInfo, cancellationToken);
	}

	private readonly CandleSerializer _serializer;

	public CandleStorage(SecurityId securityId, DataType dt, IMarketDataStorageDrive drive, IMarketDataSerializer<TCandleMessage> serializer)
		: base(securityId, dt, candle => candle.SecurityId, candle => candle.OpenTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => true)
	{
		_serializer = new CandleSerializer(Serializer);
	}

	protected override IEnumerable<TCandleMessage> FilterNewData(IEnumerable<TCandleMessage> data, IMarketDataMetaInfo metaInfo)
	{
		var lastTime = metaInfo.LastTime.UtcKind();

		foreach (var msg in data)
		{
			if (msg.State == CandleStates.Active)
				continue;

			var time = GetTruncatedTime(msg).UtcKind();

			if ((msg is TimeFrameCandleMessage && time <= lastTime) || time < lastTime)
				continue;

			lastTime = time;
			yield return msg;
		}
	}

	IAsyncEnumerable<CandleMessage> IMarketDataStorage<CandleMessage>.LoadAsync(DateTime date)
		=> LoadAsync(date);

	IMarketDataSerializer<CandleMessage> IMarketDataStorage<CandleMessage>.Serializer => _serializer;

	ValueTask<int> IMarketDataStorage<CandleMessage>.SaveAsync(IEnumerable<CandleMessage> data, CancellationToken cancellationToken)
		=> SaveAsync(data.Cast<TCandleMessage>(), cancellationToken);

	ValueTask IMarketDataStorage<CandleMessage>.DeleteAsync(IEnumerable<CandleMessage> data, CancellationToken cancellationToken)
		=> DeleteAsync(data.Cast<TCandleMessage>(), cancellationToken);
}

class Level1Storage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<Level1ChangeMessage> serializer)
	: MarketDataStorage<Level1ChangeMessage, DateTime>(securityId, DataType.Level1, value => value.SecurityId, value => value.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => m.HasChanges())
{
}

class PositionChangeStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<PositionChangeMessage> serializer)
	: MarketDataStorage<PositionChangeMessage, DateTime>(securityId, DataType.PositionChanges, value => value.SecurityId, value => value.ServerTime.StorageTruncate(serializer.TimePrecision), serializer, drive, m => m.HasChanges())
{
}

class TransactionStorage : MarketDataStorage<ExecutionMessage, long>
{
	public TransactionStorage(SecurityId securityId, IMarketDataStorageDrive drive, IMarketDataSerializer<ExecutionMessage> serializer)
		: base(securityId, DataType.Transactions, msg => msg.SecurityId, msg => msg.TransactionId, serializer, drive, m => true)
	{
		AppendOnlyNew = false;
	}
}

class NewsStorage(SecurityId securityId, IMarketDataSerializer<NewsMessage> serializer, IMarketDataStorageDrive drive)
	: MarketDataStorage<NewsMessage, VoidType>(securityId, DataType.News, m => default, m => null, serializer, drive, m => true)
{
}

class BoardStateStorage(SecurityId securityId, IMarketDataSerializer<BoardStateMessage> serializer, IMarketDataStorageDrive drive)
	: MarketDataStorage<BoardStateMessage, VoidType>(securityId, DataType.BoardState, m => default, m => null, serializer, drive, m => true)
{
}