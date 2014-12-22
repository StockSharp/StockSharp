namespace StockSharp.Studio.Core.Commands
{
	public class ChartAutoRangeCommand : BaseStudioCommand
	{
		public bool AutoRange { get; private set; }

		public ChartAutoRangeCommand(bool autoRange)
		{
			AutoRange = autoRange;
		}
	}
}