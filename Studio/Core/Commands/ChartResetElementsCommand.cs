namespace StockSharp.Studio.Core.Commands
{
	using System.Collections.Generic;

	using StockSharp.Xaml.Charting;

	public class ChartResetElementsCommand : BaseStudioCommand
	{
		public IEnumerable<IChartElement> Elements { get; private set; }

		public ChartResetElementsCommand(IEnumerable<IChartElement> elements)
		{
			Elements = elements;
		}
	}
}