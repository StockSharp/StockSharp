namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestHistoryDataMessage : BaseCommandMessage
	{
		public RequestHistoryDataMessage() : base(ApiCommands.GetHistoryData)
		{
		}

		public string Board { get; set; }
		public string SecCode { get; set; }
		public int SecId { get; set; }
		public int Period { get; set; }
		public long Count { get; set; }
		public bool Reset { get; set; }
	}
}