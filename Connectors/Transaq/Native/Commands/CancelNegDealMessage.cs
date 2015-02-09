namespace StockSharp.Transaq.Native.Commands
{
	class CancelNegDealMessage : CancelOrderMessage
	{
		public CancelNegDealMessage()
		{
			Id = ApiCommands.CancelNegDeal;
		}
	}
}