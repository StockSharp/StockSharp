namespace StockSharp.Algo.Storages.Binary;

class TickMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public override object LastId => PrevId;

	public long FirstId { get; set; } = -1;
	public long PrevId { get; set; }

	public override void Write(Stream stream)
	{
		base.Write(stream);

		stream.WriteEx(FirstId);
		stream.WriteEx(PrevId);
		stream.WriteEx(FirstPrice);
		stream.WriteEx(LastPrice);

		WriteFractionalPrice(stream);
		WriteFractionalVolume(stream);

		WriteLocalTime(stream, MarketDataVersions.Version47);

		if (Version < MarketDataVersions.Version50)
			return;

		stream.WriteEx(ServerOffset);

		if (Version < MarketDataVersions.Version54)
			return;

		WriteOffsets(stream);

		if (Version < MarketDataVersions.Version59)
			return;

		WriteSeqNums(stream);
	}

	public override void Read(Stream stream)
	{
		base.Read(stream);

		FirstId = stream.Read<long>();
		PrevId = stream.Read<long>();
		FirstPrice = stream.Read<decimal>();
		LastPrice = stream.Read<decimal>();

		ReadFractionalPrice(stream);
		ReadFractionalVolume(stream);

		ReadLocalTime(stream, MarketDataVersions.Version47);

		if (Version < MarketDataVersions.Version50)
			return;

		ServerOffset = stream.Read<TimeSpan>();

		if (Version < MarketDataVersions.Version54)
			return;

		ReadOffsets(stream);

		if (Version < MarketDataVersions.Version59)
			return;

		ReadSeqNums(stream);
	}

	public override void CopyFrom(BinaryMetaInfo src)
	{
		base.CopyFrom(src);

		var tickInfo = (TickMetaInfo)src;

		FirstId = tickInfo.FirstId;
		PrevId = tickInfo.PrevId;
	}
}

class TickBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<ExecutionMessage, TickMetaInfo>(securityId, DataType.Ticks, 50, MarketDataVersions.Version61, exchangeInfoProvider)
{
	protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, TickMetaInfo metaInfo)
	{
		if (metaInfo.IsEmpty())
		{
			var first = messages.First();

			metaInfo.FirstId = metaInfo.PrevId = first.TradeId ?? 0;
			metaInfo.ServerOffset = first.ServerTime.Offset;
			metaInfo.FirstSeqNum = metaInfo.PrevSeqNum = first.SeqNum;
		}

		writer.WriteInt(messages.Count());

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version54;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version55;
		var useLong = metaInfo.Version >= MarketDataVersions.Version56;
		var stringId = metaInfo.Version >= MarketDataVersions.Version57;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version58;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version59;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version60;
		var orderIds = metaInfo.Version >= MarketDataVersions.Version61;

		foreach (var msg in messages)
		{
			if (msg.DataType != DataType.Ticks)
				throw new ArgumentOutOfRangeException(nameof(messages), msg.DataType, LocalizedStrings.UnknownType.Put(msg));

			var tradeId = msg.TradeId ?? 0;

			// сделки для индексов имеют нулевой номер
			if (tradeId < 0)
				throw new ArgumentOutOfRangeException(nameof(messages), tradeId, LocalizedStrings.TradeId);

			// execution ticks (like option execution) may be a zero cost
			// ticks for spreads may be a zero cost or less than zero
			//if (msg.TradePrice < 0)
			//	throw new ArgumentOutOfRangeException(nameof(messages), msg.TradePrice, LocalizedStrings.PriceIsNotSpecified.Put(msg.TradeId));

			metaInfo.PrevId = writer.SerializeId(tradeId, metaInfo.PrevId);

			// pyhta4og.
			// http://stocksharp.com/forum/yaf_postsm6450_Oshibka-pri-importie-instrumientov-s-Finama.aspx#post6450

			var volume = msg.TradeVolume;

			if (metaInfo.Version < MarketDataVersions.Version53)
			{
				if (volume == null)
					throw new ArgumentException(LocalizedStrings.WrongTradeVolume.Put((object)msg.TradeId ?? msg.TradeStringId), nameof(messages));

				if (volume < 0)
					throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.WrongTradeVolume.Put(msg.TradeId));

				writer.WriteVolume(volume.Value, metaInfo, false);
			}
			else
			{
				writer.Write(volume != null);

				if (volume != null)
				{
					if (volume < 0 && !largeDecimal)
						throw new ArgumentOutOfRangeException(nameof(messages), volume, LocalizedStrings.WrongTradeVolume.Put(msg.TradeId));

					writer.WriteVolume(volume.Value, metaInfo, largeDecimal);
				}
			}
			
			writer.WritePriceEx(msg.GetTradePrice(), metaInfo, SecurityId, useLong, largeDecimal);
			writer.WriteSide(msg.OriginSide);

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Trades, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			if (metaInfo.Version < MarketDataVersions.Version40)
				continue;

			if (metaInfo.Version < MarketDataVersions.Version47)
				writer.WriteLong((msg.LocalTime - msg.ServerTime).Ticks);
			else
			{
				var hasLocalTime = true;

				if (metaInfo.Version >= MarketDataVersions.Version49)
				{
					hasLocalTime = msg.HasLocalTime(msg.ServerTime);
					writer.Write(hasLocalTime);
				}

				if (hasLocalTime)
				{
					lastOffset = metaInfo.LastLocalOffset;
					metaInfo.LastLocalTime = writer.WriteTime(msg.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Trades, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, true);
					metaInfo.LastLocalOffset = lastOffset;
				}
			}

			if (metaInfo.Version < MarketDataVersions.Version42)
				continue;

			if (metaInfo.Version >= MarketDataVersions.Version51)
			{
				writer.Write(msg.IsSystem != null);

				if (msg.IsSystem != null)
					writer.Write(msg.IsSystem.Value);
			}
			else
				writer.Write(msg.IsSystem ?? true);

			if (msg.IsSystem == false)
			{
				if (metaInfo.Version >= MarketDataVersions.Version51)
					writer.WriteNullableInt((int?)msg.TradeStatus);
				else
					writer.WriteInt((int?)msg.TradeStatus ?? 0);
			}

			var oi = msg.OpenInterest;

			if (metaInfo.Version < MarketDataVersions.Version46)
				writer.WriteVolume(oi ?? 0m, metaInfo, false);
			else
			{
				writer.Write(oi != null);

				if (oi != null)
					writer.WriteVolume(oi.Value, metaInfo, largeDecimal);
			}

			if (metaInfo.Version < MarketDataVersions.Version45)
				continue;

			writer.Write(msg.IsUpTick != null);

			if (msg.IsUpTick != null)
				writer.Write(msg.IsUpTick.Value);

			if (metaInfo.Version < MarketDataVersions.Version52)
				continue;

			writer.Write(msg.Currency != null);

			if (msg.Currency != null)
				writer.WriteInt((int)msg.Currency.Value);

			if (!stringId)
				continue;

			writer.WriteStringEx(msg.TradeStringId);

			if (!buildFrom)
				continue;

			writer.WriteBuildFrom(msg.BuildFrom);

			if (!seqNum)
				continue;

			writer.WriteSeqNum(msg, metaInfo);

			if (!orderIds)
				continue;

			writer.WriteNullableLong(msg.OrderBuyId);
			writer.WriteNullableLong(msg.OrderSellId);
		}
	}

	public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version54;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version55;
		var useLong = metaInfo.Version >= MarketDataVersions.Version56;
		var stringId = metaInfo.Version >= MarketDataVersions.Version57;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version58;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version59;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version60;
		var orderIds = metaInfo.Version >= MarketDataVersions.Version61;

		metaInfo.FirstId += reader.ReadLong();

		var volume = metaInfo.Version < MarketDataVersions.Version53
			? reader.ReadVolume(metaInfo, false)
			: reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : (decimal?)null;

		var price = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);

		var orderDirection = reader.Read() ? (reader.Read() ? Sides.Buy : Sides.Sell) : (Sides?)null;

		var prevTime = metaInfo.FirstTime;
		var lastOffset = metaInfo.FirstServerOffset;
		var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset);
		metaInfo.FirstTime = prevTime;
		metaInfo.FirstServerOffset = lastOffset;

		var msg = new ExecutionMessage
		{
			//LocalTime = metaInfo.FirstTime,
			DataTypeEx = DataType.Ticks,
			SecurityId = SecurityId,
			TradeId = metaInfo.FirstId == 0 ? null : metaInfo.FirstId,
			TradeVolume = volume,
			OriginSide = orderDirection,
			TradePrice = price,
			ServerTime = serverTime,
		};

		if (metaInfo.Version < MarketDataVersions.Version40)
			return msg;

		if (metaInfo.Version < MarketDataVersions.Version47)
		{
			msg.LocalTime = msg.ServerTime - reader.ReadLong().To<TimeSpan>() + metaInfo.LocalOffset;
		}
		else
		{
			var hasLocalTime = true;

			if (metaInfo.Version >= MarketDataVersions.Version49)
				hasLocalTime = reader.Read();

			if (hasLocalTime)
			{
				var prevLocalTime = metaInfo.FirstLocalTime;
				lastOffset = metaInfo.FirstLocalOffset;
				var localTime = reader.ReadTime(ref prevLocalTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.FirstLocalTime = prevLocalTime;
				metaInfo.FirstLocalOffset = lastOffset;
				msg.LocalTime = localTime;
			}
			//else
			//	msg.LocalTime = msg.ServerTime;
		}

		if (metaInfo.Version < MarketDataVersions.Version42)
			return msg;

		msg.IsSystem = metaInfo.Version < MarketDataVersions.Version51
					? reader.Read()
					: (reader.Read() ? reader.Read() : null);

		if (msg.IsSystem == false)
		{
			msg.TradeStatus = metaInfo.Version < MarketDataVersions.Version51
				? reader.ReadInt()
				: reader.ReadNullableInt();
		}

		if (metaInfo.Version < MarketDataVersions.Version46 || reader.Read())
			msg.OpenInterest = reader.ReadVolume(metaInfo, largeDecimal);

		if (metaInfo.Version < MarketDataVersions.Version45)
			return msg;

		if (reader.Read())
			msg.IsUpTick = reader.Read();

		if (metaInfo.Version >= MarketDataVersions.Version52)
		{
			if (reader.Read())
				msg.Currency = (CurrencyTypes)reader.ReadInt();
		}

		if (!stringId)
			return msg;
		
		msg.TradeStringId = reader.ReadStringEx();

		if (!buildFrom)
			return msg;

		msg.BuildFrom = reader.ReadBuildFrom();

		if (!seqNum)
			return msg;

		reader.ReadSeqNum(msg, metaInfo);

		if (!orderIds)
			return msg;

		msg.OrderBuyId = reader.ReadNullableLong();
		msg.OrderSellId = reader.ReadNullableLong();

		return msg;
	}
}