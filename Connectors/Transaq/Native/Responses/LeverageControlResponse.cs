namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class LeverageControlResponse : BaseResponse
	{
		public string Client { get; set; }
		public decimal? LeveragePlan { get; set; }
		public decimal? LeverageFact { get; set; }

		public IEnumerable<LeverageControlSecurity> Items { get; internal set; }
	}

	internal class LeverageControlSecurity
	{
		public string Board { get; set; }
		public string SecCode { get; set; }
		public long MaxBuy { get; set; }
		public long MaxSell { get; set; }
	}
}