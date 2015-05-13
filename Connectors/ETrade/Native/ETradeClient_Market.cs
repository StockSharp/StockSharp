namespace StockSharp.ETrade.Native
{
	using System;
	using System.Collections.Generic;

	
	partial class ETradeClient
	{
		private class ETradeMarketModule : ETradeModule
		{
			public ETradeMarketModule(ETradeClient client) : base("market", client) {}

			protected override ETradeRequest GetNextAutoRequest() { return null; }
		}
	}

	class ETradeProductLookupRequest : ETradeRequest<List<ProductInfo>>
	{
		readonly string _criteria;

		public ETradeProductLookupRequest(string criteria) { _criteria = criteria; }

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => api.ProductLookup(_criteria), out ex);

			return ETradeResponse.Create(this, result, ex);
		}
	}

}