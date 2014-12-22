namespace SampleAlfaCandles
{
	using System.Collections.Generic;

	using StockSharp.Algo.Candles;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly ChartCandleElement _candlesElem;

		public ChartWindow()
		{
			InitializeComponent();

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candlesElem = new ChartCandleElement();
			area.Elements.Add(_candlesElem);
		}

		public void DrawCandles(IEnumerable<Candle> candles)
		{
			foreach (var candle in candles)
				Chart.Draw(_candlesElem, candle);
		}
	}
}