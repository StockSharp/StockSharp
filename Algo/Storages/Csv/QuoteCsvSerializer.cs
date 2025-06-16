namespace StockSharp.Algo.Storages.Csv;

class NullableTimeQuoteChange
{
	public DateTimeOffset ServerTime { get; set; }
	public DateTimeOffset LocalTime { get; set; }
	public QuoteChange? Quote { get; set; }
	public Sides Side { get; set; }
	public QuoteChangeStates? State { get; set; }
	public DataType BuildFrom { get; set; }
	public long? SeqNum { get; set; }
}

/// <summary>
/// The quote serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QuoteCsvSerializer"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="encoding">Encoding.</param>
class QuoteCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<NullableTimeQuoteChange>(securityId, encoding)
{
	/// <inheritdoc />
	public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
	{
		return new CsvMetaInfo(date, Encoding, null, r =>
		{
			r.Skip(8);
			return r.ReadNullableEnum<QuoteChangeStates>() != null;
		});
	}

	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, NullableTimeQuoteChange data, IMarketDataMetaInfo metaInfo)
	{
		var quote = data.Quote;

		if (quote != null && quote.Value.Volume < 0)
			throw new ArgumentOutOfRangeException(nameof(data), quote.Value.Volume, LocalizedStrings.InvalidValue);

		writer.WriteRow(new[]
		{
			data.ServerTime.WriteTime(),
			data.ServerTime.ToString("zzz"),
			quote?.Price.To<string>(),
			quote?.Volume.To<string>(),
			data.Side.To<int>().ToString(),
			quote?.OrdersCount.To<string>(),
			quote?.Condition.To<int>().ToString(),
			quote?.StartPosition.To<string>(),
			quote?.EndPosition.To<string>(),
			quote?.Action.To<int?>().ToString(),
			data?.State.To<int?>().ToString(),
			data?.SeqNum.ToString(),
		}.Concat(data.BuildFrom.ToCsv()));

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
	}

	/// <inheritdoc />
	protected override NullableTimeQuoteChange Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var quote = new NullableTimeQuoteChange
		{
			ServerTime = reader.ReadTime(metaInfo.Date),
		};

		var price = reader.ReadNullableDecimal();
		var volume = reader.ReadNullableDecimal();

		quote.Side = reader.ReadEnum<Sides>();

		int? ordersCount = null;

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			ordersCount = reader.ReadNullableInt();

		QuoteConditions condition = default;

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			condition = reader.ReadNullableEnum<QuoteConditions>() ?? default;

		QuoteChange? qq = null;

		if (price != null)
		{
			qq = quote.Quote = new QuoteChange
			{
				Price = price.Value,
				Volume = volume ?? 0,
				OrdersCount = ordersCount,
				Condition = condition,
			};
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			var startPosition = reader.ReadNullableInt();
			var endPosition = reader.ReadNullableInt();
			var action = reader.ReadNullableEnum<QuoteChangeActions>();

			if (qq != null)
			{
				var temp = qq.Value;

				temp.StartPosition = startPosition;
				temp.EndPosition = endPosition;
				temp.Action = action;

				quote.Quote = temp;
			}
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			quote.State = reader.ReadNullableEnum<QuoteChangeStates>();
			quote.SeqNum = reader.ReadNullableLong();
			quote.BuildFrom = reader.ReadBuildFrom();
		}

		return quote;
	}
}