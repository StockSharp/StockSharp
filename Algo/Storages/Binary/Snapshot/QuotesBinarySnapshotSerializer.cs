namespace StockSharp.Algo.Storages.Binary.Snapshot;

using System.Runtime.InteropServices;

using Ecng.Interop;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="QuoteChangeMessage"/>.
/// </summary>
public class QuotesBinarySnapshotSerializer : ISnapshotSerializer<SecurityId, QuoteChangeMessage>
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct QuotesSnapshotRow
	{
		public BlittableDecimal Price;
		public BlittableDecimal Volume;
		public int OrdersCount;
		public byte QuoteCondition;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	private struct QuotesSnapshot
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string SecurityId;

		public long LastChangeServerTime;
		public long LastChangeLocalTime;

		public int BidCount;
		public int AskCount;

		public long SeqNum;
		public SnapshotDataType? BuildFrom;
	}

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

	Version ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Version { get; } = SnapshotVersions.V22;

	string ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Name => "OrderBook";

	byte[] ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Serialize(Version version, QuoteChangeMessage message)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var snapshot = new QuotesSnapshot
		{
			SecurityId = message.SecurityId.ToStringId().VerifySize(Sizes.S100),
			
			LastChangeServerTime = message.ServerTime.To<long>(),
			LastChangeLocalTime = message.LocalTime.To<long>(),

			BuildFrom = message.BuildFrom == null ? default(SnapshotDataType?) : (SnapshotDataType)message.BuildFrom,
			SeqNum = message.SeqNum,
		};

		var bids = message.Bids.ToArray();
		var asks = message.Asks.ToArray();

		if (MaxDepth != null)
		{
			bids = [.. bids.Take(MaxDepth.Value)];
			asks = [.. asks.Take(MaxDepth.Value)];
		}

		snapshot.BidCount = bids.Length;
		snapshot.AskCount = asks.Length;

		var snapshotSize = typeof(QuotesSnapshot).SizeOf();
		var rowSize = typeof(QuotesSnapshotRow).SizeOf();

		var buffer = new byte[snapshotSize + (bids.Length + asks.Length) * rowSize];

		var ptr = snapshot.StructToPtr();
		ptr.CopyTo(buffer, 0, snapshotSize);
		ptr.FreeHGlobal();

		var offset = snapshotSize;

		foreach (var quote in bids.Concat(asks))
		{
			var row = new QuotesSnapshotRow
			{
				Price = (BlittableDecimal)quote.Price,
				Volume = (BlittableDecimal)quote.Volume,
				OrdersCount = quote.OrdersCount ?? 0,
				QuoteCondition = (byte)quote.Condition,
			};

			var rowPtr = row.StructToPtr(rowSize);

			rowPtr.CopyTo(buffer, offset, rowSize);
			rowPtr.FreeHGlobal();

			offset += rowSize;
		}

		return buffer;
	}

	QuoteChangeMessage ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Deserialize(Version version, byte[] buffer)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		using (var handle = new GCHandle<byte[]>(buffer))
		{
			var ptr = handle.CreatePointer();

			var snapshot = ptr.ToStruct<QuotesSnapshot>(true);

			var bids = new QuoteChange[snapshot.BidCount];
			var asks = new QuoteChange[snapshot.AskCount];

			QuoteChange ReadQuote()
			{
				var row = ptr.ToStruct<QuotesSnapshotRow>(true);
				return new QuoteChange(row.Price, row.Volume, row.OrdersCount.DefaultAsNull(), (QuoteConditions)row.QuoteCondition);
			}

			for (var i = 0; i < snapshot.BidCount; i++)
				bids[i] = ReadQuote();

			for (var i = 0; i < snapshot.AskCount; i++)
				asks[i] = ReadQuote();

			return new QuoteChangeMessage
			{
				SecurityId = snapshot.SecurityId.ToSecurityId(),
				ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
				LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),
				Bids = bids,
				Asks = asks,
				BuildFrom = snapshot.BuildFrom,
				SeqNum = snapshot.SeqNum,
			};
		}
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