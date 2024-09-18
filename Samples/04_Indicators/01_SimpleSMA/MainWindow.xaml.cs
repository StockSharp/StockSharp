namespace StockSharp.Samples.Indicators.SimpleSMA;

using System;
using System.Windows.Media;

using Ecng.Drawing;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Configuration;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;
using StockSharp.Messages;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private readonly string _pathHistory = Paths.HistoryDataPath;

	public MainWindow()
	{
		InitializeComponent();

		var chartArea = new ChartArea();
		Chart.AddArea(chartArea);

		var chartCandleElement = new ChartCandleElement();
		Chart.AddElement(chartArea, chartCandleElement);

		var chartIndicatorElement = new ChartIndicatorElement()
		{
			Color = Colors.Brown,
			DrawStyle = DrawStyles.Line
		};
		Chart.AddElement(chartArea, chartIndicatorElement);

		var secId = "SBER@TQBR".ToSecurityId();

		var candleStorage = new StorageRegistry().GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), new LocalMarketDataDrive(_pathHistory), StorageFormats.Binary);
		var candles = candleStorage.Load(Paths.HistoryBeginDate, Paths.HistoryEndDate);

		var indicator = new SimpleMovingAverage()
		{
			Length = 10
		};

		foreach (var candle in candles)
		{
			var indicatorValue = indicator.Process(candle);
			var chartDrawData = new ChartDrawData();

			chartDrawData.Group(candle.OpenTime)
				.Add(chartCandleElement, candle)
				.Add(chartIndicatorElement, indicatorValue);

			Chart.Draw(chartDrawData);
		}
	}
}
