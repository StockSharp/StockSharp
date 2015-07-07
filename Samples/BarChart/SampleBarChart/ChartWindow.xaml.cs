namespace SampleFix
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows.Media;
	
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.LMAX;
	using StockSharp.Xaml.Charting;

	partial class ChartWindow
	{
		private readonly LmaxTrader _trader;
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
				Antialiasinig = false, 
				UpBodyColor = Colors.White,
				UpWickColor = Colors.Black,
				DownBodyColor = Colors.Black,
				DownWickColor = Colors.Black,
			};

			area.Elements.Add(_candleElem);

			_trader.NewHistoricalCandles += ProcessNewCandles;
			_trader.SubscribeHistoricalCandles(_candleSeries, from, to);
		}

		private void ProcessNewCandles(CandleSeries series, IEnumerable<Candle> candles)
		{
			if (series != _candleSeries)
				return;

			this.GuiAsync(() =>
			{
				foreach (var timeFrameCandle in candles)
				{
					Chart.ProcessCandle(_candleElem, timeFrameCandle);
				}
			});
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_trader.NewHistoricalCandles -= ProcessNewCandles;
			base.OnClosing(e);
		}
	}
}