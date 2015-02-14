namespace StockSharp.Transaq.Native.Commands
{
	using System;

	using StockSharp.Transaq.Native.Responses;

	abstract class NewBaseOrderMessage : BaseCommandMessage
	{
		protected NewBaseOrderMessage(string commandId)
			: base(commandId)
		{
		}

		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public string Client { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public BuySells BuySell { get; set; }
		public string BrokerRef { get; set; }
	}

	internal class NewOrderMessage : NewBaseOrderMessage
	{
		public NewOrderMessage() : base(ApiCommands.NewOrder)
		{
		}

		public int Hidden { get; set; }
		public bool ByMarket { get; set; }
		public bool UseCredit { get; set; }
		public bool NoSplit { get; set; }
		public DateTime? ExpDate { get; set; }
		public NewOrderUnfilleds Unfilled { get; set; }
	}

	internal enum NewOrderUnfilleds
	{
		PutInQueue,
		CancelBalance,
		ImmOrCancel
	}
}