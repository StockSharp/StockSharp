#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: Level1BinarySerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Binary
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

	class Level1MetaInfo : BinaryMetaInfo
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
			Turnover = new RefPair<decimal, decimal>();
			IssueSize = new RefPair<decimal, decimal>();
			Duration = new RefPair<decimal, decimal>();
			BuyBackPrice = new RefPair<decimal, decimal>();
			MinPrice = new RefPair<decimal, decimal>();
			MaxPrice = new RefPair<decimal, decimal>();
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
		public RefPair<decimal, decimal> Turnover { get; private set; }
		public RefPair<decimal, decimal> IssueSize { get; private set; }
		public RefPair<decimal, decimal> Duration { get; private set; }
		public RefPair<decimal, decimal> BuyBackPrice { get; private set; }
		public RefPair<decimal, decimal> MinPrice { get; private set; }
		public RefPair<decimal, decimal> MaxPrice { get; private set; }

		public DateTime FirstFieldTime { get; set; }
		public DateTime LastFieldTime { get; set; }

		public TimeSpan FirstBuyBackDateOffset { get; set; }
		public TimeSpan LastBuyBackDateOffset { get; set; }

		public DateTime FirstBuyBackDateTime { get; set; }
		public DateTime LastBuyBackDateTime { get; set; }

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

			stream.Write(FirstBuyBackDateTime);
			stream.Write(LastBuyBackDateTime);

			stream.Write(FirstBuyBackDateOffset);
			stream.Write(LastBuyBackDateOffset);

			if (Version < MarketDataVersions.Version58)
				return;

			stream.Write((int)MaxKnownType);

			if (Version < MarketDataVersions.Version59)
				return;

			WritePriceStep(stream);

			if (Version < MarketDataVersions.Version60)
				return;

			Write(stream, MinPrice);
			Write(stream, MaxPrice);
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

			FirstFieldTime = stream.Read<DateTime>().ChangeKind(DateTimeKind.Utc);
			LastFieldTime = stream.Read<DateTime>().ChangeKind(DateTimeKind.Utc);

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

			FirstBuyBackDateTime = stream.Read<DateTime>().ChangeKind(DateTimeKind.Utc);
			LastBuyBackDateTime = stream.Read<DateTime>().ChangeKind(DateTimeKind.Utc);

			FirstBuyBackDateOffset = stream.Read<TimeSpan>();
			LastBuyBackDateOffset = stream.Read<TimeSpan>();

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
		}

		private static void Write(Stream stream, RefPair<decimal, decimal> info)
		{
			stream.Write(info.First);
			stream.Write(info.Second);
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
			FirstBuyBackDateTime = l1Info.FirstBuyBackDateTime;
			LastBuyBackDateTime = l1Info.LastBuyBackDateTime;
			FirstBuyBackDateOffset = l1Info.FirstBuyBackDateOffset;
			LastBuyBackDateOffset = l1Info.LastBuyBackDateOffset;
			MaxKnownType = l1Info.MaxKnownType;
			MinPrice = Clone(l1Info.MinPrice);
			MaxPrice = Clone(l1Info.MaxPrice);
		}

		private static RefPair<decimal, decimal> Clone(RefPair<decimal, decimal> info)
		{
			return RefTuple.Create(info.First, info.Second);
		}
	}

	class Level1BinarySerializer : BinaryMarketDataSerializer<Level1ChangeMessage, Level1MetaInfo>
	{
		private static readonly SynchronizedPairSet<Level1Fields, int> _oldMap = new SynchronizedPairSet<Level1Fields, int>
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

		public Level1BinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 50, MarketDataVersions.Version60, exchangeInfoProvider)
		{
		}

		private static int MapTo(Level1MetaInfo metaInfo, Level1Fields field)
		{
			if (metaInfo.Version < MarketDataVersions.Version45)
			{
				if (!_oldMap.TryGetValue(field, out var fieldCode))
					throw new ArgumentException(LocalizedStrings.Str917Params.Put(field));

				return fieldCode;
			}
			
			return (int)field;
		}

		private static Level1Fields MapFrom(Level1MetaInfo metaInfo, int fieldCode)
		{
			if (metaInfo.Version < MarketDataVersions.Version45)
			{
				if (!_oldMap.TryGetKey(fieldCode, out var field))
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
				metaInfo.MaxKnownType = Level1Fields.Turnover;
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
						metaInfo.LastLocalTime = writer.WriteTime(message.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str919, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, true);
						metaInfo.LastLocalOffset = lastOffset;
					}

					var count = message.Changes.Count;

					if (count == 0)
						throw new ArgumentException(LocalizedStrings.Str920, nameof(messages));

					writer.WriteInt(count);
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
							writer.WriteVolume((decimal)value, metaInfo, SecurityId);
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
								writer.WriteVolume((decimal)value, metaInfo, SecurityId);
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
							writer.WriteVolume((decimal)value, metaInfo, SecurityId);
							break;
						}
						case Level1Fields.StepPrice:
						{
							SerializeChange(writer, metaInfo.StepPrice, (decimal)value);
							break;
						}
						case Level1Fields.LastTrade:
						{
							var trade = (Trade)value;

							SerializePrice(writer, metaInfo, trade.Price, useLong, nonAdjustPrice);
							writer.WriteVolume(trade.Volume, metaInfo, SecurityId);
							writer.WriteSide(trade.OrderDirection);

							break;
						}
						case Level1Fields.BestBid:
						case Level1Fields.BestAsk:
						{
							var quote = (Quote)value;

							SerializePrice(writer, metaInfo, quote.Price, useLong, nonAdjustPrice);
							writer.WriteVolume(quote.Volume, metaInfo, SecurityId);

							break;
						}
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
						{
							writer.WriteVolume((decimal)value, metaInfo, SecurityId);
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

							if (metaInfo.FirstFieldTime.IsDefault())
							{
								if (!isTickPrecision)
									timeValue = timeValue.StorageBinaryOldTruncate();

								metaInfo.FirstFieldTime = metaInfo.LastFieldTime = isUtc ? timeValue.UtcDateTime : timeValue.LocalDateTime;
							}

							var lastOffset = metaInfo.LastServerOffset;
							metaInfo.LastFieldTime = writer.WriteTime(timeValue, metaInfo.LastFieldTime, LocalizedStrings.Str921Params.Put(change.Key), allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
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
							var timeValue = (DateTimeOffset)value;

							if (metaInfo.FirstBuyBackDateTime.IsDefault())
							{
								if (!isTickPrecision)
									timeValue = timeValue.StorageBinaryOldTruncate();

								metaInfo.FirstBuyBackDateTime = metaInfo.LastBuyBackDateTime = isUtc ? timeValue.UtcDateTime : timeValue.LocalDateTime;
							}

							var lastOffset = metaInfo.LastBuyBackDateOffset;
							metaInfo.LastBuyBackDateTime = writer.WriteTime(timeValue, metaInfo.LastBuyBackDateTime, LocalizedStrings.Str921Params.Put(change.Key), allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
							metaInfo.LastBuyBackDateOffset = lastOffset;
							break;
						}
						case Level1Fields.BuyBackPrice:
						{
							SerializeChange(writer, metaInfo.BuyBackPrice, (decimal)value);
							break;
						}
						default:
							throw new ArgumentOutOfRangeException(nameof(messages), change.Key, LocalizedStrings.Str922);
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

							case 10:
								break;

							default:
								throw new ArgumentOutOfRangeException(nameof(unkType), unkType, LocalizedStrings.Str1291);
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
						if (storeSteps)
							l1Msg.Add(field, reader.ReadVolume(metaInfo));
						else
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
						var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);

						l1Msg.Add(Level1Fields.LastTradePrice, price);
						l1Msg.Add(Level1Fields.LastTradeVolume, reader.ReadVolume(metaInfo));
						l1Msg.Add(Level1Fields.LastTradeTime, metaInfo.FirstTime.ApplyTimeZone(metaInfo.ServerOffset));

						var origin = reader.ReadSide();

						if (origin != null)
							l1Msg.Add(Level1Fields.LastTradeOrigin, origin.Value);

						break;
					}
					case Level1Fields.BestBid:
					{
						var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);

						l1Msg.Add(Level1Fields.BestBidPrice, price);
						l1Msg.Add(Level1Fields.BestBidVolume, reader.ReadVolume(metaInfo));
						break;
					}
					case Level1Fields.BestAsk:
					{
						var price = DeserializePrice(reader, metaInfo, useLong, nonAdjustPrice);

						l1Msg.Add(Level1Fields.BestAskPrice, price);
						l1Msg.Add(Level1Fields.BestAskVolume, reader.ReadVolume(metaInfo));
						break;
					}
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
						var lastOffset = metaInfo.FirstServerOffset;
						l1Msg.Add(field, reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset));
						metaInfo.FirstFieldTime = prevTime;
						metaInfo.FirstServerOffset = lastOffset;
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
						var prevTime = metaInfo.FirstBuyBackDateTime;
						var lastOffset = metaInfo.FirstBuyBackDateOffset;
						l1Msg.Add(field, reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset));
						metaInfo.FirstBuyBackDateTime = prevTime;
						metaInfo.FirstBuyBackDateOffset = lastOffset;
						break;
					}
					case Level1Fields.BuyBackPrice:
					{
						l1Msg.Add(field, DeserializeChange(reader, metaInfo.BuyBackPrice));
						break;
					}
					default:
						throw new InvalidOperationException(LocalizedStrings.Str923Params.Put(field));
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
}