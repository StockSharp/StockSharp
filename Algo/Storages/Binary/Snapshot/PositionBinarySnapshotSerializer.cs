namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotSerializer{TMessage}"/> in binary format for <see cref="PositionChangeMessage"/>.
	/// </summary>
	public class PositionBinarySnapshotSerializer : ISnapshotSerializer<PositionChangeMessage>
	{
		private const int _snapshotSize = 1024 * 10; // 10kb

		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = _snapshotSize, CharSet = CharSet.Unicode)]
		private struct PositionSnapshot
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string SecurityId;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string Portfolio;

			public long ServerTime;
			public long LocalTime;

			
		}

		private readonly Version _version = new Version(1, 0);

		Version ISnapshotSerializer<PositionChangeMessage>.Version => _version;

		int ISnapshotSerializer<PositionChangeMessage>.GetSnapshotSize(Version version) => _snapshotSize;

		string ISnapshotSerializer<PositionChangeMessage>.FileName => "position_snapshot.bin";

		void ISnapshotSerializer<PositionChangeMessage>.Serialize(Version version, PositionChangeMessage message, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var snapshot = new PositionSnapshot
			{
				SecurityId = message.SecurityId.ToStringId(),
				Portfolio = message.PortfolioName,
				ServerTime = message.ServerTime.To<long>(),
				LocalTime = message.LocalTime.To<long>(),
			};

			foreach (var change in message.Changes)
			{
				
			}

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, _snapshotSize);
			Marshal.FreeHGlobal(ptr);
		}

		PositionChangeMessage ISnapshotSerializer<PositionChangeMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var snapshot = (PositionSnapshot)Marshal.PtrToStructure(handle.Value.AddrOfPinnedObject(), typeof(PositionSnapshot));

				var posMsg = new PositionChangeMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					PortfolioName = snapshot.Portfolio,
					ServerTime = snapshot.ServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LocalTime.To<DateTimeOffset>(),
				};

				return posMsg;
			}
		}

		SecurityId ISnapshotSerializer<PositionChangeMessage>.GetSecurityId(PositionChangeMessage message)
		{
			return message.SecurityId;
		}

		void ISnapshotSerializer<PositionChangeMessage>.Update(PositionChangeMessage message, PositionChangeMessage changes)
		{
			foreach (var pair in changes.Changes)
			{
				message.Changes[pair.Key] = pair.Value;
			}

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<PositionChangeMessage>.DataType => DataType.PositionChanges;
	}
}