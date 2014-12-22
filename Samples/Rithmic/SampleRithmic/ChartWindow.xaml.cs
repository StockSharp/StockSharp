namespace SampleRithmic
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows.Media;
	
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Rithmic;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly RithmicTrader _trader;
		private readonly CandleSeries _candleSeries;
		private readonly ChartCandleElement _candleElem;

		public ChartWindow(CandleSeries candleSeries, DateTime from, DateTime to)
		{
			InitializeComponent();

			if (candleSeries.IsNull())
				throw new ArgumentNullException("candleSeries");

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
			_trader.SubscribeCandles(_candleSeries, from, to);

			Title = candleSeries.ToString();
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

		protected override void OnClosing(CancelEventArgs e)
		{
			_trader.NewCandles -= ProcessNewCandles;
			base.OnClosing(e);
		}
	}
}