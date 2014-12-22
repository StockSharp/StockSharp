namespace SampleIB
{
	using StockSharp.Algo.Candles;
	using StockSharp.Xaml.Charting;

	public partial class CandlesWindow
	{
		private readonly ChartCandleElement _candleElement;

		public CandlesWindow()
		{
			InitializeComponent();

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candleElement = new ChartCandleElement();
			area.Elements.Add(_candleElement);
		}

		public void ProcessCandles(Candle candle)
		{
			Chart.Draw(_candleElement, candle);
		}
	}
}