namespace StockSharp.Algo.Storages.Csv
{
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The board state serializer in the CSV format.
	/// </summary>
	public class BoardStateCsvSerializer : CsvMarketDataSerializer<BoardStateMessage>
	{
		/// <inheritdoc />
		protected override void Write(CsvFileWriter writer, BoardStateMessage data, IMarketDataMetaInfo metaInfo)
		{
			writer.WriteRow(new[]
			{
				data.ServerTime.WriteTimeMls(),
				data.ServerTime.ToString("zzz"),
				data.BoardCode,
				((int)data.State).To<string>(),
			});

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
}