namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestConnectorVersionMessage : BaseCommandMessage
	{
		public RequestConnectorVersionMessage() : base(ApiCommands.GetConnectorVersion)
		{
		}
	}
}