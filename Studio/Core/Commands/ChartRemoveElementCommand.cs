namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Xaml.Charting;

	public class ChartRemoveElementCommand : BaseStudioCommand
	{
		public ChartArea Area { get; private set; }

		public IChartElement Element { get; set; }

		public ChartRemoveElementCommand(ChartArea area, IChartElement element)
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