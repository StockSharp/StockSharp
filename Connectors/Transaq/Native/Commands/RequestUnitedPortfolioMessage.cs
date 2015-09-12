namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestUnitedPortfolioMessage : RequestFortsPositionsMessage
	{
		public RequestUnitedPortfolioMessage()
		{
			Id = ApiCommands.GetUnitedPortfolio;
		}

		public string Union { get; set; }
	}
}