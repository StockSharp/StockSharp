namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestMaxBuySellTPlusMessage : BaseCommandMessage
	{
		public RequestMaxBuySellTPlusMessage()
			: base(ApiCommands.GetMaxBuySellTPlus)
		{
		}

		public int Market { get; set; }
		public string SecCode { get; set; }
	}
}