namespace StockSharp.Samples.Strategies.HistoryTrend.Avalonia;

using System;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Drawing;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting.Avalonia;
using StockSharp.Xaml.Charting.Avalonia.Specialized;
using StockSharp.Xaml.Grids.Avalonia;
using StockSharp.Xaml.PropertyGrid.Avalonia.Editors;

using DrawingColor = System.Drawing.Color;
using MonitorControl = StockSharp.Xaml.Windows.Avalonia.Monitor;

public partial class MainWindow : Window
{
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _sessionGate = new(1, 1);
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly LogManager _logManager;
	private readonly CandleDataTypeEdit _candleType;
	private readonly DatePicker _beginDate;
	private readonly DatePicker _endDate;
	private readonly Button _startButton;
	private readonly Button _stopButton;
	private readonly ProgressBar _progress;
	private readonly TextBlock _status;
	private readonly IChart _chart;
	private readonly MarketDepthControl _marketDepth;
	private readonly OrderGrid _orderGrid;
	private readonly MyTradeGrid _myTradeGrid;
	private readonly StatisticParameterGrid _statisticGrid;
	private readonly EquityCurveChart _equityCurve;
	private readonly MonitorControl _monitor;
	private HistoryStrategySession _session;
	private EventSubscription _sessionEvents;
	private IChartBandElement _pnl;
	private IChartBandElement _unrealizedPnl;
	private IChartBandElement _commission;
	private int _generation;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();

		_candleType = this.FindControl<CandleDataTypeEdit>(nameof(CandleDataTypeEdit));
		_beginDate = this.FindControl<DatePicker>(nameof(DatePickerBegin));
		_endDate = this.FindControl<DatePicker>(nameof(DatePickerEnd));
		_startButton = this.FindControl<Button>(nameof(StartButton));
		_stopButton = this.FindControl<Button>(nameof(StopButton));
		_progress = this.FindControl<ProgressBar>(nameof(Progress));
		_status = this.FindControl<TextBlock>(nameof(Status));
		_chart = this.FindControl<ChartControl>(nameof(Chart));
		_marketDepth = this.FindControl<MarketDepthControl>(nameof(MarketDepth));
		_orderGrid = this.FindControl<OrderGrid>(nameof(OrderGrid));
		_myTradeGrid = this.FindControl<MyTradeGrid>(nameof(MyTradeGrid));
		_statisticGrid = this.FindControl<StatisticParameterGrid>(nameof(StatisticGrid));
		_equityCurve = this.FindControl<EquityCurveChart>(nameof(EquityCurve));
		_monitor = this.FindControl<MonitorControl>(nameof(LogMonitor));

		_candleType.DataType = TimeSpan.FromMinutes(5).TimeFrame();
		_beginDate.SelectedDate = Paths.HistoryBeginDate;
		_endDate.SelectedDate = Paths.HistoryEndDate;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener("log.txt"));
		_logManager.Listeners.Add(new GuiLogListener(_monitor));

		Closing += OnClosing;
	}

	private async void OnStartClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await StartSessionAsync();
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Start failed: {error.Message}";
		}
	}

	private async void OnStopClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await StopSessionAsync();
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Stop failed: {error.Message}";
		}
	}

	private async Task StartSessionAsync()
	{
		await _sessionGate.WaitAsync(_lifetimeCancellation.Token);
		try
		{
			if (_session is not null || _isClosing)
				return;

			var startDate = (_beginDate.SelectedDate
				?? throw new InvalidOperationException("Select a begin date.")).Date.ChangeKind(DateTimeKind.Utc);
			var stopDate = (_endDate.SelectedDate
				?? throw new InvalidOperationException("Select an end date.")).Date.ChangeKind(DateTimeKind.Utc);

			var session = HistoryStrategySession.Create(
				startDate,
				stopDate,
				1_000_000m,
				// OneCandleTrendStrategy, StairsCountertrendStrategy and StairsTrendStrategy are drop-in alternatives.
				(security, portfolio, connector) =>
				{
					var strategy = new OneCandleCountertrendStrategy();
					try
					{
						strategy.Security = security;
						strategy.Portfolio = portfolio;
						strategy.Connector = connector;
						strategy.CandleType = _candleType.DataType;
						return strategy;
					}
					catch
					{
						strategy.Dispose();
						throw;
					}
				});

			_session = session;
			try
			{
				var generation = ++_generation;
				PreparePresentation(session.Strategy);
				AttachSessionEvents(session, generation);
				_logManager.Sources.Add(session.Connector);
				_logManager.Sources.Add(session.Strategy);
				SetRunningState(true, "Starting");
				await session.StartAsync(true, _lifetimeCancellation.Token);
			}
			catch
			{
				await TeardownSessionCoreAsync(session);
				throw;
			}
		}
		finally
		{
			_sessionGate.Release();
		}
	}

	private void PreparePresentation(Strategy strategy)
	{
		_chart.ClearAreas();
		_marketDepth.Clear();
		_orderGrid.Orders.Clear();
		_myTradeGrid.Trades.Clear();
		_statisticGrid.Parameters.Clear();
		_equityCurve.Clear();
		_progress.Value = 0;

		_pnl = _equityCurve.CreateCurve("P&L", DrawingColor.Green, DrawStyles.Area);
		_unrealizedPnl = _equityCurve.CreateCurve("unrealized", DrawingColor.Black, DrawStyles.Line);
		_commission = _equityCurve.CreateCurve("commission", DrawingColor.Red, DrawStyles.Line);
		strategy.SetChart(_chart);
		_statisticGrid.Parameters.AddRange(strategy.StatisticManager.Parameters);
	}

	private void AttachSessionEvents(HistoryStrategySession session, int generation)
	{
		Action<Subscription, IOrderBookMessage> depthReceived = (_, depth) =>
		{
			var snapshot = depth is ICloneable cloneable
				? (IOrderBookMessage)cloneable.Clone()
				: depth;
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					_marketDepth.UpdateDepth(snapshot);
			});
		};
		Action<Subscription, Order> orderReceived = (_, order)
			=> _uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					_orderGrid.Orders.TryAdd(order);
			});
		Action<Subscription, OrderFail> orderFailed = (_, fail)
			=> _uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					_orderGrid.AddRegistrationFail(fail);
			});
		Action<Subscription, MyTrade> ownTradeReceived = (_, trade)
			=> _uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					_myTradeGrid.Trades.TryAdd(trade);
			});
		Action<int> progressChanged = progress
			=> _uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					_progress.Value = progress;
			});
		Action<ChannelStates> stateChanged = state
			=> _uiEvents.Dispatch(() =>
			{
				if (!IsCurrent(session, generation))
					return;

				_status.Text = state.ToString();
				if (state == ChannelStates.Stopped)
					_ = CompleteStoppedSessionAsync(session);
			});
		Action pnlChanged = () => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				DrawPnl(session.Strategy);
		});

		_sessionEvents = new EventSubscription(
			() =>
			{
				session.Connector.OrderBookReceived += depthReceived;
				session.Connector.OrderReceived += orderReceived;
				session.Connector.OrderRegisterFailReceived += orderFailed;
				session.Connector.OwnTradeReceived += ownTradeReceived;
				session.Connector.ProgressChanged += progressChanged;
				session.Connector.StateChanged2 += stateChanged;
				session.Strategy.PnLChanged += pnlChanged;
			},
			() =>
			{
				session.Connector.OrderBookReceived -= depthReceived;
				session.Connector.OrderReceived -= orderReceived;
				session.Connector.OrderRegisterFailReceived -= orderFailed;
				session.Connector.OwnTradeReceived -= ownTradeReceived;
				session.Connector.ProgressChanged -= progressChanged;
				session.Connector.StateChanged2 -= stateChanged;
				session.Strategy.PnLChanged -= pnlChanged;
			});
		_sessionEvents.Attach();
	}

	private bool IsCurrent(HistoryStrategySession session, int generation)
		=> !_isClosing && ReferenceEquals(_session, session) && _generation == generation;

	private void DrawPnl(Strategy strategy)
	{
		var data = _equityCurve.CreateData();
		data.Group(strategy.CurrentTime)
			.Add(_pnl, strategy.PnL)
			.Add(_unrealizedPnl, strategy.PnLManager.UnrealizedPnL)
			.Add(_commission, strategy.Commission ?? 0m);
		_equityCurve.Draw(data);
	}

	private async Task CompleteStoppedSessionAsync(HistoryStrategySession session)
	{
		try
		{
			await StopSessionAsync(session);
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Cleanup failed: {error.Message}";
		}
	}

	private async Task StopSessionAsync(HistoryStrategySession expected = null)
	{
		await _sessionGate.WaitAsync();
		try
		{
			var session = _session;
			if (session is null || (expected is not null && !ReferenceEquals(session, expected)))
				return;

			await TeardownSessionCoreAsync(session);
		}
		finally
		{
			_sessionGate.Release();
		}
	}

	private async Task TeardownSessionCoreAsync(HistoryStrategySession session)
	{
		_session = null;
		_generation++;
		_sessionEvents?.Dispose();
		_sessionEvents = null;
		session.Strategy.SetChart(null);
		TryRemoveLogSource(session.Strategy);
		TryRemoveLogSource(session.Connector);

		try
		{
			await session.DisposeAsync();
		}
		finally
		{
			if (!_isClosing)
				SetRunningState(false, "Ready");
		}
	}

	private void TryRemoveLogSource(ILogSource source)
	{
		try
		{
			_logManager.Sources.Remove(source);
		}
		catch
		{
			// Source removal must never skip owned session cleanup.
		}
	}

	private void SetRunningState(bool isRunning, string status)
	{
		_startButton.IsEnabled = !isRunning;
		_stopButton.IsEnabled = isRunning;
		_candleType.IsEnabled = !isRunning;
		_beginDate.IsEnabled = !isRunning;
		_endDate.IsEnabled = !isRunning;
		_status.Text = status;
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		_lifetimeCancellation.Cancel();

		try
		{
			await StopSessionAsync();
		}
		catch
		{
			// Continue closing after every owned dependency had a disposal attempt.
		}
		finally
		{
			Closing -= OnClosing;
			_uiEvents.Dispose();
			try
			{
				_logManager.Dispose();
			}
			catch
			{
				// Continue the deterministic UI cleanup below.
			}
			finally
			{
				DisposeControl(_marketDepth);
				DisposeControl(_orderGrid);
				DisposeControl(_myTradeGrid);
				DisposeControl(_statisticGrid);
				DisposeControl(_monitor);
				_lifetimeCancellation.Dispose();
				_sessionGate.Dispose();
				_closeApproved = true;
				Close();
			}
		}
	}

	private static void DisposeControl(object control)
	{
		try
		{
			if (control is IDisposable disposable)
				disposable.Dispose();
		}
		catch
		{
			// Window shutdown must continue so every remaining control is released.
		}
	}
}
