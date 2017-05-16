#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleAlfaCandles.SampleAlfaCandlesPublic
File: ChartWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleAlfaCandles
{
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

		public void DrawCandles(Candle candle)
		{
			Chart.Draw(_candlesElem, candle);
		}
	}
}