namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestOldNewsMessage : BaseCommandMessage
	{
		public RequestOldNewsMessage() : base(ApiCommands.GetOldNews)
		{
		}


		public int Count { get; set; }
	}
}