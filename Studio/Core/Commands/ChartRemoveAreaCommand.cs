namespace StockSharp.Studio.Core.Commands
{
	using StockSharp.Xaml.Charting;

	public class ChartRemoveAreaCommand : BaseStudioCommand
	{
		public ChartArea Area { get; private set; }

		public ChartRemoveAreaCommand(ChartArea area)
		{
			Area = area;
		}
	}
}