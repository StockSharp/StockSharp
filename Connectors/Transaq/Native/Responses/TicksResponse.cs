#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: TicksResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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