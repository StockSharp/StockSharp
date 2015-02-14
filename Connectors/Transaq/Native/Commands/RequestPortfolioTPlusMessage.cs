namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestPortfolioTPlusMessage : BaseCommandMessage
	{
		public RequestPortfolioTPlusMessage() : base(ApiCommands.GetPortfolio)
		{
		}

		public string Client { get; set; }
	}
}
