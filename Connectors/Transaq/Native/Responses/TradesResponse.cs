namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	internal class TradesResponse : BaseResponse
	{
		public IEnumerable<TransaqMyTrade> Trades { get; internal set; }
	}

	internal class TransaqMyTrade : Tick
	{
		public long OrderNo { get; set; }
		public string Client { get; set; }
		public DateTime Time { get; set; }
		public string BrokerRef { get; set; }
		public decimal Value { get; set; }
		public decimal? Commission { get; set; }
		public decimal Yield { get; set; }
		public decimal? AccrueEdint { get; set; }
		public TradeTypes TradeType { get; set; }
		public string SettleCode { get; set; }
		public long CurrentPos { get; set; }
	}

	internal enum TradeTypes
	{
		T,
		N,
		R,
		P
	}
}