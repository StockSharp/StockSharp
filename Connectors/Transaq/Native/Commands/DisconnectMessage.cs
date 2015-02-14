namespace StockSharp.Transaq.Native.Commands
{
	internal class DisconnectMessage : BaseCommandMessage
	{
		public DisconnectMessage() : base(ApiCommands.Disconnect)
		{
		}
	}
}