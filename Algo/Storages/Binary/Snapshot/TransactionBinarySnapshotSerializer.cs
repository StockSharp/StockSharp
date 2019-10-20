namespace StockSharp.Algo.Storages.Binary.Snapshot
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

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

			[Obsolete]
			public long Obsolete2;

			public long TransactionId;

			public bool HasOrderInfo;
			public bool HasTradeInfo;

			public decimal OrderPrice;
			public long? OrderId;
			//public long OrderUserId;
			public decimal? OrderVolume;
			public byte? OrderType;
			//public byte OrderSide;
			public byte? OrderTif;

			public byte? IsSystem;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			public string OrderStringId;

			public long? TradeId;
			public decimal? TradePrice;
			public decimal? TradeVolume;

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
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			[Obsolete]
			public string Obsolete1;

			public byte? IsMarketMaker;
			public byte Side;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			public string OrderBoardId;

			public decimal? VisibleVolume;
			public byte? OrderState;
			public long? OrderStatus;
			public decimal? Balance;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			public string UserOrderId;

			public byte? OriginSide;
			public long? Latency;
			public decimal? PnL;
			public decimal? Position;
			public decimal? Slippage;
			public decimal? Commission;
			public int? TradeStatus;
			
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
			public string TradeStringId;

			public decimal? OpenInterest;
			public byte? IsMargin;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S256)]
			public string ConditionType;

			public int ConditionParamsCount;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct TransactionConditionParamV20
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S32)]
			public string Name;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S256)]
			public string ValueType;

			public long? NumValue;
			public decimal? DecimalValue;
			public bool? BoolValue;

			public int StringValueLen;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct TransactionConditionParamV21
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S32)]
			public string Name;

			public int ValueTypeLen;

			public long? NumValue;
			public decimal? DecimalValue;
			public bool? BoolValue;

			public int StringValueLen;
		}

		Version ISnapshotSerializer<string, ExecutionMessage>.Version { get; } = SnapshotVersions.V21;

		string ISnapshotSerializer<string, ExecutionMessage>.Name => "Transactions";

		byte[] ISnapshotSerializer<string, ExecutionMessage>.Serialize(Version version, ExecutionMessage message)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.ExecutionType != ExecutionTypes.Transaction)
				throw new ArgumentOutOfRangeException(nameof(message), message.ExecutionType, LocalizedStrings.Str1695Params.Put(message));

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
				Currency = message.Currency == null ? (short?)null : (short)message.Currency.Value,
				DepoName = message.DepoName.VerifySize(Sizes.S100),
				Error = (message.Error?.Message).VerifySize(Sizes.S200),
				ExpiryDate = message.ExpiryDate?.To<long>(),
				//Obsolete1 = message.PortfolioName,
				IsMarketMaker = message.IsMarketMaker == null ? (byte?)null : (byte)(message.IsMarketMaker.Value ? 1 : 0),
				IsMargin = message.IsMargin == null ? (byte?)null : (byte)(message.IsMargin.Value ? 1 : 0),
				Side = (byte)message.Side,
				OrderId = message.OrderId,
				OrderStringId = message.OrderStringId.VerifySize(Sizes.S100),
				OrderBoardId = message.OrderBoardId.VerifySize(Sizes.S100),
				OrderPrice = message.OrderPrice,
				OrderVolume = message.OrderVolume,
				VisibleVolume = message.VisibleVolume,
				OrderType = message.OrderType == null ? (byte?)null : (byte)message.OrderType.Value,
				OrderState = message.OrderState == null ? (byte?)null : (byte)message.OrderState.Value,
				OrderStatus = message.OrderStatus,
				Balance = message.Balance,
				UserOrderId = message.UserOrderId.VerifySize(Sizes.S100),
				OriginSide = message.OriginSide == null ? (byte?)null : (byte)message.OriginSide.Value,
				Latency = message.Latency?.Ticks,
				PnL = message.PnL,
				Position = message.Position,
				Slippage = message.Slippage,
				Commission = message.Commission,
				TradePrice = message.TradePrice,
				TradeVolume = message.TradeVolume,
				TradeStatus = message.TradeStatus,
				TradeId = message.TradeId,
				TradeStringId = message.TradeStringId.VerifySize(Sizes.S100),
				OpenInterest = message.OpenInterest,
				IsSystem = message.IsSystem == null ? (byte?)null : (byte)(message.IsSystem.Value ? 1 : 0),
				OrderTif = message.TimeInForce == null ? (byte?)null : (byte)message.TimeInForce.Value,
				ConditionType = (message.Condition?.GetType().GetTypeName(false)).VerifySize(Sizes.S256),
			};

			var conParams = message.Condition?.Parameters.Where(p => p.Value != null).ToArray() ?? ArrayHelper.Empty<KeyValuePair<string, object>>();

			snapshot.ConditionParamsCount = conParams.Length;

			var paramSize = (version > SnapshotVersions.V20 ? typeof(TransactionConditionParamV21) : typeof(TransactionConditionParamV20)).SizeOf();
			var snapshotSize = typeof(TransactionSnapshot).SizeOf();

			var result = new List<byte>();

			var buffer = new byte[snapshotSize];

			var ptr = snapshot.StructToPtr();
			Marshal.Copy(ptr, buffer, 0, snapshotSize);
			Marshal.FreeHGlobal(ptr);

			result.AddRange(buffer);

			foreach (var conParam in conParams)
			{
				var paramType = conParam.Value.GetType();

				var paramTypeName = paramType.GetTypeAsString(false);

				dynamic param;

				if (version > SnapshotVersions.V20)
				{
					param = new TransactionConditionParamV21
					{
						ValueTypeLen = paramTypeName.UTF8().Length
					};
				}
				else
				{
					param = new TransactionConditionParamV20
					{
						ValueType = paramTypeName.VerifySize(Sizes.S256),
					};
				}

				param.Name = conParam.Key.VerifySize(Sizes.S32);

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
						param.DecimalValue = (decimal)f;
						break;
					case double d:
						param.DecimalValue = (decimal)d;
						break;
					case decimal dec:
						param.DecimalValue = dec;
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
							storage.SetValue(nameof(r.Min), r.Min);

						if (r.HasMaxValue)
							storage.SetValue(nameof(r.Max), r.Max);

						if (storage.Count > 0)
							stringValue = new XmlSerializer<SettingsStorage>().Serialize(storage);

						break;
					}
					case IPersistable p:
					{
						var storage = p.Save();

						if (storage.Count > 0)
							stringValue = new XmlSerializer<SettingsStorage>().Serialize(storage);

						break;
					}
					default:
						stringValue = typeof(XmlSerializer<>).Make(paramType).CreateInstance<ISerializer>().Serialize(conParam.Value);
						break;
				}

				if (stringValue != null)
				{
					param.StringValueLen = stringValue.Length;
				}

				var paramBuff = new byte[paramSize];

				var rowPtr = version > SnapshotVersions.V20 ? ((TransactionConditionParamV21)param).StructToPtr() : ((TransactionConditionParamV20)param).StructToPtr();
				Marshal.Copy(rowPtr, paramBuff, 0, paramSize);
				Marshal.FreeHGlobal(rowPtr);

				result.AddRange(paramBuff);

				if (version > SnapshotVersions.V20)
					result.AddRange(paramTypeName.UTF8());

				if (stringValue == null)
					continue;

				result.AddRange(stringValue);
			}

			return result.ToArray();
		}

		ExecutionMessage ISnapshotSerializer<string, ExecutionMessage>.Deserialize(Version version, byte[] buffer)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// Pin the managed memory while, copy it out the data, then unpin it
			using (var handle = new GCHandle<byte[]>(buffer, GCHandleType.Pinned))
			{
				var ptr = handle.Value.AddrOfPinnedObject();

				var snapshot = ptr.ToStruct<TransactionSnapshot>();

				var execMsg = new ExecutionMessage
				{
					SecurityId = snapshot.SecurityId.ToSecurityId(),
					PortfolioName = snapshot.PortfolioName,
					ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
					LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),

					ExecutionType = ExecutionTypes.Transaction,

					//OriginalTransactionId = snapshot.OriginalTransactionId,
					TransactionId = snapshot.TransactionId,

					HasOrderInfo = snapshot.HasOrderInfo,
					HasTradeInfo = snapshot.HasTradeInfo,

					BrokerCode = snapshot.BrokerCode,
					ClientCode = snapshot.ClientCode,

					Comment = snapshot.Comment,
					SystemComment = snapshot.SystemComment,

					Currency = snapshot.Currency == null ? (CurrencyTypes?)null : (CurrencyTypes)snapshot.Currency.Value,
					DepoName = snapshot.DepoName,
					Error = snapshot.Error.IsEmpty() ? null : new InvalidOperationException(snapshot.Error),

					ExpiryDate = snapshot.ExpiryDate?.To<DateTimeOffset>(),
					IsMarketMaker = snapshot.IsMarketMaker == null ? (bool?)null : (snapshot.IsMarketMaker.Value == 1),
					IsMargin = snapshot.IsMargin == null ? (bool?)null : (snapshot.IsMargin.Value == 1),
					Side = (Sides)snapshot.Side,
					OrderId = snapshot.OrderId,
					OrderStringId = snapshot.OrderStringId,
					OrderBoardId = snapshot.OrderBoardId,
					OrderPrice = snapshot.OrderPrice,
					OrderVolume = snapshot.OrderVolume,
					VisibleVolume = snapshot.VisibleVolume,
					OrderType = snapshot.OrderType == null ? (OrderTypes?)null : (OrderTypes)snapshot.OrderType.Value,
					OrderState = snapshot.OrderState == null ? (OrderStates?)null : (OrderStates)snapshot.OrderState.Value,
					OrderStatus = snapshot.OrderStatus,
					Balance = snapshot.Balance,
					UserOrderId = snapshot.UserOrderId,
					OriginSide = snapshot.OriginSide == null ? (Sides?)null : (Sides)snapshot.OriginSide.Value,
					Latency = snapshot.Latency == null ? (TimeSpan?)null : TimeSpan.FromTicks(snapshot.Latency.Value),
					PnL = snapshot.PnL,
					Position = snapshot.Position,
					Slippage = snapshot.Slippage,
					Commission = snapshot.Commission,
					TradePrice = snapshot.TradePrice,
					TradeVolume = snapshot.TradeVolume,
					TradeStatus = snapshot.TradeStatus,
					TradeId = snapshot.TradeId,
					TradeStringId = snapshot.TradeStringId,
					OpenInterest = snapshot.OpenInterest,
					IsSystem = snapshot.IsSystem == null ? (bool?)null : (snapshot.IsSystem.Value == 1),
					TimeInForce = snapshot.OrderTif == null ? (TimeInForce?)null : (TimeInForce)snapshot.OrderTif.Value,
				};

				ptr += typeof(TransactionSnapshot).SizeOf();

				var paramSize = (version > SnapshotVersions.V20 ? typeof(TransactionConditionParamV21) : typeof(TransactionConditionParamV20)).SizeOf();

				if (!snapshot.ConditionType.IsEmpty())
				{
					execMsg.Condition = snapshot.ConditionType.To<Type>().CreateInstance<OrderCondition>();
					execMsg.Condition.Parameters.Clear(); // removing pre-defined values
				}

				for (var i = 0; i < snapshot.ConditionParamsCount; i++)
				{
					dynamic param;
					string paramTypeName;

					if (version > SnapshotVersions.V20)
					{
						var s = ptr.ToStruct<TransactionConditionParamV21>();

						ptr += paramSize;

						var typeBuffer = new byte[s.ValueTypeLen];
						Marshal.Copy(ptr, typeBuffer, 0, typeBuffer.Length);

						paramTypeName = typeBuffer.UTF8();

						ptr += s.ValueTypeLen;

						param = s;
					}
					else
					{
						var s = ptr.ToStruct<TransactionConditionParamV20>();
						paramTypeName = s.ValueType;

						ptr += paramSize;

						param = s;
					}

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
							var strBuffer = new byte[(int)param.StringValueLen];
							Marshal.Copy(ptr, strBuffer, 0, strBuffer.Length);
							ptr += strBuffer.Length;

							if (typeof(IPersistable).IsAssignableFrom(paramType))
							{
								var persistable = paramType.CreateInstance<IPersistable>();
								persistable.Load(new XmlSerializer<SettingsStorage>().Deserialize(strBuffer));
								value = persistable;
							}
							else if (typeof(IRange).IsAssignableFrom(paramType))
							{
								var range = paramType.CreateInstance<IRange>();

								var storage = new XmlSerializer<SettingsStorage>().Deserialize(strBuffer);

								if (storage.ContainsKey(nameof(range.Min)))
									range.Min = storage.GetValue<object>(nameof(range.Min));

								if (storage.ContainsKey(nameof(range.Max)))
									range.Max = storage.GetValue<object>(nameof(range.Max));

								value = range;
							}
							else
							{
								value = typeof(XmlSerializer<>).Make(paramType).CreateInstance<ISerializer>().Deserialize(strBuffer);
							}
						}
						else
							value = null;
					
						value = value.To(paramType);
						execMsg.Condition.Parameters.Add((string)param.Name, value);
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

		ExecutionMessage ISnapshotSerializer<string, ExecutionMessage>.CreateCopy(ExecutionMessage message)
		{
			if (message.SecurityId.IsDefault())
				throw new ArgumentException(message.ToString());

			var copy = (ExecutionMessage)message.Clone();

			//if (copy.TransactionId == 0)
			//	copy.TransactionId = message.OriginalTransactionId;

			//copy.OriginalTransactionId = 0;

			return copy;
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

			if (changes.Currency != null)
				message.Currency = changes.Currency;

			if (changes.Condition != null)
				message.Condition = changes.Condition.Clone();

			if (!changes.DepoName.IsEmpty())
				message.DepoName = changes.DepoName;

			if (changes.Error != null)
				message.Error = changes.Error;

			if (changes.ExpiryDate != null)
				message.ExpiryDate = changes.ExpiryDate;

			if (!changes.PortfolioName.IsEmpty())
				message.PortfolioName = changes.PortfolioName;

			if (changes.IsMarketMaker != null)
				message.IsMarketMaker = changes.IsMarketMaker;

			if (changes.HasOrderInfo)
				message.Side = changes.Side;

			if (changes.OrderId != null)
				message.OrderId = changes.OrderId;

			if (!changes.OrderBoardId.IsEmpty())
				message.OrderBoardId = changes.OrderBoardId;

			if (!changes.OrderStringId.IsEmpty())
				message.OrderStringId = changes.OrderStringId;

			if (changes.OrderType != null)
				message.OrderType = changes.OrderType;

			if (changes.OrderPrice != 0)
				message.OrderPrice = changes.OrderPrice;

			if (changes.OrderVolume != null)
				message.OrderVolume = changes.OrderVolume;

			if (changes.VisibleVolume != null)
				message.VisibleVolume = changes.VisibleVolume;

			if (changes.OrderState != null)
				message.OrderState = changes.OrderState;

			if (changes.OrderStatus != null)
				message.OrderStatus = changes.OrderStatus;

			if (changes.Balance != null)
				message.Balance = changes.Balance;

			if (!changes.UserOrderId.IsEmpty())
				message.UserOrderId = changes.UserOrderId;

			if (changes.OriginSide != null)
				message.OriginSide = changes.OriginSide;

			if (changes.Latency != null)
				message.Latency = changes.Latency;

			if (changes.PnL != null)
				message.PnL = changes.PnL;

			if (changes.Position != null)
				message.Position = changes.Position;

			if (changes.Slippage != null)
				message.Slippage = changes.Slippage;

			if (changes.Commission != null)
				message.Commission = changes.Commission;

			if (changes.TradePrice != null)
				message.TradePrice = changes.TradePrice;

			if (changes.TradeVolume != null)
				message.TradeVolume = changes.TradeVolume;

			if (changes.TradeStatus != null)
				message.TradeStatus = changes.TradeStatus;

			if (changes.TradeId != null)
				message.TradeId = changes.TradeId;

			if (!changes.TradeStringId.IsEmpty())
				message.TradeStringId = changes.TradeStringId;

			if (changes.OpenInterest != null)
				message.OpenInterest = changes.OpenInterest;

			if (changes.IsMargin != null)
				message.IsMargin = changes.IsMargin;

			if (changes.TimeInForce != null)
				message.TimeInForce = changes.TimeInForce;

			//if (changes.OriginalTransactionId != 0)
			//	message.OriginalTransactionId = changes.OriginalTransactionId;

			//if (changes.TransactionId != 0)
			//	message.TransactionId = changes.TransactionId;

			if (changes.HasOrderInfo)
				message.HasOrderInfo = true;

			if (changes.HasTradeInfo)
				message.HasTradeInfo = true;

			message.LocalTime = changes.LocalTime;
			message.ServerTime = changes.ServerTime;
		}

		DataType ISnapshotSerializer<string, ExecutionMessage>.DataType => DataType.Transactions;
	}
}