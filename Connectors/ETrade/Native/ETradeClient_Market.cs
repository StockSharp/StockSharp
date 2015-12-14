#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeClient_Market.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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