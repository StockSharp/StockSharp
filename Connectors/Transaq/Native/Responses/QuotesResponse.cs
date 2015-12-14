#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: QuotesResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class QuotesResponse : BaseResponse
	{
		public IEnumerable<TransaqQuote> Quotes { get; internal set; }
	}

	internal class TransaqQuote
	{
		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public string Source { get; set; }
		public decimal Price { get; set; }
		public int Yield { get; set; }
		public int? Buy { get; set; }
		public int? Sell { get; set; }
	}
}