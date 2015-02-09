namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestSecuritiesMessage : BaseCommandMessage
	{
		public RequestSecuritiesMessage() : base(ApiCommands.GetSecurities)
		{
		}
	}
}