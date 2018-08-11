namespace SampleSmart
{
	using System;
	using System.ComponentModel;
	using System.Windows.Media;

	using StockSharp.Algo.Candles;
	using StockSharp.SmartCom;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly SmartTrader _trader;
		private readonly CandleSeries _candleSeries;
		private readonly ChartCandleElement _candleElem;

		public ChartWindow(CandleSeries candleSeries)
		{
			InitializeComponent();

			if (candleSeries == null)
				throw new ArgumentNullException(nameof(candleSeries));

			_candleSeries = candleSeries;
			_trader = MainWindow.Instance.Trader;

			Chart.ChartTheme = ChartThemes.ExpressionDark;

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candleElem = new ChartCandleElement
			{
				AntiAliasing = false, 
				UpFillColor = Colors.White,
				UpBorderColor = Colors.Black,
				DownFillColor = Colors.Black,
				DownBorderColor = Colors.Black,
			};

			area.Elements.Add(_candleElem);

			var tf = (TimeSpan)candleSeries.Arg;

			_trader.CandleSeriesProcessing += ProcessNewCandle;
			_trader.SubscribeCandles(_candleSeries, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 1000)));
		}

		private void ProcessNewCandle(CandleSeries series, Candle candle)
		{
			if (series != _candleSeries)
				return;

			Chart.Draw(_candleElem, candle);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_trader.UnSubscribeCandles(_candleSeries);
			_trader.CandleSeriesProcessing -= ProcessNewCandle;

			base.OnClosing(e);
		}
	}
}