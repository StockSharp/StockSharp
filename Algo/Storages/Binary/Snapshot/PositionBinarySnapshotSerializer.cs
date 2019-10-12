namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="PositionChangeMessage"/>.
	/// </summary>
	public class PositionBinarySnapshotSerializer : ISnapshotSerializer<SecurityId, PositionChangeMessage>
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
		private struct PositionSnapshot
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			public string SecurityId;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			public string Portfolio;

			public long LastChangeServerTime;
			public long LastChangeLocalTime;

			public decimal? BeginValue;
			public decimal? CurrentValue;
			public decimal? BlockedValue;
			public decimal? CurrentPrice;
			public decimal? AveragePrice;
			public decimal? UnrealizedPnL;
			public decimal? RealizedPnL;
			public decimal? VariationMargin;
			public short? Currency;
			public decimal? Leverage;
			public decimal? Commission;
			public decimal? CurrentValueInLots;
			public byte? State;
		}

		Version ISnapshotSerializer<SecurityId, PositionChangeMessage>.Version { get; } = SnapshotVersions.V20;

		string ISnapshotSerializer<SecurityId, PositionChangeMessage>.Name => "Positions";

		byte[] ISnapshotSerializer<SecurityId, PositionChangeMessage>.Serialize(Version version, PositionChangeMessage message)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var snapshot = new PositionSnapshot
			{
				SecurityId = message.SecurityId.ToStringId().VerifySize(Sizes.S100),
				Portfolio = message.PortfolioName.VerifySize(Sizes.S100),
				LastChangeServerTime = message.ServerTime.To<long>(),
				LastChangeLocalTime = message.LocalTime.To<long>(),
			};

			foreach (var change in message.Changes)
			{
				switch (change.Key)
				{
					case PositionChangeTypes.BeginValue:
						snapshot.BeginValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentValue:
						snapshot.CurrentValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.BlockedValue:
						snapshot.BlockedValue = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentPrice:
						snapshot.CurrentPrice = (decimal)change.Value;
						break;
					case PositionChangeTypes.AveragePrice:
						snapshot.AveragePrice = (decimal)change.Value;
						break;
					case PositionChangeTypes.UnrealizedPnL:
						snapshot.UnrealizedPnL = (decimal)change.Value;
						break;
					case PositionChangeTypes.RealizedPnL:
						snapshot.RealizedPnL = (decimal)change.Value;
						break;
					case PositionChangeTypes.VariationMargin:
						snapshot.VariationMargin = (decimal)change.Value;
						break;
					case PositionChangeTypes.Currency:
						snapshot.Currency = (short)(CurrencyTypes)change.Value;
						break;
					case PositionChangeTypes.Leverage:
						snapshot.Leverage = (decimal)change.Value;
						break;
					case PositionChangeTypes.Commission:
						snapshot.Commission = (decimal)change.Value;
						break;
					case PositionChangeTypes.CurrentValueInLots:
						snapshot.CurrentValueInLots = (decimal)change.Value;
						break;
					case PositionChangeTypes.State:
						snapshot.State = (byte)(PortfolioStates)change.Value;
						break;
				}
			}

			var buffer = new byte[typeof(PositionSnapshot).SizeOf()];

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, buffer.Length);
			Marshal.FreeHGlobal(ptr);

			return buffer;
		}

		PositionChangeMessage ISnapshotSerializer<SecurityId, PositionChangeMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var snapshot = handle.Value.AddrOfPinnedObject().ToStruct<PositionSnapshot>();

				var posMsg = new PositionChangeMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					PortfolioName = snapshot.Portfolio,
					ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),
				}
				.TryAdd(PositionChangeTypes.BeginValue, snapshot.BeginValue, true)
				.TryAdd(PositionChangeTypes.CurrentValue, snapshot.CurrentValue, true)
				.TryAdd(PositionChangeTypes.BlockedValue, snapshot.BlockedValue, true)
				.TryAdd(PositionChangeTypes.CurrentPrice, snapshot.CurrentPrice, true)
				.TryAdd(PositionChangeTypes.AveragePrice, snapshot.AveragePrice, true)
				.TryAdd(PositionChangeTypes.UnrealizedPnL, snapshot.UnrealizedPnL, true)
				.TryAdd(PositionChangeTypes.RealizedPnL, snapshot.RealizedPnL, true)
				.TryAdd(PositionChangeTypes.VariationMargin, snapshot.VariationMargin, true)
				.TryAdd(PositionChangeTypes.Leverage, snapshot.Leverage, true)
				.TryAdd(PositionChangeTypes.Commission, snapshot.Commission, true)
				.TryAdd(PositionChangeTypes.CurrentValueInLots, snapshot.CurrentValueInLots, true)
				;

				if (snapshot.Currency != null)
					posMsg.Add(PositionChangeTypes.Currency, (CurrencyTypes)snapshot.Currency.Value);

				if (snapshot.State != null)
					posMsg.Add(PositionChangeTypes.State, (PortfolioStates)snapshot.State.Value);

				return posMsg;
			}
		}

		SecurityId ISnapshotSerializer<SecurityId, PositionChangeMessage>.GetKey(PositionChangeMessage message)
		{
			return message.SecurityId;
		}

		PositionChangeMessage ISnapshotSerializer<SecurityId, PositionChangeMessage>.CreateCopy(PositionChangeMessage message)
		{
			return (PositionChangeMessage)message.Clone();
		}

		void ISnapshotSerializer<SecurityId, PositionChangeMessage>.Update(PositionChangeMessage message, PositionChangeMessage changes)
		{
			foreach (var pair in changes.Changes)
			{
				message.Changes[pair.Key] = pair.Value;
			}

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<SecurityId, PositionChangeMessage>.DataType => DataType.PositionChanges;
	}
}