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