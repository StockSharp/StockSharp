namespace StockSharp.Transaq.Native.Commands
{
	internal class CancelStopOrderMessage : CancelOrderMessage
	{
		public CancelStopOrderMessage()
		{
			Id = ApiCommands.CancelStopOrder;
		}
	}
}