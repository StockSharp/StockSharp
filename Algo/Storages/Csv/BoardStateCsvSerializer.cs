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
	protected override ValueTask WriteAsync(CsvFileWriter writer, BoardStateMessage data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		return writer.WriteRowAsync(data.ServerTime.WriteTime().Concat(
		[
			data.BoardCode,
			((int)data.State).To<string>(),
		]), cancellationToken);
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