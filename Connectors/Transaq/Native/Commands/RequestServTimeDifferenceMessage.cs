namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestServTimeDifferenceMessage : BaseCommandMessage
	{
		public RequestServTimeDifferenceMessage() : base(ApiCommands.GetServTimeDifference)
		{
		}
	}
}