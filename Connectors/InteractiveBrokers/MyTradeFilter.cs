namespace StockSharp.InteractiveBrokers
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Argument passed to interactive brokers when requesting execution history.
	/// </summary>
	class MyTradeFilter
	{
		/// <summary>
		/// Filter the results of the ReqExecutions() method based on the clientId.
		/// </summary>
		public int ClientId { get; set; }

		/// <summary>
		/// Filter the results of the ReqExecutions() method based on an account code.
		/// </summary>
		/// <remarks>This is only relevant for Financial Advisor (FA) accounts.</remarks>
		public string Portfolio { get; set; }

		/// <summary>
		/// Filter the results of the ReqExecutions() method based on execution reports received after the specified time. 
		/// </summary>
		/// <remarks>The format for timeFilter is "yyyymmdd-hh:mm:ss"</remarks>
		public DateTime Time { get; set; }

		/// <summary>
		/// Filter the results of the ReqExecutions() method based on the order symbol.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Filter the results of the ReqExecutions() method based on the order security type. 
		/// </summary>
		/// <remarks>Refer to the Contract struct for the list of valid security types.</remarks>
		public SecurityTypes? SecurityType { get; set; }

		/// <summary>
		/// Filter the results of the ReqExecutions() method based on the order exchange.
		/// </summary>
		public string BoardCode { get; set; }

		/// <summary>
		/// Filter the results of the ReqExecutions() method based on the order action. 
		/// </summary>
		/// <remarks>Refer to the Order struct for the list of valid order actions.</remarks>
		public Sides? Side { get; set; }
	}
}