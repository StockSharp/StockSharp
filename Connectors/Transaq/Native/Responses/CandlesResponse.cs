#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: CandlesResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System;

	internal class CandlesResponse : BaseResponse
	{
		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public int Period { get; set; }
		public CandleResponseStatus Status { get; set; }

		public TransaqCandle[] Candles { get; internal set; }
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