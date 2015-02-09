namespace StockSharp.Transaq.Native
{
	internal class BaseCommandMessage
	{
		public string Id { get; protected set; }

		protected BaseCommandMessage(string commandId)
		{
			Id = commandId;
		}
	}

	internal static class ApiCommands
	{
		public const string Connect = "connect";
		public const string Disconnect = "disconnect";
		public const string GetHistoryData = "gethistorydata";
		public const string ServerStatus = "server_status";
		public const string GetSecurities = "get_securities";
		public const string Subscribe = "subscribe";
		public const string Unsubscribe = "unsubscribe";
		public const string NewOrder = "neworder";
		public const string NewCondOrder = "newcondorder";
		public const string NewStopOrder = "newstoporder";
		public const string NewRpsOrder = "newrpsorder";
		public const string NewRepoOrder = "newrepoorder";
		public const string NewMRepoOrder = "newmrepoorder";
		public const string CancelOrder = "cancelorder";
		public const string CancelStopOrder = "cancelstoporder";
		public const string CancelNegDeal = "cancelnegdeal";
		public const string CancelReport = "cancelreport4";
		public const string GetFortsPositions = "get_forts_positions";
		public const string GetClientLimits = "get_client_limits";
		public const string GetMarkets = "get_markets";
		public const string GetServTimeDifference = "get_servtime_difference";
		public const string GetLeverageControl = "get_leverage_control";
		public const string ChangePass = "change_pass";
		public const string SubscribeTicks = "subscribe_ticks";
		public const string GetConnectorVersion = "get_connector_version";
		public const string GetSecuritiesInfo = "get_securities_info";
		public const string MoveOrder = "moveorder";
		public const string GetServerId = "get_server_id";
		public const string GetOldNews = "get_old_news";
		public const string GetNewsBody = "get_news_body";
		public const string GetPortfolio = "get_portfolio";
		public const string GetMaxBuySellTPlus = "get_max_buy_sell_tplus";
	}
}