namespace StockSharp.ETrade.Native
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Ecng.Common;

	partial class ETradeClient
	{

		private class ETradeAccountsModule : ETradeModule
		{
			public ETradeAccountsModule(ETradeClient client) : base("accounts", client) {}

			protected override ETradeRequest GetNextAutoRequest()
			{
				if (!Client.IsConnected || !Client.IsExportStarted)
					return null;

				if(CurrentAutoRequest == null)
					return CreateAccountsRequest();
				
				var portfolios = Client._portfolioNames.ToArray();

				if(CurrentAutoRequest is ETradeGetAccountsRequest)
					return portfolios.Length > 0 ? CreatePositionsRequest(portfolios[0]) : CreateAccountsRequest();

				var req = (ETradeGetPositionsRequest)CurrentAutoRequest;
				var index = portfolios.IndexOf(req.PortfolioName);

				if(index == portfolios.Length - 1 || index == -1)
					return CreateAccountsRequest();

				return CreatePositionsRequest(portfolios[index+1]);
			}

			ETradeRequest CreateAccountsRequest()
			{
				return new ETradeGetAccountsRequest { ResponseHandler = GetAccountsResponseHanlder };
			}

			ETradeRequest CreatePositionsRequest(string portfName)
			{
				return new ETradeGetPositionsRequest(portfName) { ResponseHandler = GetPositionsResponseHandler };
			}

			private void GetAccountsResponseHanlder(ETradeResponse<List<AccountInfo>> response)
			{
				Client.AccountsData.SafeInvoke(response.Data, response.Exception);

				var isNew = false;
				foreach (var name in response.Data.Select(info => info.accountId.ToString(CultureInfo.InvariantCulture)).Where(name => !Client._portfolioNames.Contains(name))) {
					Client._portfolioNames.Add(name);
					isNew = true;
				}

				if (isNew)
				{
					Client._marketModule.Wakeup();
					Client._orderModule.Wakeup();
				}
			}

			private void GetPositionsResponseHandler(ETradeResponse<List<PositionInfo>> response)
			{
				var portfName = ((ETradeGetPositionsRequest)response.Request).PortfolioName;
				Client.PositionsData.SafeInvoke(portfName, response.Data, response.Exception);
			}
		}
	}

	class ETradeGetAccountsRequest : ETradeRequest<List<AccountInfo>>
	{
		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(api.GetAccounts, out ex);

			return ETradeResponse.Create(this, result, ex);
		}
	}

	class ETradeGetPositionsRequest : ETradeRequest<List<PositionInfo>>
	{
		public string PortfolioName {get; private set;}
		string _marker;

		public ETradeGetPositionsRequest(string portfName) { PortfolioName = portfName; }

		protected override ETradeResponse ExecuteNextPartInternal(ETradeApi api)
		{
			Exception ex;

			var result = PerformApiRequest(() => api.GetPositions(PortfolioName, ref _marker), out ex);

			if(result == null) result = new List<PositionInfo>();

			return ETradeResponse.Create(this, result, ex);
		}

		protected override bool GetIsDone() {
			return _marker.IsEmpty();
		}
	}
}