namespace StockSharp.Algo.Storages.Binary.Snapshot;

using System.Runtime.InteropServices;

using Ecng.Interop;

/// <summary>
/// Implementation of <see cref="ISnapshotSerializer{TKey,TMessage}"/> in binary format for <see cref="Level1ChangeMessage"/>.
/// </summary>
public class Level1BinarySnapshotSerializer : ISnapshotSerializer<SecurityId, Level1ChangeMessage>
{
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	private struct Level1Snapshot
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string SecurityId;

		public long LastChangeServerTime;
		public long LastChangeLocalTime;

		public BlittableDecimal? OpenPrice;
		public BlittableDecimal? HighPrice;
		public BlittableDecimal? LowPrice;
		public BlittableDecimal? ClosePrice;
		public BlittableDecimal? StepPrice;
		public BlittableDecimal? IV;
		public BlittableDecimal? TheorPrice;
		public BlittableDecimal? OI;
		public BlittableDecimal? MinPrice;
		public BlittableDecimal? MaxPrice;
		public BlittableDecimal? BidsVolume;
		public int? BidsCount;
		public BlittableDecimal? AsksVolume;
		public int? AsksCount;
		public BlittableDecimal? HV;
		public BlittableDecimal? Delta;
		public BlittableDecimal? Gamma;
		public BlittableDecimal? Vega;
		public BlittableDecimal? Theta;
		public BlittableDecimal? MarginBuy;
		public BlittableDecimal? MarginSell;
		public BlittableDecimal? PriceStep;
		public BlittableDecimal? VolumeStep;
		public byte? State;
		public BlittableDecimal? LastTradePrice;
		public BlittableDecimal? LastTradeVolume;
		public BlittableDecimal? Volume;
		public BlittableDecimal? AveragePrice;
		public BlittableDecimal? SettlementPrice;
		public BlittableDecimal? Change;
		public BlittableDecimal? BestBidPrice;
		public BlittableDecimal? BestBidVolume;
		public BlittableDecimal? BestAskPrice;
		public BlittableDecimal? BestAskVolume;
		public BlittableDecimal? Rho;
		public BlittableDecimal? AccruedCouponIncome;
		public BlittableDecimal? HighBidPrice;
		public BlittableDecimal? LowAskPrice;
		public BlittableDecimal? Yield;
		public long? LastTradeTime;
		public int? TradesCount;
		public BlittableDecimal? VWAP;
		public long? LastTradeId;
		public long? BestBidTime;
		public long? BestAskTime;
		public byte? LastTradeUpDown;
		public byte? LastTradeOrigin;
		public BlittableDecimal? Multiplier;
		public BlittableDecimal? PriceEarnings;
		public BlittableDecimal? ForwardPriceEarnings;
		public BlittableDecimal? PriceEarningsGrowth;
		public BlittableDecimal? PriceSales;
		public BlittableDecimal? PriceBook;
		public BlittableDecimal? PriceCash;
		public BlittableDecimal? PriceFreeCash;
		public BlittableDecimal? Payout;
		public BlittableDecimal? SharesOutstanding;
		public BlittableDecimal? SharesFloat;
		public BlittableDecimal? FloatShort;
		public BlittableDecimal? ShortRatio;
		public BlittableDecimal? ReturnOnAssets;
		public BlittableDecimal? ReturnOnEquity;
		public BlittableDecimal? ReturnOnInvestment;
		public BlittableDecimal? CurrentRatio;
		public BlittableDecimal? QuickRatio;
		public BlittableDecimal? LongTermDebtEquity;
		public BlittableDecimal? TotalDebtEquity;
		public BlittableDecimal? GrossMargin;
		public BlittableDecimal? OperatingMargin;
		public BlittableDecimal? ProfitMargin;
		public BlittableDecimal? Beta;
		public BlittableDecimal? AverageTrueRange;
		public BlittableDecimal? HistoricalVolatilityWeek;
		public BlittableDecimal? HistoricalVolatilityMonth;
		public byte? IsSystem;
		public int? Decimals;
		public BlittableDecimal? Duration;
		public BlittableDecimal? IssueSize;
		public long? BuyBackDate;
		public BlittableDecimal? BuyBackPrice;
		public BlittableDecimal? Turnover;
		public BlittableDecimal? SpreadMiddle;
		public BlittableDecimal? Dividend;
		public BlittableDecimal? AfterSplit;
		public BlittableDecimal? BeforeSplit;
		public BlittableDecimal? CommissionTaker;
		public BlittableDecimal? CommissionMaker;
		public BlittableDecimal? MinVolume;
		public BlittableDecimal? UnderlyingMinVolume;
		public BlittableDecimal? CouponValue;
		public long? CouponDate;
		public BlittableDecimal? CouponPeriod;
		public BlittableDecimal? MarketPriceYesterday;
		public BlittableDecimal? MarketPriceToday;
		public BlittableDecimal? VWAPPrev;
		public BlittableDecimal? YieldVWAP;
		public BlittableDecimal? YieldVWAPPrev;
		public BlittableDecimal? Index;
		public BlittableDecimal? Imbalance;
		public BlittableDecimal? UnderlyingPrice;
		public BlittableDecimal? MaxVolume;
		public BlittableDecimal? LowBidPrice;
		public BlittableDecimal? HighAskPrice;
		public BlittableDecimal? LastTradeVolumeLow;
		public BlittableDecimal? LastTradeVolumeHigh;
		public BlittableDecimal? OptionMargin;
		public BlittableDecimal? OptionSyntheticMargin;

		public long SeqNum;
		public SnapshotDataType? BuildFrom;

		public BlittableDecimal? LowBidVolume;
		public BlittableDecimal? HighAskVolume;
		public BlittableDecimal? UnderlyingBestBidPrice;
		public BlittableDecimal? UnderlyingBestAskPrice;
		public BlittableDecimal? MedianPrice;
		public BlittableDecimal? HighPrice52Week;
		public BlittableDecimal? LowPrice52Week;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Sizes.S100)]
		public string LastTradeStringId;
	}

	Version ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Version { get; } = SnapshotVersions.V23;

	string ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Name => "Level1";

	byte[] ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Serialize(Version version, Level1ChangeMessage message)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var snapshot = new Level1Snapshot
		{
			SecurityId = message.SecurityId.ToStringId().VerifySize(Sizes.S100),
			LastChangeServerTime = message.ServerTime.To<long>(),
			LastChangeLocalTime = message.LocalTime.To<long>(),
			BuildFrom = message.BuildFrom == null ? default(SnapshotDataType?) : (SnapshotDataType)message.BuildFrom,
			SeqNum = message.SeqNum,
		};

		foreach (var change in message.Changes)
		{
			switch (change.Key)
			{
				case Level1Fields.OpenPrice:
					snapshot.OpenPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HighPrice:
					snapshot.HighPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LowPrice:
					snapshot.LowPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ClosePrice:
					snapshot.ClosePrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.StepPrice:
					snapshot.StepPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ImpliedVolatility:
					snapshot.IV = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.TheorPrice:
					snapshot.TheorPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.OpenInterest:
					snapshot.OI = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MinPrice:
					snapshot.MinPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MaxPrice:
					snapshot.MaxPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BidsVolume:
					snapshot.BidsVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BidsCount:
					snapshot.BidsCount = (int)change.Value;
					break;
				case Level1Fields.AsksVolume:
					snapshot.AsksVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.AsksCount:
					snapshot.AsksCount = (int)change.Value;
					break;
				case Level1Fields.HistoricalVolatility:
					snapshot.HV = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Delta:
					snapshot.Delta = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Gamma:
					snapshot.Gamma = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Vega:
					snapshot.Vega = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Theta:
					snapshot.Theta = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MarginBuy:
					snapshot.MarginBuy = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MarginSell:
					snapshot.MarginSell = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.State:
					snapshot.State = (byte)(SecurityStates)change.Value;
					break;
				case Level1Fields.LastTradePrice:
					snapshot.LastTradePrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LastTradeVolume:
					snapshot.LastTradeVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Volume:
					snapshot.Volume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.AveragePrice:
					snapshot.AveragePrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.SettlementPrice:
					snapshot.SettlementPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Change:
					snapshot.Change = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BestBidPrice:
					snapshot.BestBidPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BestBidVolume:
					snapshot.BestBidVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BestAskPrice:
					snapshot.BestAskPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BestAskVolume:
					snapshot.BestAskVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Rho:
					snapshot.Rho = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.AccruedCouponIncome:
					snapshot.AccruedCouponIncome = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HighBidPrice:
					snapshot.HighBidPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LowAskPrice:
					snapshot.LowAskPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Yield:
					snapshot.Yield = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LastTradeTime:
					snapshot.LastTradeTime = change.Value.To<long>();
					break;
				case Level1Fields.TradesCount:
					snapshot.TradesCount = (int)change.Value;
					break;
				case Level1Fields.VWAP:
					snapshot.VWAP = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LastTradeId:
					snapshot.LastTradeId = (long)change.Value;
					break;
				case Level1Fields.LastTradeUpDown:
					snapshot.LastTradeUpDown = (bool)change.Value ? (byte?)1 : 0;
					break;
				case Level1Fields.LastTradeOrigin:
					snapshot.LastTradeOrigin = (byte)(Sides)change.Value;
					break;
				case Level1Fields.Beta:
					snapshot.Beta = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.AverageTrueRange:
					snapshot.AverageTrueRange = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Duration:
					snapshot.Duration = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Turnover:
					snapshot.Turnover = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.SpreadMiddle:
					snapshot.SpreadMiddle = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceEarnings:
					snapshot.PriceEarnings = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ForwardPriceEarnings:
					snapshot.ForwardPriceEarnings = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceEarningsGrowth:
					snapshot.PriceEarningsGrowth = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceSales:
					snapshot.PriceSales = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceBook:
					snapshot.PriceBook = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceCash:
					snapshot.PriceCash = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceFreeCash:
					snapshot.PriceFreeCash = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Payout:
					snapshot.Payout = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.SharesOutstanding:
					snapshot.SharesOutstanding = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.SharesFloat:
					snapshot.SharesFloat = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.FloatShort:
					snapshot.FloatShort = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ShortRatio:
					snapshot.ShortRatio = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ReturnOnAssets:
					snapshot.ReturnOnAssets = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ReturnOnEquity:
					snapshot.ReturnOnEquity = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ReturnOnInvestment:
					snapshot.ReturnOnInvestment = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.CurrentRatio:
					snapshot.CurrentRatio = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.QuickRatio:
					snapshot.QuickRatio = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HistoricalVolatilityWeek:
					snapshot.HistoricalVolatilityWeek = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HistoricalVolatilityMonth:
					snapshot.HistoricalVolatilityMonth = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.IssueSize:
					snapshot.IssueSize = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BuyBackPrice:
					snapshot.BuyBackPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BuyBackDate:
					snapshot.BuyBackDate = change.Value.To<long>();
					break;
				case Level1Fields.Dividend:
					snapshot.Dividend = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.AfterSplit:
					snapshot.AfterSplit = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BeforeSplit:
					snapshot.BeforeSplit = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.CommissionTaker:
					snapshot.CommissionTaker = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.CommissionMaker:
					snapshot.CommissionMaker = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MinVolume:
					snapshot.MinVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.UnderlyingMinVolume:
					snapshot.UnderlyingMinVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.CouponValue:
					snapshot.CouponValue = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.CouponDate:
					snapshot.CouponDate = ((DateTimeOffset)change.Value).To<long>();
					break;
				case Level1Fields.CouponPeriod:
					snapshot.CouponPeriod = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MarketPriceYesterday:
					snapshot.MarketPriceYesterday = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MarketPriceToday:
					snapshot.MarketPriceToday = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.VWAPPrev:
					snapshot.VWAPPrev = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.YieldVWAP:
					snapshot.YieldVWAP = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.YieldVWAPPrev:
					snapshot.YieldVWAPPrev = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Index:
					snapshot.Index = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.Imbalance:
					snapshot.Imbalance = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.UnderlyingPrice:
					snapshot.UnderlyingPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MaxVolume:
					snapshot.MaxVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LowBidPrice:
					snapshot.LowBidPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HighAskPrice:
					snapshot.HighAskPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LastTradeVolumeLow:
					snapshot.LastTradeVolumeLow = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LastTradeVolumeHigh:
					snapshot.LastTradeVolumeHigh = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.OptionMargin:
					snapshot.OptionMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.OptionSyntheticMargin:
					snapshot.OptionSyntheticMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.PriceStep:
					snapshot.PriceStep = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.VolumeStep:
					snapshot.VolumeStep = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.BestBidTime:
					snapshot.BestBidTime = ((DateTimeOffset)change.Value).To<long>();
					break;
				case Level1Fields.BestAskTime:
					snapshot.BestAskTime = ((DateTimeOffset)change.Value).To<long>();
					break;
				case Level1Fields.Multiplier:
					snapshot.Multiplier = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LongTermDebtEquity:
					snapshot.LongTermDebtEquity = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.TotalDebtEquity:
					snapshot.TotalDebtEquity = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.GrossMargin:
					snapshot.GrossMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.OperatingMargin:
					snapshot.OperatingMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.ProfitMargin:
					snapshot.ProfitMargin = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.IsSystem:
					snapshot.IsSystem = ((bool)change.Value).ToByte();
					break;
				case Level1Fields.Decimals:
					snapshot.Decimals = (int)change.Value;
					break;
				case Level1Fields.LowBidVolume:
					snapshot.LowBidVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HighAskVolume:
					snapshot.HighAskVolume = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.UnderlyingBestBidPrice:
					snapshot.UnderlyingBestBidPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.UnderlyingBestAskPrice:
					snapshot.UnderlyingBestAskPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.MedianPrice:
					snapshot.MedianPrice = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.HighPrice52Week:
					snapshot.HighPrice52Week = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LowPrice52Week:
					snapshot.LowPrice52Week = (BlittableDecimal)(decimal)change.Value;
					break;
				case Level1Fields.LastTradeStringId:
					snapshot.LastTradeStringId = (string)change.Value;
					break;
			}
		}

		var buffer = new byte[typeof(Level1Snapshot).SizeOf()];

		var ptr = snapshot.StructToPtr();
		ptr.CopyTo(buffer);
		ptr.FreeHGlobal();

		return buffer;
	}

	Level1ChangeMessage ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Deserialize(Version version, byte[] buffer)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		using (var handle = new GCHandle<byte[]>(buffer))
		{
			var snapshot = handle.CreatePointer().ToStruct<Level1Snapshot>(true);

			var level1Msg = new Level1ChangeMessage
			{
				SecurityId = snapshot.SecurityId.ToSecurityId(),
				ServerTime = snapshot.LastChangeServerTime.To<DateTimeOffset>(),
				LocalTime = snapshot.LastChangeLocalTime.To<DateTimeOffset>(),
				BuildFrom = snapshot.BuildFrom,
				SeqNum = snapshot.SeqNum,
			};

			level1Msg
				.TryAdd(Level1Fields.LastTradePrice, snapshot.LastTradePrice)
				.TryAdd(Level1Fields.LastTradeVolume, snapshot.LastTradeVolume)
				.TryAdd(Level1Fields.LastTradeId, snapshot.LastTradeId)

				.TryAdd(Level1Fields.BestBidPrice, snapshot.BestBidPrice)
				.TryAdd(Level1Fields.BestAskPrice, snapshot.BestAskPrice)

				.TryAdd(Level1Fields.BestBidVolume, snapshot.BestBidVolume)
				.TryAdd(Level1Fields.BestAskVolume, snapshot.BestAskVolume)

				.TryAdd(Level1Fields.BidsVolume, snapshot.BidsVolume)
				.TryAdd(Level1Fields.AsksVolume, snapshot.AsksVolume)

				.TryAdd(Level1Fields.BidsCount, snapshot.BidsCount)
				.TryAdd(Level1Fields.AsksCount, snapshot.AsksCount)

				.TryAdd(Level1Fields.HighBidPrice, snapshot.HighBidPrice)
				.TryAdd(Level1Fields.LowAskPrice, snapshot.LowAskPrice)

				.TryAdd(Level1Fields.OpenPrice, snapshot.OpenPrice)
				.TryAdd(Level1Fields.HighPrice, snapshot.HighPrice)
				.TryAdd(Level1Fields.LowPrice, snapshot.LowPrice)
				.TryAdd(Level1Fields.ClosePrice, snapshot.ClosePrice)
				.TryAdd(Level1Fields.Volume, snapshot.Volume)

				.TryAdd(Level1Fields.StepPrice, snapshot.StepPrice)
				.TryAdd(Level1Fields.OpenInterest, snapshot.OI)

				.TryAdd(Level1Fields.MinPrice, snapshot.MinPrice)
				.TryAdd(Level1Fields.MaxPrice, snapshot.MaxPrice)

				.TryAdd(Level1Fields.MarginBuy, snapshot.MarginBuy)
				.TryAdd(Level1Fields.MarginSell, snapshot.MarginSell)

				.TryAdd(Level1Fields.ImpliedVolatility, snapshot.IV)
				.TryAdd(Level1Fields.HistoricalVolatility, snapshot.HV)
				.TryAdd(Level1Fields.TheorPrice, snapshot.TheorPrice)
				.TryAdd(Level1Fields.Delta, snapshot.Delta)
				.TryAdd(Level1Fields.Gamma, snapshot.Gamma)
				.TryAdd(Level1Fields.Vega, snapshot.Vega)
				.TryAdd(Level1Fields.Theta, snapshot.Theta)
				.TryAdd(Level1Fields.Rho, snapshot.Rho)

				.TryAdd(Level1Fields.AveragePrice, snapshot.AveragePrice)
				.TryAdd(Level1Fields.SettlementPrice, snapshot.SettlementPrice)
				.TryAdd(Level1Fields.Change, snapshot.Change)
				.TryAdd(Level1Fields.AccruedCouponIncome, snapshot.AccruedCouponIncome)
				.TryAdd(Level1Fields.Yield, snapshot.Yield)
				.TryAdd(Level1Fields.VWAP, snapshot.VWAP)
							
				.TryAdd(Level1Fields.TradesCount, snapshot.TradesCount)
							
				.TryAdd(Level1Fields.Beta, snapshot.Beta)
				.TryAdd(Level1Fields.AverageTrueRange, snapshot.AverageTrueRange)
				.TryAdd(Level1Fields.Duration, snapshot.Duration)
				.TryAdd(Level1Fields.Turnover, snapshot.Turnover)
				.TryAdd(Level1Fields.SpreadMiddle, snapshot.SpreadMiddle)

				.TryAdd(Level1Fields.PriceEarnings, snapshot.PriceEarnings)
				.TryAdd(Level1Fields.ForwardPriceEarnings, snapshot.ForwardPriceEarnings)
				.TryAdd(Level1Fields.PriceEarningsGrowth, snapshot.PriceEarningsGrowth)
				.TryAdd(Level1Fields.PriceSales, snapshot.PriceSales)
				.TryAdd(Level1Fields.PriceBook, snapshot.PriceBook)
				.TryAdd(Level1Fields.PriceCash, snapshot.PriceCash)
				.TryAdd(Level1Fields.PriceFreeCash, snapshot.PriceFreeCash)
				.TryAdd(Level1Fields.Payout, snapshot.Payout)
				.TryAdd(Level1Fields.SharesOutstanding, snapshot.SharesOutstanding)
				.TryAdd(Level1Fields.SharesFloat, snapshot.SharesFloat)
				.TryAdd(Level1Fields.FloatShort, snapshot.FloatShort)
				.TryAdd(Level1Fields.ShortRatio, snapshot.ShortRatio)
				.TryAdd(Level1Fields.ReturnOnAssets, snapshot.ReturnOnAssets)
				.TryAdd(Level1Fields.ReturnOnEquity, snapshot.ReturnOnEquity)
				.TryAdd(Level1Fields.ReturnOnInvestment, snapshot.ReturnOnInvestment)
				.TryAdd(Level1Fields.CurrentRatio, snapshot.CurrentRatio)
				.TryAdd(Level1Fields.QuickRatio, snapshot.QuickRatio)
				.TryAdd(Level1Fields.HistoricalVolatilityWeek, snapshot.HistoricalVolatilityWeek)
				.TryAdd(Level1Fields.HistoricalVolatilityMonth, snapshot.HistoricalVolatilityMonth)
				.TryAdd(Level1Fields.IssueSize, snapshot.IssueSize)
				.TryAdd(Level1Fields.BuyBackPrice, snapshot.BuyBackPrice)
				.TryAdd(Level1Fields.Dividend, snapshot.Dividend)
				.TryAdd(Level1Fields.AfterSplit, snapshot.AfterSplit)
				.TryAdd(Level1Fields.BeforeSplit, snapshot.BeforeSplit)
				.TryAdd(Level1Fields.CommissionTaker, snapshot.CommissionTaker)
				.TryAdd(Level1Fields.CommissionMaker, snapshot.CommissionMaker)
				.TryAdd(Level1Fields.MinVolume, snapshot.MinVolume)
				.TryAdd(Level1Fields.UnderlyingMinVolume, snapshot.UnderlyingMinVolume)
				.TryAdd(Level1Fields.CouponValue, snapshot.CouponValue)
				.TryAdd(Level1Fields.CouponDate, snapshot.CouponDate?.To<DateTimeOffset>())
				.TryAdd(Level1Fields.CouponPeriod, snapshot.CouponPeriod)
				.TryAdd(Level1Fields.MarketPriceYesterday, snapshot.MarketPriceYesterday)
				.TryAdd(Level1Fields.MarketPriceToday, snapshot.MarketPriceToday)
				.TryAdd(Level1Fields.VWAPPrev, snapshot.VWAPPrev)
				.TryAdd(Level1Fields.YieldVWAP, snapshot.YieldVWAP)
				.TryAdd(Level1Fields.YieldVWAPPrev, snapshot.YieldVWAPPrev)
				.TryAdd(Level1Fields.Index, snapshot.Index)
				.TryAdd(Level1Fields.Imbalance, snapshot.Imbalance)
				.TryAdd(Level1Fields.UnderlyingPrice, snapshot.UnderlyingPrice)
				.TryAdd(Level1Fields.MaxVolume, snapshot.MaxVolume)
				.TryAdd(Level1Fields.LowBidPrice, snapshot.LowBidPrice)
				.TryAdd(Level1Fields.HighAskPrice, snapshot.HighAskPrice)
				.TryAdd(Level1Fields.LastTradeVolumeLow, snapshot.LastTradeVolumeLow)
				.TryAdd(Level1Fields.LastTradeVolumeHigh, snapshot.LastTradeVolumeHigh)
				.TryAdd(Level1Fields.OptionMargin, snapshot.OptionMargin)
				.TryAdd(Level1Fields.OptionSyntheticMargin, snapshot.OptionSyntheticMargin)
				.TryAdd(Level1Fields.PriceStep, snapshot.PriceStep)
				.TryAdd(Level1Fields.VolumeStep, snapshot.VolumeStep)
				.TryAdd(Level1Fields.BestBidTime, snapshot.BestBidTime?.To<DateTimeOffset>())
				.TryAdd(Level1Fields.BestAskTime, snapshot.BestAskTime?.To<DateTimeOffset>())
				.TryAdd(Level1Fields.Multiplier, snapshot.Multiplier)
				.TryAdd(Level1Fields.LongTermDebtEquity, snapshot.LongTermDebtEquity)
				.TryAdd(Level1Fields.TotalDebtEquity, snapshot.TotalDebtEquity)
				.TryAdd(Level1Fields.GrossMargin, snapshot.GrossMargin)
				.TryAdd(Level1Fields.OperatingMargin, snapshot.OperatingMargin)
				.TryAdd(Level1Fields.ProfitMargin, snapshot.ProfitMargin)
				.TryAdd(Level1Fields.IsSystem, snapshot.IsSystem?.ToBool())
				.TryAdd(Level1Fields.Decimals, snapshot.Decimals, true)
				.TryAdd(Level1Fields.LowBidVolume, snapshot.LowBidVolume)
				.TryAdd(Level1Fields.HighAskVolume, snapshot.HighAskVolume)
				.TryAdd(Level1Fields.UnderlyingBestBidPrice, snapshot.UnderlyingBestBidPrice)
				.TryAdd(Level1Fields.UnderlyingBestAskPrice, snapshot.UnderlyingBestAskPrice)
				.TryAdd(Level1Fields.MedianPrice, snapshot.MedianPrice)
				.TryAdd(Level1Fields.HighPrice52Week, snapshot.HighPrice52Week)
				.TryAdd(Level1Fields.LowPrice52Week, snapshot.LowPrice52Week)
				.TryAdd(Level1Fields.LastTradeStringId, snapshot.LastTradeStringId)
				;

			if (snapshot.LastTradeTime != null)
				level1Msg.Add(Level1Fields.LastTradeTime, snapshot.LastTradeTime.Value.To<DateTimeOffset>());

			if (snapshot.LastTradeUpDown != null)
				level1Msg.Add(Level1Fields.LastTradeUpDown, snapshot.LastTradeUpDown.Value == 1);

			if (snapshot.LastTradeOrigin != null)
				level1Msg.Add(Level1Fields.LastTradeOrigin, (Sides)snapshot.LastTradeOrigin.Value);

			if (snapshot.State != null)
				level1Msg.Add(Level1Fields.State, (SecurityStates)snapshot.State.Value);

			if (snapshot.BuyBackDate != null)
				level1Msg.Add(Level1Fields.BuyBackDate, snapshot.BuyBackDate.Value.To<DateTimeOffset>());

			return level1Msg;
		}
	}

	SecurityId ISnapshotSerializer<SecurityId, Level1ChangeMessage>.GetKey(Level1ChangeMessage message)
	{
		return message.SecurityId;
	}

	void ISnapshotSerializer<SecurityId, Level1ChangeMessage>.Update(Level1ChangeMessage message, Level1ChangeMessage changes)
	{
		var lastTradeFound = false;
		var bestBidFound = false;
		var bestAskFound = false;

		foreach (var pair in changes.Changes)
		{
			var field = pair.Key;

			if (!lastTradeFound)
			{
				if (field.IsLastTradeField())
				{
					message.Changes.Remove(Level1Fields.LastTradeUpDown);
					message.Changes.Remove(Level1Fields.LastTradeTime);
					message.Changes.Remove(Level1Fields.LastTradeId);
					message.Changes.Remove(Level1Fields.LastTradeOrigin);
					message.Changes.Remove(Level1Fields.LastTradePrice);
					message.Changes.Remove(Level1Fields.LastTradeVolume);

					lastTradeFound = true;
				}
			}

			if (!bestBidFound)
			{
				if (field.IsBestBidField())
				{
					message.Changes.Remove(Level1Fields.BestBidPrice);
					message.Changes.Remove(Level1Fields.BestBidTime);
					message.Changes.Remove(Level1Fields.BestBidVolume);

					bestBidFound = true;
				}
			}

			if (!bestAskFound)
			{
				if (field.IsBestAskField())
				{
					message.Changes.Remove(Level1Fields.BestAskPrice);
					message.Changes.Remove(Level1Fields.BestAskTime);
					message.Changes.Remove(Level1Fields.BestAskVolume);

					bestAskFound = true;
				}
			}

			message.Changes[pair.Key] = pair.Value;
		}

		message.LocalTime = changes.LocalTime;
		message.ServerTime = changes.ServerTime;

		if (changes.BuildFrom != default)
			message.BuildFrom = changes.BuildFrom;

		if (changes.SeqNum != default)
			message.SeqNum = changes.SeqNum;
	}

	DataType ISnapshotSerializer<SecurityId, Level1ChangeMessage>.DataType => DataType.Level1;
}