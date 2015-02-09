namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	internal class CandlesResponse : BaseResponse
	{
		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public int Period { get; set; }
		public CandleResponseStatus Status { get; set; }

		public IEnumerable<TransaqCandle> Candles { get; internal set; }
	}

	enum CandleResponseStatus
	{
		Finished,
		Done,
		Continue,
		NotAvailable
	}

	internal class TransaqCandle
	{
		public DateTime Date { get; set; }
		public decimal Open { get; set; }
		public decimal High { get; set; }
		public decimal Low { get; set; }
		public decimal Close { get; set; }
		public int Volume { get; set; }
		public int Oi { get; set; }
	}
}