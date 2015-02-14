namespace StockSharp.Transaq.Native.Commands
{
	using System.Collections.Generic;

	internal class SubscribeTicksMessage : BaseCommandMessage
	{
		public SubscribeTicksMessage() : base(ApiCommands.SubscribeTicks)
		{
			Items = new List<SubscribeTicksSecurity>();
		}

		public bool Filter { get; set; }
		public List<SubscribeTicksSecurity> Items { get; private set; }
	}

	internal class SubscribeTicksSecurity
	{
		public string Board { get; set; }
		public string SecCode { get; set; }
		public int TradeNo { get; set; }
		public int SecId { get; set; }
	}
}