namespace StockSharp.Transaq.Native.Commands
{
	internal class ServerStatusMessage : BaseCommandMessage
	{
		public ServerStatusMessage() : base(ApiCommands.ServerStatus)
		{
		}
	}
}