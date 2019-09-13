namespace SampleMultiConnection
{
	using System;
	using System.Windows.Media;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly Connector _connector;
		private readonly CandleSeries _candleSeries;
		private readonly ChartCandleElement _candleElem;

		public ChartWindow(CandleSeries candleSeries)
		{
			if (candleSeries == null)
				throw new ArgumentNullException(nameof(candleSeries));

			InitializeComponent();

			_candleSeries = candleSeries;
			_connector = MainWindow.Instance.Connector;

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

			_connector.CandleSeriesProcessing += ProcessNewCandle;
			_connector.SubscribeCandles(_candleSeries, DateTime.Today - TimeSpan.FromTicks(((TimeSpan)candleSeries.Arg).Ticks * 10000));
		}

		private void ProcessNewCandle(CandleSeries series, Candle candle)
		{
			if (series != _candleSeries)
				return;

			Chart.Draw(_candleElem, candle);
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			_connector.CandleSeriesProcessing -= ProcessNewCandle;
			_connector.UnSubscribeCandles(_candleSeries);

			base.OnClosing(e);
		}
	}
}
