namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	internal class TicksResponse : BaseResponse
	{
		public IEnumerable<Tick> Ticks { get; internal set; }
	}

	internal class Tick
	{
		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public long TradeNo { get; set; }
		public DateTime TradeTime { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public TicksPeriods? Period { get; set; }
		public BuySells BuySell { get; set; }
		public int OpenInterest { get; set; }
	}

	internal enum TicksPeriods
	{
		O,
		N,
		C
	}

	internal enum BuySells
	{
		B,
		S
	}
}