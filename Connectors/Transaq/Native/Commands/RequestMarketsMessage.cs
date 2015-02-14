namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestMarketsMessage : BaseCommandMessage
	{
		public RequestMarketsMessage() : base(ApiCommands.GetMarkets)
		{
		}
	}
}