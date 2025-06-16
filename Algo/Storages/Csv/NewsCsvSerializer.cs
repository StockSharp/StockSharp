namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The news serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BoardStateCsvSerializer"/>.
/// </remarks>
/// <param name="encoding">Encoding.</param>
public class NewsCsvSerializer(Encoding encoding) : CsvMarketDataSerializer<NewsMessage>(encoding)
{
	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, NewsMessage data, IMarketDataMetaInfo metaInfo)
	{
		writer.WriteRow(
		[
			data.ServerTime.WriteTime(),
			data.ServerTime.ToString("zzz"),
			data.Headline,
			data.Source,
			data.Url,
			data.Id,
			data.BoardCode,
			data.SecurityId?.SecurityCode,
			data.Priority?.To<string>(),
			data.Language,
			data.SecurityId?.BoardCode,
			data.ExpiryDate?.WriteDateTimeEx(),
			data.SeqNum.DefaultAsNull().ToString(),
		]);

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
	}

	/// <inheritdoc />
	protected override NewsMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var news = new NewsMessage
		{
			ServerTime = reader.ReadTime(metaInfo.Date),
			Headline = reader.ReadString(),
			Source = reader.ReadString(),
			Url = reader.ReadString(),
			Id = reader.ReadString(),
			BoardCode = reader.ReadString(),
		};

		var secCode = reader.ReadString();

		if (!secCode.IsEmpty())
			news.SecurityId = new SecurityId { SecurityCode = secCode };

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			news.Priority = reader.ReadNullableEnum<NewsPriorities>();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			news.Language = reader.ReadString();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
		{
			var boardCode = reader.ReadString();

			if (news.SecurityId != null)
			{
				var secId = news.SecurityId.Value;
				secId.BoardCode = boardCode;
				news.SecurityId = secId;
			}
		}

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			news.ExpiryDate = reader.ReadNullableDateTimeEx();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			news.SeqNum = reader.ReadNullableLong() ?? 0L;

		return news;
	}
}