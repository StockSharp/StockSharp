namespace StockSharp.Algo.Storages.Binary.Snapshot;

using System.Runtime.InteropServices;

using Ecng.Interop;

using StockSharp.Configuration;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="ExecutionMessage"/>.
/// </summary>
public class TransactionBinarySnapshotSerializer : ISnapshotSerializer<string, ExecutionMessage>
{
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	private struct TransactionSnapshot
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string SecurityId;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string PortfolioName;

		public long LastChangeServerTime;
		public long LastChangeLocalTime;

		public long TransactionId;

		public bool HasOrderInfo;
		public bool HasTradeInfo;

		public BlittableDecimal OrderPrice;
		public long? OrderId;
		//public long OrderUserId;
		public BlittableDecimal? OrderVolume;
		public byte? OrderType;
		//public byte OrderSide;
		public byte? OrderTif;

		public byte? IsSystem;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string OrderStringId;

		public long? TradeId;
		public BlittableDecimal? TradePrice;
		public BlittableDecimal? TradeVolume;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string BrokerCode;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string ClientCode;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string Comment;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string SystemComment;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S200)]
		public string Error;

		public short? Currency;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string DepoName;

		public long? ExpiryDate;

		public byte? IsMarketMaker;
		public byte Side;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string OrderBoardId;

		public BlittableDecimal? VisibleVolume;
		public byte? OrderState;
		public long? OrderStatus;
		public BlittableDecimal? Balance;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string UserOrderId;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string StrategyId;

		public byte? OriginSide;
		public long? Latency;
		public BlittableDecimal? PnL;
		public BlittableDecimal? Position;
		public BlittableDecimal? Slippage;
		public BlittableDecimal? Commission;
		public int? TradeStatus;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string TradeStringId;

		public BlittableDecimal? OpenInterest;
		public byte? MarginMode;
		public byte? IsManual;

		public BlittableDecimal? AveragePrice;
		public BlittableDecimal? Yield;
		public BlittableDecimal? MinVolume;
		public byte? PositionEffect;
		public byte? PostOnly;
		public byte? Initiator;

		public long SeqNum;
		public SnapshotDataType? BuildFrom;

		public int? Leverage;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S256)]
		public string ConditionType;

		public int ConditionParamsCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct TransactionConditionParamV21
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S32)]
		public string Name;

		public int ValueTypeLen;

		public long? NumValue;
		public BlittableDecimal? DecimalValue;
		public bool? BoolValue;

		public int StringValueLen;
	}

	Version ISnapshotSerializer<string, ExecutionMessage>.Version { get; } = SnapshotVersions.V23;

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

		var snapshot = new TransactionSnapshot
		{
			SecurityId = message.SecurityId.ToStringId().VerifySize(Sizes.S100),
			PortfolioName = message.PortfolioName.VerifySize(Sizes.S100),
			LastChangeServerTime = message.ServerTime.To<long>(),
			LastChangeLocalTime = message.LocalTime.To<long>(),

			//OriginalTransactionId = message.OriginalTransactionId,
			TransactionId = message.TransactionId,

			HasOrderInfo = message.HasOrderInfo,
			HasTradeInfo = message.HasTradeInfo,

			BrokerCode = message.BrokerCode.VerifySize(Sizes.S100),
			ClientCode = message.ClientCode.VerifySize(Sizes.S100),
			Comment = message.Comment.VerifySize(Sizes.S100),
			SystemComment = message.SystemComment.VerifySize(Sizes.S100),
			Currency = message.Currency == null ? null : (short)message.Currency.Value,
			DepoName = message.DepoName.VerifySize(Sizes.S100),
			Error = (message.Error?.Message).VerifySize(Sizes.S200),
			ExpiryDate = message.ExpiryDate?.To<long>(),
			IsMarketMaker = message.IsMarketMaker?.ToByte(),
			MarginMode = message.MarginMode?.ToByte(),
			IsManual = message.IsManual?.ToByte(),
			Side = (byte)message.Side,
			OrderId = message.OrderId,
			OrderStringId = message.OrderStringId.VerifySize(Sizes.S100),
			OrderBoardId = message.OrderBoardId.VerifySize(Sizes.S100),
			OrderPrice = (BlittableDecimal)message.OrderPrice,
			OrderVolume = (BlittableDecimal?)message.OrderVolume,
			VisibleVolume = (BlittableDecimal?)message.VisibleVolume,
			OrderType = message.OrderType?.ToByte(),
			OrderState = message.OrderState?.ToByte(),
			OrderStatus = message.OrderStatus,
			Balance = (BlittableDecimal?)message.Balance,
			UserOrderId = message.UserOrderId.VerifySize(Sizes.S100),
			StrategyId = message.StrategyId.VerifySize(Sizes.S100),
			OriginSide = message.OriginSide?.ToByte(),
			Latency = message.Latency?.Ticks,
			PnL = (BlittableDecimal?)message.PnL,
			Position = (BlittableDecimal?)message.Position,
			Slippage = (BlittableDecimal?)message.Slippage,
			Commission = (BlittableDecimal?)message.Commission,
			TradePrice = (BlittableDecimal?)message.TradePrice,
			TradeVolume = (BlittableDecimal?)message.TradeVolume,
			TradeStatus = (int?)message.TradeStatus,
			TradeId = message.TradeId,
			TradeStringId = message.TradeStringId.VerifySize(Sizes.S100),
			OpenInterest = (BlittableDecimal?)message.OpenInterest,
			IsSystem = message.IsSystem?.ToByte(),
			OrderTif = message.TimeInForce?.ToByte(),

			AveragePrice = (BlittableDecimal?)message.AveragePrice,
			Yield = (BlittableDecimal?)message.Yield,
			MinVolume = (BlittableDecimal?)message.MinVolume,
			PositionEffect = message.PositionEffect?.ToByte(),
			PostOnly = message.PostOnly?.ToByte(),
			Initiator = message.Initiator?.ToByte(),
			SeqNum = message.SeqNum,
			BuildFrom = message.BuildFrom == null ? default(SnapshotDataType?) : (SnapshotDataType)message.BuildFrom,
			Leverage = message.Leverage,

			ConditionType = (message.Condition?.GetType().GetTypeName(false)).VerifySize(Sizes.S256),
		};

		var conParams = message.Condition?.Parameters.Where(p => p.Value != null).ToArray() ?? [];

		snapshot.ConditionParamsCount = conParams.Length;

		var paramSize = typeof(TransactionConditionParamV21).SizeOf();

		var result = new List<byte>();

		var buffer = new byte[typeof(TransactionSnapshot).SizeOf()];

		var ptr = snapshot.StructToPtr();
		ptr.CopyTo(buffer);
		ptr.FreeHGlobal();

		result.AddRange(buffer);

		foreach (var conParam in conParams)
		{
			var paramType = conParam.Value.GetType();

			var paramTypeName = paramType.GetTypeAsString(false);

			var param = new TransactionConditionParamV21
			{
				ValueTypeLen = paramTypeName.UTF8().Length,
				Name = conParam.Key.VerifySize(Sizes.S32)
			};

			byte[] stringValue = null;

			switch (conParam.Value)
			{
				case byte b:
					param.NumValue = b;
					break;
				case sbyte sb:
					param.NumValue = sb;
					break;
				case int i:
					param.NumValue = i;
					break;
				case short s:
					param.NumValue = s;
					break;
				case long l:
					param.NumValue = l;
					break;
				case uint ui:
					param.NumValue = ui;
					break;
				case ushort us:
					param.NumValue = us;
					break;
				case ulong ul:
					param.NumValue = (long)ul;
					break;
				case DateTimeOffset dto:
					param.NumValue = dto.To<long>();
					break;
				case DateTime dt:
					param.NumValue = dt.To<long>();
					break;
				case TimeSpan ts:
					param.NumValue = ts.To<long>();
					break;
				case float f:
					param.DecimalValue = (BlittableDecimal)(decimal)f;
					break;
				case double d:
					param.DecimalValue = (BlittableDecimal)(decimal)d;
					break;
				case decimal dec:
					param.DecimalValue = (BlittableDecimal)dec;
					break;
				case bool bln:
					param.BoolValue = bln;
					break;
				case Enum e:
					param.NumValue = e.To<long>();
					break;
				case IRange r:
				{
					var storage = new SettingsStorage();

					if (r.HasMinValue)
						storage.SetValue(nameof(r.Min), r.Min.ToStorage());

					if (r.HasMaxValue)
						storage.SetValue(nameof(r.Max), r.Max.ToStorage());

					if (storage.Count > 0)
						stringValue = storage.Serialize();

					break;
				}
				case IPersistable p:
				{
					var storage = p.Save();

					if (storage.Count > 0)
						stringValue = storage.Serialize();

					break;
				}
				default:
					//stringValue = Paths.CreateSerializer(paramType).Serialize(conParam.Value);
					break;
			}

			if (stringValue != null)
			{
				param.StringValueLen = stringValue.Length;
			}

			var paramBuff = new byte[paramSize];

			var rowPtr = param.StructToPtr();
			rowPtr.CopyTo(paramBuff);
			rowPtr.FreeHGlobal();

			result.AddRange(paramBuff);

			if (version > SnapshotVersions.V20)
				result.AddRange(paramTypeName.UTF8());

			if (stringValue == null)
				continue;

			result.AddRange(stringValue);
		}

		return [.. result];
	}

	ExecutionMessage ISnapshotSerializer<string, ExecutionMessage>.Deserialize(Version version, byte[] buffer)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		using (var handle = new GCHandle<byte[]>(buffer))
		{
			var ptr = handle.CreatePointer();

			var snapshot = ptr.ToStruct<TransactionSnapshot>(true);

			var execMsg = new ExecutionMessage
			{
				SecurityId = snapshot.SecurityId.ToSecurityId(),
				PortfolioName = snapshot.PortfolioName,
				ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
				LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),

				DataTypeEx = DataType.Transactions,

				//OriginalTransactionId = snapshot.OriginalTransactionId,
				TransactionId = snapshot.TransactionId,

				HasOrderInfo = snapshot.HasOrderInfo,
				//HasTradeInfo = snapshot.HasTradeInfo,

				
			};

			execMsg.BrokerCode = snapshot.BrokerCode;
			execMsg.ClientCode = snapshot.ClientCode;

			execMsg.Comment = snapshot.Comment;
			execMsg.SystemComment = snapshot.SystemComment;

			execMsg.Currency = snapshot.Currency == null ? null : (CurrencyTypes)snapshot.Currency.Value;
			execMsg.DepoName = snapshot.DepoName;
			execMsg.Error = snapshot.Error.IsEmpty() ? null : new InvalidOperationException(snapshot.Error);

			execMsg.ExpiryDate = snapshot.ExpiryDate?.To<DateTimeOffset>();
			execMsg.IsMarketMaker = snapshot.IsMarketMaker?.ToBool();
			execMsg.MarginMode = (MarginModes?)snapshot.MarginMode;
			execMsg.IsManual = snapshot.IsManual?.ToBool();
			execMsg.Side = (Sides)snapshot.Side;
			execMsg.OrderId = snapshot.OrderId;
			execMsg.OrderStringId = snapshot.OrderStringId;
			execMsg.OrderBoardId = snapshot.OrderBoardId;
			execMsg.OrderPrice = snapshot.OrderPrice;
			execMsg.OrderVolume = snapshot.OrderVolume;
			execMsg.VisibleVolume = snapshot.VisibleVolume;
			execMsg.OrderType = snapshot.OrderType?.ToEnum<OrderTypes>();
			execMsg.OrderState = snapshot.OrderState?.ToEnum<OrderStates>();
			execMsg.OrderStatus = snapshot.OrderStatus;
			execMsg.Balance = snapshot.Balance;
			execMsg.UserOrderId = snapshot.UserOrderId;
			execMsg.StrategyId = snapshot.StrategyId;
			execMsg.OriginSide = snapshot.OriginSide?.ToEnum<Sides>();
			execMsg.Latency = snapshot.Latency == null ? null : TimeSpan.FromTicks(snapshot.Latency.Value);
			execMsg.PnL = snapshot.PnL;
			execMsg.Position = snapshot.Position;
			execMsg.Slippage = snapshot.Slippage;
			execMsg.Commission = snapshot.Commission;
			execMsg.TradePrice = snapshot.TradePrice;
			execMsg.TradeVolume = snapshot.TradeVolume;
			execMsg.TradeStatus = snapshot.TradeStatus;
			execMsg.TradeId = snapshot.TradeId;
			execMsg.TradeStringId = snapshot.TradeStringId;
			execMsg.OpenInterest = snapshot.OpenInterest;
			execMsg.IsSystem = snapshot.IsSystem?.ToBool();
			execMsg.TimeInForce = snapshot.OrderTif?.ToEnum<TimeInForce>();

			execMsg.AveragePrice = snapshot.AveragePrice;
			execMsg.Yield = snapshot.Yield;
			execMsg.MinVolume = snapshot.MinVolume;
			execMsg.PositionEffect = snapshot.PositionEffect?.ToEnum<OrderPositionEffects>();
			execMsg.PostOnly = snapshot.PostOnly?.ToBool();
			execMsg.Initiator = snapshot.Initiator?.ToBool();
			execMsg.SeqNum = snapshot.SeqNum;
			execMsg.BuildFrom = snapshot.BuildFrom;
			execMsg.Leverage = snapshot.Leverage;

			//var paramSize = (version > SnapshotVersions.V20 ? typeof(TransactionConditionParamV21) : typeof(TransactionConditionParamV20)).SizeOf();

			if (!snapshot.ConditionType.IsEmpty())
			{
				execMsg.Condition = snapshot.ConditionType.To<Type>().CreateInstance<OrderCondition>();
				execMsg.Condition.Parameters.Clear(); // removing pre-defined values
			}

			for (var i = 0; i < snapshot.ConditionParamsCount; i++)
			{
				var param = ptr.ToStruct<TransactionConditionParamV21>(true);

				var typeBuffer = new byte[param.ValueTypeLen];
				ptr.CopyTo(typeBuffer, true);

				var paramTypeName = typeBuffer.UTF8();

				try
				{
					var paramType = paramTypeName.To<Type>();

					object value;

					if (param.NumValue != null)
						value = (long)param.NumValue;
					else if (param.DecimalValue != null)
						value = (decimal)param.DecimalValue;
					else if (param.BoolValue != null)
						value = (bool)param.BoolValue;
					//else if (paramType == typeof(Unit))
					//	value = param.StringValue.ToUnit();
					else if (param.StringValueLen > 0)
					{
						var strBuffer = new byte[param.StringValueLen];
						ptr.CopyTo(strBuffer, true);

						if (paramType.IsPersistable())
						{
							value = strBuffer.Deserialize<SettingsStorage>()?.Load(paramType) ?? throw new InvalidOperationException("unable to deserialize param value");
						}
						else if (paramType.Is<IRange>())
						{
							var range = paramType.CreateInstance<IRange>();

							var storage = strBuffer.Deserialize<SettingsStorage>() ?? throw new InvalidOperationException("unable to deserialize IRange param value");

							if (storage.ContainsKey(nameof(range.Min)))
								range.Min = storage.GetValue<SettingsStorage>(nameof(range.Min)).FromStorage();

							if (storage.ContainsKey(nameof(range.Max)))
								range.Max = storage.GetValue<SettingsStorage>(nameof(range.Max)).FromStorage();

							value = range;
						}
						else
						{
							value = null;
							//value = Paths.CreateSerializer(paramType).Deserialize(strBuffer);
						}
					}
					else
						value = null;

					value = value.To(paramType);
					execMsg.Condition.Parameters.Add(param.Name, value);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}

			return execMsg;
		}
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

		//if (changes.HasOrderInfo)
		//	message.Side = changes.Side;

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

		//if (changes.OriginalTransactionId != default)
		//	message.OriginalTransactionId = changes.OriginalTransactionId;

		//if (changes.TransactionId != default)
		//	message.TransactionId = changes.TransactionId;

		if (changes.HasOrderInfo)
			message.HasOrderInfo = true;

		//if (changes.HasTradeInfo)
		//	message.HasTradeInfo = true;

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
