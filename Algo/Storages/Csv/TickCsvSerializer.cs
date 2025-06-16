namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The tick serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TickCsvSerializer"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="encoding">Encoding.</param>
public class TickCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<ExecutionMessage>(securityId, encoding)
{
	/// <inheritdoc />
	public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
	{
		return new CsvMetaInfo(date, Encoding, r => r.ReadNullableLong());
	}

	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, ExecutionMessage data, IMarketDataMetaInfo metaInfo)
	{
		writer.WriteRow(new[]
		{
			data.ServerTime.WriteTime(),
			data.ServerTime.ToString("zzz"),
			data.TradeId.ToString(),
			data.TradePrice.ToString(),
			data.TradeVolume.ToString(),
			data.OriginSide.To<int?>().ToString(),
			data.OpenInterest.ToString(),
			data.IsSystem.To<int?>().ToString(),
			data.IsUpTick.To<int?>().ToString(),
			data.TradeStringId,
			data.Currency.To<int?>().ToString(),
		}.Concat(data.BuildFrom.ToCsv()).Concat(
		[
			data.SeqNum.DefaultAsNull().ToString(),
			data.OrderBuyId.ToString(),
			data.OrderSellId.ToString(),
		]));

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
		metaInfo.LastId = data.TradeId;
	}

	/// <inheritdoc />
	protected override ExecutionMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var execMsg = new ExecutionMessage
		{
			SecurityId = SecurityId,
			DataTypeEx = DataType.Ticks,
			ServerTime = reader.ReadTime(metaInfo.Date),
			TradeId = reader.ReadNullableLong(),
			TradePrice = reader.ReadNullableDecimal(),
			TradeVolume = reader.ReadNullableDecimal(),
			OriginSide = reader.ReadNullableEnum<Sides>(),
			OpenInterest = reader.ReadNullableDecimal(),
			IsSystem = reader.ReadNullableBool(),
		};

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			execMsg.IsUpTick = reader.ReadNullableBool();
			execMsg.TradeStringId = reader.ReadString();
			execMsg.Currency = reader.ReadNullableEnum<CurrencyTypes>();
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			execMsg.BuildFrom = reader.ReadBuildFrom();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			execMsg.SeqNum = reader.ReadNullableLong() ?? 0L;

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			execMsg.OrderBuyId = reader.ReadNullableLong();
			execMsg.OrderSellId = reader.ReadNullableLong();
		}

		return execMsg;
	}
}