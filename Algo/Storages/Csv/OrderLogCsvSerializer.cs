namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The order log serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderLogCsvSerializer"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="encoding">Encoding.</param>
public class OrderLogCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<ExecutionMessage>(securityId, encoding)
{
	/// <inheritdoc />
	public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
	{
		return new CsvMetaInfo(date, Encoding, r => r.ReadNullableLong());
	}

	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, ExecutionMessage data, IMarketDataMetaInfo metaInfo)
	{
		writer.WriteRow(
		[
			data.ServerTime.WriteTime(),
			data.ServerTime.ToString("zzz"),
			data.TransactionId.ToString(),
			data.OrderId.ToString(),
			data.OrderPrice.ToString(),
			data.OrderVolume.ToString(),
			data.Side.To<int?>().ToString(),
			data.OrderState.To<int?>().ToString(),
			data.TimeInForce.To<int?>().ToString(),
			data.TradeId.ToString(),
			data.TradePrice.ToString(),
			data.PortfolioName,
			data.IsSystem.To<int?>().ToString(),
			data.Balance.ToString(),
			data.SeqNum.DefaultAsNull().ToString(),
			data.OrderStringId,
			data.TradeStringId,
			data.OrderBuyId.ToString(),
			data.OrderSellId.ToString(),
			data.IsUpTick.To<int?>().ToString(),
			data.Yield.ToString(),
			data.TradeStatus.ToString(),
			data.OpenInterest.ToString(),
			data.OriginSide.To<int?>().ToString(),
			data.TradeVolume.ToString()
		]);

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
		metaInfo.LastId = data.TransactionId;
	}

	/// <inheritdoc />
	protected override ExecutionMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var ol = new ExecutionMessage
		{
			SecurityId = SecurityId,
			DataTypeEx = DataType.OrderLog,
			ServerTime = reader.ReadTime(metaInfo.Date),
			TransactionId = reader.ReadLong(),
			OrderId = reader.ReadNullableLong(),
			OrderPrice = reader.ReadDecimal(),
			OrderVolume = reader.ReadDecimal(),
			Side = reader.ReadEnum<Sides>(),
			OrderState = reader.ReadEnum<OrderStates>(),
			TimeInForce = reader.ReadNullableEnum<TimeInForce>(),
			TradeId = reader.ReadNullableLong(),
			TradePrice = reader.ReadNullableDecimal(),
			PortfolioName = reader.ReadString(),
			IsSystem = reader.ReadNullableBool(),
		};

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			ol.Balance = reader.ReadNullableDecimal();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			ol.SeqNum = reader.ReadNullableLong() ?? 0L;

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			ol.OrderStringId = reader.ReadString();
			ol.TradeStringId = reader.ReadString();

			ol.OrderBuyId = reader.ReadNullableLong();
			ol.OrderSellId = reader.ReadNullableLong();

			ol.IsUpTick = reader.ReadNullableBool();
			ol.Yield = reader.ReadNullableDecimal();
			ol.TradeStatus = reader.ReadNullableLong();
			ol.OpenInterest = reader.ReadNullableDecimal();
			ol.OriginSide = reader.ReadNullableEnum<Sides>();
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			ol.TradeVolume = reader.ReadNullableDecimal();

		return ol;
	}
}