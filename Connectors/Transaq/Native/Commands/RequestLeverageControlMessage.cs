namespace StockSharp.Transaq.Native.Commands
{
	using System.Collections.Generic;

	using StockSharp.Messages;

	internal class RequestLeverageControlMessage : BaseCommandMessage
	{
		public RequestLeverageControlMessage() : base(ApiCommands.GetLeverageControl)
		{
			SecIds = new List<SecurityId>();
		}

		public string Client { get; set; }
		public List<SecurityId> SecIds { get; private set; }
	}
}