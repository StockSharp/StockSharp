namespace StockSharp.Studio.Core.Commands
{
	public class LogInCommand : BaseStudioCommand
	{
		public bool CanAutoLogon { get; private set; }

		public LogInCommand(bool canAutoLogon = true)
		{
			CanAutoLogon = canAutoLogon;
		}
	}
}