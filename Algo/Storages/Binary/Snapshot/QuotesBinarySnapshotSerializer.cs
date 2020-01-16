namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Linq;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="QuoteChangeMessage"/>.
	/// </summary>
	public class QuotesBinarySnapshotSerializer : ISnapshotSerializer<SecurityId, QuoteChangeMessage>
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct QuotesSnapshotRow
		{
			public decimal Price;
			public decimal Volume;
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

		Version ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Version { get; } = SnapshotVersions.V21;

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
			};

			var bids = message.Bids.ToArray();
			var asks = message.Asks.ToArray();

			if (MaxDepth != null)
			{
				bids = bids.Take(MaxDepth.Value).ToArray();
				asks = asks.Take(MaxDepth.Value).ToArray();
			}

			snapshot.BidCount = bids.Length;
			snapshot.AskCount = asks.Length;

			var snapshotSize = typeof(QuotesSnapshot).SizeOf();
			var rowSizeOld = typeof(QuotesSnapshotRow).SizeOf();
			var rowSize = rowSizeOld;

			var is21 = version == SnapshotVersions.V21;

			if (is21)
				rowSize += sizeof(int);

			var buffer = new byte[snapshotSize + (bids.Length + asks.Length) * rowSize];

			var ptr = snapshot.StructToPtr();
			ptr.CopyTo(buffer);
			ptr.FreeHGlobal();

			var offset = snapshotSize;

			foreach (var quote in bids.Concat(asks))
			{
				var row = new QuotesSnapshotRow
				{
					Price = quote.Price,
					Volume = quote.Volume,
				};

				var rowPtr = row.StructToPtr(rowSize);

				if (is21)
				{
					rowPtr += rowSizeOld;
					rowPtr.Write(quote.OrdersCount ?? 0);
					rowPtr -= rowSizeOld;
				}

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

				var is21 = version == SnapshotVersions.V21;

				QuoteChange ReadQuote()
				{
					var row = ptr.ToStruct<QuotesSnapshotRow>(true);
					var quote = new QuoteChange(row.Price, row.Volume);

					if (is21)
						quote.OrdersCount = ptr.Read<int>(true).DefaultAsNull();

					return quote;
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
					IsSorted = true,
				};
			}
		}

		SecurityId ISnapshotSerializer<SecurityId, QuoteChangeMessage>.GetKey(QuoteChangeMessage message)
		{
			return message.SecurityId;
		}

		QuoteChangeMessage ISnapshotSerializer<SecurityId, QuoteChangeMessage>.CreateCopy(QuoteChangeMessage message)
		{
			return (QuoteChangeMessage)message.Clone();
		}

		void ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Update(QuoteChangeMessage message, QuoteChangeMessage changes)
		{
			if (!changes.IsSorted)
			{
				message.Bids = changes.Bids.OrderByDescending(q => q.Price).ToArray();
				message.Asks = changes.Asks.OrderBy(q => q.Price).ToArray();
			}
			else
			{
				message.Bids = changes.Bids.ToArray();
				message.Asks = changes.Asks.ToArray();
			}

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<SecurityId, QuoteChangeMessage>.DataType => DataType.MarketDepth;
	}
}