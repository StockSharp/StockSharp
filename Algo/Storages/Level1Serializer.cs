namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class Level1MetaInfo : BinaryMetaInfo<Level1MetaInfo>
	{
		public Level1MetaInfo(DateTime date)
			: base(date)
		{
			Price = new RefPair<decimal, decimal>();
			ImpliedVolatility = new RefPair<decimal, decimal>();
			HistoricalVolatility = new RefPair<decimal, decimal>();
			TheorPrice = new RefPair<decimal, decimal>();
			StepPrice = new RefPair<decimal, decimal>();
			Delta = new RefPair<decimal, decimal>();
			Gamma = new RefPair<decimal, decimal>();
			Vega = new RefPair<decimal, decimal>();
			Theta = new RefPair<decimal, decimal>();
			MarginBuy = new RefPair<decimal, decimal>();
			MarginSell = new RefPair<decimal, decimal>();
			Change = new RefPair<decimal, decimal>();
			Rho = new RefPair<decimal, decimal>();
			AccruedCouponIncome = new RefPair<decimal, decimal>();
			Yield = new RefPair<decimal, decimal>();
			VWAP = new RefPair<decimal, decimal>();
			PriceEarnings = new RefPair<decimal, decimal>();
			ForwardPriceEarnings = new RefPair<decimal, decimal>();
			PriceEarningsGrowth = new RefPair<decimal, decimal>();
			PriceSales = new RefPair<decimal, decimal>();
			PriceBook = new RefPair<decimal, decimal>();
			PriceCash = new RefPair<decimal, decimal>();
			PriceFreeCash = new RefPair<decimal, decimal>();
			Payout = new RefPair<decimal, decimal>();
			SharesOutstanding = new RefPair<decimal, decimal>();
			SharesFloat = new RefPair<decimal, decimal>();
			FloatShort = new RefPair<decimal, decimal>();
			ShortRatio = new RefPair<decimal, decimal>();
			ReturnOnAssets = new RefPair<decimal, decimal>();
			ReturnOnEquity = new RefPair<decimal, decimal>();
			ReturnOnInvestment = new RefPair<decimal, decimal>();
			CurrentRatio = new RefPair<decimal, decimal>();
			QuickRatio = new RefPair<decimal, decimal>();
			LongTermDebtEquity = new RefPair<decimal, decimal>();
			TotalDebtEquity = new RefPair<decimal, decimal>();
			GrossMargin = new RefPair<decimal, decimal>();
			OperatingMargin = new RefPair<decimal, decimal>();
			ProfitMargin = new RefPair<decimal, decimal>();
			Beta = new RefPair<decimal, decimal>();
			AverageTrueRange = new RefPair<decimal, decimal>();
			HistoricalVolatilityWeek = new RefPair<decimal, decimal>();
			HistoricalVolatilityMonth = new RefPair<decimal, decimal>();
			AveragePrice = new RefPair<decimal, decimal>();
		}

		public RefPair<decimal, decimal> Price { get; private set; }
		public RefPair<decimal, decimal> ImpliedVolatility { get; private set; }
		public RefPair<decimal, decimal> HistoricalVolatility { get; private set; }
		public RefPair<decimal, decimal> TheorPrice { get; private set; }
		public RefPair<decimal, decimal> StepPrice { get; private set; }
		public RefPair<decimal, decimal> Delta { get; private set; }
		public RefPair<decimal, decimal> Gamma { get; private set; }
		public RefPair<decimal, decimal> Vega { get; private set; }
		public RefPair<decimal, decimal> Theta { get; private set; }
		public RefPair<decimal, decimal> MarginBuy { get; private set; }
		public RefPair<decimal, decimal> MarginSell { get; private set; }
		public RefPair<decimal, decimal> Change { get; private set; }
		public RefPair<decimal, decimal> Rho { get; private set; }
		public RefPair<decimal, decimal> AccruedCouponIncome { get; private set; }
		public RefPair<decimal, decimal> Yield { get; private set; }
		public RefPair<decimal, decimal> VWAP { get; private set; }
		public RefPair<decimal, decimal> PriceEarnings { get; private set; }
		public RefPair<decimal, decimal> ForwardPriceEarnings { get; private set; }
		public RefPair<decimal, decimal> PriceEarningsGrowth { get; private set; }
		public RefPair<decimal, decimal> PriceSales { get; private set; }
		public RefPair<decimal, decimal> PriceBook { get; private set; }
		public RefPair<decimal, decimal> PriceCash { get; private set; }
		public RefPair<decimal, decimal> PriceFreeCash { get; private set; }
		public RefPair<decimal, decimal> Payout { get; private set; }
		public RefPair<decimal, decimal> SharesOutstanding { get; private set; }
		public RefPair<decimal, decimal> SharesFloat { get; private set; }
		public RefPair<decimal, decimal> FloatShort { get; private set; }
		public RefPair<decimal, decimal> ShortRatio { get; private set; }
		public RefPair<decimal, decimal> ReturnOnAssets { get; private set; }
		public RefPair<decimal, decimal> ReturnOnEquity { get; private set; }
		public RefPair<decimal, decimal> ReturnOnInvestment { get; private set; }
		public RefPair<decimal, decimal> CurrentRatio { get; private set; }
		public RefPair<decimal, decimal> QuickRatio { get; private set; }
		public RefPair<decimal, decimal> LongTermDebtEquity { get; private set; }
		public RefPair<decimal, decimal> TotalDebtEquity { get; private set; }
		public RefPair<decimal, decimal> GrossMargin { get; private set; }
		public RefPair<decimal, decimal> OperatingMargin { get; private set; }
		public RefPair<decimal, decimal> ProfitMargin { get; private set; }
		public RefPair<decimal, decimal> Beta { get; private set; }
		public RefPair<decimal, decimal> AverageTrueRange { get; private set; }
		public RefPair<decimal, decimal> HistoricalVolatilityWeek { get; private set; }
		public RefPair<decimal, decimal> HistoricalVolatilityMonth { get; private set; }
		public RefPair<decimal, decimal> AveragePrice { get; private set; }

		public DateTime FirstFieldTime { get; set; }
		public DateTime LastFieldTime { get; set; }

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

			stream.Write(FirstFieldTime);
			stream.Write(LastFieldTime);

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

			stream.Write(ServerOffset);
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

			FirstFieldTime = stream.Read<DateTime>();
			LastFieldTime = stream.Read<DateTime>();

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
		}

		private static void Write(Stream stream, RefPair<decimal, decimal> info)
		{
			stream.Write(info.First);
			stream.Write(info.Second);
		}

		private static RefPair<decimal, decimal> ReadInfo(Stream stream)
		{
			return new RefPair<decimal, decimal>(stream.Read<decimal>(), stream.Read<decimal>());
		}

		protected override void CopyFrom(Level1MetaInfo src)
		{
			base.CopyFrom(src);

			Price = Clone(src.Price);
			ImpliedVolatility = Clone(src.ImpliedVolatility);
			TheorPrice = Clone(src.TheorPrice);
			StepPrice = Clone(src.StepPrice);
			HistoricalVolatility = Clone(src.HistoricalVolatility);
			Delta = Clone(src.Delta);
			Gamma = Clone(src.Gamma);
			Vega = Clone(src.Vega);
			Theta = Clone(src.Theta);
			MarginBuy = Clone(src.MarginBuy);
			MarginSell = Clone(src.MarginSell);
			Change = Clone(src.Change);
			Rho = Clone(src.Rho);
			AccruedCouponIncome = Clone(src.AccruedCouponIncome);
			Yield = Clone(src.Yield);
			FirstFieldTime = src.FirstFieldTime;
			LastFieldTime = src.LastFieldTime;
			VWAP = src.VWAP;
			PriceEarnings = src.PriceEarnings;
			ForwardPriceEarnings = src.ForwardPriceEarnings;
			PriceEarningsGrowth = src.PriceEarningsGrowth;
			PriceSales = src.PriceSales;
			PriceBook = src.PriceBook;
			PriceCash = src.PriceCash;
			PriceFreeCash = src.PriceFreeCash;
			Payout = src.Payout;
			SharesOutstanding = src.SharesOutstanding;
			SharesFloat = src.SharesFloat;
			FloatShort = src.FloatShort;
			ShortRatio = src.ShortRatio;
			ReturnOnAssets = src.ReturnOnAssets;
			ReturnOnEquity = src.ReturnOnEquity;
			ReturnOnInvestment = src.ReturnOnInvestment;
			CurrentRatio = src.CurrentRatio;
			QuickRatio = src.QuickRatio;
			LongTermDebtEquity = src.LongTermDebtEquity;
			TotalDebtEquity = src.TotalDebtEquity;
			GrossMargin = src.GrossMargin;
			OperatingMargin = src.OperatingMargin;
			ProfitMargin = src.ProfitMargin;
			Beta = src.Beta;
			AverageTrueRange = src.AverageTrueRange;
			HistoricalVolatilityWeek = src.HistoricalVolatilityWeek;
			HistoricalVolatilityMonth = src.HistoricalVolatilityMonth;
			AveragePrice = src.AveragePrice;
		}

		private static RefPair<decimal, decimal> Clone(RefPair<decimal, decimal> info)
		{
			return new RefPair<decimal, decimal>(info.First, info.Second);
		}
	}

	class Level1Serializer : BinaryMarketDataSerializer<Level1ChangeMessage, Level1MetaInfo>
	{
		private static readonly SynchronizedPairSet<Level1Fields, int> _map = new SynchronizedPairSet<Level1Fields, int>
		{
			{ Level1Fields.OpenPrice,				1 },
			{ Level1Fields.HighPrice,				1 << 1 },
			{ Level1Fields.LowPrice,				1 << 2 },
			{ Level1Fields.ClosePrice,				1 << 3 },
			{ Level1Fields.LastTrade,				1 << 4 },
			{ Level1Fields.StepPrice,				1 << 5 },
			{ Level1Fields.BestBid,					1 << 6 },
			{ Level1Fields.BestAsk,					1 << 7 },
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

		public Level1Serializer(SecurityId securityId)
			: base(securityId, 50)
		{
			Version = MarketDataVersions.Version53;
		}

		private static int MapTo(Level1MetaInfo metaInfo, Level1Fields field)
		{
			if (metaInfo.Version < MarketDataVersions.Version45)
			{
				int fieldCode;

				if (!_map.TryGetValue(field, out fieldCode))
					throw new ArgumentException(LocalizedStrings.Str917Params.Put(field));

				return fieldCode;
			}
			
			return (int)field;
		}

		private static Level1Fields MapFrom(Level1MetaInfo metaInfo, int fieldCode)
		{
			if (metaInfo.Version < MarketDataVersions.Version45)
			{
				Level1Fields field;

				if (!_map.TryGetKey(fieldCode, out field))
					throw new ArgumentException(LocalizedStrings.Str918Params.Put(fieldCode));

				return field;
			}

			return (Level1Fields)fieldCode;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<Level1ChangeMessage> messages, Level1MetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var msg = messages.First();

				metaInfo.ServerOffset = msg.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version53;

			foreach (var message in messages)
			{
				if (metaInfo.Version >= MarketDataVersions.Version49)
				{
					metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, "level1", allowNonOrdered, isUtc, metaInfo.ServerOffset);

					var hasLocalTime = !message.LocalTime.IsDefault() && message.LocalTime != message.ServerTime;

					writer.Write(hasLocalTime);

					if (hasLocalTime)
						metaInfo.LastLocalTime = writer.WriteTime(message.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str919, allowNonOrdered, isUtc, metaInfo.LocalOffset);

					var count = message.Changes.Count;

					if (count == 0)
						throw new ArgumentException(LocalizedStrings.Str920, "messages");

					writer.WriteInt(count);
				}

				foreach (var change in message.Changes)
				{
					if (metaInfo.Version < MarketDataVersions.Version49)
						metaInfo.LastTime = writer.WriteTime(message.ServerTime, metaInfo.LastTime, "level1", allowNonOrdered, isUtc, metaInfo.ServerOffset);

					writer.WriteInt(MapTo(metaInfo, change.Key));

					switch (change.Key)
					{
						case Level1Fields.OpenPrice:
						case Level1Fields.HighPrice:
						case Level1Fields.LowPrice:
						case Level1Fields.ClosePrice:
						case Level1Fields.MinPrice:
						{
							SerializePrice(writer, metaInfo, (decimal)change.Value);
							break;
						}
						case Level1Fields.MaxPrice:
						{
							var value = (decimal)change.Value;
							SerializePrice(writer, metaInfo, value == int.MaxValue ? metaInfo.PriceStep : value);
							break;
						}
						case Level1Fields.BidsVolume:
						case Level1Fields.AsksVolume:
						case Level1Fields.OpenInterest:
						{
							writer.WriteVolume((decimal)change.Value, metaInfo, SecurityId);
							break;
						}
						case Level1Fields.ImpliedVolatility:
						{
							SerializeChange(writer, metaInfo.ImpliedVolatility, (decimal)change.Value);
							break;
						}
						case Level1Fields.HistoricalVolatility:
						{
							SerializeChange(writer, metaInfo.HistoricalVolatility, (decimal)change.Value);
							break;
						}
						case Level1Fields.TheorPrice:
						{
							SerializeChange(writer, metaInfo.TheorPrice, (decimal)change.Value);
							break;
						}
						case Level1Fields.Delta:
						{
							SerializeChange(writer, metaInfo.Delta, (decimal)change.Value);
							break;
						}
						case Level1Fields.Gamma:
						{
							SerializeChange(writer, metaInfo.Gamma, (decimal)change.Value);
							break;
						}
						case Level1Fields.Vega:
						{
							SerializeChange(writer, metaInfo.Vega, (decimal)change.Value);
							break;
						}
						case Level1Fields.Theta:
						{
							SerializeChange(writer, metaInfo.Theta, (decimal)change.Value);
							break;
						}
						case Level1Fields.MarginBuy:
						{
							SerializeChange(writer, metaInfo.MarginBuy, (decimal)change.Value);
							break;
						}
						case Level1Fields.MarginSell:
						{
							SerializeChange(writer, metaInfo.MarginSell, (decimal)change.Value);
							break;
						}
						case Level1Fields.PriceStep:
						case Level1Fields.VolumeStep:
						{
							//нет необходимости хранить шаги цены и объема, т.к. они есть в metaInfo
							break;
						}
						case Level1Fields.Multiplier:
						{
							writer.WriteVolume((decimal)change.Value, metaInfo, SecurityId);
							break;
						}
						case Level1Fields.StepPrice:
						{
							SerializeChange(writer, metaInfo.StepPrice, (decimal)change.Value);
							break;
						}
						case Level1Fields.LastTrade:
						{
							var trade = (Trade)change.Value;

							SerializePrice(writer, metaInfo, trade.Price);
							writer.WriteVolume(trade.Volume, metaInfo, SecurityId);
							writer.WriteSide(trade.OrderDirection);

							break;
						}
						case Level1Fields.BestBid:
						case Level1Fields.BestAsk:
						{
							var quote = (Quote)change.Value;

							SerializePrice(writer, metaInfo, quote.Price);
							writer.WriteVolume(quote.Volume, metaInfo, SecurityId);

							break;
						}
						case Level1Fields.State:
						{
							writer.WriteInt((int)(SecurityStates)change.Value);
							break;
						}
						case Level1Fields.BestBidPrice:
						case Level1Fields.BestAskPrice:
						case Level1Fields.LastTradePrice:
						case Level1Fields.SettlementPrice:
						case Level1Fields.HighBidPrice:
						case Level1Fields.LowAskPrice:
						{
							SerializePrice(writer, metaInfo, (decimal)change.Value);
							break;
						}
						case Level1Fields.AveragePrice:
						{
							if (metaInfo.Version < MarketDataVersions.Version51)
								SerializePrice(writer, metaInfo, (decimal)change.Value);
							else
								SerializeChange(writer, metaInfo.AveragePrice, (decimal)change.Value);

							break;
						}
						case Level1Fields.Volume:
						case Level1Fields.LastTradeVolume:
						case Level1Fields.BestBidVolume:
						case Level1Fields.BestAskVolume:
						{
							writer.WriteVolume((decimal)change.Value, metaInfo, SecurityId);
							break;
						}
						case Level1Fields.Change:
						{
							SerializeChange(writer, metaInfo.Change, (decimal)change.Value);
							break;
						}
						case Level1Fields.Rho:
						{
							SerializeChange(writer, metaInfo.Rho, (decimal)change.Value);
							break;
						}
						case Level1Fields.AccruedCouponIncome:
						{
							SerializeChange(writer, metaInfo.AccruedCouponIncome, (decimal)change.Value);
							break;
						}
						case Level1Fields.Yield:
						{
							SerializeChange(writer, metaInfo.Yield, (decimal)change.Value);
							break;
						}
						case Level1Fields.VWAP:
						{
							SerializeChange(writer, metaInfo.VWAP, (decimal)change.Value);
							break;
						}
						case Level1Fields.LastTradeTime:
						case Level1Fields.BestBidTime:
						case Level1Fields.BestAskTime:
						{
							writer.WriteTime((DateTimeOffset)change.Value, metaInfo.LastFieldTime, LocalizedStrings.Str921Params.Put(change.Key), allowNonOrdered, isUtc, metaInfo.ServerOffset);
							break;
						}
						case Level1Fields.BidsCount:
						case Level1Fields.AsksCount:
						{
							if (metaInfo.Version < MarketDataVersions.Version46)
								SerializePrice(writer, metaInfo, (int)change.Value);
							else
								writer.WriteInt((int)change.Value);

							break;
						}
						case Level1Fields.TradesCount:
						{
							writer.WriteInt((int)change.Value);
							break;
						}
						case Level1Fields.LastTradeId:
						{
							writer.WriteLong((long)change.Value);
							break;
						}
						case Level1Fields.LastTradeUpDown:
						{
							writer.Write((bool)change.Value);
							break;
						}
						case Level1Fields.LastTradeOrigin:
						{
							writer.WriteSide((Sides?)change.Value);
							break;
						}
						case Level1Fields.PriceEarnings:
						{
							SerializeChange(writer, metaInfo.PriceEarnings, (decimal)change.Value);
							break;
						}
						case Level1Fields.ForwardPriceEarnings:
						{
							SerializeChange(writer, metaInfo.ForwardPriceEarnings, (decimal)change.Value);
							break;
						}
						case Level1Fields.PriceEarningsGrowth:
						{
							SerializeChange(writer, metaInfo.PriceEarningsGrowth, (decimal)change.Value);
							break;
						}
						case Level1Fields.PriceSales:
						{
							SerializeChange(writer, metaInfo.PriceSales, (decimal)change.Value);
							break;
						}
						case Level1Fields.PriceBook:
						{
							SerializeChange(writer, metaInfo.PriceBook, (decimal)change.Value);
							break;
						}
						case Level1Fields.PriceCash:
						{
							SerializeChange(writer, metaInfo.PriceCash, (decimal)change.Value);
							break;
						}
						case Level1Fields.PriceFreeCash:
						{
							SerializeChange(writer, metaInfo.PriceFreeCash, (decimal)change.Value);
							break;
						}
						case Level1Fields.Payout:
						{
							SerializeChange(writer, metaInfo.Payout, (decimal)change.Value);
							break;
						}
						case Level1Fields.SharesOutstanding:
						{
							SerializeChange(writer, metaInfo.SharesOutstanding, (decimal)change.Value);
							break;
						}
						case Level1Fields.SharesFloat:
						{
							SerializeChange(writer, metaInfo.SharesFloat, (decimal)change.Value);
							break;
						}
						case Level1Fields.FloatShort:
						{
							SerializeChange(writer, metaInfo.FloatShort, (decimal)change.Value);
							break;
						}
						case Level1Fields.ShortRatio:
						{
							SerializeChange(writer, metaInfo.ShortRatio, (decimal)change.Value);
							break;
						}
						case Level1Fields.ReturnOnAssets:
						{
							SerializeChange(writer, metaInfo.ReturnOnAssets, (decimal)change.Value);
							break;
						}
						case Level1Fields.ReturnOnEquity:
						{
							SerializeChange(writer, metaInfo.ReturnOnEquity, (decimal)change.Value);
							break;
						}
						case Level1Fields.ReturnOnInvestment:
						{
							SerializeChange(writer, metaInfo.ReturnOnInvestment, (decimal)change.Value);
							break;
						}
						case Level1Fields.CurrentRatio:
						{
							SerializeChange(writer, metaInfo.CurrentRatio, (decimal)change.Value);
							break;
						}
						case Level1Fields.QuickRatio:
						{
							SerializeChange(writer, metaInfo.QuickRatio, (decimal)change.Value);
							break;
						}
						case Level1Fields.LongTermDebtEquity:
						{
							SerializeChange(writer, metaInfo.LongTermDebtEquity, (decimal)change.Value);
							break;
						}
						case Level1Fields.TotalDebtEquity:
						{
							SerializeChange(writer, metaInfo.TotalDebtEquity, (decimal)change.Value);
							break;
						}
						case Level1Fields.GrossMargin:
						{
							SerializeChange(writer, metaInfo.GrossMargin, (decimal)change.Value);
							break;
						}
						case Level1Fields.OperatingMargin:
						{
							SerializeChange(writer, metaInfo.OperatingMargin, (decimal)change.Value);
							break;
						}
						case Level1Fields.ProfitMargin:
						{
							SerializeChange(writer, metaInfo.ProfitMargin, (decimal)change.Value);
							break;
						}
						case Level1Fields.Beta:
						{
							SerializeChange(writer, metaInfo.Beta, (decimal)change.Value);
							break;
						}
						case Level1Fields.AverageTrueRange:
						{
							SerializeChange(writer, metaInfo.AverageTrueRange, (decimal)change.Value);
							break;
						}
						case Level1Fields.HistoricalVolatilityWeek:
						{
							SerializeChange(writer, metaInfo.HistoricalVolatilityWeek, (decimal)change.Value);
							break;
						}
						case Level1Fields.HistoricalVolatilityMonth:
						{
							SerializeChange(writer, metaInfo.HistoricalVolatilityMonth, (decimal)change.Value);
							break;
						}
						case Level1Fields.IsSystem:
						{
							writer.Write((bool)change.Value);
							break;
						}
						default:
							throw new ArgumentOutOfRangeException("messages", change.Key, LocalizedStrings.Str922);
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

			var l1Msg = new Level1ChangeMessage { SecurityId = SecurityId };

			var changeCount = 1;

			if (metaInfo.Version >= MarketDataVersions.Version49)
			{
				var prevTime = metaInfo.FirstTime;
				l1Msg.ServerTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId));
				metaInfo.FirstTime = prevTime;

				if (reader.Read())
				{
					prevTime = metaInfo.FirstLocalTime;
					l1Msg.LocalTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset).LocalDateTime;
					metaInfo.FirstLocalTime = prevTime;
				}
				//else
				//	l1Msg.LocalTime = l1Msg.ServerTime;

				changeCount = reader.ReadInt();
			}
			else
			{
				var prevTime = metaInfo.FirstTime;
				l1Msg.ServerTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset);
				l1Msg.LocalTime = metaInfo.FirstTime = prevTime;
			}

			for (var i = 0; i < changeCount; i++)
			{
				var field = MapFrom(metaInfo, reader.ReadInt());

				switch (field)
				{
					case Level1Fields.OpenPrice:
					case Level1Fields.HighPrice:
					case Level1Fields.LowPrice:
					case Level1Fields.ClosePrice:
					case Level1Fields.MinPrice:
					{
						metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo);
						l1Msg.Add(field, metaInfo.Price.First);
						break;
					}
					case Level1Fields.MaxPrice:
					{
						metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo);
						l1Msg.Add(field, metaInfo.Price.First == metaInfo.PriceStep ? int.MaxValue : metaInfo.Price.First);
						break;
					}
					case Level1Fields.BidsVolume:
					case Level1Fields.AsksVolume:
					case Level1Fields.OpenInterest:
					{
						l1Msg.Add(field, reader.ReadVolume(metaInfo));
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
						l1Msg.Add(field, metaInfo.PriceStep);
						break;
					}
					case Level1Fields.VolumeStep:
					{
						l1Msg.Add(field, metaInfo.VolumeStep);
						break;
					}
					case Level1Fields.Multiplier:
					{
						l1Msg.Add(field, reader.ReadVolume(metaInfo));
						break;
					}
					case Level1Fields.StepPrice:
					{
						l1Msg.Add(field, DeserializeChange(reader, metaInfo.StepPrice));
						break;
					}
					case Level1Fields.LastTrade:
					{
						metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo);

						l1Msg.Add(Level1Fields.LastTradePrice, metaInfo.Price.First);
						l1Msg.Add(Level1Fields.LastTradeVolume, reader.ReadVolume(metaInfo));
						l1Msg.Add(Level1Fields.LastTradeTime, metaInfo.FirstTime.ApplyTimeZone(metaInfo.ServerOffset));

						var origin = reader.ReadSide();

						if (origin != null)
							l1Msg.Add(Level1Fields.LastTradeOrigin, origin);

						break;
					}
					case Level1Fields.BestBid:
					{
						metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo);

						l1Msg.Add(Level1Fields.BestBidPrice, metaInfo.Price.First);
						l1Msg.Add(Level1Fields.BestBidVolume, reader.ReadVolume(metaInfo));
						break;
					}
					case Level1Fields.BestAsk:
					{
						metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo);

						l1Msg.Add(Level1Fields.BestAskPrice, metaInfo.Price.First);
						l1Msg.Add(Level1Fields.BestAskVolume, reader.ReadVolume(metaInfo));
						break;
					}
					case Level1Fields.State:
					{
						l1Msg.Add(field, reader.ReadInt());
						break;
					}
					case Level1Fields.BestBidPrice:
					case Level1Fields.BestAskPrice:
					case Level1Fields.LastTradePrice:
					case Level1Fields.SettlementPrice:
					case Level1Fields.HighBidPrice:
					case Level1Fields.LowAskPrice:
					{
						l1Msg.Add(field, metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo));
						break;
					}
					case Level1Fields.AveragePrice:
					{
						if (metaInfo.Version < MarketDataVersions.Version51)
							l1Msg.Add(field, metaInfo.Price.First = reader.ReadPrice(metaInfo.Price.First, metaInfo));
						else
							l1Msg.Add(field, DeserializeChange(reader, metaInfo.AveragePrice));

						break;
					}
					case Level1Fields.Volume:
					case Level1Fields.LastTradeVolume:
					case Level1Fields.BestBidVolume:
					case Level1Fields.BestAskVolume:
					{
						l1Msg.Add(field, reader.ReadVolume(metaInfo));
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
						l1Msg.Add(field, reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId)));
						metaInfo.FirstFieldTime = prevTime;
						break;
					}
					case Level1Fields.BidsCount:
					case Level1Fields.AsksCount:
					{
						l1Msg.Add(field, metaInfo.Version < MarketDataVersions.Version46 ? (int)reader.ReadVolume(metaInfo) : reader.ReadInt());
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
					case Level1Fields.LastTradeUpDown:
					{
						l1Msg.Add(field, reader.Read());
						break;
					}
					case Level1Fields.LastTradeOrigin:
					{
						l1Msg.Add(field, reader.ReadSide());
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
					default:
						throw new InvalidOperationException(LocalizedStrings.Str923Params.Put(field));
				}
			}
			
			return l1Msg;
		}

		private void SerializePrice(BitArrayWriter writer, Level1MetaInfo metaInfo, decimal price)
		{
			// execution ticks (like option execution) may be a zero cost
			// ticks for spreads may be a zero cost or less than zero
			//if (price == 0)
			//	throw new ArgumentOutOfRangeException("price");

			var pair = metaInfo.Price;

			if (pair.First == 0)
				pair.First = pair.Second = price;

			writer.WritePrice(price, pair.Second, metaInfo, SecurityId);
			pair.Second = price;
		}

		private static void SerializeChange(BitArrayWriter writer, RefPair<decimal, decimal> info, decimal price)
		{
			if (price == 0)
				throw new ArgumentOutOfRangeException("price");

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
}