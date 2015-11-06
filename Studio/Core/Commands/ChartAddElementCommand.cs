namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Xaml.Charting;

	public class ChartAddElementCommand : BaseStudioCommand
	{
		public ChartArea Area { get; private set; }

		public IChartElement Element { get; set; }

		public ChartAddElementCommand(ChartArea area, IChartElement element)
		{
			if (area == null)
				throw new ArgumentNullException(nameof(area));

			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Area = area;
			Element = element;
		}
	}
}