namespace StockSharp.Algo.Storages.Binary.Snapshot;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="QuoteChangeMessage"/>.
/// </summary>
public class QuotesBinarySnapshotSerializer : ISnapshotSerializer<SecurityId, QuoteChangeMessage>
{
	private int? _maxDepth;

	/// <summary>
	/// The maximum depth of order book.
	/// </summary>
	public int? MaxDepth
	{
		get => _maxDepth;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			_maxDepth = value;
		}
	}

	Version ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Version { get; } = SnapshotVersions.V24;

	string ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Name => "OrderBook";

	byte[] ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Serialize(Version version, QuoteChangeMessage message)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var bids = message.Bids.ToArray();
		var asks = message.Asks.ToArray();

		if (MaxDepth != null)
		{
			bids = [.. bids.Take(MaxDepth.Value)];
			asks = [.. asks.Take(MaxDepth.Value)];
		}

		// Estimate buffer size
		var secIdBytes = message.SecurityId.ToStringId().UTF8();
		var estimatedSize =
			sizeof(int) + secIdBytes.Length + // SecurityId length + bytes
			sizeof(long) + // LastChangeServerTime
			sizeof(long) + // LastChangeLocalTime
			sizeof(long) + // SeqNum
			sizeof(byte) + (message.BuildFrom != null ? SnapshotDataType.Size : 0) + // BuildFrom
			sizeof(int) + // BidCount
			sizeof(int) + // AskCount
			(bids.Length + asks.Length) * (sizeof(decimal) + sizeof(decimal) + sizeof(int) + sizeof(byte)); // Price + Volume + OrdersCount + Condition per quote

		var buffer = new byte[estimatedSize];
		var writer = new SpanWriter(buffer);

		// Write base fields
		writer.WriteInt32(secIdBytes.Length);
		writer.WriteSpan(secIdBytes);

		writer.WriteInt64(message.ServerTime.To<long>());
		writer.WriteInt64(message.LocalTime.To<long>());
		writer.WriteInt64(message.SeqNum);

		// Write BuildFrom
		writer.WriteBoolean(message.BuildFrom != null);
		if (message.BuildFrom != null)
			((SnapshotDataType)message.BuildFrom).Write(ref writer);

		// Write quotes counts
		writer.WriteInt32(bids.Length);
		writer.WriteInt32(asks.Length);

		// Write bids
		foreach (var quote in bids)
		{
			writer.WriteDecimal(quote.Price);
			writer.WriteDecimal(quote.Volume);
			writer.WriteInt32(quote.OrdersCount ?? 0);
			writer.WriteByte((byte)quote.Condition);
		}

		// Write asks
		foreach (var quote in asks)
		{
			writer.WriteDecimal(quote.Price);
			writer.WriteDecimal(quote.Volume);
			writer.WriteInt32(quote.OrdersCount ?? 0);
			writer.WriteByte((byte)quote.Condition);
		}

		// Return actual written data
		return writer.GetWrittenSpan().ToArray();
	}

	QuoteChangeMessage ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Deserialize(Version version, byte[] buffer)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (buffer == null || buffer.Length == 0)
			throw new ArgumentNullException(nameof(buffer));

		var reader = new SpanReader(buffer);

		// Read base fields
		var secIdLen = reader.ReadInt32();
		var secIdBytes = reader.ReadSpan(secIdLen);
		var securityId = secIdBytes.ToArray().UTF8().ToSecurityId();

		var serverTime = reader.ReadInt64().To<DateTime>();
		var localTime = reader.ReadInt64().To<DateTime>();
		var seqNum = reader.ReadInt64();

		var hasBuildFrom = reader.ReadBoolean();
		SnapshotDataType? buildFrom = null;
		if (hasBuildFrom)
			buildFrom = SnapshotDataType.Read(ref reader);

		// Read quotes counts
		var bidCount = reader.ReadInt32();
		var askCount = reader.ReadInt32();

		var bids = new QuoteChange[bidCount];
		var asks = new QuoteChange[askCount];

		// Read bids
		for (var i = 0; i < bidCount; i++)
		{
			var price = reader.ReadDecimal();
			var volume = reader.ReadDecimal();
			var ordersCount = reader.ReadInt32();
			var condition = (QuoteConditions)reader.ReadByte();

			bids[i] = new QuoteChange(price, volume, ordersCount.DefaultAsNull(), condition);
		}

		// Read asks
		for (var i = 0; i < askCount; i++)
		{
			var price = reader.ReadDecimal();
			var volume = reader.ReadDecimal();
			var ordersCount = reader.ReadInt32();
			var condition = (QuoteConditions)reader.ReadByte();

			asks[i] = new QuoteChange(price, volume, ordersCount.DefaultAsNull(), condition);
		}

		return new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime.UtcKind(),
			LocalTime = localTime.UtcKind(),
			Bids = bids,
			Asks = asks,
			BuildFrom = buildFrom,
			SeqNum = seqNum,
		};
	}

	SecurityId ISnapshotSerializer<SecurityId, QuoteChangeMessage>.GetKey(QuoteChangeMessage message)
	{
		return message.SecurityId;
	}

	void ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Update(QuoteChangeMessage message, QuoteChangeMessage changes)
	{
		message.Bids = [.. changes.Bids];
		message.Asks = [.. changes.Asks];

		if (changes.BuildFrom != default)
			message.BuildFrom = changes.BuildFrom;

		if (changes.SeqNum != default)
			message.SeqNum = changes.SeqNum;

		message.LocalTime = changes.LocalTime;
		message.ServerTime = changes.ServerTime;
	}

	DataType ISnapshotSerializer<SecurityId, QuoteChangeMessage>.DataType => DataType.MarketDepth;
}
