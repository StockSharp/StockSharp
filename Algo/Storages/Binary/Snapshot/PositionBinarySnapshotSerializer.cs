namespace StockSharp.Algo.Storages.Binary.Snapshot;

using System.Runtime.InteropServices;

using Ecng.Interop;

using Key = System.ValueTuple<Messages.SecurityId, string, string>;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="PositionChangeMessage"/>.
/// </summary>
public class PositionBinarySnapshotSerializer : ISnapshotSerializer<Key, PositionChangeMessage>
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

		public BlittableDecimal? BeginValue;
		public BlittableDecimal? CurrentValue;
		public BlittableDecimal? BlockedValue;
		public BlittableDecimal? CurrentPrice;
		public BlittableDecimal? AveragePrice;
		public BlittableDecimal? UnrealizedPnL;
		public BlittableDecimal? RealizedPnL;
		public BlittableDecimal? VariationMargin;
		public short? Currency;
		public BlittableDecimal? Leverage;
		public BlittableDecimal? Commission;
		public BlittableDecimal? CurrentValueInLots;
		public byte? State;
		public long? ExpirationDate;
		public BlittableDecimal? CommissionTaker;
		public BlittableDecimal? CommissionMaker;
		public BlittableDecimal? SettlementPrice;
		public int? BuyOrdersCount;
		public int? SellOrdersCount;
		public BlittableDecimal? BuyOrdersMargin;
		public BlittableDecimal? SellOrdersMargin;
		public BlittableDecimal? OrdersMargin;
		public int? OrdersCount;
		public int? TradesCount;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string DepoName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string BoardCode;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string ClientCode;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string Description;

		public int? LimitType;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string StrategyId;

		public int? Side;

		public SnapshotDataType? BuildFrom;

		public BlittableDecimal? LiquidationPrice;
	}

	Version ISnapshotSerializer<Key, PositionChangeMessage>.Version { get; } = SnapshotVersions.V24;

	string ISnapshotSerializer<Key, PositionChangeMessage>.Name => "Positions";

	byte[] ISnapshotSerializer<Key, PositionChangeMessage>.Serialize(Version version, PositionChangeMessage message)
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
			DepoName = message.DepoName,
			LimitType = (int?)message.LimitType,
			BoardCode = message.BoardCode,
			ClientCode = message.ClientCode,
			Description = message.Description,
			StrategyId = message.StrategyId,
			Side = (int?)message.Side,
			BuildFrom = message.BuildFrom == null ? default(SnapshotDataType?) : (SnapshotDataType)message.BuildFrom,
		};

		foreach (var change in message.Changes)
		{
			switch (change.Key)
			{
				case PositionChangeTypes.BeginValue:
					snapshot.BeginValue = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.CurrentValue:
					snapshot.CurrentValue = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.BlockedValue:
					snapshot.BlockedValue = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.CurrentPrice:
					snapshot.CurrentPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.AveragePrice:
					snapshot.AveragePrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.UnrealizedPnL:
					snapshot.UnrealizedPnL = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.RealizedPnL:
					snapshot.RealizedPnL = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.VariationMargin:
					snapshot.VariationMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.Currency:
					snapshot.Currency = (short)(CurrencyTypes)change.Value;
					break;
				case PositionChangeTypes.Leverage:
					snapshot.Leverage = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.Commission:
					snapshot.Commission = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.CurrentValueInLots:
					snapshot.CurrentValueInLots = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.State:
					snapshot.State = (byte)(PortfolioStates)change.Value;
					break;
				case PositionChangeTypes.ExpirationDate:
					snapshot.ExpirationDate = change.Value.To<long?>();
					break;
				case PositionChangeTypes.CommissionTaker:
					snapshot.CommissionTaker = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.CommissionMaker:
					snapshot.CommissionMaker = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.SettlementPrice:
					snapshot.SettlementPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.BuyOrdersCount:
					snapshot.BuyOrdersCount = (int)change.Value;
					break;
				case PositionChangeTypes.SellOrdersCount:
					snapshot.SellOrdersCount = (int)change.Value;
					break;
				case PositionChangeTypes.BuyOrdersMargin:
					snapshot.BuyOrdersMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.SellOrdersMargin:
					snapshot.SellOrdersMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.OrdersMargin:
					snapshot.OrdersMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case PositionChangeTypes.OrdersCount:
					snapshot.OrdersCount = (int)change.Value;
					break;
				case PositionChangeTypes.TradesCount:
					snapshot.TradesCount = (int)change.Value;
					break;
				case PositionChangeTypes.LiquidationPrice:
					snapshot.LiquidationPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				default:
					throw new InvalidOperationException(change.Key.To<string>());
			}
		}

		var buffer = new byte[typeof(PositionSnapshot).SizeOf()];

		var ptr = snapshot.StructToPtr();
		ptr.CopyTo(buffer);
		ptr.FreeHGlobal();

		return buffer;
	}

	PositionChangeMessage ISnapshotSerializer<Key, PositionChangeMessage>.Deserialize(Version version, byte[] buffer)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		using (var handle = new GCHandle<byte[]>(buffer))
		{
			var snapshot = handle.CreatePointer().ToStruct<PositionSnapshot>(true);

			var posMsg = new PositionChangeMessage
			{
				SecurityId = snapshot.SecurityId.ToSecurityId(),
				PortfolioName = snapshot.Portfolio,
				ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
				LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),
				ClientCode = snapshot.ClientCode,
				DepoName = snapshot.DepoName,
				BoardCode = snapshot.BoardCode,
				LimitType = (TPlusLimits?)snapshot.LimitType,
				Description = snapshot.Description,
				StrategyId = snapshot.StrategyId,
				Side = (Sides?)snapshot.Side,
				BuildFrom = snapshot.BuildFrom,
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
			.TryAdd(PositionChangeTypes.CommissionTaker, snapshot.CommissionTaker, true)
			.TryAdd(PositionChangeTypes.CommissionMaker, snapshot.CommissionMaker, true)
			.TryAdd(PositionChangeTypes.SettlementPrice, snapshot.SettlementPrice, true)
			.TryAdd(PositionChangeTypes.ExpirationDate, snapshot.ExpirationDate.To<DateTimeOffset?>())
			.TryAdd(PositionChangeTypes.BuyOrdersCount, snapshot.BuyOrdersCount, true)
			.TryAdd(PositionChangeTypes.SellOrdersCount, snapshot.SellOrdersCount, true)
			.TryAdd(PositionChangeTypes.BuyOrdersMargin, snapshot.BuyOrdersMargin, true)
			.TryAdd(PositionChangeTypes.SellOrdersMargin, snapshot.SellOrdersMargin, true)
			.TryAdd(PositionChangeTypes.OrdersMargin, snapshot.OrdersMargin, true)
			.TryAdd(PositionChangeTypes.OrdersCount, snapshot.OrdersCount, true)
			.TryAdd(PositionChangeTypes.TradesCount, snapshot.TradesCount, true)
			.TryAdd(PositionChangeTypes.State, snapshot.State?.ToEnum<PortfolioStates>())
			.TryAdd(PositionChangeTypes.LiquidationPrice, snapshot.LiquidationPrice, true)
			;

			if (snapshot.Currency != null)
				posMsg.Add(PositionChangeTypes.Currency, (CurrencyTypes)snapshot.Currency.Value);

			return posMsg;
		}
	}

	Key ISnapshotSerializer<Key, PositionChangeMessage>.GetKey(PositionChangeMessage message)
		=> (message.SecurityId, message.PortfolioName, message.StrategyId ?? string.Empty);

	void ISnapshotSerializer<Key, PositionChangeMessage>.Update(PositionChangeMessage message, PositionChangeMessage changes)
	{
		foreach (var pair in changes.Changes)
		{
			message.Changes[pair.Key] = pair.Value;
		}

		if (changes.LimitType != null)
			message.LimitType = changes.LimitType;

		if (!changes.DepoName.IsEmpty())
			message.DepoName = changes.DepoName;

		if (!changes.ClientCode.IsEmpty())
			message.ClientCode = changes.ClientCode;

		if (!changes.BoardCode.IsEmpty())
			message.BoardCode = changes.BoardCode;

		if (!changes.Description.IsEmpty())
			message.Description = changes.Description;

		if (!changes.StrategyId.IsEmpty())
			message.StrategyId = changes.StrategyId;

		if (changes.Side != null)
			message.Side = changes.Side;

		if (changes.BuildFrom != default)
			message.BuildFrom = changes.BuildFrom;

		message.LocalTime = changes.LocalTime;
		message.ServerTime = changes.ServerTime;
	}

	DataType ISnapshotSerializer<Key, PositionChangeMessage>.DataType => DataType.PositionChanges;
}