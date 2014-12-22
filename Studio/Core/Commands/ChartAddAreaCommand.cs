namespace StockSharp.Studio.Core.Commands
{
	using StockSharp.Xaml.Charting;

	public class ChartAddAreaCommand : BaseStudioCommand
	{
		public ChartArea Area { get; private set; }

		public ChartAddAreaCommand(ChartArea area)
		{
			Area = area;
		}
	}
}