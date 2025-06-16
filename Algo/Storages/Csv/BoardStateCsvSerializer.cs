namespace StockSharp.Algo.Storages.Csv;

/// <summary>
/// The board state serializer in the CSV format.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BoardStateCsvSerializer"/>.
/// </remarks>
/// <param name="encoding">Encoding.</param>
public class BoardStateCsvSerializer(Encoding encoding) : CsvMarketDataSerializer<BoardStateMessage>(encoding)
{
	/// <inheritdoc />
	protected override void Write(CsvFileWriter writer, BoardStateMessage data, IMarketDataMetaInfo metaInfo)
	{
		writer.WriteRow(
		[
			data.ServerTime.WriteTime(),
			data.ServerTime.ToString("zzz"),
			data.BoardCode,
			((int)data.State).To<string>(),
		]);

		metaInfo.LastTime = data.ServerTime.UtcDateTime;
	}

	/// <inheritdoc />
	protected override BoardStateMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		var state = new BoardStateMessage
		{
			ServerTime = reader.ReadTime(metaInfo.Date),
			BoardCode = reader.ReadString(),
			State = reader.ReadEnum<SessionStates>()
		};

		return state;
	}
}