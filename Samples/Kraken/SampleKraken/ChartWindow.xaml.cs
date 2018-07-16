namespace SampleKraken
{
	using System;
	using System.ComponentModel;
	using System.Windows.Media;

	using StockSharp.Algo.Candles;
	using StockSharp.Kraken;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly KrakenTrader _trader;
		private readonly CandleSeries _candleSeries;
		private readonly ChartCandleElement _candleElem;

		public ChartWindow(CandleSeries candleSeries, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			InitializeComponent();

			_candleSeries = candleSeries ?? throw new ArgumentNullException(nameof(candleSeries));
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

			_trader.CandleSeriesProcessing += ProcessNewCandle;
			_trader.SubscribeCandles(_candleSeries, from, to);

			Title = candleSeries.ToString();
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