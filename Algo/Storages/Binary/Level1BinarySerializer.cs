namespace StockSharp.Algo.Storages.Binary;

class Level1MetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public class DateInfo
	{
		public TimeSpan FirstDateOffset { get; set; }
		public TimeSpan LastDateOffset { get; set; }

		public DateTime FirstDateTime { get; set; }
		public DateTime LastDateTime { get; set; }

		public void Write(Stream stream)
		{
			stream.WriteEx(FirstDateTime);
			stream.WriteEx(LastDateTime);

			stream.WriteEx(FirstDateOffset);
			stream.WriteEx(LastDateOffset);
		}

		public void Read(Stream stream)
		{
			FirstDateTime = stream.Read<DateTime>().UtcKind();
			LastDateTime = stream.Read<DateTime>().UtcKind();

			FirstDateOffset = stream.Read<TimeSpan>();
			LastDateOffset = stream.Read<TimeSpan>();
		}

		public DateInfo Clone()
		{
			return new DateInfo
			{
				FirstDateTime = FirstDateTime,
				LastDateTime = LastDateTime,
				FirstDateOffset = FirstDateOffset,
				LastDateOffset = LastDateOffset,
			};
		}
	}

	public RefPair<decimal, decimal> Price { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ImpliedVolatility { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> HistoricalVolatility { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> TheorPrice { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> StepPrice { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Delta { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Gamma { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Vega { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Theta { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> MarginBuy { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> MarginSell { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Change { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Rho { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> AccruedCouponIncome { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Yield { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> VWAP { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> PriceEarnings { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ForwardPriceEarnings { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> PriceEarningsGrowth { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> PriceSales { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> PriceBook { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> PriceCash { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> PriceFreeCash { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Payout { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> SharesOutstanding { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> SharesFloat { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> FloatShort { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ShortRatio { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ReturnOnAssets { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ReturnOnEquity { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ReturnOnInvestment { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> CurrentRatio { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> QuickRatio { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> LongTermDebtEquity { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> TotalDebtEquity { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> GrossMargin { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> OperatingMargin { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> ProfitMargin { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Beta { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> AverageTrueRange { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> HistoricalVolatilityWeek { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> HistoricalVolatilityMonth { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> AveragePrice { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Turnover { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> IssueSize { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> Duration { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> BuyBackPrice { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> MinPrice { get; private set; } = new RefPair<decimal, decimal>();
	public RefPair<decimal, decimal> MaxPrice { get; private set; } = new RefPair<decimal, decimal>();

	public DateTime FirstFieldTime { get; set; }
	public DateTime LastFieldTime { get; set; }

	public DateInfo BuyBackInfo { get; private set; } = new DateInfo();
	public DateInfo CouponInfo { get; private set; } = new DateInfo();

	public Level1Fields MaxKnownType { get; set; }

	public override void Write(Stream stream)
	{
		base.Write(stream);

		Write(stream, Price);
		Write(stream, ImpliedVolatility);
		Write(stream, TheorPrice);
		Write(stream, StepPrice);

		if (Version < MarketDataVersions.Version41)
			return;

		Write(stream, HistoricalVolatility);

		Write(stream, Delta);
		Write(stream, Gamma);
		Write(stream, Vega);
		Write(stream, Theta);

		Write(stream, MarginBuy);
		Write(stream, MarginSell);

		WriteFractionalVolume(stream);

		if (Version < MarketDataVersions.Version45)
			return;

		Write(stream, Change);
		Write(stream, Rho);

		if (Version < MarketDataVersions.Version46)
			return;

		Write(stream, AccruedCouponIncome);
		Write(stream, Yield);

		stream.WriteEx(FirstFieldTime);
		stream.WriteEx(LastFieldTime);

		if (Version < MarketDataVersions.Version47)
			return;

		Write(stream, VWAP);

		if (Version < MarketDataVersions.Version50)
			return;

		Write(stream, PriceEarnings);
		Write(stream, ForwardPriceEarnings);
		Write(stream, PriceEarningsGrowth);
		Write(stream, PriceSales);
		Write(stream, PriceBook);
		Write(stream, PriceCash);
		Write(stream, PriceFreeCash);
		Write(stream, Payout);
		Write(stream, SharesOutstanding);
		Write(stream, SharesFloat);
		Write(stream, FloatShort);
		Write(stream, ShortRatio);
		Write(stream, ReturnOnAssets);
		Write(stream, ReturnOnEquity);
		Write(stream, ReturnOnInvestment);
		Write(stream, CurrentRatio);
		Write(stream, QuickRatio);
		Write(stream, LongTermDebtEquity);
		Write(stream, TotalDebtEquity);
		Write(stream, GrossMargin);
		Write(stream, OperatingMargin);
		Write(stream, ProfitMargin);
		Write(stream, Beta);
		Write(stream, AverageTrueRange);
		Write(stream, HistoricalVolatilityWeek);
		Write(stream, HistoricalVolatilityMonth);

		if (Version < MarketDataVersions.Version51)
			return;

		Write(stream, AveragePrice);

		WriteLocalTime(stream, MarketDataVersions.Version52);

		if (Version < MarketDataVersions.Version53)
			return;

		stream.WriteEx(ServerOffset);

		if (Version < MarketDataVersions.Version54)
			return;

		WriteOffsets(stream);

		if (Version < MarketDataVersions.Version56)
			return;

		Write(stream, Turnover);

		if (Version < MarketDataVersions.Version57)
			return;

		Write(stream, IssueSize);
		Write(stream, Duration);
		Write(stream, BuyBackPrice);

		BuyBackInfo.Write(stream);

		if (Version < MarketDataVersions.Version58)
			return;

		stream.WriteEx((int)MaxKnownType);

		if (Version < MarketDataVersions.Version59)
			return;

		WritePriceStep(stream);

		if (Version < MarketDataVersions.Version60)
			return;

		Write(stream, MinPrice);
		Write(stream, MaxPrice);

		if (Version < MarketDataVersions.Version61)
			return;

		CouponInfo.Write(stream);

		if (Version < MarketDataVersions.Version63)
			return;

		WriteSeqNums(stream);
	}

	public override void Read(Stream stream)
	{
		base.Read(stream);

		Price = ReadInfo(stream);
		ImpliedVolatility = ReadInfo(stream);
		TheorPrice = ReadInfo(stream);
		StepPrice = ReadInfo(stream);

		if (Version < MarketDataVersions.Version41)
			return;

		HistoricalVolatility = ReadInfo(stream);

		Delta = ReadInfo(stream);
		Gamma = ReadInfo(stream);
		Vega = ReadInfo(stream);
		Theta = ReadInfo(stream);

		MarginBuy = ReadInfo(stream);
		MarginSell = ReadInfo(stream);

		ReadFractionalVolume(stream);

		if (Version < MarketDataVersions.Version45)
			return;

		Change = ReadInfo(stream);
		Rho = ReadInfo(stream);

		if (Version < MarketDataVersions.Version46)
			return;

		AccruedCouponIncome = ReadInfo(stream);
		Yield = ReadInfo(stream);

		FirstFieldTime = stream.Read<DateTime>().UtcKind();
		LastFieldTime = stream.Read<DateTime>().UtcKind();

		if (Version < MarketDataVersions.Version47)
			return;

		VWAP = ReadInfo(stream);

		if (Version < MarketDataVersions.Version50)
			return;

		PriceEarnings = ReadInfo(stream);
		ForwardPriceEarnings = ReadInfo(stream);
		PriceEarningsGrowth = ReadInfo(stream);
		PriceSales = ReadInfo(stream);
		PriceBook = ReadInfo(stream);
		PriceCash = ReadInfo(stream);
		PriceFreeCash = ReadInfo(stream);
		Payout = ReadInfo(stream);
		SharesOutstanding = ReadInfo(stream);
		SharesFloat = ReadInfo(stream);
		FloatShort = ReadInfo(stream);
		ShortRatio = ReadInfo(stream);
		ReturnOnAssets = ReadInfo(stream);
		ReturnOnEquity = ReadInfo(stream);
		ReturnOnInvestment = ReadInfo(stream);
		CurrentRatio = ReadInfo(stream);
		QuickRatio = ReadInfo(stream);
		LongTermDebtEquity = ReadInfo(stream);
		TotalDebtEquity = ReadInfo(stream);
		GrossMargin = ReadInfo(stream);
		OperatingMargin = ReadInfo(stream);
		ProfitMargin = ReadInfo(stream);
		Beta = ReadInfo(stream);
		AverageTrueRange = ReadInfo(stream);
		HistoricalVolatilityWeek = ReadInfo(stream);
		HistoricalVolatilityMonth = ReadInfo(stream);

		if (Version < MarketDataVersions.Version51)
			return;

		AveragePrice = ReadInfo(stream);

		ReadLocalTime(stream, MarketDataVersions.Version52);

		if (Version < MarketDataVersions.Version53)
			return;

		ServerOffset = stream.Read<TimeSpan>();

		if (Version < MarketDataVersions.Version54)
			return;

		ReadOffsets(stream);

		if (Version < MarketDataVersions.Version56)
			return;

		Turnover = ReadInfo(stream);

		if (Version < MarketDataVersions.Version57)
			return;

		IssueSize = ReadInfo(stream);
		Duration = ReadInfo(stream);
		BuyBackPrice = ReadInfo(stream);

		BuyBackInfo.Read(stream);

		if (Version < MarketDataVersions.Version58)
			return;

		MaxKnownType = (Level1Fields)stream.Read<int>();

		if (Version < MarketDataVersions.Version59)
			return;

		ReadPriceStep(stream);

		if (Version < MarketDataVersions.Version60)
			return;

		MinPrice = ReadInfo(stream);
		MaxPrice = ReadInfo(stream);

		if (Version < MarketDataVersions.Version61)
			return;

		CouponInfo.Read(stream);

		if (Version < MarketDataVersions.Version63)
			return;

		ReadSeqNums(stream);
	}

	private static void Write(Stream stream, RefPair<decimal, decimal> info)
	{
		stream.WriteEx(info.First);
		stream.WriteEx(info.Second);
	}

	private static RefPair<decimal, decimal> ReadInfo(Stream stream)
	{
		return RefTuple.Create(stream.Read<decimal>(), stream.Read<decimal>());
	}

	public override void CopyFrom(BinaryMetaInfo src)
	{
		base.CopyFrom(src);

		var l1Info = (Level1MetaInfo)src;

		Price = Clone(l1Info.Price);
		ImpliedVolatility = Clone(l1Info.ImpliedVolatility);
		TheorPrice = Clone(l1Info.TheorPrice);
		StepPrice = Clone(l1Info.StepPrice);
		HistoricalVolatility = Clone(l1Info.HistoricalVolatility);
		Delta = Clone(l1Info.Delta);
		Gamma = Clone(l1Info.Gamma);
		Vega = Clone(l1Info.Vega);
		Theta = Clone(l1Info.Theta);
		MarginBuy = Clone(l1Info.MarginBuy);
		MarginSell = Clone(l1Info.MarginSell);
		Change = Clone(l1Info.Change);
		Rho = Clone(l1Info.Rho);
		AccruedCouponIncome = Clone(l1Info.AccruedCouponIncome);
		Yield = Clone(l1Info.Yield);
		FirstFieldTime = l1Info.FirstFieldTime;
		LastFieldTime = l1Info.LastFieldTime;
		VWAP = Clone(l1Info.VWAP);
		PriceEarnings = Clone(l1Info.PriceEarnings);
		ForwardPriceEarnings = Clone(l1Info.ForwardPriceEarnings);
		PriceEarningsGrowth = Clone(l1Info.PriceEarningsGrowth);
		PriceSales = Clone(l1Info.PriceSales);
		PriceBook = Clone(l1Info.PriceBook);
		PriceCash = Clone(l1Info.PriceCash);
		PriceFreeCash = Clone(l1Info.PriceFreeCash);
		Payout = Clone(l1Info.Payout);
		SharesOutstanding = Clone(l1Info.SharesOutstanding);
		SharesFloat = Clone(l1Info.SharesFloat);
		FloatShort = Clone(l1Info.FloatShort);
		ShortRatio = Clone(l1Info.ShortRatio);
		ReturnOnAssets = Clone(l1Info.ReturnOnAssets);
		ReturnOnEquity = Clone(l1Info.ReturnOnEquity);
		ReturnOnInvestment = Clone(l1Info.ReturnOnInvestment);
		CurrentRatio = Clone(l1Info.CurrentRatio);
		QuickRatio = Clone(l1Info.QuickRatio);
		LongTermDebtEquity = Clone(l1Info.LongTermDebtEquity);
		TotalDebtEquity = Clone(l1Info.TotalDebtEquity);
		GrossMargin = Clone(l1Info.GrossMargin);
		OperatingMargin = Clone(l1Info.OperatingMargin);
		ProfitMargin = Clone(l1Info.ProfitMargin);
		Beta = Clone(l1Info.Beta);
		AverageTrueRange = Clone(l1Info.AverageTrueRange);
		HistoricalVolatilityWeek = Clone(l1Info.HistoricalVolatilityWeek);
		HistoricalVolatilityMonth = Clone(l1Info.HistoricalVolatilityMonth);
		AveragePrice = Clone(l1Info.AveragePrice);
		Turnover = Clone(l1Info.Turnover);
		IssueSize = Clone(l1Info.IssueSize);
		Duration = Clone(l1Info.Duration);
		BuyBackPrice = Clone(l1Info.BuyBackPrice);
		BuyBackInfo = l1Info.BuyBackInfo.Clone();
		MaxKnownType = l1Info.MaxKnownType;
		MinPrice = Clone(l1Info.MinPrice);
		MaxPrice = Clone(l1Info.MaxPrice);
		CouponInfo = l1Info.CouponInfo.Clone();
	}

	private static RefPair<decimal, decimal> Clone(RefPair<decimal, decimal> info)
	{
		return RefTuple.Create(info.First, info.Second);
	}
}

class Level1BinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<Level1ChangeMessage, Level1MetaInfo>(securityId, DataType.Level1, 50, MarketDataVersions.Version64, exchangeInfoProvider)
{
	private static readonly SynchronizedPairSet<Level1Fields, int> _oldMap = new()
	{
		{ Level1Fields.OpenPrice,				1 },
		{ Level1Fields.HighPrice,				1 << 1 },
		{ Level1Fields.LowPrice,				1 << 2 },
		{ Level1Fields.ClosePrice,				1 << 3 },
#pragma warning disable CS0612 // Type or member is obsolete
		{ Level1Fields.LastTrade,				1 << 4 },
		{ Level1Fields.BestBid,					1 << 6 },
		{ Level1Fields.BestAsk,					1 << 7 },
#pragma warning restore CS0612 // Type or member is obsolete
		{ Level1Fields.StepPrice,				1 << 5 },
		{ Level1Fields.ImpliedVolatility,		1 << 8 },
		{ Level1Fields.TheorPrice,				1 << 9 },
		{ Level1Fields.OpenInterest,			1 << 10 },
		{ Level1Fields.MinPrice,				1 << 11 },
		{ Level1Fields.MaxPrice,				1 << 12 },
		{ Level1Fields.BidsVolume,				1 << 13 },
		{ Level1Fields.BidsCount,				1 << 14 },
		{ Level1Fields.AsksVolume,				1 << 15 },
		{ Level1Fields.AsksCount,				1 << 16 },
		{ Level1Fields.HistoricalVolatility,	1 << 17 },
		{ Level1Fields.Delta,					1 << 18 },
		{ Level1Fields.Gamma,					1 << 19 },
		{ Level1Fields.Vega,					1 << 20 },
		{ Level1Fields.Theta,					1 << 21 },
		{ Level1Fields.MarginBuy,				1 << 22 },
		{ Level1Fields.MarginSell,				1 << 23 },
		{ Level1Fields.PriceStep,				1 << 24 },
		{ Level1Fields.VolumeStep,				1 << 25 },
	};

	private static int MapTo(Level1MetaInfo metaInfo, Level1Fields field)
	{
		if (metaInfo.Version < MarketDataVersions.Version45)
		{
			if (!_oldMap.TryGetValue(field, out var fieldCode))
				throw new ArgumentException(LocalizedStrings.CodeForFieldNotFound.Put(field));

			return fieldCode;
		}
		
		return (int)field;
	}

	private static Level1Fields MapFrom(Level1MetaInfo metaInfo, int fieldCode)
	{
		if (metaInfo.Version < MarketDataVersions.Version45)
		{
			if (!_oldMap.TryGetKey(fieldCode, out var field))
				throw new ArgumentException(LocalizedStrings.FieldForCodeNotFound.Put(fieldCode));

			return field;
		}

		return (Level1Fields)fieldCode;
	}

	protected override void OnSave(BitArrayWriter writer, IEnumerable<Level1ChangeMessage> messages, Level1MetaInfo metaInfo)
	{
		if (metaInfo.IsEmpty())
		{
			var first = messages.First();

			metaInfo.ServerOffset = first.ServerTime.Offset;
			metaInfo.MaxKnownType = Level1Fields.LastTradeStringId;
			metaInfo.FirstSeqNum = metaInfo.PrevSeqNum = first.SeqNum;
		}

		writer.WriteInt(messages.Count());

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version53;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version54;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version55;
		var unkTypes = metaInfo.Version >= MarketDataVersions.Version58;
		var nonAdjustPrice = metaInfo.Version >= MarketDataVersions.Version59;
		var minMaxPrice = metaInfo.Version >= MarketDataVersions.Version60;
		var useLong = metaInfo.Version >= MarketDataVersions.Version60;
		var storeSteps = metaInfo.Version >= MarketDataVersions.Version60;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version62;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version63;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version64;

		foreach (var message in messages)
		{
			if (metaInfo.Version >= MarketDataVersions.Version49)
			{
				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, "level1", allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				var hasLocalTime = message.HasLocalTime(message.ServerTime);

				writer.Write(hasLocalTime);

				if (hasLocalTime)
				{
					lastOffset = metaInfo.LastLocalOffset;
					metaInfo.LastLocalTime = writer.WriteTime(message.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Level1, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, true);
					metaInfo.LastLocalOffset = lastOffset;
				}

				var count = message.Changes.Count;

				if (count == 0)
					throw new ArgumentException(LocalizedStrings.MessageDoNotContainsChanges, nameof(messages));

				writer.WriteInt(count);

				if (buildFrom)
					writer.WriteBuildFrom(message.BuildFrom);

				if (seqNum)
					writer.WriteSeqNum(message, metaInfo);
			}

			foreach (var change in message.Changes)
			{
				if (metaInfo.Version < MarketDataVersions.Version49)
				{
					var offset = TimeSpan.Zero;
					metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, "level1", allowNonOrdered, isUtc, metaInfo.ServerOffset, false, false, ref offset);
				}

				var field = change.Key;
				var value = change.Value;

				writer.WriteInt(MapTo(metaInfo, field));

				if (unkTypes)
				{
					var isKnown = (int)field <= (int)metaInfo.MaxKnownType;
					writer.Write(isKnown);

					if (!isKnown)
					{
						switch (value)
						{
							case decimal d:
								writer.WriteInt(0);
								writer.WriteDecimal(d, 0);
								break;
							case long l:
								writer.WriteInt(1);
								writer.WriteLong(l);
								break;
							case int i:
								writer.WriteInt(2);
								writer.WriteInt(i);
								break;
							case DateTimeOffset d:
								writer.WriteInt(3);
								writer.WriteLong(d.To<long>());
								break;
							case string s:
								writer.WriteInt(4);
								writer.WriteString(s);
								break;
							default:
								writer.WriteInt(10);
								break;
						}

						continue;
					}
				}

				switch (field)
				{
					case Level1Fields.OpenPrice:
					case Level1Fields.HighPrice:
					case Level1Fields.LowPrice:
					case Level1Fields.ClosePrice:
					{
						SerializePrice(writer, metaInfo, (decimal)value, useLong, nonAdjustPrice);
						break;
					}
					case Level1Fields.MinPrice:
					{
						if (minMaxPrice)
							SerializeChange(writer, metaInfo.MinPrice, (decimal)value);
						else
							SerializePrice(writer, metaInfo, (decimal)value, useLong, nonAdjustPrice);

						break;
					}
					case Level1Fields.MaxPrice:
					{
						var price = (decimal)value;

						if (minMaxPrice)
							SerializeChange(writer, metaInfo.MaxPrice, (decimal)value);
						else
							SerializePrice(writer, metaInfo, price == int.MaxValue ? metaInfo.LastPriceStep : price, useLong, nonAdjustPrice);
						
						break;
					}
					case Level1Fields.BidsVolume:
					case Level1Fields.AsksVolume:
					case Level1Fields.OpenInterest:
					{
						writer.WriteVolume((decimal)value, metaInfo, largeDecimal);
						break;
					}
					case Level1Fields.ImpliedVolatility:
					{
						SerializeChange(writer, metaInfo.ImpliedVolatility, (decimal)value);
						break;
					}
					case Level1Fields.HistoricalVolatility:
					{
						SerializeChange(writer, metaInfo.HistoricalVolatility, (decimal)value);
						break;
					}
					case Level1Fields.TheorPrice:
					{
						SerializeChange(writer, metaInfo.TheorPrice, (decimal)value);
						break;
					}
					case Level1Fields.Delta:
					{
						SerializeChange(writer, metaInfo.Delta, (decimal)value);
						break;
					}
					case Level1Fields.Gamma:
					{
						SerializeChange(writer, metaInfo.Gamma, (decimal)value);
						break;
					}
					case Level1Fields.Vega:
					{
						SerializeChange(writer, metaInfo.Vega, (decimal)value);
						break;
					}
					case Level1Fields.Theta:
					{
						SerializeChange(writer, metaInfo.Theta, (decimal)value);
						break;
					}
					case Level1Fields.MarginBuy:
					{
						SerializeChange(writer, metaInfo.MarginBuy, (decimal)value);
						break;
					}
					case Level1Fields.MarginSell:
					{
						SerializeChange(writer, metaInfo.MarginSell, (decimal)value);
						break;
					}
					case Level1Fields.PriceStep:
					{
						if (storeSteps)
							SerializePrice(writer, metaInfo, (decimal)value, useLong, nonAdjustPrice);
						else
						{
							//нет необходимости хранить шаги цены и объема, т.к. они есть в metaInfo
						}

						break;
					}
					case Level1Fields.VolumeStep:
					{
						if (storeSteps)
							writer.WriteVolume((decimal)value, metaInfo, largeDecimal);
						else
						{
							//нет необходимости хранить шаги цены и объема, т.к. они есть в metaInfo
						}

						break;
					}
					case Level1Fields.Decimals:
					{
						writer.WriteInt((int)value);
						break;
					}
					case Level1Fields.Multiplier:
					{
						writer.WriteVolume((decimal)value, metaInfo, largeDecimal);
						break;
					}
					case Level1Fields.StepPrice:
					{
						SerializeChange(writer, metaInfo.StepPrice, (decimal)value);
						break;
					}
#pragma warning disable CS0612 // Type or member is obsolete
					case Level1Fields.LastTrade:
					{
						var trade = (ITickTradeMessage)value;

						SerializePrice(writer, metaInfo, trade.Price, useLong, nonAdjustPrice);
						writer.WriteVolume(trade.Volume, metaInfo, largeDecimal);
						writer.WriteSide(trade.OriginSide);

						break;
					}
					case Level1Fields.BestBid:
					case Level1Fields.BestAsk:
					{
						var quote = (QuoteChange)value;

						SerializePrice(writer, metaInfo, quote.Price, useLong, nonAdjustPrice);
						writer.WriteVolume(quote.Volume, metaInfo, largeDecimal);

						break;
					}
#pragma warning restore CS0612 // Type or member is obsolete
					case Level1Fields.State:
					{
						writer.WriteInt((int)(SecurityStates)value);
						break;
					}
					case Level1Fields.BestBidPrice:
					case Level1Fields.BestAskPrice:
					case Level1Fields.LastTradePrice:
					case Level1Fields.SettlementPrice:
					case Level1Fields.HighBidPrice:
					case Level1Fields.LowAskPrice:
					case Level1Fields.SpreadMiddle:
					case Level1Fields.LowBidPrice:
					case Level1Fields.HighAskPrice:
					case Level1Fields.UnderlyingBestBidPrice:
					case Level1Fields.UnderlyingBestAskPrice:
					case Level1Fields.MedianPrice:
					case Level1Fields.HighPrice52Week:
					case Level1Fields.LowPrice52Week:
					{
						SerializePrice(writer, metaInfo, (decimal)value, useLong, nonAdjustPrice);
						break;
					}
					case Level1Fields.AveragePrice:
					{
						if (metaInfo.Version < MarketDataVersions.Version51)
							SerializePrice(writer, metaInfo, (decimal)value, false, false);
						else
							SerializeChange(writer, metaInfo.AveragePrice, (decimal)value);

						break;
					}
					case Level1Fields.Volume:
					case Level1Fields.LastTradeVolume:
					case Level1Fields.BestBidVolume:
					case Level1Fields.BestAskVolume:
					case Level1Fields.MinVolume:
					case Level1Fields.MaxVolume:
					case Level1Fields.LastTradeVolumeLow:
					case Level1Fields.LastTradeVolumeHigh:
					case Level1Fields.LowBidVolume:
					case Level1Fields.HighAskVolume:
					{
						writer.WriteVolume((decimal)value, metaInfo, largeDecimal);
						break;
					}
					case Level1Fields.Change:
					{
						SerializeChange(writer, metaInfo.Change, (decimal)value);
						break;
					}
					case Level1Fields.Rho:
					{
						SerializeChange(writer, metaInfo.Rho, (decimal)value);
						break;
					}
					case Level1Fields.AccruedCouponIncome:
					{
						SerializeChange(writer, metaInfo.AccruedCouponIncome, (decimal)value);
						break;
					}
					case Level1Fields.Yield:
					{
						SerializeChange(writer, metaInfo.Yield, (decimal)value);
						break;
					}
					case Level1Fields.VWAP:
					{
						SerializeChange(writer, metaInfo.VWAP, (decimal)value);
						break;
					}
					case Level1Fields.LastTradeTime:
					case Level1Fields.BestBidTime:
					case Level1Fields.BestAskTime:
					{
						var timeValue = (DateTimeOffset)value;

						if (metaInfo.FirstFieldTime == default)
						{
							if (!isTickPrecision)
								timeValue = timeValue.StorageBinaryOldTruncate();

							metaInfo.FirstFieldTime = metaInfo.LastFieldTime = isUtc ? timeValue.UtcDateTime : timeValue.LocalDateTime;
						}

						var lastOffset = metaInfo.LastServerOffset;
						metaInfo.LastFieldTime = writer.WriteTime(timeValue, metaInfo.LastFieldTime, LocalizedStrings.LastTradeTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
						metaInfo.LastServerOffset = lastOffset;
						break;
					}
					case Level1Fields.BidsCount:
					case Level1Fields.AsksCount:
					{
						if (metaInfo.Version < MarketDataVersions.Version46)
							SerializePrice(writer, metaInfo, (int)value, false, false);
						else
							writer.WriteInt((int)value);

						break;
					}
					case Level1Fields.TradesCount:
					{
						writer.WriteInt((int)value);
						break;
					}
					case Level1Fields.LastTradeId:
					{
						writer.WriteLong((long)value);
						break;
					}
					case Level1Fields.LastTradeStringId:
					{
						writer.WriteString((string)value);
						break;
					}
					case Level1Fields.LastTradeUpDown:
					{
						writer.Write((bool)value);
						break;
					}
					case Level1Fields.LastTradeOrigin:
					{
						writer.WriteSide((Sides?)value);
						break;
					}
					case Level1Fields.PriceEarnings:
					{
						SerializeChange(writer, metaInfo.PriceEarnings, (decimal)value);
						break;
					}
					case Level1Fields.ForwardPriceEarnings:
					{
						SerializeChange(writer, metaInfo.ForwardPriceEarnings, (decimal)value);
						break;
					}
					case Level1Fields.PriceEarningsGrowth:
					{
						SerializeChange(writer, metaInfo.PriceEarningsGrowth, (decimal)value);
						break;
					}
					case Level1Fields.PriceSales:
					{
						SerializeChange(writer, metaInfo.PriceSales, (decimal)value);
						break;
					}
					case Level1Fields.PriceBook:
					{
						SerializeChange(writer, metaInfo.PriceBook, (decimal)value);
						break;
					}
					case Level1Fields.PriceCash:
					{
						SerializeChange(writer, metaInfo.PriceCash, (decimal)value);
						break;
					}
					case Level1Fields.PriceFreeCash:
					{
						SerializeChange(writer, metaInfo.PriceFreeCash, (decimal)value);
						break;
					}
					case Level1Fields.Payout:
					{
						SerializeChange(writer, metaInfo.Payout, (decimal)value);
						break;
					}
					case Level1Fields.SharesOutstanding:
					{
						SerializeChange(writer, metaInfo.SharesOutstanding, (decimal)value);
						break;
					}
					case Level1Fields.SharesFloat:
					{
						SerializeChange(writer, metaInfo.SharesFloat, (decimal)value);
						break;
					}
					case Level1Fields.FloatShort:
					{
						SerializeChange(writer, metaInfo.FloatShort, (decimal)value);
						break;
					}
					case Level1Fields.ShortRatio:
					{
						SerializeChange(writer, metaInfo.ShortRatio, (decimal)value);
						break;
					}
					case Level1Fields.ReturnOnAssets:
					{
						SerializeChange(writer, metaInfo.ReturnOnAssets, (decimal)value);
						break;
					}
					case Level1Fields.ReturnOnEquity:
					{
						SerializeChange(writer, metaInfo.ReturnOnEquity, (decimal)value);
						break;
					}
					case Level1Fields.ReturnOnInvestment:
					{
						SerializeChange(writer, metaInfo.ReturnOnInvestment, (decimal)value);
						break;
					}
					case Level1Fields.CurrentRatio:
					{
						SerializeChange(writer, metaInfo.CurrentRatio, (decimal)value);
						break;
					}
					case Level1Fields.QuickRatio:
					{
						SerializeChange(writer, metaInfo.QuickRatio, (decimal)value);
						break;
					}
					case Level1Fields.LongTermDebtEquity:
					{
						SerializeChange(writer, metaInfo.LongTermDebtEquity, (decimal)value);
						break;
					}
					case Level1Fields.TotalDebtEquity:
					{
						SerializeChange(writer, metaInfo.TotalDebtEquity, (decimal)value);
						break;
					}
					case Level1Fields.GrossMargin:
					{
						SerializeChange(writer, metaInfo.GrossMargin, (decimal)value);
						break;
					}
					case Level1Fields.OperatingMargin:
					{
						SerializeChange(writer, metaInfo.OperatingMargin, (decimal)value);
						break;
					}
					case Level1Fields.ProfitMargin:
					{
						SerializeChange(writer, metaInfo.ProfitMargin, (decimal)value);
						break;
					}
					case Level1Fields.Beta:
					{
						SerializeChange(writer, metaInfo.Beta, (decimal)value);
						break;
					}
					case Level1Fields.AverageTrueRange:
					{
						SerializeChange(writer, metaInfo.AverageTrueRange, (decimal)value);
						break;
					}
					case Level1Fields.HistoricalVolatilityWeek:
					{
						SerializeChange(writer, metaInfo.HistoricalVolatilityWeek, (decimal)value);
						break;
					}
					case Level1Fields.HistoricalVolatilityMonth:
					{
						SerializeChange(writer, metaInfo.HistoricalVolatilityMonth, (decimal)value);
						break;
					}
					case Level1Fields.IsSystem:
					{
						writer.Write((bool)value);
						break;
					}
					case Level1Fields.Turnover:
					{
						SerializeChange(writer, metaInfo.Turnover, (decimal)value);
						break;
					}
					case Level1Fields.IssueSize:
					{
						SerializeChange(writer, metaInfo.IssueSize, (decimal)value);
						break;
					}
					case Level1Fields.Duration:
					{
						SerializeChange(writer, metaInfo.Duration, (decimal)value);
						break;
					}
					case Level1Fields.BuyBackDate:
					{
						var info = metaInfo.BuyBackInfo;
						var timeValue = (DateTimeOffset)value;
						
						if (info.FirstDateTime == default)
						{
							if (!isTickPrecision)
								timeValue = timeValue.StorageBinaryOldTruncate();

							info.FirstDateTime = info.LastDateTime = isUtc ? timeValue.UtcDateTime : timeValue.LocalDateTime;
						}

						var lastOffset = info.LastDateOffset;
						info.LastDateTime = writer.WriteTime(timeValue, info.LastDateTime, LocalizedStrings.LastTradeTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
						info.LastDateOffset = lastOffset;
						break;
					}
					case Level1Fields.BuyBackPrice:
					{
						SerializeChange(writer, metaInfo.BuyBackPrice, (decimal)value);
						break;
					}
					case Level1Fields.Dividend:
					case Level1Fields.BeforeSplit:
					case Level1Fields.AfterSplit:
					case Level1Fields.CommissionMaker:
					case Level1Fields.CommissionTaker:
					case Level1Fields.UnderlyingMinVolume:
					case Level1Fields.CouponValue:
					case Level1Fields.CouponPeriod:
					case Level1Fields.MarketPriceYesterday:
					case Level1Fields.MarketPriceToday:
					case Level1Fields.VWAPPrev:
					case Level1Fields.YieldVWAP:
					case Level1Fields.YieldVWAPPrev:
					case Level1Fields.Index:
					case Level1Fields.Imbalance:
					case Level1Fields.UnderlyingPrice:
					case Level1Fields.OptionMargin:
					case Level1Fields.OptionSyntheticMargin:
					{
						writer.WriteDecimal((decimal)value, 0);
						break;
					}
					case Level1Fields.CouponDate:
					{
						var info = metaInfo.CouponInfo;
						var timeValue = (DateTimeOffset)value;

						if (info.FirstDateTime == default)
						{
							if (!isTickPrecision)
								timeValue = timeValue.StorageBinaryOldTruncate();

							info.FirstDateTime = info.LastDateTime = isUtc ? timeValue.UtcDateTime : timeValue.LocalDateTime;
						}

						var lastOffset = info.LastDateOffset;
						info.LastDateTime = writer.WriteTime(timeValue, info.LastDateTime, LocalizedStrings.LastTradeTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
						info.LastDateOffset = lastOffset;
						break;
					}
					default:
						throw new ArgumentOutOfRangeException(nameof(messages), change.Key, LocalizedStrings.InvalidValue);
				}
			}
		}
	}

	public override Level1ChangeMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;
		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version53;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version54;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version55;
		var unkTypes = metaInfo.Version >= MarketDataVersions.Version58;
		var nonAdjustPrice = metaInfo.Version >= MarketDataVersions.Version59;
		var minMaxPrice = metaInfo.Version >= MarketDataVersions.Version60;
		var useLong = metaInfo.Version >= MarketDataVersions.Version60;
		var storeSteps = metaInfo.Version >= MarketDataVersions.Version60;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version62;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version63;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version64;

		var l1Msg = new Level1ChangeMessage { SecurityId = SecurityId };

		var changeCount = 1;

		if (metaInfo.Version >= MarketDataVersions.Version49)
		{
			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			l1Msg.ServerTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			if (reader.Read())
			{
				prevTime = metaInfo.FirstLocalTime;
				lastOffset = metaInfo.FirstLocalOffset;
				l1Msg.LocalTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.FirstLocalTime = prevTime;
				metaInfo.FirstLocalOffset = lastOffset;
			}
			//else
			//	l1Msg.LocalTime = l1Msg.ServerTime;

			changeCount = reader.ReadInt();

			if (buildFrom)
				l1Msg.BuildFrom = reader.ReadBuildFrom();

			if (seqNum)
				reader.ReadSeqNum(l1Msg, metaInfo);
		}
		else
		{
			var prevTime = metaInfo.FirstTime;
			var offset = TimeSpan.Zero;
			l1Msg.ServerTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, false, false, ref offset);
			l1Msg.LocalTime = metaInfo.FirstTime = prevTime;
		}

		for (var i = 0; i < changeCount; i++)
		{
			var field = MapFrom(metaInfo, reader.ReadInt());

			if (unkTypes)
			{
				if (!reader.Read())
				{
					var unkType = reader.ReadInt();
					switch (unkType)
					{
						case 0:
							l1Msg.Add(field, reader.ReadDecimal(0));
							break;

						case 1:
							l1Msg.Add(field, reader.ReadLong());
							break;

						case 2:
							l1Msg.Add(field, reader.ReadInt());
							break;

						case 3:
							l1Msg.Add(field, reader.ReadLong().To<DateTimeOffset>());
							break;

						case 4:
							l1Msg.Add(field, reader.ReadString());
							break;

						case 10:
							break;

						default:
							throw new ArgumentOutOfRangeException(nameof(unkType), unkType, LocalizedStrings.InvalidValue);
					}

					continue;
				}
			}

			switch (field)
			{
				case Level1Fields.OpenPrice:
				case Level1Fields.HighPrice:
				case Level1Fields.LowPrice:
				case Level1Fields.ClosePrice:
				{
					var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);
					l1Msg.Add(field, price);
					break;
				}
				case Level1Fields.MinPrice:
				case Level1Fields.MaxPrice:
				{
					if (minMaxPrice)
					{
						l1Msg.Add(field, DeserializeChange(reader, field == Level1Fields.MinPrice ? metaInfo.MinPrice : metaInfo.MaxPrice));
					}
					else
					{
						var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);
						l1Msg.Add(field, price);
					}

					break;
				}
				//case Level1Fields.MaxPrice:
				//{
				//	metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo);
				//	l1Msg.Add(field, metaInfo.Price.First == metaInfo.PriceStep ? int.MaxValue : metaInfo.Price.First);
				//	break;
				//}
				case Level1Fields.BidsVolume:
				case Level1Fields.AsksVolume:
				case Level1Fields.OpenInterest:
				{
					l1Msg.Add(field, reader.ReadVolume(metaInfo, largeDecimal));
					break;
				}
				case Level1Fields.ImpliedVolatility:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ImpliedVolatility));
					break;
				}
				case Level1Fields.HistoricalVolatility:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.HistoricalVolatility));
					break;
				}
				case Level1Fields.TheorPrice:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.TheorPrice));
					break;
				}
				case Level1Fields.Delta:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Delta));
					break;
				}
				case Level1Fields.Gamma:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Gamma));
					break;
				}
				case Level1Fields.Vega:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Vega));
					break;
				}
				case Level1Fields.Theta:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Theta));
					break;
				}
				case Level1Fields.MarginBuy:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.MarginBuy));
					break;
				}
				case Level1Fields.MarginSell:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.MarginSell));
					break;
				}
				case Level1Fields.PriceStep:
				{
					if (storeSteps)
					{
						var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);
						l1Msg.Add(field, price);
					}
					else
						l1Msg.Add(field, metaInfo.PriceStep);

					break;
				}
				case Level1Fields.Decimals:
				{
					l1Msg.Add(field, reader.ReadInt());
					break;
				}
				case Level1Fields.VolumeStep:
				{
					l1Msg.Add(field, storeSteps ? reader.ReadVolume(metaInfo, largeDecimal) : metaInfo.VolumeStep);
					break;
				}
				case Level1Fields.Multiplier:
				{
					l1Msg.Add(field, reader.ReadVolume(metaInfo, largeDecimal));
					break;
				}
				case Level1Fields.StepPrice:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.StepPrice));
					break;
				}
#pragma warning disable CS0612 // Type or member is obsolete
				case Level1Fields.LastTrade:
				{
					var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);

					l1Msg
						.Add(Level1Fields.LastTradePrice, price)
						.Add(Level1Fields.LastTradeVolume, reader.ReadVolume(metaInfo, largeDecimal))
						.Add(Level1Fields.LastTradeTime, metaInfo.FirstTime.ApplyTimeZone(metaInfo.ServerOffset));

					var origin = reader.ReadSide();

					if (origin != null)
						l1Msg.Add(Level1Fields.LastTradeOrigin, origin.Value);

					break;
				}
				case Level1Fields.BestBid:
				{
					var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);

					l1Msg
						.Add(Level1Fields.BestBidPrice, price)
						.Add(Level1Fields.BestBidVolume, reader.ReadVolume(metaInfo, largeDecimal));

					break;
				}
				case Level1Fields.BestAsk:
				{
					var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);

					l1Msg
						.Add(Level1Fields.BestAskPrice, price)
						.Add(Level1Fields.BestAskVolume, reader.ReadVolume(metaInfo, largeDecimal));

					break;
				}
#pragma warning restore CS0612 // Type or member is obsolete
				case Level1Fields.State:
				{
					l1Msg.Add(field, (SecurityStates)reader.ReadInt());
					break;
				}
				case Level1Fields.BestBidPrice:
				case Level1Fields.BestAskPrice:
				case Level1Fields.LastTradePrice:
				case Level1Fields.SettlementPrice:
				case Level1Fields.HighBidPrice:
				case Level1Fields.LowAskPrice:
				case Level1Fields.SpreadMiddle:
				case Level1Fields.LowBidPrice:
				case Level1Fields.HighAskPrice:
				case Level1Fields.UnderlyingBestBidPrice:
				case Level1Fields.UnderlyingBestAskPrice:
				case Level1Fields.MedianPrice:
				case Level1Fields.HighPrice52Week:
				case Level1Fields.LowPrice52Week:
				{
					var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);
					l1Msg.Add(field, price);
					break;
				}
				case Level1Fields.AveragePrice:
				{
					if (metaInfo.Version < MarketDataVersions.Version51)
					{
						var price = DeserializePrice(reader, metaInfo, false, false);
						l1Msg.Add(field, price);
					}
					else
						l1Msg.Add(field, DeserializeChange(reader, metaInfo.AveragePrice));

					break;
				}
				case Level1Fields.Volume:
				case Level1Fields.LastTradeVolume:
				case Level1Fields.BestBidVolume:
				case Level1Fields.BestAskVolume:
				case Level1Fields.MinVolume:
				case Level1Fields.MaxVolume:
				case Level1Fields.LastTradeVolumeLow:
				case Level1Fields.LastTradeVolumeHigh:
				case Level1Fields.LowBidVolume:
				case Level1Fields.HighAskVolume:
				{
					l1Msg.Add(field, reader.ReadVolume(metaInfo, largeDecimal));
					break;
				}
				case Level1Fields.Change:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Change));
					break;
				}
				case Level1Fields.Rho:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Rho));
					break;
				}
				case Level1Fields.AccruedCouponIncome:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.AccruedCouponIncome));
					break;
				}
				case Level1Fields.Yield:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Yield));
					break;
				}
				case Level1Fields.VWAP:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.VWAP));
					break;
				}
				case Level1Fields.LastTradeTime:
				case Level1Fields.BestBidTime:
				case Level1Fields.BestAskTime:
				{
					var prevTime = metaInfo.FirstFieldTime;
					var lastOffset = metaInfo.FirstServerOffset;
					l1Msg.Add(field, reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset));
					metaInfo.FirstFieldTime = prevTime;
					metaInfo.FirstServerOffset = lastOffset;
					break;
				}
				case Level1Fields.BidsCount:
				case Level1Fields.AsksCount:
				{
					l1Msg.Add(field, metaInfo.Version < MarketDataVersions.Version46 ? (int)reader.ReadVolume(metaInfo, false) : reader.ReadInt());
					break;
				}
				case Level1Fields.TradesCount:
				{
					l1Msg.Add(field, reader.ReadInt());
					break;
				}
				case Level1Fields.LastTradeId:
				{
					l1Msg.Add(field, reader.ReadLong());
					break;
				}
				case Level1Fields.LastTradeStringId:
				{
					l1Msg.Add(field, reader.ReadString());
					break;
				}
				case Level1Fields.LastTradeUpDown:
				{
					l1Msg.Add(field, reader.Read());
					break;
				}
				case Level1Fields.LastTradeOrigin:
				{
					l1Msg.TryAdd(field, reader.ReadSide());
					break;
				}
				case Level1Fields.PriceEarnings:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.PriceEarnings));
					break;
				}
				case Level1Fields.ForwardPriceEarnings:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ForwardPriceEarnings));
					break;
				}
				case Level1Fields.PriceEarningsGrowth:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.PriceEarningsGrowth));
					break;
				}
				case Level1Fields.PriceSales:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.PriceSales));
					break;
				}
				case Level1Fields.PriceBook:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.PriceBook));
					break;
				}
				case Level1Fields.PriceCash:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.PriceCash));
					break;
				}
				case Level1Fields.PriceFreeCash:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.PriceFreeCash));
					break;
				}
				case Level1Fields.Payout:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Payout));
					break;
				}
				case Level1Fields.SharesOutstanding:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.SharesOutstanding));
					break;
				}
				case Level1Fields.SharesFloat:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.SharesFloat));
					break;
				}
				case Level1Fields.FloatShort:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.FloatShort));
					break;
				}
				case Level1Fields.ShortRatio:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ShortRatio));
					break;
				}
				case Level1Fields.ReturnOnAssets:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ReturnOnAssets));
					break;
				}
				case Level1Fields.ReturnOnEquity:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ReturnOnEquity));
					break;
				}
				case Level1Fields.ReturnOnInvestment:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ReturnOnInvestment));
					break;
				}
				case Level1Fields.CurrentRatio:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.CurrentRatio));
					break;
				}
				case Level1Fields.QuickRatio:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.QuickRatio));
					break;
				}
				case Level1Fields.LongTermDebtEquity:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.LongTermDebtEquity));
					break;
				}
				case Level1Fields.TotalDebtEquity:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.TotalDebtEquity));
					break;
				}
				case Level1Fields.GrossMargin:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.GrossMargin));
					break;
				}
				case Level1Fields.OperatingMargin:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.OperatingMargin));
					break;
				}
				case Level1Fields.ProfitMargin:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.ProfitMargin));
					break;
				}
				case Level1Fields.Beta:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Beta));
					break;
				}
				case Level1Fields.AverageTrueRange:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.AverageTrueRange));
					break;
				}
				case Level1Fields.HistoricalVolatilityWeek:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.HistoricalVolatilityWeek));
					break;
				}
				case Level1Fields.HistoricalVolatilityMonth:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.HistoricalVolatilityMonth));
					break;
				}
				case Level1Fields.IsSystem:
				{
					l1Msg.Add(field, reader.Read());
					break;
				}
				case Level1Fields.Turnover:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Turnover));
					break;
				}
				case Level1Fields.IssueSize:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.IssueSize));
					break;
				}
				case Level1Fields.Duration:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.Duration));
					break;
				}
				case Level1Fields.BuyBackDate:
				{
					var info = metaInfo.BuyBackInfo;
					var prevTime = info.FirstDateTime;
					var lastOffset = info.FirstDateOffset;
					l1Msg.Add(field, reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset));
					info.FirstDateTime = prevTime;
					info.FirstDateOffset = lastOffset;
					break;
				}
				case Level1Fields.BuyBackPrice:
				{
					l1Msg.Add(field, DeserializeChange(reader, metaInfo.BuyBackPrice));
					break;
				}
				case Level1Fields.Dividend:
				case Level1Fields.BeforeSplit:
				case Level1Fields.AfterSplit:
				case Level1Fields.CommissionMaker:
				case Level1Fields.CommissionTaker:
				case Level1Fields.UnderlyingMinVolume:
				case Level1Fields.CouponValue:
				case Level1Fields.CouponPeriod:
				case Level1Fields.MarketPriceYesterday:
				case Level1Fields.MarketPriceToday:
				case Level1Fields.VWAPPrev:
				case Level1Fields.YieldVWAP:
				case Level1Fields.YieldVWAPPrev:
				case Level1Fields.Index:
				case Level1Fields.Imbalance:
				case Level1Fields.UnderlyingPrice:
				case Level1Fields.OptionMargin:
				case Level1Fields.OptionSyntheticMargin:
				{
					l1Msg.Add(field, reader.ReadDecimal(0));
					break;
				}
				case Level1Fields.CouponDate:
				{
					var info = metaInfo.CouponInfo;
					var prevTime = info.FirstDateTime;
					var lastOffset = info.FirstDateOffset;
					l1Msg.Add(field, reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset));
					info.FirstDateTime = prevTime;
					info.FirstDateOffset = lastOffset;
					break;
				}
				default:
					throw new InvalidOperationException(LocalizedStrings.UnsupportedType.Put(field));
			}
		}
		
		return l1Msg;
	}

	private decimal DeserializePrice(BitArrayReader reader, Level1MetaInfo metaInfo, bool useLong, bool nonAdjustPrice)
	{
		var prevPrice = metaInfo.Price.First;
		var price = reader.ReadPrice(ref prevPrice, metaInfo, useLong, nonAdjustPrice);
		metaInfo.Price.First = prevPrice;

		return price;
	}

	private void SerializePrice(BitArrayWriter writer, Level1MetaInfo metaInfo, decimal price, bool useLong, bool nonAdjustPrice)
	{
		// execution ticks (like option execution) may be a zero cost
		// ticks for spreads may be a zero cost or less than zero
		//if (price == 0)
		//	throw new ArgumentOutOfRangeException(nameof(price));

		var pair = metaInfo.Price;

		if (pair.First == 0)
			pair.First = pair.Second = price;

		var prevPrice = pair.Second;
		writer.WritePrice(price, ref prevPrice, metaInfo, SecurityId, useLong, nonAdjustPrice);
		pair.Second = prevPrice;
	}

	private static void SerializeChange(BitArrayWriter writer, RefPair<decimal, decimal> info, decimal price)
	{
		if (price == 0)
			throw new ArgumentOutOfRangeException(nameof(price));

		if (info.First == 0)
			info.First = info.Second = price;

		info.Second = writer.WriteDecimal(price, info.Second);
	}

	private static decimal DeserializeChange(BitArrayReader reader, RefPair<decimal, decimal> info)
	{
		info.First = reader.ReadDecimal(info.First);
		return info.First;
	}
}