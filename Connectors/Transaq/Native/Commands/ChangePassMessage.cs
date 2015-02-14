namespace StockSharp.Transaq.Native.Commands
{
	internal class ChangePassMessage : BaseCommandMessage
	{
		public ChangePassMessage() : base(ApiCommands.ChangePass)
		{
		}

		public string NewPass { get; set; }
		public string OldPass { get; set; }
	}
}