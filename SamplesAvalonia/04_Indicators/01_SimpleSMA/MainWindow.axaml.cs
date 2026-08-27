namespace StockSharp.Samples.Indicators.SimpleSMA.Avalonia;

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Threading;

using Ecng.Drawing;
using Ecng.IO;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml.Charting.Avalonia;
using StockSharp.Xaml.Charting.Interfaces;

public partial class MainWindow : Window
{
	private const int _drawBatchSize = 128;

	private readonly string _historyPath = Paths.HistoryDataPath;
	private readonly IFileSystem _fileSystem = Paths.FileSystem;
	private readonly CancellationTokenSource _loadCancellation = new();
	private readonly IChart _chart;
	private readonly IChartCandleElement _candles;
	private readonly IChartIndicatorElement _average;
	private Task _loadTask = Task.CompletedTask;
	private bool _loadStarted;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();

		_chart = this.FindControl<ChartControl>(nameof(Chart));
		var area = _chart.CreateArea();
		area.Title = "Candles and SMA(10)";

		_candles = _chart.CreateCandleElement();
		_candles.FullTitle = "Candles";
		_average = _chart.CreateIndicatorElement();
		_average.FullTitle = "SMA(10)";
		_average.Color = Color.Brown;
		_average.DrawStyle = DrawStyles.Line;

		_chart.AddArea(area);
		_chart.AddElement(area, _candles);
		_chart.AddElement(area, _average);

		Opened += OnOpened;
		Closing += OnClosing;
	}

	private void OnOpened(object sender, EventArgs e)
	{
		if (_loadStarted)
			return;

		_loadStarted = true;
		_loadTask = Task.Run(() => LoadHistoryAsync(_loadCancellation.Token));
	}

	private async Task LoadHistoryAsync(CancellationToken cancellationToken)
	{
		try
		{
			using var drive = new LocalMarketDataDrive(_fileSystem, _historyPath);
			using var registry = new StorageRegistry();
			var securityId = Paths.HistoryDefaultSecurity.ToSecurityId();
			var storage = registry.GetTimeFrameCandleMessageStorage(
				securityId,
				TimeSpan.FromMinutes(1),
				drive,
				StorageFormats.Binary);
			var average = new SimpleMovingAverage { Length = 10 };
			var batch = new ChartDrawDataImpl();
			var batchCount = 0;

			await foreach (var candle in storage
				.LoadAsync(Paths.HistoryBeginDate, Paths.HistoryEndDate)
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false))
			{
				var value = average.Process(candle);
				batch.Group(candle.OpenTime)
					.Add(_candles, candle)
					.Add(_average, value);

				if (++batchCount < _drawBatchSize)
					continue;

				await DrawAsync(batch, cancellationToken).ConfigureAwait(false);
				batch = new ChartDrawDataImpl();
				batchCount = 0;
			}

			if (batchCount > 0)
				await DrawAsync(batch, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			try
			{
				await Dispatcher.UIThread.InvokeAsync(
					() => Title = $"Simple moving average - {error.Message}",
					DispatcherPriority.Background);
			}
			catch
			{
				// The UI dispatcher can already be shutting down while the load is unwinding.
			}
		}
	}

	private async Task DrawAsync(IChartDrawData batch, CancellationToken cancellationToken)
		=> await Dispatcher.UIThread.InvokeAsync(
			() => _chart.Draw(batch),
			DispatcherPriority.Background,
			cancellationToken);

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		_loadCancellation.Cancel();
		try
		{
			await _loadTask;
		}
		finally
		{
			Opened -= OnOpened;
			Closing -= OnClosing;
			_loadCancellation.Dispose();
			_closeApproved = true;
			Close();
		}
	}
}
