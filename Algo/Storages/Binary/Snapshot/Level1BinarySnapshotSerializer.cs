namespace StockSharp.Algo.Storages.Binary.Snapshot;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="Level1ChangeMessage"/>.
/// </summary>
public class Level1BinarySnapshotSerializer : ISnapshotSerializer<SecurityId, Level1ChangeMessage>
{
	Version ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Version { get; } = SnapshotVersions.V24;

	string ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Name => "Level1";

	byte[] ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Serialize(Version version, Level1ChangeMessage message)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		// Estimate buffer size
		var secIdBytes = message.SecurityId.ToStringId().UTF8();
		var estimatedSize =
			sizeof(int) + secIdBytes.Length + // SecurityId length + bytes
			sizeof(long) + // LastChangeServerTime
			sizeof(long) + // LastChangeLocalTime
			sizeof(long) + // SeqNum
			sizeof(byte) + (message.BuildFrom != null ? SnapshotDataType.Size : 0) + // BuildFrom (hasValue + value)
			sizeof(int) + // changes count
			message.Changes.Count * (sizeof(int) + sizeof(byte) + 20); // approximate per field

		var buffer = new byte[estimatedSize];
		var writer = new SpanWriter(buffer);

		// Write base fields
		writer.WriteInt32(secIdBytes.Length);
		writer.WriteSpan(secIdBytes);

		writer.WriteInt64(message.ServerTime.To<long>());
		writer.WriteInt64(message.LocalTime.To<long>());
		writer.WriteInt64(message.SeqNum);

		// Write BuildFrom
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
				case Level1Fields.OpenPrice:
				case Level1Fields.HighPrice:
				case Level1Fields.LowPrice:
				case Level1Fields.ClosePrice:
				case Level1Fields.StepPrice:
				case Level1Fields.ImpliedVolatility:
				case Level1Fields.TheorPrice:
				case Level1Fields.OpenInterest:
				case Level1Fields.MinPrice:
				case Level1Fields.MaxPrice:
				case Level1Fields.BidsVolume:
				case Level1Fields.AsksVolume:
				case Level1Fields.HistoricalVolatility:
				case Level1Fields.Delta:
				case Level1Fields.Gamma:
				case Level1Fields.Vega:
				case Level1Fields.Theta:
				case Level1Fields.MarginBuy:
				case Level1Fields.MarginSell:
				case Level1Fields.PriceStep:
				case Level1Fields.VolumeStep:
				case Level1Fields.LastTradePrice:
				case Level1Fields.LastTradeVolume:
				case Level1Fields.Volume:
				case Level1Fields.AveragePrice:
				case Level1Fields.SettlementPrice:
				case Level1Fields.Change:
				case Level1Fields.BestBidPrice:
				case Level1Fields.BestBidVolume:
				case Level1Fields.BestAskPrice:
				case Level1Fields.BestAskVolume:
				case Level1Fields.Rho:
				case Level1Fields.AccruedCouponIncome:
				case Level1Fields.HighBidPrice:
				case Level1Fields.LowAskPrice:
				case Level1Fields.Yield:
				case Level1Fields.VWAP:
				case Level1Fields.Beta:
				case Level1Fields.AverageTrueRange:
				case Level1Fields.Duration:
				case Level1Fields.Turnover:
				case Level1Fields.SpreadMiddle:
				case Level1Fields.PriceEarnings:
				case Level1Fields.ForwardPriceEarnings:
				case Level1Fields.PriceEarningsGrowth:
				case Level1Fields.PriceSales:
				case Level1Fields.PriceBook:
				case Level1Fields.PriceCash:
				case Level1Fields.PriceFreeCash:
				case Level1Fields.Payout:
				case Level1Fields.SharesOutstanding:
				case Level1Fields.SharesFloat:
				case Level1Fields.FloatShort:
				case Level1Fields.ShortRatio:
				case Level1Fields.ReturnOnAssets:
				case Level1Fields.ReturnOnEquity:
				case Level1Fields.ReturnOnInvestment:
				case Level1Fields.CurrentRatio:
				case Level1Fields.QuickRatio:
				case Level1Fields.LongTermDebtEquity:
				case Level1Fields.TotalDebtEquity:
				case Level1Fields.GrossMargin:
				case Level1Fields.OperatingMargin:
				case Level1Fields.ProfitMargin:
				case Level1Fields.HistoricalVolatilityWeek:
				case Level1Fields.HistoricalVolatilityMonth:
				case Level1Fields.IssueSize:
				case Level1Fields.BuyBackPrice:
				case Level1Fields.Dividend:
				case Level1Fields.AfterSplit:
				case Level1Fields.BeforeSplit:
				case Level1Fields.CommissionTaker:
				case Level1Fields.CommissionMaker:
				case Level1Fields.MinVolume:
				case Level1Fields.UnderlyingMinVolume:
				case Level1Fields.CouponValue:
				case Level1Fields.CouponPeriod:
				case Level1Fields.MarketPriceYesterday:
				case Level1Fields.MarketPriceToday:
				case Level1Fields.VWAPPrev:
				case Level1Fields.YieldVWAP:
				case Level1Fields.YieldVWAPPrev:
				case Level1Fields.Index:
				case Level1Fields.Imbalance:
				case Level1Fields.UnderlyingPrice:
				case Level1Fields.MaxVolume:
				case Level1Fields.LowBidPrice:
				case Level1Fields.HighAskPrice:
				case Level1Fields.LastTradeVolumeLow:
				case Level1Fields.LastTradeVolumeHigh:
				case Level1Fields.OptionMargin:
				case Level1Fields.OptionSyntheticMargin:
				case Level1Fields.Multiplier:
				case Level1Fields.LowBidVolume:
				case Level1Fields.HighAskVolume:
				case Level1Fields.UnderlyingBestBidPrice:
				case Level1Fields.UnderlyingBestAskPrice:
				case Level1Fields.MedianPrice:
				case Level1Fields.HighPrice52Week:
				case Level1Fields.LowPrice52Week:
					writer.WriteByte((byte)TypeCode.Decimal);
					writer.WriteDecimal((decimal)change.Value);
					break;

				// Int fields
				case Level1Fields.BidsCount:
				case Level1Fields.AsksCount:
				case Level1Fields.TradesCount:
				case Level1Fields.Decimals:
					writer.WriteByte((byte)TypeCode.Int32);
					writer.WriteInt32((int)change.Value);
					break;

				// Long fields
				case Level1Fields.LastTradeId:
					writer.WriteByte((byte)TypeCode.Int64);
					writer.WriteInt64((long)change.Value);
					break;

				// Byte fields
				case Level1Fields.State:
					writer.WriteByte((byte)TypeCode.Byte);
					writer.WriteByte((byte)(SecurityStates)change.Value);
					break;

				case Level1Fields.LastTradeOrigin:
					writer.WriteByte((byte)TypeCode.Byte);
					writer.WriteByte((byte)(Sides)change.Value);
					break;

				case Level1Fields.IsSystem:
					writer.WriteByte((byte)TypeCode.Byte);
					writer.WriteByte(((bool)change.Value).ToByte());
					break;

				// Bool fields
				case Level1Fields.LastTradeUpDown:
					writer.WriteByte((byte)TypeCode.Boolean);
					writer.WriteBoolean((bool)change.Value);
					break;

				// DateTime fields
				case Level1Fields.LastTradeTime:
				case Level1Fields.BestBidTime:
				case Level1Fields.BestAskTime:
				case Level1Fields.CouponDate:
				case Level1Fields.BuyBackDate:
					writer.WriteByte((byte)TypeCode.DateTime);
					writer.WriteInt64(change.Value.To<long>());
					break;

				// String fields
				case Level1Fields.LastTradeStringId:
					writer.WriteByte((byte)TypeCode.String);
					var strBytes = ((string)change.Value ?? string.Empty).UTF8();
					writer.WriteInt32(strBytes.Length);
					writer.WriteSpan(strBytes);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(change.Key), change.Key, "Unknown Level1Field");
			}
		}

		// Return actual written data
		return writer.GetWrittenSpan().ToArray();
	}

	Level1ChangeMessage ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Deserialize(Version version, byte[] buffer)
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

		var serverTime = reader.ReadInt64().To<DateTime>();
		var localTime = reader.ReadInt64().To<DateTime>();
		var seqNum = reader.ReadInt64();

		var hasBuildFrom = reader.ReadBoolean();
		SnapshotDataType? buildFrom = null;
		if (hasBuildFrom)
			buildFrom = SnapshotDataType.Read(ref reader);

		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime.UtcKind(),
			LocalTime = localTime.UtcKind(),
			BuildFrom = buildFrom,
			SeqNum = seqNum,
		};

		// Read changes count
		var changesCount = reader.ReadInt32();

		// Read each change
		for (var i = 0; i < changesCount; i++)
		{
			var fieldId = (Level1Fields)reader.ReadInt32();
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
					value = reader.ReadInt64();
					break;

				case TypeCode.Byte:
					var byteVal = reader.ReadByte();

					// Convert back to proper type based on field
					if (fieldId == Level1Fields.State)
						value = (SecurityStates)byteVal;
					else if (fieldId == Level1Fields.LastTradeOrigin)
						value = (Sides)byteVal;
					else if (fieldId == Level1Fields.IsSystem)
						value = byteVal.ToBool();
					else
						value = byteVal;
					break;

				case TypeCode.Boolean:
					value = reader.ReadBoolean();
					break;

				case TypeCode.DateTime:
					value = reader.ReadInt64().To<DateTime>();
					break;

				case TypeCode.String:
					var strLen = reader.ReadInt32();
					var strBytes = reader.ReadSpan(strLen);
					value = strBytes.ToArray().UTF8();
					break;

				default:
					throw new InvalidOperationException($"Unknown value type: {valueType}");
			}

			level1Msg.Add(fieldId, value);
		}

		return level1Msg;
	}

	SecurityId ISnapshotSerializer<SecurityId, Level1ChangeMessage>.GetKey(Level1ChangeMessage message)
	{
		return message.SecurityId;
	}

	void ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Update(Level1ChangeMessage message, Level1ChangeMessage changes)
	{
		var lastTradeFound = false;
		var bestBidFound = false;
		var bestAskFound = false;

		foreach (var pair in changes.Changes)
		{
			var field = pair.Key;

			if (!lastTradeFound)
			{
				if (field.IsLastTradeField())
				{
					message.Changes.Remove(Level1Fields.LastTradeUpDown);
					message.Changes.Remove(Level1Fields.LastTradeTime);
					message.Changes.Remove(Level1Fields.LastTradeId);
					message.Changes.Remove(Level1Fields.LastTradeOrigin);
					message.Changes.Remove(Level1Fields.LastTradePrice);
					message.Changes.Remove(Level1Fields.LastTradeVolume);

					lastTradeFound = true;
				}
			}

			if (!bestBidFound)
			{
				if (field.IsBestBidField())
				{
					message.Changes.Remove(Level1Fields.BestBidPrice);
					message.Changes.Remove(Level1Fields.BestBidTime);
					message.Changes.Remove(Level1Fields.BestBidVolume);

					bestBidFound = true;
				}
			}

			if (!bestAskFound)
			{
				if (field.IsBestAskField())
				{
					message.Changes.Remove(Level1Fields.BestAskPrice);
					message.Changes.Remove(Level1Fields.BestAskTime);
					message.Changes.Remove(Level1Fields.BestAskVolume);

					bestAskFound = true;
				}
			}

			message.Changes[pair.Key] = pair.Value;
		}

		message.LocalTime = changes.LocalTime;
		message.ServerTime = changes.ServerTime;

		if (changes.BuildFrom != default)
			message.BuildFrom = changes.BuildFrom;

		if (changes.SeqNum != default)
			message.SeqNum = changes.SeqNum;
	}

	DataType ISnapshotSerializer<SecurityId, Level1ChangeMessage>.DataType => DataType.Level1;
}
