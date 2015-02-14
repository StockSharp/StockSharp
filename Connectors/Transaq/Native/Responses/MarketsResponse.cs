namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class MarketsResponse : BaseResponse
	{
		public IEnumerable<Market> Markets { get; internal set; }
	}

	internal class Market
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}