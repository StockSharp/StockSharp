namespace StockSharp.Transaq.Native.Commands
{
	class NewRepoOrderMessage : NewRpsOrderMessage
	{
		public NewRepoOrderMessage()
		{
			Id = ApiCommands.NewRepoOrder;
		}

		public int? Rate { get; set; }
	}
}