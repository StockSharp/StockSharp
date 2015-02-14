namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	class PitsResponse : BaseResponse
	{
		public IEnumerable<Pit> Pits { get; internal set; }
	}

	internal class Pit
	{
		public string SecCode { get; set; }
		public string Board { get; set; }
		public string Market { get; set; }
		public string Decimals { get; set; }
		public decimal MinStep { get; set; }
		public int LotSize { get; set; }
		public decimal PointCost { get; set; }
	}
}
