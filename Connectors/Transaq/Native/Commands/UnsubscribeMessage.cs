namespace StockSharp.Transaq.Native.Commands
{
	internal class UnsubscribeMessage : SubscribeMessage
	{
		public UnsubscribeMessage()
		{
			Id = ApiCommands.Unsubscribe;
		}
	}
}