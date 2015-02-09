namespace StockSharp.Transaq.Native.Commands
{
	class NewMRepoOrderMessage : NewRepoOrderMessage
	{
		public NewMRepoOrderMessage()
		{
			Id = ApiCommands.NewMRepoOrder;
		}

		public decimal? Value { get; set; }
		public int? StartDiscount { get; set; }
		public int? LowerDiscount { get; set; }
		public int? UpperDiscount { get; set; }
		public bool? BlockSecurities { get; set; }
		public int? Term { get; set; }
	}
}