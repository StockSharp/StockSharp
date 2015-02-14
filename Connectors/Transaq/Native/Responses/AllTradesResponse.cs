namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class AllTradesResponse : BaseResponse
	{
		public IEnumerable<Tick> AllTrades { get; internal set; }
	}
}