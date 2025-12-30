namespace StockSharp.Samples.Indicators.SimpleSMA;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Ecng.Drawing;
using Ecng.IO;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Configuration;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;
using StockSharp.Messages;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private readonly string _pathHistory = Paths.HistoryDataPath;
	private readonly IFileSystem _fileSystem = Paths.FileSystem;

	public MainWindow()
	{
		InitializeComponent();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.ApplyDefaultTheme();

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

		Task.Run(async () =>
		{
			var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

			var candleStorage = new StorageRegistry().GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), new LocalMarketDataDrive(_fileSystem, _pathHistory), StorageFormats.Binary);
			var candles = candleStorage.LoadAsync(Paths.HistoryBeginDate, Paths.HistoryEndDate);

			var indicator = new SimpleMovingAverage()
			{
				Length = 10
			};

			await foreach (var candle in candles)
			{
				var indicatorValue = indicator.Process(candle);
				var chartDrawData = new ChartDrawData();

				chartDrawData.Group(candle.OpenTime)
					.Add(chartCandleElement, candle)
					.Add(chartIndicatorElement, indicatorValue);

				Chart.Draw(chartDrawData);
			}
		});
	}
}
