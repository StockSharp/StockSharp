#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleOEC.SampleOECPublic
File: ChartWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleCQG
{
	using System;
	using System.ComponentModel;
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
			InitializeComponent();

			if (candleSeries == null)
				throw new ArgumentNullException(nameof(candleSeries));

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

			var tf = (TimeSpan)candleSeries.Arg;

			_connector.CandleSeriesProcessing += ProcessNewCandles;
			_connector.SubscribeCandles(_candleSeries, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 10000)));
		}

		private void ProcessNewCandles(CandleSeries series, Candle candle)
		{
			if (series != _candleSeries)
				return;

			Chart.Draw(_candleElem, candle);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_connector.UnSubscribeCandles(_candleSeries);
			_connector.CandleSeriesProcessing -= ProcessNewCandles;
			base.OnClosing(e);
		}
	}
}