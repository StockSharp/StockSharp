#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: QuotationsResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	internal class QuotationsResponse : BaseResponse
	{
		public IEnumerable<Quotation> Quotations { get; internal set; }
	}

	internal class Quotation
	{
		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public decimal? AccruedIntValue { get; set; }
		public decimal? Open { get; set; }
		public decimal? WAPrice { get; set; }
		public int? BestBidVolume { get; set; }
		public int? BidsVolume { get; set; }
		public int? BidsCount { get; set; }
		public int? BestAskVolume { get; set; }
		public int? AsksVolume { get; set; }
		public decimal? BestBidPrice { get; set; }
		public decimal? BestAskPrice { get; set; }
		public int? AsksCount { get; set; }
		public int? TradesCount { get; set; }
		public int? VolToday { get; set; }
		public int? OpenInterest { get; set; }
		public int? DeltaPositions { get; set; }
		public decimal? LastTradePrice { get; set; }
		public int? LastTradeVolume { get; set; }
		public DateTime? LastTradeTime { get; set; }
		public decimal? Change { get; set; }
		public decimal? PriceMinusPrevWAPrice { get; set; }
		public decimal? ValToday { get; set; }
		public decimal? Yield { get; set; }
		public decimal? YieldAtWAPrice { get; set; }
		public decimal? MarketPriceToday { get; set; }
		public decimal? HighBid { get; set; }
		public decimal? LowAsk { get; set; }
		public decimal? High { get; set; }
		public decimal? Low { get; set; }
		public decimal? ClosePrice { get; set; }
		public decimal? CloseYield { get; set; }
		public TransaqSecurityStatus? Status { get; set; }
		public string SessionStatus { get; set; }
		public decimal? BuyDeposit { get; set; }
		public decimal? SellDeposit { get; set; }
		public decimal? Volatility { get; set; }
		public decimal? TheoreticalPrice { get; set; }
		public decimal? BgoBuy { get; set; }
		public decimal? PointCost { get; set; }
	}

	internal enum TransaqSecurityStatus
	{
		A,
		S
	}
}