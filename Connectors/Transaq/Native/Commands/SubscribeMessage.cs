namespace StockSharp.Transaq.Native.Commands
{
	using System.Collections.Generic;

	using StockSharp.Messages;

	internal class SubscribeMessage : BaseCommandMessage
	{
		public SubscribeMessage() : base(ApiCommands.Subscribe)
		{
			AllTrades = new List<SecurityId>();
			Quotations = new List<SecurityId>();
			Quotes = new List<SecurityId>();
		}

		public List<SecurityId> AllTrades { get; private set; }
		public List<SecurityId> Quotations { get; private set; }
		public List<SecurityId> Quotes { get; private set; }
	}
}