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

		public override async ValueTask ReadAsync(Stream stream, CancellationToken cancellationToken)
		{
			await Do.InvariantAsync(async () =>
			{
				var count = 0;
				var firstTimeRead = false;

				using var reader = stream.CreateCsvReader(serializer.Encoding);

				while (await reader.NextLineAsync(cancellationToken))
				{
					var message = serializer.Read(reader, this);

					var openTime = message.OpenTime;

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
				var openTime = message.OpenTime;

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
	public override ValueTask SerializeAsync(Stream stream, IEnumerable<TCandleMessage> data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		var candleMetaInfo = (CandleCsvMetaInfo)metaInfo;

		var toWrite = candleMetaInfo.Process(data);

		return Do.InvariantAsync(async () =>
		{
			using var writer = stream.CreateCsvWriter(Encoding);

			foreach (var item in toWrite)
			{
				await WriteAsync(writer, item, candleMetaInfo, cancellationToken);
			}
		}).AsValueTask();
	}

	/// <inheritdoc />
	protected override ValueTask WriteAsync(CsvFileWriter writer, TCandleMessage data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		if (data.State == CandleStates.Active)
			throw new ArgumentException(LocalizedStrings.CandleActiveNotSupport.Put(data), nameof(data));

		return writer.WriteRowAsync(data.OpenTime.WriteTime().Concat(
		[
			data.OpenPrice.ToString(),
			data.HighPrice.ToString(),
			data.LowPrice.ToString(),
			data.ClosePrice.ToString(),
			data.TotalVolume.ToString()
		]).Concat(data.BuildFrom.ToCsv()).Concat(
		[
			data.SeqNum.DefaultAsNull().ToString(),
		]), cancellationToken);
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