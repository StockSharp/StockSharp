namespace StockSharp.Algo.Storages.Binary;

class BoardStateMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public override void Read(Stream stream)
	{
		base.Read(stream);

		ServerOffset = stream.Read<TimeSpan>();

		ReadOffsets(stream);
	}

	public override void Write(Stream stream)
	{
		base.Write(stream);

		stream.WriteEx(ServerOffset);

		WriteOffsets(stream);
	}
}

class BoardStateBinarySerializer(IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<BoardStateMessage, BoardStateMetaInfo>(default, DataType.BoardState, 200, MarketDataVersions.Version31, exchangeInfoProvider)
{
	protected override void OnSave(BitArrayWriter writer, IEnumerable<BoardStateMessage> messages, BoardStateMetaInfo metaInfo)
	{
		var isMetaEmpty = metaInfo.IsEmpty();

		writer.WriteInt(messages.Count());

		foreach (var msg in messages)
		{
			if (isMetaEmpty)
			{
				metaInfo.ServerOffset = msg.ServerTime.Offset;
				isMetaEmpty = false;
			}

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.BoardInfo, true, true, metaInfo.ServerOffset, true, true, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			writer.WriteStringEx(msg.BoardCode);
			writer.WriteInt((int)msg.State);
		}
	}

	public override BoardStateMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;

		var message = new BoardStateMessage();

		var prevTime = metaInfo.FirstTime;
		var lastOffset = metaInfo.FirstServerOffset;
		message.ServerTime = reader.ReadTime(ref prevTime, true, true, metaInfo.ServerOffset, true, true, ref lastOffset);
		metaInfo.FirstTime = prevTime;
		metaInfo.FirstServerOffset = lastOffset;

		message.BoardCode = reader.ReadStringEx();
		message.State = (SessionStates)reader.ReadInt();

		return message;
	}
}