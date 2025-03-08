namespace StockSharp.Algo.Storages.Binary;

class NewsMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public override void Read(Stream stream)
	{
		base.Read(stream);

		if (Version < MarketDataVersions.Version45)
			return;

		ServerOffset = stream.Read<TimeSpan>();

		if (Version < MarketDataVersions.Version46)
			return;

		ReadOffsets(stream);

		if (Version < MarketDataVersions.Version51)
			return;

		ReadSeqNums(stream);
	}

	public override void Write(Stream stream)
	{
		base.Write(stream);

		if (Version < MarketDataVersions.Version45)
			return;

		stream.WriteEx(ServerOffset);

		if (Version < MarketDataVersions.Version46)
			return;

		WriteOffsets(stream);

		if (Version < MarketDataVersions.Version51)
			return;

		WriteSeqNums(stream);
	}
}

class NewsBinarySerializer(IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<NewsMessage, NewsMetaInfo>(default, DataType.News, 200, MarketDataVersions.Version51, exchangeInfoProvider)
{
	protected override void OnSave(BitArrayWriter writer, IEnumerable<NewsMessage> messages, NewsMetaInfo metaInfo)
	{
		if (metaInfo.IsEmpty())
		{
			var first = messages.First();

			metaInfo.ServerOffset = first.ServerTime.Offset;
			metaInfo.FirstSeqNum = metaInfo.PrevSeqNum = first.SeqNum;
		}

		writer.WriteInt(messages.Count());

		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version46;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version47;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version51;

		foreach (var news in messages)
		{
			writer.WriteStringEx(news.Id);

			writer.WriteString(news.Headline);

			writer.WriteStringEx(news.Story);
			writer.WriteStringEx(news.Source);
			writer.WriteStringEx(news.BoardCode);
			writer.WriteStringEx(news.SecurityId?.SecurityCode);
			writer.WriteStringEx(news.Url);

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(news.ServerTime, metaInfo.LastTime, LocalizedStrings.News, true, true, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			if (metaInfo.Version < MarketDataVersions.Version48)
				continue;

			writer.Write(news.Priority != null);

			if (news.Priority != null)
				writer.WriteInt((int)news.Priority.Value);

			if (metaInfo.Version < MarketDataVersions.Version49)
				continue;

			writer.WriteStringEx(news.Language);

			if (metaInfo.Version < MarketDataVersions.Version50)
				continue;

			writer.Write(news.ExpiryDate != null);

			if (news.ExpiryDate != null)
				writer.WriteLong(news.ExpiryDate.Value.To<long>());

			writer.WriteStringEx(news.SecurityId?.BoardCode);

			if (!seqNum)
				continue;

			writer.WriteSeqNum(news, metaInfo);
		}
	}

	public override NewsMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;

		var message = new NewsMessage
		{
			Id = reader.ReadStringEx(),
			Headline = reader.ReadString(),
			Story = reader.ReadStringEx(),
			Source = reader.ReadStringEx(),
			BoardCode = reader.ReadStringEx(),
			SecurityId = reader.Read() ? new SecurityId { SecurityCode = reader.ReadString() } : null,
			Url = reader.ReadStringEx(),
		};

		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version46;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version47;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version51;

		var prevTime = metaInfo.FirstTime;
		var lastOffset = metaInfo.FirstServerOffset;
		message.ServerTime = reader.ReadTime(ref prevTime, true, true, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
		metaInfo.FirstTime = prevTime;
		metaInfo.FirstServerOffset = lastOffset;

		if (metaInfo.Version < MarketDataVersions.Version48)
			return message;

		if (reader.Read())
			message.Priority = (NewsPriorities)reader.ReadInt();

		if (metaInfo.Version < MarketDataVersions.Version49)
			return message;

		message.Language = reader.ReadStringEx();

		if (metaInfo.Version < MarketDataVersions.Version50)
			return message;

		message.ExpiryDate = reader.Read() ? reader.ReadLong().To<DateTimeOffset>() : null;

		var secBoard = reader.ReadStringEx();
		if (!secBoard.IsEmpty() && message.SecurityId != null)
		{
			var secId = message.SecurityId.Value;
			secId.BoardCode = secBoard;
			message.SecurityId = secId;
		}

		if (!seqNum)
			return message;

		reader.ReadSeqNum(message, metaInfo);

		return message;
	}
}