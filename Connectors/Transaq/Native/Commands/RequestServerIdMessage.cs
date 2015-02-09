namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestServerIdMessage : BaseCommandMessage
	{
		public RequestServerIdMessage() : base(ApiCommands.GetServerId)
		{
		}
	}
}