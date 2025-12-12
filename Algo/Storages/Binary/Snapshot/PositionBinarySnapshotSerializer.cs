namespace StockSharp.Algo.Storages.Binary.Snapshot;

using Key = ValueTuple<SecurityId, string, string>;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="PositionChangeMessage"/>.
/// </summary>
public class PositionBinarySnapshotSerializer : ISnapshotSerializer<Key, PositionChangeMessage>
{
	Version ISnapshotSerializer<Key, PositionChangeMessage>.Version { get; } = SnapshotVersions.V24;

	string ISnapshotSerializer<Key, PositionChangeMessage>.Name => "Positions";

	byte[] ISnapshotSerializer<Key, PositionChangeMessage>.Serialize(Version version, PositionChangeMessage message)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		// Estimate buffer size
		var secIdBytes = message.SecurityId.ToStringId().UTF8();
		var portfolioBytes = (message.PortfolioName ?? string.Empty).UTF8();
		var depoBytes = (message.DepoName ?? string.Empty).UTF8();
		var boardBytes = (message.BoardCode ?? string.Empty).UTF8();
		var clientBytes = (message.ClientCode ?? string.Empty).UTF8();
		var descBytes = (message.Description ?? string.Empty).UTF8();
		var strategyBytes = (message.StrategyId ?? string.Empty).UTF8();

		var estimatedSize =
			sizeof(int) + secIdBytes.Length +
			sizeof(int) + portfolioBytes.Length +
			sizeof(long) + // ServerTime
			sizeof(long) + // LocalTime
			sizeof(int) + depoBytes.Length +
			sizeof(int) + boardBytes.Length +
			sizeof(int) + clientBytes.Length +
			sizeof(int) + descBytes.Length +
			sizeof(int) + strategyBytes.Length +
			sizeof(byte) + sizeof(int) + // LimitType (hasValue + value)
			sizeof(byte) + sizeof(int) + // Side (hasValue + value)
			sizeof(byte) + (message.BuildFrom != null ? SnapshotDataType.Size : 0) + // BuildFrom (hasValue + value)
			sizeof(int) + // changes count
			message.Changes.Count * (sizeof(int) + sizeof(byte) + 20); // approximate per field

		var buffer = new byte[estimatedSize];
		var writer = new SpanWriter(buffer);

		// Write base fields
		writer.WriteInt32(secIdBytes.Length);
		writer.WriteSpan(secIdBytes);

		writer.WriteInt32(portfolioBytes.Length);
		writer.WriteSpan(portfolioBytes);

		writer.WriteInt64(message.ServerTime.To<long>());
		writer.WriteInt64(message.LocalTime.To<long>());

		// Write string fields with length prefix
		writer.WriteInt32(depoBytes.Length);
		writer.WriteSpan(depoBytes);

		writer.WriteInt32(boardBytes.Length);
		writer.WriteSpan(boardBytes);

		writer.WriteInt32(clientBytes.Length);
		writer.WriteSpan(clientBytes);

		writer.WriteInt32(descBytes.Length);
		writer.WriteSpan(descBytes);

		writer.WriteInt32(strategyBytes.Length);
		writer.WriteSpan(strategyBytes);

		// Write nullable fields
		writer.WriteBoolean(message.LimitType.HasValue);
		if (message.LimitType.HasValue)
			writer.WriteInt32((int)message.LimitType.Value);

		writer.WriteBoolean(message.Side.HasValue);
		if (message.Side.HasValue)
			writer.WriteInt32((int)message.Side.Value);

		writer.WriteBoolean(message.BuildFrom != null);
		if (message.BuildFrom != null)
			((SnapshotDataType)message.BuildFrom).Write(ref writer);

		// Write changes count
		writer.WriteInt32(message.Changes.Count);

		// Write each change
		foreach (var change in message.Changes)
		{
			writer.WriteInt32((int)change.Key);

			switch (change.Key)
			{
				// Decimal fields
				case PositionChangeTypes.BeginValue:
				case PositionChangeTypes.CurrentValue:
				case PositionChangeTypes.BlockedValue:
				case PositionChangeTypes.CurrentPrice:
				case PositionChangeTypes.AveragePrice:
				case PositionChangeTypes.UnrealizedPnL:
				case PositionChangeTypes.RealizedPnL:
				case PositionChangeTypes.VariationMargin:
				case PositionChangeTypes.Leverage:
				case PositionChangeTypes.Commission:
				case PositionChangeTypes.CurrentValueInLots:
				case PositionChangeTypes.CommissionTaker:
				case PositionChangeTypes.CommissionMaker:
				case PositionChangeTypes.SettlementPrice:
				case PositionChangeTypes.BuyOrdersMargin:
				case PositionChangeTypes.SellOrdersMargin:
				case PositionChangeTypes.OrdersMargin:
				case PositionChangeTypes.LiquidationPrice:
					writer.WriteByte((byte)TypeCode.Decimal);
					writer.WriteDecimal((decimal)change.Value);
					break;

				// Int fields
				case PositionChangeTypes.BuyOrdersCount:
				case PositionChangeTypes.SellOrdersCount:
				case PositionChangeTypes.OrdersCount:
				case PositionChangeTypes.TradesCount:
					writer.WriteByte((byte)TypeCode.Int32);
					writer.WriteInt32((int)change.Value);
					break;

				// Long fields (DateTime)
				case PositionChangeTypes.ExpirationDate:
					writer.WriteByte((byte)TypeCode.Int64);
					writer.WriteInt64(change.Value.To<long>());
					break;

				// Short fields
				case PositionChangeTypes.Currency:
					writer.WriteByte((byte)TypeCode.Int16);
					writer.WriteInt16((short)(CurrencyTypes)change.Value);
					break;

				// Byte fields
				case PositionChangeTypes.State:
					writer.WriteByte((byte)TypeCode.Byte);
					writer.WriteByte((byte)(PortfolioStates)change.Value);
					break;

				default:
					throw new InvalidOperationException(change.Key.To<string>());
			}
		}

		// Return actual written data
		return writer.GetWrittenSpan().ToArray();
	}

	PositionChangeMessage ISnapshotSerializer<Key, PositionChangeMessage>.Deserialize(Version version, byte[] buffer)
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

		var portfolioLen = reader.ReadInt32();
		var portfolioBytes = reader.ReadSpan(portfolioLen);
		var portfolioName = portfolioBytes.ToArray().UTF8();

		var serverTime = reader.ReadInt64().To<DateTime>();
		var localTime = reader.ReadInt64().To<DateTime>();

		// Read string fields
		var depoLen = reader.ReadInt32();
		var depoBytes = reader.ReadSpan(depoLen);
		var depoName = depoBytes.ToArray().UTF8();

		var boardLen = reader.ReadInt32();
		var boardBytes = reader.ReadSpan(boardLen);
		var boardCode = boardBytes.ToArray().UTF8();

		var clientLen = reader.ReadInt32();
		var clientBytes = reader.ReadSpan(clientLen);
		var clientCode = clientBytes.ToArray().UTF8();

		var descLen = reader.ReadInt32();
		var descBytes = reader.ReadSpan(descLen);
		var description = descBytes.ToArray().UTF8();

		var strategyLen = reader.ReadInt32();
		var strategyBytes = reader.ReadSpan(strategyLen);
		var strategyId = strategyBytes.ToArray().UTF8();

		// Read nullable fields
		var hasLimitType = reader.ReadBoolean();
		TPlusLimits? limitType = null;
		if (hasLimitType)
			limitType = (TPlusLimits)reader.ReadInt32();

		var hasSide = reader.ReadBoolean();
		Sides? side = null;
		if (hasSide)
			side = (Sides)reader.ReadInt32();

		var hasBuildFrom = reader.ReadBoolean();
		SnapshotDataType? buildFrom = null;
		if (hasBuildFrom)
			buildFrom = SnapshotDataType.Read(ref reader);

		var posMsg = new PositionChangeMessage
		{
			SecurityId = securityId,
			PortfolioName = portfolioName,
			ServerTime = serverTime.UtcKind(),
			LocalTime = localTime.UtcKind(),
			ClientCode = clientCode.IsEmpty() ? null : clientCode,
			DepoName = depoName.IsEmpty() ? null : depoName,
			BoardCode = boardCode.IsEmpty() ? null : boardCode,
			LimitType = limitType,
			Description = description.IsEmpty() ? null : description,
			StrategyId = strategyId.IsEmpty() ? null : strategyId,
			Side = side,
			BuildFrom = buildFrom,
		};

		// Read changes count
		var changesCount = reader.ReadInt32();

		// Read each change
		for (var i = 0; i < changesCount; i++)
		{
			var changeType = (PositionChangeTypes)reader.ReadInt32();
			var valueType = (TypeCode)reader.ReadByte();

			object value;

			switch (valueType)
			{
				case TypeCode.Decimal:
					value = reader.ReadDecimal();
					break;

				case TypeCode.Int32:
					value = reader.ReadInt32();
					break;

				case TypeCode.Int64:
					value = reader.ReadInt64().To<DateTime>();
					break;

				case TypeCode.Int16:
					value = (CurrencyTypes)reader.ReadInt16();
					break;

				case TypeCode.Byte:
					value = (PortfolioStates)reader.ReadByte();
					break;

				default:
					throw new InvalidOperationException($"Unknown value type: {valueType}");
			}

			posMsg.Add(changeType, value);
		}

		return posMsg;
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
