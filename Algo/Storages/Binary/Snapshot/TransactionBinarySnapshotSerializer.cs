namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotSerializer{TMessage}"/> in binary format for <see cref="ExecutionMessage"/>.
	/// </summary>
	public class TransactionBinarySnapshotSerializer : ISnapshotSerializer<ExecutionMessage>
	{
		private const int _snapshotSize = 1024 * 10; // 10kb

		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = _snapshotSize, CharSet = CharSet.Unicode)]
		private struct TransactionSnapshot
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string SecurityId;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
			public string Portfolio;

			public long ServerTime;
			public long LocalTime;

			
		}

		private readonly Version _version = new Version(1, 0);

		Version ISnapshotSerializer<ExecutionMessage>.Version => _version;

		int ISnapshotSerializer<ExecutionMessage>.GetSnapshotSize(Version version) => _snapshotSize;

		string ISnapshotSerializer<ExecutionMessage>.FileName => "transaction_snapshot.bin";

		void ISnapshotSerializer<ExecutionMessage>.Serialize(Version version, ExecutionMessage message, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var snapshot = new TransactionSnapshot
			{
				SecurityId = message.SecurityId.ToStringId(),
				Portfolio = message.PortfolioName,
				ServerTime = message.ServerTime.To<long>(),
				LocalTime = message.LocalTime.To<long>(),
			};

			

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, _snapshotSize);
			Marshal.FreeHGlobal(ptr);
		}

		ExecutionMessage ISnapshotSerializer<ExecutionMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var snapshot = (TransactionSnapshot)Marshal.PtrToStructure(handle.Value.AddrOfPinnedObject(), typeof(TransactionSnapshot));

				var execMsg = new ExecutionMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					PortfolioName = snapshot.Portfolio,
					ServerTime = snapshot.ServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LocalTime.To<DateTimeOffset>(),
				};

				return execMsg;
			}
		}

		SecurityId ISnapshotSerializer<ExecutionMessage>.GetSecurityId(ExecutionMessage message)
		{
			return message.SecurityId;
		}

		void ISnapshotSerializer<ExecutionMessage>.Update(ExecutionMessage message, ExecutionMessage changes)
		{
			

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<ExecutionMessage>.DataType => DataType.Transactions;
	}
}