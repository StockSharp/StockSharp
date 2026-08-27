namespace StockSharp.Samples.Chart.Performance.Avalonia;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Threading;

using Ecng.Common;
using Ecng.Drawing;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml.Charting.Interfaces;

public partial class MainWindow : Window
{
	private static readonly string _historyPath = Paths.HistoryDataPath;
	private static readonly SecurityId _securityId = Paths.HistoryDefaultSecurity.ToSecurityId();
	private const int _timeframe = 1;
	private const decimal _priceStep = 0.01m;
	private const int _candlesPacketSize = 10;
	private const int _maxHistoryDays = 50;

	private readonly TimeSpan _timeFrameSpan = TimeSpan.FromMinutes(_timeframe);
	private readonly Security _security = new()
	{
		Id = _securityId.ToStringId(),
		PriceStep = _priceStep,
		Board = ExchangeBoard.Forts,
	};
	private readonly List<TimeFrameCandleMessage> _candles = [];
	private readonly DispatcherTimer _chartUpdateTimer;
	private CancellationTokenSource _loadCancellation = new();
	private Task _loadTask = Task.CompletedTask;
	private int _loadGeneration;
	private bool _dataIsLoaded;
	private bool _isClosing;
	private bool _closeApproved;
	private decimal _lastPrice;
	private DateTime _lastTime;
	private IChartArea _area;
	private IChartCandleElement _candleElement;
	private IChartIndicatorElement _indicatorElement;
	private MyMovingAverage _indicator;

	public MainWindow()
	{
		InitializeComponent();

		_chartUpdateTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(100),
		};
		_chartUpdateTimer.Tick += OnRealtimeTick;
		_chartUpdateTimer.Start();

		Opened += OnOpened;
		Closing += OnClosing;
		DoubleTapped += OnDoubleTapped;
		PointerWheelChanged += OnPointerWheelChanged;
		PointerPressed += OnPointerPressed;
	}

	private void OnOpened(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		InitializeChart();
		StartHistoryLoad();
	}

	private void InitializeChart()
	{
		var chart = (IChart)Chart;
		foreach (var area in chart.Areas.ToArray())
			chart.RemoveArea(area);

		_area = chart.CreateArea();
		_area.Title = $"{_securityId} - {_timeframe} minute candles";
		chart.AddArea(_area);
		Chart.ActiveArea = _area;

		var yAxis = _area.YAxises.First();
		yAxis.AutoRange = true;
		Chart.IsAutoRange = true;
		Chart.IsAutoScroll = true;

		var subscription = new Subscription(_timeFrameSpan.TimeFrame(), _security);
		_candleElement = chart.CreateCandleElement();
		_candleElement.FullTitle = "Candles";
		_candleElement.YAxisId = yAxis.Id;
		chart.AddElement(_area, _candleElement, subscription);

		_indicator = new MyMovingAverage(200)
		{
			Name = "MyMA(200)",
		};
		_indicatorElement = chart.CreateIndicatorElement();
		_indicatorElement.FullTitle = _indicator.Name;
		_indicatorElement.DrawStyle = DrawStyles.Line;
		_indicatorElement.AntiAliasing = true;
		_indicatorElement.StrokeThickness = 1;
		_indicatorElement.Color = System.Drawing.Color.Blue;
		_indicatorElement.YAxisId = yAxis.Id;
		chart.AddElement(_area, _indicatorElement, subscription, _indicator);
	}

	private void StartHistoryLoad()
	{
		var previousCancellation = _loadCancellation;
		var previousTask = _loadTask;
		var cancellation = new CancellationTokenSource();
		_loadCancellation = cancellation;
		previousCancellation.Cancel();
		var generation = ++_loadGeneration;
		_loadTask = LoadAfterPreviousAsync(
			previousTask,
			previousCancellation,
			generation,
			cancellation.Token);
	}

	private async Task LoadAfterPreviousAsync(
		Task previousTask,
		CancellationTokenSource previousCancellation,
		int generation,
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
			await SetLoadingStateAsync(generation, true, "Loading history...", cancellationToken).ConfigureAwait(false);

			var candles = new List<TimeFrameCandleMessage>();
			var lastPrice = 0m;
			var lastTime = default(DateTime);
			var currentDate = DateTime.MinValue;
			var loadedDays = 0;

			using var registry = new StorageRegistry();
			using var drive = new LocalMarketDataDrive(Paths.FileSystem, _historyPath);
			var storage = registry.GetTickMessageStorage(_securityId, drive);

			await foreach (var tick in storage.LoadAsync(null, null)
				.WithCancellation(cancellationToken)
				.ConfigureAwait(false))
			{
				if (tick.TradePrice is null)
					continue;

				if (currentDate != tick.ServerTime.Date)
				{
					currentDate = tick.ServerTime.Date;
					if (++loadedDays > _maxHistoryDays)
						break;

					await SetLoadingTextAsync(
						generation,
						$"Loading ticks for {currentDate:dd MMM yyyy} ({loadedDays}/{_maxHistoryDays})...",
						cancellationToken).ConfigureAwait(false);
				}

				AppendTick(candles, tick, ref lastPrice, ref lastTime);
			}

			cancellationToken.ThrowIfCancellationRequested();
			var drawWatch = Stopwatch.StartNew();
			var drawn = 0;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (generation != _loadGeneration)
					return;

				_dataIsLoaded = false;
				_candles.Clear();
				_candles.AddRange(candles);
				_lastPrice = lastPrice;
				_lastTime = lastTime;
				_indicator.Reset();
				((IChart)Chart).Reset([_candleElement, _indicatorElement]);
			}, DispatcherPriority.Background, cancellationToken);

			for (var offset = 0; offset < candles.Count; offset += _candlesPacketSize)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var packet = candles
					.Skip(offset)
					.Take(Math.Min(_candlesPacketSize, candles.Count - offset))
					.ToArray();

				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					if (generation != _loadGeneration)
						return;
					DrawPacket(packet);
				}, DispatcherPriority.Background, cancellationToken);
				drawn += packet.Length;
			}

			drawWatch.Stop();
			var candlesPerSecond = drawWatch.Elapsed.TotalSeconds <= 0
				? drawn
				: drawn / drawWatch.Elapsed.TotalSeconds;

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (generation != _loadGeneration)
					return;

				_dataIsLoaded = candles.Count > 0;
				BusyOverlay.IsVisible = false;
				Chart.IsAutoRange = false;
				StatusText.Text = candles.Count == 0
					? "No tick history was found."
					: $"Rendered {drawn:N0} candles in {drawWatch.Elapsed.TotalMilliseconds:N0} ms ({candlesPerSecond:N0} candles/sec).";
			}, DispatcherPriority.Background, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			await TryFinishWithErrorAsync(generation, error.Message).ConfigureAwait(false);
		}
	}

	private void DrawPacket(IEnumerable<TimeFrameCandleMessage> candles)
	{
		var data = new ChartDrawDataImpl();
		foreach (var candle in candles)
		{
			candle.State = CandleStates.Finished;
			var group = data.Group(candle.OpenTime);
			group.Add(_candleElement, candle);
			group.Add(_indicatorElement, _indicator.Process(candle));
		}

		((IChart)Chart).Draw(data);
	}

	private void OnRealtimeTick(object sender, EventArgs e)
	{
		if (!_dataIsLoaded || IsRealtime.IsChecked != true || _lastPrice == 0m)
			return;

		_lastTime += TimeSpan.FromSeconds(10);
		var previousCandle = _candles.LastOrDefault();
		var price = RoundToStep(
			_lastPrice + ((RandomGen.GetDouble().To<decimal>() - 0.5m) * 5m * _priceStep),
			_priceStep);
		AppendTick(_candles, new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = _securityId,
			ServerTime = _lastTime,
			TradePrice = price,
			TradeVolume = RandomGen.GetInt(50) + 1,
		}, ref _lastPrice, ref _lastTime);

		var currentCandle = _candles[^1];
		var data = new ChartDrawDataImpl();
		if (previousCandle is not null && !ReferenceEquals(previousCandle, currentCandle))
		{
			previousCandle.State = CandleStates.Finished;
			var previousGroup = data.Group(previousCandle.OpenTime);
			previousGroup.Add(_candleElement, previousCandle);
			previousGroup.Add(_indicatorElement, _indicator.Process(previousCandle));
		}

		currentCandle.State = CandleStates.Active;
		var currentGroup = data.Group(currentCandle.OpenTime);
		currentGroup.Add(_candleElement, currentCandle);
		currentGroup.Add(_indicatorElement, _indicator.Process(currentCandle));
		((IChart)Chart).Draw(data);
	}

	private static void AppendTick(
		ICollection<TimeFrameCandleMessage> candles,
		ExecutionMessage tick,
		ref decimal lastPrice,
		ref DateTime lastTime)
	{
		if (tick.TradePrice is not decimal price)
			return;

		var candle = candles.LastOrDefault();
		if (candle is null || tick.ServerTime >= candle.CloseTime)
		{
			if (candle is not null)
				candle.State = CandleStates.Finished;

			var bounds = TimeSpan.FromMinutes(_timeframe).GetCandleBounds(tick.ServerTime, ExchangeBoard.Forts);
			candle = new TimeFrameCandleMessage
			{
				OpenTime = bounds.Min,
				CloseTime = bounds.Max,
				OpenPrice = price,
				HighPrice = price,
				LowPrice = price,
				ClosePrice = price,
				SecurityId = _securityId,
				TypedArg = TimeSpan.FromMinutes(_timeframe),
				State = CandleStates.Active,
			};
			candles.Add(candle);
		}

		if (tick.ServerTime < candle.OpenTime)
			throw new InvalidOperationException("Tick history is not ordered by time.");

		candle.HighPrice = Math.Max(candle.HighPrice, price);
		candle.LowPrice = Math.Min(candle.LowPrice, price);
		candle.ClosePrice = price;
		candle.TotalVolume += tick.TradeVolume ?? 0m;
		lastPrice = price;
		lastTime = tick.ServerTime;
	}

	private static decimal RoundToStep(decimal value, decimal step)
		=> (value / step).Round() * step;

	private async Task SetLoadingStateAsync(
		int generation,
		bool isVisible,
		string text,
		CancellationToken cancellationToken)
		=> await Dispatcher.UIThread.InvokeAsync(() =>
		{
			if (generation != _loadGeneration)
				return;
			_dataIsLoaded = false;
			BusyOverlay.IsVisible = isVisible;
			LoadingText.Text = text;
			StatusText.Text = "Loading tick history...";
		}, DispatcherPriority.Background, cancellationToken);

	private async Task SetLoadingTextAsync(
		int generation,
		string text,
		CancellationToken cancellationToken)
		=> await Dispatcher.UIThread.InvokeAsync(() =>
		{
			if (generation == _loadGeneration)
				LoadingText.Text = text;
		}, DispatcherPriority.Background, cancellationToken);

	private async Task TryFinishWithErrorAsync(int generation, string message)
	{
		try
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (generation != _loadGeneration)
					return;
				_dataIsLoaded = false;
				BusyOverlay.IsVisible = false;
				StatusText.Text = $"History load failed: {message}";
			}, DispatcherPriority.Background);
		}
		catch
		{
		}
	}

	private void OnDoubleTapped(object sender, TappedEventArgs e)
		=> Chart.IsAutoRange = true;

	private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
		=> Chart.IsAutoRange = false;

	private void OnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
			Chart.IsAutoRange = false;
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		_chartUpdateTimer.Stop();
		_loadCancellation.Cancel();
		try
		{
			await _loadTask;
		}
		catch
		{
		}
		finally
		{
			_chartUpdateTimer.Tick -= OnRealtimeTick;
			DoubleTapped -= OnDoubleTapped;
			PointerWheelChanged -= OnPointerWheelChanged;
			PointerPressed -= OnPointerPressed;
			Closing -= OnClosing;
			_loadCancellation.Dispose();
			_closeApproved = true;
			Close();
		}
	}
}

internal sealed class MyMovingAverage : BaseIndicator
{
	private readonly int _period;
	private readonly LinkedList<(DateTime Time, decimal Value)> _values = [];
	private decimal _sum;

	public MyMovingAverage(int period)
	{
		if (period <= 0)
			throw new ArgumentOutOfRangeException(nameof(period));
		_period = period;
	}

	public decimal Current { get; private set; }

	public override int NumValuesToInitialize => _period;

	protected override bool CalcIsFormed() => _values.Count >= _period;

	public DecimalIndicatorValue Process(ICandleMessage candle)
		=> ProcessValue(candle.OpenTime, candle.ClosePrice, candle.State == CandleStates.Finished);

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
		=> ProcessValue(input.Time, input.GetValue<decimal>(), input.IsFinal);

	public override void Reset()
	{
		_values.Clear();
		_sum = 0m;
		Current = 0m;
		base.Reset();
	}

	private DecimalIndicatorValue ProcessValue(DateTime time, decimal value, bool isFinal)
	{
		if (_values.Last?.Value.Time == time)
		{
			_sum -= _values.Last.Value.Value;
			_values.Last.Value = (time, value);
		}
		else
		{
			_values.AddLast((time, value));
		}

		_sum += value;
		while (_values.Count > _period)
		{
			_sum -= _values.First.Value.Value;
			_values.RemoveFirst();
		}

		Current = _sum / _values.Count;
		return new DecimalIndicatorValue(this, Current, time)
		{
			IsEmpty = false,
			IsFinal = isFinal,
		};
	}
}
