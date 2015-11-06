namespace SampleTransaq
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Media;

	using StockSharp.Algo.Candles;
	using StockSharp.Transaq;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly TransaqTrader _trader;
		private readonly CandleSeries _candleSeries;
		private readonly ChartCandleElement _candleElem;

		public ChartWindow(CandleSeries candleSeries)
		{
			InitializeComponent();

			if (candleSeries == null)
				throw new ArgumentNullException(nameof(candleSeries));

			_candleSeries = candleSeries;
			_trader = MainWindow.Instance.Trader;

			Chart.ChartTheme = "ExpressionDark";

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candleElem = new ChartCandleElement
			{
				Antialiasing = false, 
				UpFillColor = Colors.White,
				UpBorderColor = Colors.Black,
				DownFillColor = Colors.Black,
				DownBorderColor = Colors.Black,
			};

			area.Elements.Add(_candleElem);

			_trader.NewCandles += ProcessNewCandles;
			_trader.SubscribeCandles(_candleSeries, DateTime.Today - TimeSpan.FromTicks(((TimeSpan)candleSeries.Arg).Ticks * 100), DateTimeOffset.MaxValue);
		}

		private void ProcessNewCandles(CandleSeries series, IEnumerable<Candle> candles)
		{
			if (series != _candleSeries)
				return;

			foreach (var timeFrameCandle in candles)
			{
				Chart.Draw(_candleElem, timeFrameCandle);
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			_trader.NewCandles -= ProcessNewCandles;
			base.OnClosing(e);
		}
	}
}