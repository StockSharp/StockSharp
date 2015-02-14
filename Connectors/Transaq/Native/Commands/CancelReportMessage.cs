namespace StockSharp.Transaq.Native.Commands
{
	class CancelReportMessage : CancelOrderMessage
	{
		public CancelReportMessage()
		{
			Id = ApiCommands.CancelReport;
		}
	}
}