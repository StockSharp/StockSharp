#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIB.SampleIBPublic
File: CandlesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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