namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Collections.Generic;
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
		//private const int _snapshotSize = 1024 * 10; // 10kb

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct QuotesSnapshotRow
		{
			public decimal Price;
			public decimal Volume;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = _snapshotSize*/, CharSet = CharSet.Unicode)]
		private struct QuotesSnapshot
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string SecurityId;

			public long LastChangeServerTime;
			public long LastChangeLocalTime;

			public int BidCount;
			public int AskCount;
		}

		//private const int _rowsOffset = 224;

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

		Version ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Version { get; } = new Version(2, 0);

		//int ISnapshotSerializer<SecurityId, QuoteChangeMessage>.GetSnapshotSize(Version version) => _snapshotSize;

		string ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Name => "OrderBook";

		byte[] ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Serialize(Version version, QuoteChangeMessage message)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var snapshot = new QuotesSnapshot
			{
				SecurityId = message.SecurityId.ToStringId(),
				
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
			var rowSize = typeof(QuotesSnapshotRow).SizeOf();

			var buffer = new byte[snapshotSize + (bids.Length + asks.Length) * rowSize];

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, snapshotSize);
			Marshal.FreeHGlobal(ptr);

			var offset = snapshotSize;

			foreach (var quote in bids.Concat(asks))
			{
				var row = new QuotesSnapshotRow
				{
					Price = quote.Price,
					Volume = quote.Volume,
				};

				var rowPtr = row.StructToPtr();
				Marshal.Copy(rowPtr, buffer, offset, rowSize);
				Marshal.FreeHGlobal(rowPtr);

				offset += rowSize;
			}

			return buffer;
		}

		QuoteChangeMessage ISnapshotSerializer<SecurityId, QuoteChangeMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var ptr = handle.Value.AddrOfPinnedObject();

				var snapshot = ptr.ToStruct<QuotesSnapshot>();

				var bids = new List<QuoteChange>();
				var asks = new List<QuoteChange>();

				var quotesMsg = new QuoteChangeMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),
					Bids = bids,
					Asks = asks,
					IsSorted = true,
				};

				ptr += typeof(QuotesSnapshot).SizeOf();

				var rowSize = Marshal.SizeOf(typeof(QuotesSnapshotRow));

				for (var i = 0; i < snapshot.BidCount; i++)
				{
					var row = ptr.ToStruct<QuotesSnapshotRow>();
					bids.Add(new QuoteChange(Sides.Buy, row.Price, row.Volume));
					ptr += rowSize;
				}

				for (var i = 0; i < snapshot.AskCount; i++)
				{
					var row = ptr.ToStruct<QuotesSnapshotRow>();
					asks.Add(new QuoteChange(Sides.Sell, row.Price, row.Volume));
					ptr += rowSize;
				}

				return quotesMsg;
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