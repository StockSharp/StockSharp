namespace StockSharp.Algo.Storages.Binary.Snapshot;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="ExecutionMessage"/>.
/// </summary>
public class TransactionBinarySnapshotSerializer : ISnapshotSerializer<string, ExecutionMessage>
{
	Version ISnapshotSerializer<string, ExecutionMessage>.Version { get; } = SnapshotVersions.V24;

	string ISnapshotSerializer<string, ExecutionMessage>.Name => "Transactions";

	byte[] ISnapshotSerializer<string, ExecutionMessage>.Serialize(Version version, ExecutionMessage message)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (message.DataType != DataType.Transactions)
			throw new ArgumentOutOfRangeException(nameof(message), message.DataType, LocalizedStrings.UnknownType.Put(message));

		if (message.TransactionId == 0)
			throw new InvalidOperationException("TransId == 0");

		// Prepare condition parameters
		var conParams = message.Condition?.Parameters.Where(p => p.Value != null).ToArray() ?? [];

		// Estimate buffer size
		var secIdBytes = message.SecurityId.ToStringId().UTF8();
		var portfolioBytes = (message.PortfolioName ?? string.Empty).UTF8();
		var brokerCodeBytes = (message.BrokerCode ?? string.Empty).UTF8();
		var clientCodeBytes = (message.ClientCode ?? string.Empty).UTF8();
		var commentBytes = (message.Comment ?? string.Empty).UTF8();
		var systemCommentBytes = (message.SystemComment ?? string.Empty).UTF8();
		var errorBytes = (message.Error?.Message ?? string.Empty).UTF8();
		var depoBytes = (message.DepoName ?? string.Empty).UTF8();
		var orderStringIdBytes = (message.OrderStringId ?? string.Empty).UTF8();
		var orderBoardIdBytes = (message.OrderBoardId ?? string.Empty).UTF8();
		var userOrderIdBytes = (message.UserOrderId ?? string.Empty).UTF8();
		var strategyIdBytes = (message.StrategyId ?? string.Empty).UTF8();
		var tradeStringIdBytes = (message.TradeStringId ?? string.Empty).UTF8();
		var conditionTypeBytes = (message.Condition?.GetType().GetTypeName(false) ?? string.Empty).UTF8();

		var estimatedSize =
			4 + secIdBytes.Length +
			4 + portfolioBytes.Length +
			8 + 8 + // ServerTime, LocalTime
			8 + // TransactionId
			1 + 1 + // HasOrderInfo, HasTradeInfo
			16 + // OrderPrice (decimal)
			1 + 8 + // OrderId (nullable)
			1 + 16 + // OrderVolume (nullable)
			1 + 1 + // OrderType (nullable)
			1 + 1 + // OrderTif (nullable)
			1 + 1 + // IsSystem (nullable)
			4 + orderStringIdBytes.Length +
			1 + 8 + // TradeId (nullable)
			1 + 16 + // TradePrice (nullable)
			1 + 16 + // TradeVolume (nullable)
			4 + brokerCodeBytes.Length +
			4 + clientCodeBytes.Length +
			4 + commentBytes.Length +
			4 + systemCommentBytes.Length +
			4 + errorBytes.Length +
			1 + 2 + // Currency (nullable)
			4 + depoBytes.Length +
			1 + 8 + // ExpiryDate (nullable)
			1 + 1 + // IsMarketMaker (nullable)
			1 + // Side
			4 + orderBoardIdBytes.Length +
			1 + 16 + // VisibleVolume (nullable)
			1 + 1 + // OrderState (nullable)
			1 + 8 + // OrderStatus (nullable)
			1 + 16 + // Balance (nullable)
			4 + userOrderIdBytes.Length +
			4 + strategyIdBytes.Length +
			1 + 1 + // OriginSide (nullable)
			1 + 8 + // Latency (nullable)
			1 + 16 + // PnL (nullable)
			1 + 16 + // Position (nullable)
			1 + 16 + // Slippage (nullable)
			1 + 16 + // Commission (nullable)
			1 + 4 + // TradeStatus (nullable)
			4 + tradeStringIdBytes.Length +
			1 + 16 + // OpenInterest (nullable)
			1 + 1 + // MarginMode (nullable)
			1 + 1 + // IsManual (nullable)
			1 + 16 + // AveragePrice (nullable)
			1 + 16 + // Yield (nullable)
			1 + 16 + // MinVolume (nullable)
			1 + 1 + // PositionEffect (nullable)
			1 + 1 + // PostOnly (nullable)
			1 + 1 + // Initiator (nullable)
			8 + // SeqNum
			1 + 1 + // BuildFrom (nullable)
			1 + 4 + // Leverage (nullable)
			4 + conditionTypeBytes.Length +
			4 + // ConditionParamsCount
			conParams.Length * 200; // approximate per condition parameter

		var buffer = new byte[estimatedSize];
		var writer = new SpanWriter(buffer);

		// Helper to write nullable string
		void WriteNullableString(ref SpanWriter writer, byte[] bytes)
		{
			writer.WriteInt32(bytes.Length);
			if (bytes.Length > 0)
				writer.WriteSpan(bytes);
		}

		// Helper to write nullable decimal
		void WriteNullableDecimal(ref SpanWriter writer, decimal? value)
		{
			writer.WriteBoolean(value.HasValue);
			if (value.HasValue)
				writer.WriteDecimal(value.Value);
		}

		// Helper to write nullable long
		void WriteNullableLong(ref SpanWriter writer, long? value)
		{
			writer.WriteBoolean(value.HasValue);
			if (value.HasValue)
				writer.WriteInt64(value.Value);
		}

		// Helper to write nullable byte
		void WriteNullableByte(ref SpanWriter writer, byte? value)
		{
			writer.WriteBoolean(value.HasValue);
			if (value.HasValue)
				writer.WriteByte(value.Value);
		}

		// Helper to write nullable short
		void WriteNullableShort(ref SpanWriter writer, short? value)
		{
			writer.WriteBoolean(value.HasValue);
			if (value.HasValue)
				writer.WriteInt16(value.Value);
		}

		// Helper to write nullable int
		void WriteNullableInt(ref SpanWriter writer, int? value)
		{
			writer.WriteBoolean(value.HasValue);
			if (value.HasValue)
				writer.WriteInt32(value.Value);
		}

		// Write base fields
		WriteNullableString(ref writer, secIdBytes);
		WriteNullableString(ref writer, portfolioBytes);

		writer.WriteInt64(message.ServerTime.To<long>());
		writer.WriteInt64(message.LocalTime.To<long>());

		writer.WriteInt64(message.TransactionId);

		writer.WriteBoolean(message.HasOrderInfo);
		writer.WriteBoolean(message.HasTradeInfo);

		writer.WriteDecimal(message.OrderPrice);
		WriteNullableLong(ref writer, message.OrderId);
		WriteNullableDecimal(ref writer, message.OrderVolume);
		WriteNullableByte(ref writer, message.OrderType?.ToByte());
		WriteNullableByte(ref writer, message.TimeInForce?.ToByte());
		WriteNullableByte(ref writer, message.IsSystem?.ToByte());

		WriteNullableString(ref writer, orderStringIdBytes);

		WriteNullableLong(ref writer, message.TradeId);
		WriteNullableDecimal(ref writer, message.TradePrice);
		WriteNullableDecimal(ref writer, message.TradeVolume);

		WriteNullableString(ref writer, brokerCodeBytes);
		WriteNullableString(ref writer, clientCodeBytes);
		WriteNullableString(ref writer, commentBytes);
		WriteNullableString(ref writer, systemCommentBytes);
		WriteNullableString(ref writer, errorBytes);

		WriteNullableShort(ref writer, message.Currency == null ? null : (short)message.Currency.Value);

		WriteNullableString(ref writer, depoBytes);

		WriteNullableLong(ref writer, message.ExpiryDate?.To<long>());
		WriteNullableByte(ref writer, message.IsMarketMaker?.ToByte());

		writer.WriteByte((byte)message.Side);

		WriteNullableString(ref writer, orderBoardIdBytes);

		WriteNullableDecimal(ref writer, message.VisibleVolume);
		WriteNullableByte(ref writer, message.OrderState?.ToByte());
		WriteNullableLong(ref writer, message.OrderStatus);
		WriteNullableDecimal(ref writer, message.Balance);

		WriteNullableString(ref writer, userOrderIdBytes);
		WriteNullableString(ref writer, strategyIdBytes);

		WriteNullableByte(ref writer, message.OriginSide?.ToByte());
		WriteNullableLong(ref writer, message.Latency?.Ticks);
		WriteNullableDecimal(ref writer, message.PnL);
		WriteNullableDecimal(ref writer, message.Position);
		WriteNullableDecimal(ref writer, message.Slippage);
		WriteNullableDecimal(ref writer, message.Commission);
		WriteNullableInt(ref writer, (int?)message.TradeStatus);

		WriteNullableString(ref writer, tradeStringIdBytes);

		WriteNullableDecimal(ref writer, message.OpenInterest);
		WriteNullableByte(ref writer, message.MarginMode?.ToByte());
		WriteNullableByte(ref writer, message.IsManual?.ToByte());

		WriteNullableDecimal(ref writer, message.AveragePrice);
		WriteNullableDecimal(ref writer, message.Yield);
		WriteNullableDecimal(ref writer, message.MinVolume);
		WriteNullableByte(ref writer, message.PositionEffect?.ToByte());
		WriteNullableByte(ref writer, message.PostOnly?.ToByte());
		WriteNullableByte(ref writer, message.Initiator?.ToByte());

		writer.WriteInt64(message.SeqNum);

		writer.WriteBoolean(message.BuildFrom != null);
		if (message.BuildFrom != null)
			((SnapshotDataType)message.BuildFrom).Write(ref writer);

		WriteNullableInt(ref writer, message.Leverage);

		WriteNullableString(ref writer, conditionTypeBytes);

		// Write condition parameters count
		writer.WriteInt32(conParams.Length);

		// Write each condition parameter
		foreach (var conParam in conParams)
		{
			var paramType = conParam.Value.GetType();
			var paramTypeName = paramType.GetTypeAsString(false);
			var paramTypeNameBytes = paramTypeName.UTF8();

			var nameBytes = conParam.Key.UTF8();

			// Write parameter name
			writer.WriteInt32(nameBytes.Length);
			writer.WriteSpan(nameBytes);

			// Write parameter type name
			writer.WriteInt32(paramTypeNameBytes.Length);
			writer.WriteSpan(paramTypeNameBytes);

			byte[] stringValue = null;

			// Determine parameter value type and write it
			switch (conParam.Value)
			{
				case byte b:
				case sbyte sb:
				case int i:
				case short s:
				case long l:
				case uint ui:
				case ushort us:
				case ulong ul:
				case DateTimeOffset dto:
				case DateTime dt:
				case TimeSpan ts:
				case Enum e:
					writer.WriteByte((byte)TypeCode.Int64);
					writer.WriteInt64(conParam.Value.To<long>());
					break;

				case float f:
				case double d:
				case decimal dec:
					writer.WriteByte((byte)TypeCode.Decimal);
					writer.WriteDecimal(conParam.Value.To<decimal>());
					break;

				case bool bln:
					writer.WriteByte((byte)TypeCode.Boolean);
					writer.WriteBoolean(bln);
					break;

				case IRange r:
				{
					var storage = new SettingsStorage();

					if (r.HasMinValue)
						storage.SetValue("Min", r.MinObj.ToStorage());

					if (r.HasMaxValue)
						storage.SetValue("Max", r.MaxObj.ToStorage());

					if (storage.Count > 0)
						stringValue = storage.Serialize();

					writer.WriteByte((byte)TypeCode.String);
					writer.WriteInt32(stringValue?.Length ?? 0);
					if (stringValue != null)
						writer.WriteSpan(stringValue);
					break;
				}

				case IPersistable p:
				{
					var storage = p.Save();

					if (storage.Count > 0)
						stringValue = storage.Serialize();

					writer.WriteByte((byte)TypeCode.String);
					writer.WriteInt32(stringValue?.Length ?? 0);
					if (stringValue != null)
						writer.WriteSpan(stringValue);
					break;
				}

				default:
					// Unknown type - skip
					writer.WriteByte((byte)TypeCode.String);
					writer.WriteInt32(0);
					break;
			}
		}

		// Return actual written data
		return writer.GetWrittenSpan().ToArray();
	}

	ExecutionMessage ISnapshotSerializer<string, ExecutionMessage>.Deserialize(Version version, byte[] buffer)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (buffer == null || buffer.Length == 0)
			throw new ArgumentNullException(nameof(buffer));

		var reader = new SpanReader(buffer);

		// Helper to read nullable string
		string ReadNullableString(ref SpanReader reader)
		{
			var len = reader.ReadInt32();
			if (len == 0)
				return null;

			var bytes = reader.ReadSpan(len);
			return bytes.ToArray().UTF8();
		}

		// Helper to read nullable decimal
		decimal? ReadNullableDecimal(ref SpanReader reader)
		{
			var hasValue = reader.ReadBoolean();
			return hasValue ? reader.ReadDecimal() : null;
		}

		// Helper to read nullable long
		long? ReadNullableLong(ref SpanReader reader)
		{
			var hasValue = reader.ReadBoolean();
			return hasValue ? reader.ReadInt64() : null;
		}

		// Helper to read nullable byte
		byte? ReadNullableByte(ref SpanReader reader)
		{
			var hasValue = reader.ReadBoolean();
			return hasValue ? reader.ReadByte() : null;
		}

		// Helper to read nullable short
		short? ReadNullableShort(ref SpanReader reader)
		{
			var hasValue = reader.ReadBoolean();
			return hasValue ? reader.ReadInt16() : null;
		}

		// Helper to read nullable int
		int? ReadNullableInt(ref SpanReader reader)
		{
			var hasValue = reader.ReadBoolean();
			return hasValue ? reader.ReadInt32() : null;
		}

		// Read base fields
		var securityId = ReadNullableString(ref reader).ToSecurityId();
		var portfolioName = ReadNullableString(ref reader);

		var serverTime = reader.ReadInt64().To<DateTime>();
		var localTime = reader.ReadInt64().To<DateTime>();

		var transactionId = reader.ReadInt64();

		var hasOrderInfo = reader.ReadBoolean();
		var hasTradeInfo = reader.ReadBoolean();

		var orderPrice = reader.ReadDecimal();
		var orderId = ReadNullableLong(ref reader);
		var orderVolume = ReadNullableDecimal(ref reader);
		var orderType = ReadNullableByte(ref reader);
		var orderTif = ReadNullableByte(ref reader);
		var isSystem = ReadNullableByte(ref reader);

		var orderStringId = ReadNullableString(ref reader);

		var tradeId = ReadNullableLong(ref reader);
		var tradePrice = ReadNullableDecimal(ref reader);
		var tradeVolume = ReadNullableDecimal(ref reader);

		var brokerCode = ReadNullableString(ref reader);
		var clientCode = ReadNullableString(ref reader);
		var comment = ReadNullableString(ref reader);
		var systemComment = ReadNullableString(ref reader);
		var error = ReadNullableString(ref reader);

		var currency = ReadNullableShort(ref reader);

		var depoName = ReadNullableString(ref reader);

		var expiryDate = ReadNullableLong(ref reader);
		var isMarketMaker = ReadNullableByte(ref reader);

		var side = reader.ReadByte();

		var orderBoardId = ReadNullableString(ref reader);

		var visibleVolume = ReadNullableDecimal(ref reader);
		var orderState = ReadNullableByte(ref reader);
		var orderStatus = ReadNullableLong(ref reader);
		var balance = ReadNullableDecimal(ref reader);

		var userOrderId = ReadNullableString(ref reader);
		var strategyId = ReadNullableString(ref reader);

		var originSide = ReadNullableByte(ref reader);
		var latency = ReadNullableLong(ref reader);
		var pnl = ReadNullableDecimal(ref reader);
		var position = ReadNullableDecimal(ref reader);
		var slippage = ReadNullableDecimal(ref reader);
		var commission = ReadNullableDecimal(ref reader);
		var tradeStatus = ReadNullableInt(ref reader);

		var tradeStringId = ReadNullableString(ref reader);

		var openInterest = ReadNullableDecimal(ref reader);
		var marginMode = ReadNullableByte(ref reader);
		var isManual = ReadNullableByte(ref reader);

		var averagePrice = ReadNullableDecimal(ref reader);
		var yield = ReadNullableDecimal(ref reader);
		var minVolume = ReadNullableDecimal(ref reader);
		var positionEffect = ReadNullableByte(ref reader);
		var postOnly = ReadNullableByte(ref reader);
		var initiator = ReadNullableByte(ref reader);

		var seqNum = reader.ReadInt64();

		var hasBuildFrom = reader.ReadBoolean();
		SnapshotDataType? buildFrom = null;
		if (hasBuildFrom)
			buildFrom = SnapshotDataType.Read(ref reader);

		var leverage = ReadNullableInt(ref reader);

		var conditionType = ReadNullableString(ref reader);

		// Create message
		var execMsg = new ExecutionMessage
		{
			SecurityId = securityId,
			PortfolioName = portfolioName,
			ServerTime = serverTime.UtcKind(),
			LocalTime = localTime.UtcKind(),

			DataTypeEx = DataType.Transactions,

			TransactionId = transactionId,

			HasOrderInfo = hasOrderInfo,

			BrokerCode = brokerCode,
			ClientCode = clientCode,

			Comment = comment,
			SystemComment = systemComment,

			Currency = currency == null ? null : (CurrencyTypes)currency.Value,
			DepoName = depoName,
			Error = error.IsEmpty() ? null : new InvalidOperationException(error),

			ExpiryDate = expiryDate?.To<DateTime>().UtcKind(),
			IsMarketMaker = isMarketMaker?.ToBool(),
			MarginMode = (MarginModes?)marginMode,
			IsManual = isManual?.ToBool(),
			Side = (Sides)side,
			OrderId = orderId,
			OrderStringId = orderStringId,
			OrderBoardId = orderBoardId,
			OrderPrice = orderPrice,
			OrderVolume = orderVolume,
			VisibleVolume = visibleVolume,
			OrderType = orderType?.ToEnum<OrderTypes>(),
			OrderState = orderState?.ToEnum<OrderStates>(),
			OrderStatus = orderStatus,
			Balance = balance,
			UserOrderId = userOrderId,
			StrategyId = strategyId,
			OriginSide = originSide?.ToEnum<Sides>(),
			Latency = latency == null ? null : TimeSpan.FromTicks(latency.Value),
			PnL = pnl,
			Position = position,
			Slippage = slippage,
			Commission = commission,
			TradePrice = tradePrice,
			TradeVolume = tradeVolume,
			TradeStatus = tradeStatus,
			TradeId = tradeId,
			TradeStringId = tradeStringId,
			OpenInterest = openInterest,
			IsSystem = isSystem?.ToBool(),
			TimeInForce = orderTif?.ToEnum<TimeInForce>(),

			AveragePrice = averagePrice,
			Yield = yield,
			MinVolume = minVolume,
			PositionEffect = positionEffect?.ToEnum<OrderPositionEffects>(),
			PostOnly = postOnly?.ToBool(),
			Initiator = initiator?.ToBool(),
			SeqNum = seqNum,
			BuildFrom = buildFrom,
			Leverage = leverage,
		};

		// Read condition parameters
		if (!conditionType.IsEmpty())
		{
			execMsg.Condition = conditionType.To<Type>().CreateInstance<OrderCondition>();
			execMsg.Condition.Parameters.Clear(); // removing pre-defined values
		}

		var conditionParamsCount = reader.ReadInt32();

		for (var i = 0; i < conditionParamsCount; i++)
		{
			// Read parameter name
			var nameLen = reader.ReadInt32();
			var nameBytes = reader.ReadSpan(nameLen);
			var paramName = nameBytes.ToArray().UTF8();

			// Read parameter type name
			var typeNameLen = reader.ReadInt32();
			var typeNameBytes = reader.ReadSpan(typeNameLen);
			var paramTypeName = typeNameBytes.ToArray().UTF8();

			try
			{
				var paramType = paramTypeName.To<Type>();

				// Read parameter value
				var valueType = (TypeCode)reader.ReadByte();

				object value;

				switch (valueType)
				{
					case TypeCode.Int64:
						value = reader.ReadInt64();
						break;

					case TypeCode.Decimal:
						value = reader.ReadDecimal();
						break;

					case TypeCode.Boolean:
						value = reader.ReadBoolean();
						break;

					case TypeCode.String:
						var strLen = reader.ReadInt32();
						if (strLen > 0)
						{
							var strBytes = reader.ReadSpan(strLen);

							if (paramType.IsPersistable())
							{
								value = strBytes.ToArray().Deserialize<SettingsStorage>()?.Load(paramType) ?? throw new InvalidOperationException("unable to deserialize param value");
							}
							else if (paramType.Is<IRange>())
							{
								var range = paramType.CreateInstance<IRange>();

								var storage = strBytes.ToArray().Deserialize<SettingsStorage>() ?? throw new InvalidOperationException("unable to deserialize IRange param value");

								if (storage.ContainsKey("Min"))
									range.MinObj = storage.GetValue<SettingsStorage>("Min").FromStorage();

								if (storage.ContainsKey("Max"))
									range.MaxObj = storage.GetValue<SettingsStorage>("Max").FromStorage();

								value = range;
							}
							else
							{
								value = null;
							}
						}
						else
						{
							value = null;
						}
						break;

					default:
						throw new InvalidOperationException($"Unknown condition parameter value type: {valueType}");
				}

				if (value != null)
				{
					value = value.To(paramType);
					execMsg.Condition.Parameters.Add(paramName, value);
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		return execMsg;
	}

	string ISnapshotSerializer<string, ExecutionMessage>.GetKey(ExecutionMessage message)
	{
		if (message.TransactionId == 0)
			throw new InvalidOperationException("TransId == 0");

		var key = message.TransactionId.To<string>();

		if (message.TradeId != null)
			key += "-" + message.TradeId;
		else if (!message.TradeStringId.IsEmpty())
			key += "-" + message.TradeStringId;

		return key.ToLowerInvariant();
	}

	void ISnapshotSerializer<string, ExecutionMessage>.Update(ExecutionMessage message, ExecutionMessage changes)
	{
		if (!changes.BrokerCode.IsEmpty())
			message.BrokerCode = changes.BrokerCode;

		if (!changes.ClientCode.IsEmpty())
			message.ClientCode = changes.ClientCode;

		if (!changes.Comment.IsEmpty())
			message.Comment = changes.Comment;

		if (!changes.SystemComment.IsEmpty())
			message.SystemComment = changes.SystemComment;

		if (changes.Currency != default)
			message.Currency = changes.Currency;

		if (changes.Condition != default)
			message.Condition = changes.Condition.Clone();

		if (!changes.DepoName.IsEmpty())
			message.DepoName = changes.DepoName;

		if (changes.Error != default)
			message.Error = changes.Error;

		if (changes.ExpiryDate != default)
			message.ExpiryDate = changes.ExpiryDate;

		if (!changes.PortfolioName.IsEmpty())
			message.PortfolioName = changes.PortfolioName;

		if (changes.IsMarketMaker != default)
			message.IsMarketMaker = changes.IsMarketMaker;

		if (changes.OrderId != default)
			message.OrderId = changes.OrderId;

		if (!changes.OrderBoardId.IsEmpty())
			message.OrderBoardId = changes.OrderBoardId;

		if (!changes.OrderStringId.IsEmpty())
			message.OrderStringId = changes.OrderStringId;

		if (changes.OrderType != default)
			message.OrderType = changes.OrderType;

		if (changes.OrderPrice != default)
			message.OrderPrice = changes.OrderPrice;

		if (changes.OrderVolume != default)
			message.OrderVolume = changes.OrderVolume;

		if (changes.VisibleVolume != default)
			message.VisibleVolume = changes.VisibleVolume;

		if (changes.OrderState != default)
			message.OrderState = changes.OrderState;

		if (changes.OrderStatus != default)
			message.OrderStatus = changes.OrderStatus;

		if (changes.Balance != default)
			message.Balance = changes.Balance;

		if (!changes.UserOrderId.IsEmpty())
			message.UserOrderId = changes.UserOrderId;

		if (!changes.StrategyId.IsEmpty())
			message.StrategyId = changes.StrategyId;

		if (changes.OriginSide != default)
			message.OriginSide = changes.OriginSide;

		if (changes.Latency != default)
			message.Latency = changes.Latency;

		if (changes.PnL != default)
			message.PnL = changes.PnL;

		if (changes.Position != default)
			message.Position = changes.Position;

		if (changes.Slippage != default)
			message.Slippage = changes.Slippage;

		if (changes.Commission != default)
			message.Commission = changes.Commission;

		if (changes.TradePrice != default)
			message.TradePrice = changes.TradePrice;

		if (changes.TradeVolume != default)
			message.TradeVolume = changes.TradeVolume;

		if (changes.TradeStatus != default)
			message.TradeStatus = changes.TradeStatus;

		if (changes.TradeId != default)
			message.TradeId = changes.TradeId;

		if (!changes.TradeStringId.IsEmpty())
			message.TradeStringId = changes.TradeStringId;

		if (changes.OpenInterest != default)
			message.OpenInterest = changes.OpenInterest;

		if (changes.MarginMode != default)
			message.MarginMode = changes.MarginMode;

		if (changes.TimeInForce != default)
			message.TimeInForce = changes.TimeInForce;

		if (changes.HasOrderInfo)
			message.HasOrderInfo = true;

		if (changes.AveragePrice != default)
			message.AveragePrice = changes.AveragePrice;

		if (changes.MinVolume != default)
			message.MinVolume = changes.MinVolume;

		if (changes.Yield != default)
			message.Yield = changes.Yield;

		if (changes.PositionEffect != default)
			message.PositionEffect = changes.PositionEffect;

		if (changes.PostOnly != default)
			message.PostOnly = changes.PostOnly;

		if (changes.Initiator != default)
			message.Initiator = changes.Initiator;

		if (changes.SeqNum != default)
			message.SeqNum = changes.SeqNum;

		if (changes.BuildFrom != default)
			message.BuildFrom = changes.BuildFrom;

		if (changes.Leverage != default)
			message.Leverage = changes.Leverage;

		message.LocalTime = changes.LocalTime;
		message.ServerTime = changes.ServerTime;
	}

	DataType ISnapshotSerializer<string, ExecutionMessage>.DataType => DataType.Transactions;
}
