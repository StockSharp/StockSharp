namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class CandleKindsResponse : BaseResponse
	{
		public IEnumerable<CandleKind> Kinds { get; internal set; }
	}

	internal class CandleKind
	{
		public int Id { get; set; }
		public int Period { get; set; }
		public string Name { get; set; }
	}
}