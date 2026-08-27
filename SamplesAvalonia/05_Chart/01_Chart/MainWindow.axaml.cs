namespace StockSharp.Samples.Chart.Avalonia;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Platform.Storage;
using global::Avalonia.Threading;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Drawing;
using Ecng.IO;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing.Generation;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml.Charting.Avalonia.Controls;
using StockSharp.Xaml.Charting.Interfaces;

public partial class MainWindow : Window
{
	private sealed record TimeFrameOption(string Name, TimeSpan Value)
	{
		public override string ToString() => Name;
	}

	private static readonly TimeFrameOption[] _timeFrames =
	[
		new("1 min", TimeSpan.FromMinutes(1)),
		new("5 min", TimeSpan.FromMinutes(5)),
		new("15 min", TimeSpan.FromMinutes(15)),
		new("1 hour", TimeSpan.FromHours(1)),
	];

	private static readonly string[] _chartThemes =
	[
		"Default",
		"ExpressionDark",
		"Chrome",
		"Electric",
	];
	private static readonly ChartAnnotationTypes[] _annotationTypes = Enum
		.GetValues<ChartAnnotationTypes>()
		.Where(type => type != ChartAnnotationTypes.None)
		.ToArray();

	private readonly IFileSystem _fileSystem = Paths.FileSystem;
	private readonly SortedDictionary<DateTime, TimeFrameCandleMessage> _allCandles = [];
	private readonly Dictionary<IChartIndicatorElement, IIndicator> _indicators = [];
	private readonly DispatcherTimer _realtimeTimer;
	private CancellationTokenSource _loadCancellation = new();
	private CancellationTokenSource _securityScanCancellation = new();
	private Task _loadTask = Task.CompletedTask;
	private Task _securityScanTask = Task.CompletedTask;
	private int _loadGeneration;
	private bool _initialized;
	private bool _historyLoaded;
	private bool _isClosing;
	private bool _closeApproved;
	private int _themeIndex;
	private int _annotationId;
	private DateTime _lastRealtimeDraw;
	private DateTime _lastTime;
	private decimal _lastPrice;
	private Security _security;
	private RandomWalkTradeGenerator _tradeGenerator;
	private TimeFrameCandleMessage _realtimeCandle;
	private IChartArea _area;
	private IChartCandleElement _candleElement;
	private Subscription _subscription;
	private IChartAnnotationElement _annotation;
	private IAnnotationData _annotationData;

	public MainWindow()
	{
		InitializeComponent();

		HistoryPath.Text = Paths.HistoryDataPath;
		TimeFrame.ItemsSource = _timeFrames;
		TimeFrame.SelectedIndex = 0;
		Format.ItemsSource = Enum.GetValues<StorageFormats>();
		Format.SelectedItem = StorageFormats.Binary;
		var zones = TimeZoneInfo.GetSystemTimeZones()
			.Prepend(TimeZoneInfo.Utc)
			.DistinctBy(zone => zone.Id)
			.ToArray();
		TimeZone.ItemsSource = zones;
		TimeZone.SelectedItem = zones.First(zone => zone.Id == TimeZoneInfo.Utc.Id);

		Chart.SubscribeCandleElement += OnSubscribeCandleElement;
		Chart.SubscribeIndicatorElement += OnSubscribeIndicatorElement;
		Chart.UnSubscribeElement += OnUnSubscribeElement;
		Chart.AnnotationCreated += OnAnnotationCreated;
		Chart.AnnotationModified += OnAnnotationModified;
		Chart.AnnotationDeleted += OnAnnotationDeleted;
		Chart.AnnotationSelected += OnAnnotationSelected;
		Chart.RegisterOrder += OnRegisterOrder;

		_realtimeTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(50),
		};
		_realtimeTimer.Tick += OnRealtimeTick;
		_realtimeTimer.Start();

		Opened += OnOpened;
		Closing += OnClosing;
		_initialized = true;
		ApplyCapabilities();
	}

	private void OnOpened(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		StartSecurityScan(HistoryPath.Text);
	}

	private async void OnBrowseClick(object sender, RoutedEventArgs e)
	{
		var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = "Select StockSharp history folder",
			AllowMultiple = false,
		});
		var path = folders.FirstOrDefault()?.TryGetLocalPath();
		if (string.IsNullOrWhiteSpace(path))
			return;

		HistoryPath.Text = path;
		StartSecurityScan(path);
	}

	private void OnRefreshSecuritiesClick(object sender, RoutedEventArgs e)
		=> StartSecurityScan(HistoryPath.Text);

	private void StartSecurityScan(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return;

		var previousCancellation = _securityScanCancellation;
		var previousTask = _securityScanTask;
		var cancellation = new CancellationTokenSource();
		_securityScanCancellation = cancellation;
		previousCancellation.Cancel();
		_securityScanTask = ScanSecuritiesAfterPreviousAsync(previousTask, previousCancellation, path, cancellation.Token);
	}

	private async Task ScanSecuritiesAfterPreviousAsync(
		Task previousTask,
		CancellationTokenSource previousCancellation,
		string path,
		CancellationToken cancellationToken)
	{
		try
		{
			await previousTask.ConfigureAwait(false);
		}
		catch
		{
		}
		finally
		{
			previousCancellation.Dispose();
		}

		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			using var drive = new LocalMarketDataDrive(_fileSystem, path);
			var securities = await drive.GetAvailableSecuritiesAsync()
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				Securities.ItemsSource = securities;
				Securities.SelectedIndex = securities.Length == 0 ? -1 : 0;
				StatusText.Text = securities.Length == 0
					? "No securities were found in the selected folder."
					: $"Found {securities.Length} securities.";
			}, DispatcherPriority.Background, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			await TrySetStatusAsync($"Security scan failed: {error.Message}").ConfigureAwait(false);
		}
	}

	private void OnSecuritySelectionChanged(object sender, SelectionChangedEventArgs e)
		=> DrawButton.IsEnabled = Securities.SelectedItem is SecurityId;

	private void OnDrawClick(object sender, RoutedEventArgs e)
		=> RefreshChart();

	private void RefreshChart()
	{
		if (Securities.SelectedItem is not SecurityId securityId ||
			TimeFrame.SelectedItem is not TimeFrameOption timeFrame)
			return;

		CancelCurrentLoad();
		_historyLoaded = false;
		_allCandles.Clear();
		_indicators.Clear();
		_realtimeCandle = null;
		_lastPrice = 0m;

		var chart = (IChart)Chart;
		foreach (var area in chart.Areas.ToArray())
			chart.RemoveArea(area);

		_area = chart.CreateArea();
		_area.Title = securityId.ToStringId();
		chart.AddArea(_area);
		Chart.ActiveArea = _area;

		_security = new Security
		{
			Id = securityId.ToStringId(),
			PriceStep = 0.01m,
			Board = ExchangeBoard.Forts,
		};
		Chart.OrderSettings.Security = _security;
		Chart.OrderSettings.Volume = 1m;

		_tradeGenerator = new RandomWalkTradeGenerator(securityId);
		_tradeGenerator.Init();
		_tradeGenerator.Process(_security.ToMessage());

		_subscription = new Subscription(timeFrame.Value.TimeFrame(), _security);
		_candleElement = chart.CreateCandleElement();
		_candleElement.FullTitle = $"{securityId} {timeFrame.Name}";
		_candleElement.PriceStep = 20;
		chart.AddElement(_area, _candleElement, _subscription);
		StatusText.Text = "Loading history...";
	}

	private void OnSubscribeCandleElement(IChartCandleElement element, Subscription subscription)
		=> StartHistoryLoad(element, subscription);

	private void StartHistoryLoad(IChartCandleElement element, Subscription subscription)
	{
		if (Securities.SelectedItem is not SecurityId securityId ||
			TimeFrame.SelectedItem is not TimeFrameOption timeFrame ||
			Format.SelectedItem is not StorageFormats format)
			return;

		var previousCancellation = _loadCancellation;
		var previousTask = _loadTask;
		var cancellation = new CancellationTokenSource();
		_loadCancellation = cancellation;
		previousCancellation.Cancel();
		var generation = ++_loadGeneration;
		var buildFromTicks = BuildFromTicks.IsChecked == true;
		var path = HistoryPath.Text;
		_loadTask = LoadAfterPreviousAsync(
			previousTask,
			previousCancellation,
			generation,
			element,
			securityId,
			timeFrame.Value,
			format,
			path,
			buildFromTicks,
			cancellation.Token);
	}

	private async Task LoadAfterPreviousAsync(
		Task previousTask,
		CancellationTokenSource previousCancellation,
		int generation,
		IChartCandleElement element,
		SecurityId securityId,
		TimeSpan timeFrame,
		StorageFormats format,
		string path,
		bool buildFromTicks,
		CancellationToken cancellationToken)
	{
		try
		{
			await previousTask.ConfigureAwait(false);
		}
		catch
		{
		}
		finally
		{
			previousCancellation.Dispose();
		}

		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			using var registry = new StorageRegistry();
			using var drive = new LocalMarketDataDrive(_fileSystem, path);
			var maxDays = buildFromTicks ? 15 : 30 * Math.Max(1, (int)timeFrame.TotalMinutes);

			if (buildFromTicks)
				await LoadTicksAsync(registry, drive, element, securityId, timeFrame, format, maxDays, generation, cancellationToken).ConfigureAwait(false);
			else
				await LoadCandlesAsync(registry, drive, element, securityId, timeFrame, format, maxDays, generation, cancellationToken).ConfigureAwait(false);

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (generation != _loadGeneration)
					return;

				_historyLoaded = _allCandles.Count > 0;
				if (_allCandles.Count > 0)
				{
					_realtimeCandle = _allCandles.Values.Last().TypedClone();
					_lastPrice = _realtimeCandle.ClosePrice;
					_lastTime = _realtimeCandle.CloseTime;
					_tradeGenerator?.Process(new ExecutionMessage
					{
						DataTypeEx = DataType.Ticks,
						SecurityId = securityId,
						ServerTime = _lastTime,
						TradePrice = _lastPrice,
					});
				}

				Chart.IsAutoRange = false;
				AddIndicatorButton.IsEnabled = _historyLoaded && Chart.AllowAddIndicators && Chart.IsInteracted;
				NewAnnotationButton.IsEnabled = _historyLoaded;
				ModifyAnnotationButton.IsEnabled = _annotation is not null;
				StatusText.Text = $"Loaded {_allCandles.Count:N0} candles.";
			}, DispatcherPriority.Background, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			await TrySetStatusAsync($"History load failed: {error.Message}").ConfigureAwait(false);
		}
	}

	private async Task LoadTicksAsync(
		StorageRegistry registry,
		LocalMarketDataDrive drive,
		IChartCandleElement element,
		SecurityId securityId,
		TimeSpan timeFrame,
		StorageFormats format,
		int maxDays,
		int generation,
		CancellationToken cancellationToken)
	{
		var day = DateTime.MinValue;
		var loadedDays = 0;
		var current = (TimeFrameCandleMessage)null;
		var updates = new Dictionary<DateTime, TimeFrameCandleMessage>();
		var tickCount = 0;

		await foreach (var tick in registry.GetTickMessageStorage(securityId, drive, format)
			.LoadAsync(null, null)
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			if (tick.TradePrice is null)
				continue;

			if (day != tick.ServerTime.Date)
			{
				day = tick.ServerTime.Date;
				if (++loadedDays > maxDays)
					break;
			}

			current = UpdateCandle(current, tick, timeFrame, updates);
			if (++tickCount % 512 != 0)
				continue;

			await ApplyCandleBatchAsync(element, updates.Values.ToArray(), generation, cancellationToken).ConfigureAwait(false);
			updates.Clear();
		}

		if (updates.Count > 0)
			await ApplyCandleBatchAsync(element, updates.Values.ToArray(), generation, cancellationToken).ConfigureAwait(false);
	}

	private async Task LoadCandlesAsync(
		StorageRegistry registry,
		LocalMarketDataDrive drive,
		IChartCandleElement element,
		SecurityId securityId,
		TimeSpan timeFrame,
		StorageFormats format,
		int maxDays,
		int generation,
		CancellationToken cancellationToken)
	{
		var day = DateTime.MinValue;
		var loadedDays = 0;
		var batch = new List<TimeFrameCandleMessage>(128);
		var storage = registry.GetTimeFrameCandleMessageStorage(securityId, timeFrame, drive, format);

		await foreach (var candle in storage.LoadAsync(null, null)
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			if (day != candle.OpenTime.Date)
			{
				day = candle.OpenTime.Date;
				if (++loadedDays > maxDays)
					break;
			}

			candle.State = CandleStates.Finished;
			batch.Add((TimeFrameCandleMessage)candle.TypedClone());

			if (batch.Count < 128)
				continue;

			await ApplyCandleBatchAsync(element, batch.ToArray(), generation, cancellationToken).ConfigureAwait(false);
			batch.Clear();
		}

		if (batch.Count > 0)
			await ApplyCandleBatchAsync(element, batch.ToArray(), generation, cancellationToken).ConfigureAwait(false);
	}

	private TimeFrameCandleMessage UpdateCandle(
		TimeFrameCandleMessage current,
		ExecutionMessage tick,
		TimeSpan timeFrame,
		IDictionary<DateTime, TimeFrameCandleMessage> updates)
	{
		var time = tick.ServerTime;
		var price = tick.TradePrice.Value;
		if (current is null || time >= current.CloseTime)
		{
			if (current is not null)
			{
				current.State = CandleStates.Finished;
				updates[current.OpenTime] = current.TypedClone();
			}

			var bounds = timeFrame.GetCandleBounds(time, _security?.Board ?? ExchangeBoard.Forts);
			current = new TimeFrameCandleMessage
			{
				TypedArg = timeFrame,
				OpenTime = bounds.Min,
				CloseTime = bounds.Max,
				SecurityId = tick.SecurityId,
				OpenPrice = price,
				HighPrice = price,
				LowPrice = price,
				ClosePrice = price,
				State = CandleStates.Active,
			};
		}

		current.HighPrice = Math.Max(current.HighPrice, price);
		current.LowPrice = Math.Min(current.LowPrice, price);
		current.ClosePrice = price;
		current.TotalVolume += tick.TradeVolume ?? 0m;
		updates[current.OpenTime] = current.TypedClone();
		return current;
	}

	private async Task ApplyCandleBatchAsync(
		IChartCandleElement element,
		IReadOnlyList<TimeFrameCandleMessage> candles,
		int generation,
		CancellationToken cancellationToken)
		=> await Dispatcher.UIThread.InvokeAsync(() =>
		{
			if (generation != _loadGeneration || !ReferenceEquals(element, _candleElement))
				return;

			var data = new ChartDrawDataImpl();
			foreach (var candle in candles.OrderBy(candle => candle.OpenTime))
			{
				_allCandles[candle.OpenTime] = candle;
				var group = data.Group(candle.OpenTime)
					.Add(element, candle);
				if (CustomColors2.IsChecked == true)
					group.Add(element, GetRandomColor());

				foreach (var (indicatorElement, indicator) in _indicators)
					group.Add(indicatorElement, indicator.Process(candle));
			}

			((IChart)Chart).Draw(data);
		}, DispatcherPriority.Background, cancellationToken);

	private void OnSubscribeIndicatorElement(
		IChartIndicatorElement element,
		Subscription subscription,
		IIndicator indicator)
	{
		indicator.Reset();
		var data = new ChartDrawDataImpl();
		foreach (var candle in _allCandles.Values)
			data.Group(candle.OpenTime).Add(element, indicator.Process(candle));

		((IChart)Chart).Reset([element]);
		((IChart)Chart).Draw(data);
		_indicators[element] = indicator;
	}

	private void OnUnSubscribeElement(IChartElement element)
	{
		if (element is IChartIndicatorElement indicator)
			_indicators.Remove(indicator);
	}

	private void OnAddIndicatorClick(object sender, RoutedEventArgs e)
	{
		if (_area is null || _subscription is null)
			return;

		var indicator = new SimpleMovingAverage { Length = 10, Name = "SMA(10)" };
		var element = ((IChart)Chart).CreateIndicatorElement();
		element.FullTitle = indicator.Name;
		element.Color = Color.Goldenrod;
		element.DrawStyle = DrawStyles.Line;
		((IChart)Chart).AddElement(_area, element, _subscription, indicator);
	}

	private void OnRealtimeTick(object sender, EventArgs e)
	{
		if (!_historyLoaded || IsRealtime.IsChecked != true || _tradeGenerator is null || _security is null)
			return;

		var next = _tradeGenerator.Process(new TimeMessage { ServerTime = _lastTime }) as ExecutionMessage;
		_lastTime += TimeSpan.FromMilliseconds(RandomGen.GetInt(100, 10_000));
		if (next?.TradePrice is null)
			return;

		var updates = new Dictionary<DateTime, TimeFrameCandleMessage>();
		_realtimeCandle = UpdateCandle(_realtimeCandle, next, GetSelectedTimeFrame(), updates);
		_lastPrice = next.TradePrice.Value;
		if (DateTime.UtcNow - _lastRealtimeDraw < TimeSpan.FromMilliseconds(100))
			return;

		_lastRealtimeDraw = DateTime.UtcNow;
		ApplyRealtimeUpdates(updates.Values);
	}

	private void ApplyRealtimeUpdates(IEnumerable<TimeFrameCandleMessage> candles)
	{
		var data = new ChartDrawDataImpl();
		foreach (var candle in candles.OrderBy(candle => candle.OpenTime))
		{
			_allCandles[candle.OpenTime] = candle;
			var group = data.Group(candle.OpenTime).Add(_candleElement, candle);
			if (CustomColors2.IsChecked == true)
				group.Add(_candleElement, GetRandomColor());
			foreach (var (element, indicator) in _indicators)
				group.Add(element, indicator.Process(candle));
		}

		((IChart)Chart).Draw(data);
	}

	private void OnCustomColorsChanged(object sender, RoutedEventArgs e)
	{
		if (_candleElement is null)
			return;

		var enabled = CustomColors.IsChecked == true;
		_candleElement.Colorer = enabled
			? (time, isUp, isLast) => time.Hour % 2 == 0 ? (isUp ? Color.Chartreuse : Color.Aqua) : null
			: null;
		foreach (var element in _indicators.Keys)
			element.Colorer = enabled
				? coordinate => coordinate is DateTime time && time.Hour % 2 == 0 ? Color.Magenta : null
				: null;
		((IChart)Chart).Draw(new ChartDrawDataImpl());
	}

	private void OnCustomColors2Changed(object sender, RoutedEventArgs e)
	{
		if (_candleElement is null || _allCandles.Count == 0)
			return;

		var enabled = CustomColors2.IsChecked == true;
		var data = new ChartDrawDataImpl();
		foreach (var candle in _allCandles.Values)
			data.Group(candle.OpenTime).Add(_candleElement, enabled ? GetRandomColor() : null);
		((IChart)Chart).Draw(data);
	}

	private static Color GetRandomColor()
		=> Color.FromArgb(255, RandomGen.GetInt(0, 255), RandomGen.GetInt(0, 255), RandomGen.GetInt(0, 255));

	private void OnThemeClick(object sender, RoutedEventArgs e)
	{
		_themeIndex = (_themeIndex + 1) % _chartThemes.Length;
		Chart.ChartTheme = _chartThemes[_themeIndex];
		StatusText.Text = $"Chart theme: {_chartThemes[_themeIndex]}.";
	}

	private void OnTimeZoneChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_initialized && TimeZone.SelectedItem is TimeZoneInfo zone)
			Chart.TimeZone = zone;
	}

	private void OnCapabilitiesChanged(object sender, RoutedEventArgs e)
	{
		if (_initialized)
			ApplyCapabilities();
	}

	private void ApplyCapabilities()
	{
		Chart.IsInteracted = AllowInteraction.IsChecked == true;
		Chart.AllowAddArea = AllowAreas.IsChecked == true;
		Chart.AllowAddAxis = AllowAxes.IsChecked == true;
		Chart.AllowAddCandles = AllowCandles.IsChecked == true;
		Chart.AllowAddIndicators = AllowIndicators.IsChecked == true;
		Chart.AllowAddOwnTrades = AllowTrades.IsChecked == true;
		Chart.AllowAddOrders = AllowOrders.IsChecked == true;
		AddIndicatorButton.IsEnabled = _historyLoaded && Chart.IsInteracted && Chart.AllowAddIndicators;
	}

	private void OnNewAnnotationClick(object sender, RoutedEventArgs e)
	{
		if (_area is null || !TryGetMiddle(out var time, out var price))
			return;

		_annotation = ((IChart)Chart).CreateAnnotation();
		_annotation.Type = _annotationTypes[RandomGen.GetInt(0, _annotationTypes.Length)];
		((IChart)Chart).AddElement(_area, _annotation);
		_annotationData = null;
		var step = _security?.PriceStep ?? 0.01m;
		var mode = RandomGen.GetInt(0, 2) == 0
			? AnnotationCoordinateMode.Absolute
			: AnnotationCoordinateMode.Relative;
		var data = new ChartAnnotationData
		{
			X1 = mode == AnnotationCoordinateMode.Absolute ? time - GetSelectedTimeFrame() * 5 : 0.42,
			X2 = mode == AnnotationCoordinateMode.Absolute ? time + GetSelectedTimeFrame() * 5 : 0.58,
			Y1 = mode == AnnotationCoordinateMode.Absolute ? price - step * 5 : 0.42,
			Y2 = mode == AnnotationCoordinateMode.Absolute ? price + step * 5 : 0.58,
			Text = $"annotation #{++_annotationId}",
			ShowLabel = true,
			LabelPlacement = LabelPlacement.Axis,
			IsVisible = true,
			IsEditable = true,
			CoordinateMode = mode,
			Stroke = GetRandomBrush(),
			Fill = GetRandomBrush(64),
			Foreground = GetRandomBrush(),
		};
		((IChart)Chart).Draw(new ChartDrawDataImpl().Add(_annotation, data));
		_annotationData = data;
		ModifyAnnotationButton.IsEnabled = true;
	}

	private void OnModifyAnnotationClick(object sender, RoutedEventArgs e)
	{
		if (_annotation is null || _annotationData is null)
			return;

		var mode = _annotationData.CoordinateMode ?? AnnotationCoordinateMode.Absolute;
		IComparable x1;
		IComparable x2;
		IComparable y1;
		IComparable y2;

		if (mode == AnnotationCoordinateMode.Absolute &&
			_annotationData.X1 is DateTime oldX1 &&
			_annotationData.X2 is DateTime oldX2 &&
			_annotationData.Y1 is decimal oldY1 &&
			_annotationData.Y2 is decimal oldY2)
		{
			var step = _security?.PriceStep ?? 0.01m;
			x1 = oldX1 - GetSelectedTimeFrame();
			x2 = oldX2 + GetSelectedTimeFrame();
			y1 = oldY1 + step;
			y2 = oldY2 - step;
		}
		else if (mode == AnnotationCoordinateMode.Relative &&
			_annotationData.X1 is double relativeX1 &&
			_annotationData.X2 is double relativeX2 &&
			_annotationData.Y1 is double relativeY1 &&
			_annotationData.Y2 is double relativeY2)
		{
			x1 = Math.Clamp(relativeX1 - 0.03, 0d, 1d);
			x2 = Math.Clamp(relativeX2 + 0.03, 0d, 1d);
			y1 = Math.Clamp(relativeY1 - 0.03, 0d, 1d);
			y2 = Math.Clamp(relativeY2 + 0.03, 0d, 1d);
		}
		else
		{
			StatusText.Text = $"Annotation coordinate mode '{mode}' is not modified by this lesson.";
			return;
		}

		var data = new ChartAnnotationData
		{
			X1 = x1,
			X2 = x2,
			Y1 = y1,
			Y2 = y2,
			Text = $"modified annotation #{_annotationId}",
			ShowLabel = true,
			LabelPlacement = LabelPlacement.Axis,
			IsVisible = true,
			IsEditable = true,
			CoordinateMode = mode,
			Stroke = GetRandomBrush(),
			Fill = GetRandomBrush(64),
			Foreground = GetRandomBrush(),
		};
		((IChart)Chart).Draw(new ChartDrawDataImpl().Add(_annotation, data));
		_annotationData = data;
	}

	private static Ecng.Drawing.SolidBrush GetRandomBrush(int alpha = 255)
		=> new(Color.FromArgb(
			alpha,
			RandomGen.GetInt(0, 255),
			RandomGen.GetInt(0, 255),
			RandomGen.GetInt(0, 255)));

	private bool TryGetMiddle(out DateTime time, out decimal price)
	{
		if (_allCandles.Count == 0)
		{
			time = default;
			price = default;
			return false;
		}

		var first = _allCandles.Values.First();
		var last = _allCandles.Values.Last();
		time = first.OpenTime + TimeSpan.FromTicks((last.OpenTime - first.OpenTime).Ticks / 2);
		price = _allCandles.Values.Min(candle => candle.LowPrice) +
			(_allCandles.Values.Max(candle => candle.HighPrice) - _allCandles.Values.Min(candle => candle.LowPrice)) / 2;
		return true;
	}

	private void OnAnnotationCreated(IChartAnnotationElement annotation)
		=> _annotation = annotation;

	private void OnAnnotationModified(IChartAnnotationElement annotation, IAnnotationData data)
	{
		_annotation = annotation;
		_annotationData = data;
		ModifyAnnotationButton.IsEnabled = true;
	}

	private void OnAnnotationSelected(IChartAnnotationElement annotation, IAnnotationData data)
	{
		_annotation = annotation;
		_annotationData = data;
		ModifyAnnotationButton.IsEnabled = annotation is not null;
	}

	private void OnAnnotationDeleted(IChartAnnotationElement annotation)
	{
		if (!ReferenceEquals(annotation, _annotation))
			return;
		_annotation = null;
		_annotationData = null;
		ModifyAnnotationButton.IsEnabled = false;
	}

	private void OnRegisterOrder(IChartArea area, Order order)
		=> StatusText.Text = $"Register order requested: {order.Side} {order.Volume}@{order.Price}.";

	private TimeSpan GetSelectedTimeFrame()
		=> (TimeFrame.SelectedItem as TimeFrameOption)?.Value ?? TimeSpan.FromMinutes(1);

	private void CancelCurrentLoad()
	{
		_loadCancellation.Cancel();
		_loadGeneration++;
	}

	private async Task TrySetStatusAsync(string message)
	{
		try
		{
			await Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = message, DispatcherPriority.Background);
		}
		catch
		{
		}
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		_realtimeTimer.Stop();
		_loadCancellation.Cancel();
		_securityScanCancellation.Cancel();
		try
		{
			try
			{
				await _loadTask;
			}
			catch
			{
			}

			try
			{
				await _securityScanTask;
			}
			catch
			{
			}
		}
		finally
		{
			_realtimeTimer.Tick -= OnRealtimeTick;
			Chart.SubscribeCandleElement -= OnSubscribeCandleElement;
			Chart.SubscribeIndicatorElement -= OnSubscribeIndicatorElement;
			Chart.UnSubscribeElement -= OnUnSubscribeElement;
			Chart.AnnotationCreated -= OnAnnotationCreated;
			Chart.AnnotationModified -= OnAnnotationModified;
			Chart.AnnotationDeleted -= OnAnnotationDeleted;
			Chart.AnnotationSelected -= OnAnnotationSelected;
			Chart.RegisterOrder -= OnRegisterOrder;
			Closing -= OnClosing;
			_loadCancellation.Dispose();
			_securityScanCancellation.Dispose();
			_closeApproved = true;
			Close();
		}
	}
}
