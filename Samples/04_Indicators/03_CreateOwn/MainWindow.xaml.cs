namespace StockSharp.Samples.Indicators.CreateOwn;

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

		var chartIndicatorElement1 = new ChartIndicatorElement()
		{
			FullTitle = "SMA",
			Color = Colors.Brown,
			DrawStyle = DrawStyles.Line
		};

		var chartIndicatorElement2 = new ChartIndicatorElement()
		{
			FullTitle = "Lazy",
			Color = Colors.DodgerBlue,
			DrawStyle = DrawStyles.Line
		};
		Chart.AddElement(chartArea, chartIndicatorElement1);
		Chart.AddElement(chartArea, chartIndicatorElement2);

		var secId = "SBER@TQBR".ToSecurityId();

		var candleStorage = new StorageRegistry().GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), new LocalMarketDataDrive(_pathHistory), StorageFormats.Binary);
		var candles = candleStorage.Load(Paths.HistoryBeginDate, Paths.HistoryEndDate);

		var indicator1 = new SimpleMovingAverage()
		{
			Length = 10
		};
		var indicator2 = new LazyMovingAverage()
		{
			Length = 10
		};

		foreach (var candle in candles)
		{
			var indicatorValue1 = indicator1.Process(candle);
			var indicatorValue2 = indicator2.Process(candle);
			var chartDrawData = new ChartDrawData();

			chartDrawData.Group(candle.OpenTime)
				.Add(chartCandleElement, candle)
				.Add(chartIndicatorElement1, indicatorValue1)
				.Add(chartIndicatorElement2, indicatorValue2);
			Chart.Draw(chartDrawData);
		}
	}
}

internal class LazyMovingAverage : BaseIndicator
{
	public int Length { get; set; } = 32;
	private decimal? _outputValue;

	protected override bool CalcIsFormed() => true;

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = input.GetValue<decimal>();
		
		if (_outputValue == null)
			_outputValue = value;
		else
			_outputValue += (value - _outputValue) / Length;

		return new DecimalIndicatorValue(this, _outputValue.Value, input.Time);
	}
}
