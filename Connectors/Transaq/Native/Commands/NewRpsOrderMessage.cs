namespace StockSharp.Transaq.Native.Commands
{
	class NewRpsOrderMessage : NewBaseOrderMessage
	{
		public NewRpsOrderMessage()
			: base(ApiCommands.NewRpsOrder)
		{
		}

		public string CpFirmId { get; set; }
		public string MatchRef { get; set; }
		public string SettleCode { get; set; }
		//public DateTime? SettleDate { get; set; }
		public int? RefundRate { get; set; }
	}
}