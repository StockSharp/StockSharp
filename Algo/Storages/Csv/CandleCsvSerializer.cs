namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The candle serializer in the CSV format.
/// </summary>
/// <typeparam name="TCandleMessage"><see cref="CandleMessage"/> derived type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="CandleCsvSerializer{TCandleMessage}"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="dataType"><see cref="DataType"/>.</param>
/// <param name="encoding">Encoding.</param>
public class CandleCsvSerializer<TCandleMessage>(SecurityId securityId, DataType dataType, Encoding encoding) : CsvMarketDataSerializer<TCandleMessage>(securityId, encoding)
	where TCandleMessage : CandleMessage, new()
{
	private class CandleCsvMetaInfo(CandleCsvSerializer<TCandleMessage> serializer, DateTime date) : MetaInfo(date)
	{
		public override object LastId { get; set; }

		public override void Write(Stream stream)
		{
		}

		public override void Read(Stream stream)
		{
			Do.Invariant(() =>
			{
				var count = 0;
				var firstTimeRead = false;

				var reader = stream.CreateCsvReader(serializer.Encoding);

				while (reader.NextLine())
				{
					var message = serializer.Read(reader, this);

					var openTime = message.OpenTime.UtcDateTime;

					if (!firstTimeRead)
					{
						FirstTime = openTime;
						firstTimeRead = true;
					}

					LastTime = openTime;

					count++;
				}

				Count = count;

				stream.Position = 0;
			});
		}

		public IEnumerable<TCandleMessage> Process(IEnumerable<TCandleMessage> messages)
		{
			messages = [.. messages];

			foreach (var message in messages)
			{
				var openTime = message.OpenTime.UtcDateTime;

				LastTime = openTime;
			}

			return messages;
		}
	}

	private readonly DataType _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));

	/// <inheritdoc />
	public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
	{
		return new CandleCsvMetaInfo(this, date);
	}

	/// <inheritdoc />
	public override void Serialize(Stream stream, IEnumerable<TCandleMessage> data, IMarketDataMetaInfo metaInfo)
	{
		var candleMetaInfo = (CandleCsvMetaInfo)metaInfo;

		var toWrite = candleMetaInfo.Process(data);

		Do.Invariant(() =>
		{
			var writer = stream.CreateCsvWriter(Encoding);

			try
			{
				foreach (var item in toWrite)
				{
					Write(writer, item, candleMetaInfo);
				}
			}
			finally
			{
				writer.Flush();
			}
		});
	}

	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, TCandleMessage data, IMarketDataMetaInfo metaInfo)
	{
		if (data.State == CandleStates.Active)
			throw new ArgumentException(LocalizedStrings.CandleActiveNotSupport.Put(data), nameof(data));

		writer.WriteRow(new[]
		{
			data.OpenTime.WriteTime(),
			data.OpenTime.ToString("zzz"),
			data.OpenPrice.ToString(),
			data.HighPrice.ToString(),
			data.LowPrice.ToString(),
			data.ClosePrice.ToString(),
			data.TotalVolume.ToString()
		}.Concat(data.BuildFrom.ToCsv()).Concat(
		[
			data.SeqNum.DefaultAsNull().ToString(),
		]));
	}

	/// <inheritdoc />
	protected override TCandleMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var message = new TCandleMessage
		{
			SecurityId = SecurityId,
			DataType = _dataType,
			OpenTime = reader.ReadTime(metaInfo.Date),
			OpenPrice = reader.ReadDecimal(),
			HighPrice = reader.ReadDecimal(),
			LowPrice = reader.ReadDecimal(),
			ClosePrice = reader.ReadDecimal(),
			TotalVolume = reader.ReadDecimal(),
			State = CandleStates.Finished
		};

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			message.BuildFrom = reader.ReadBuildFrom();

		if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			message.SeqNum = reader.ReadNullableLong() ?? 0L;

		return message;
	}
}