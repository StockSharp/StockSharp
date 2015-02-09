namespace StockSharp.Transaq.Native.Commands
{
	using System;

	internal class NewCondOrderMessage : NewOrderMessage
	{
		public NewCondOrderMessage()
		{
			Id = ApiCommands.NewCondOrder;
		}

		public TransaqAlgoOrderConditionTypes CondType { get; set; }
		public decimal CondValue { get; set; }
		
		public TransaqAlgoOrderValidTypes ValidAfterType { get; set; }
		public DateTime? ValidAfter { get; set; }
		
		public TransaqAlgoOrderValidTypes ValidBeforeType { get; set; }
		public DateTime? ValidBefore { get; set; }
	}
}