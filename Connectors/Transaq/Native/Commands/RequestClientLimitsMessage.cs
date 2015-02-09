namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestClientLimitsMessage : RequestFortsPositionsMessage
	{
		public RequestClientLimitsMessage()
		{
			Id = ApiCommands.GetClientLimits;
		}
	}
}