namespace StockSharp.Samples.Strategies.HistoryQuoting.Avalonia;

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

using DrawingColor = System.Drawing.Color;

public partial class MainWindow : Window
{
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _sessionGate = new(1, 1);
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly LogManager _logManager;
	private HistoryStrategySession _session;
	private CancellationTokenSource _sessionCancellation;
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
		CandleType.DataType = TimeSpan.FromMinutes(1).TimeFrame();
		BeginDate.SelectedDate = Paths.HistoryBeginDate;
		EndDate.SelectedDate = Paths.HistoryEndDate;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener("history-quoting.log"));
		_logManager.Listeners.Add(new GuiLogListener(Monitor));
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
				Status.Text = $"Start failed: {error.Message}";
		}
	}

	private async void OnStopClick(object sender, RoutedEventArgs e)
	{
		_sessionCancellation?.Cancel();
		try
		{
			await StopSessionAsync();
		}
		catch (Exception error)
		{
			if (!_isClosing)
				Status.Text = $"Stop failed: {error.Message}";
		}
	}

	private async Task StartSessionAsync()
	{
		await _sessionGate.WaitAsync(_lifetimeCancellation.Token);
		try
		{
			if (_session is not null || _isClosing)
				return;

			var start = (BeginDate.SelectedDate ?? throw new InvalidOperationException("Select a begin date.")).Date.ChangeKind(DateTimeKind.Utc);
			var stop = (EndDate.SelectedDate ?? throw new InvalidOperationException("Select an end date.")).Date.ChangeKind(DateTimeKind.Utc);
			var session = HistoryStrategySession.Create(
				start,
				stop,
				1_000_000m,
				(security, portfolio, connector) => new StairsCountertrendStrategy
				{
					Security = security,
					Portfolio = portfolio,
					Connector = connector,
					CandleDataType = CandleType.DataType,
					Volume = 1m,
				});
			var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);

			_session = session;
			_sessionCancellation = cancellation;
			try
			{
				var generation = ++_generation;
				PreparePresentation(session.Strategy);
				AttachEvents(session, generation);
				_logManager.Sources.Add(session.Connector);
				_logManager.Sources.Add(session.Strategy);
				SetRunningState(true, "Starting historical quoting...");
				await session.StartAsync(cancellation.Token);
			}
			catch
			{
				await TeardownCoreAsync(session);
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
		((IChart)Chart).ClearAreas();
		MarketDepth.Clear();
		OrderGrid.Orders.Clear();
		MyTradeGrid.Trades.Clear();
		StatisticGrid.Parameters.Clear();
		EquityCurve.Clear();
		Progress.Value = 0;

		_pnl = EquityCurve.CreateCurve("P&L", DrawingColor.Green, DrawStyles.Area);
		_unrealizedPnl = EquityCurve.CreateCurve("unrealized", DrawingColor.Black, DrawStyles.Line);
		_commission = EquityCurve.CreateCurve("commission", DrawingColor.Red, DrawStyles.Line);
		strategy.SetChart(Chart);
		StatisticGrid.Parameters.AddRange(strategy.StatisticManager.Parameters);
	}

	private void AttachEvents(HistoryStrategySession session, int generation)
	{
		Action<Subscription, IOrderBookMessage> depthReceived = (_, depth) =>
		{
			var snapshot = depth is ICloneable cloneable ? (IOrderBookMessage)cloneable.Clone() : depth;
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					MarketDepth.UpdateDepth(snapshot);
			});
		};
		Action<Subscription, Order> orderReceived = (_, order) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				OrderGrid.Orders.TryAdd(order);
		});
		Action<Subscription, OrderFail> orderFailed = (_, fail) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				OrderGrid.AddRegistrationFail(fail);
		});
		Action<Subscription, MyTrade> ownTradeReceived = (_, trade) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				MyTradeGrid.Trades.TryAdd(trade);
		});
		Action<int> progressChanged = value => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				Progress.Value = value;
		});
		Action<ChannelStates> stateChanged = state => _uiEvents.Dispatch(() =>
		{
			if (!IsCurrent(session, generation))
				return;
			Status.Text = state.ToString();
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
		var data = EquityCurve.CreateData();
		data.Group(strategy.CurrentTime)
			.Add(_pnl, strategy.PnL)
			.Add(_unrealizedPnl, strategy.PnLManager.UnrealizedPnL)
			.Add(_commission, strategy.Commission ?? 0m);
		EquityCurve.Draw(data);
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
				Status.Text = $"Cleanup failed: {error.Message}";
		}
	}

	private async Task StopSessionAsync(HistoryStrategySession expected = null)
	{
		await _sessionGate.WaitAsync();
		try
		{
			var session = _session;
			if (session is null || expected is not null && !ReferenceEquals(expected, session))
				return;
			await TeardownCoreAsync(session);
		}
		finally
		{
			_sessionGate.Release();
		}
	}

	private async Task TeardownCoreAsync(HistoryStrategySession session)
	{
		_session = null;
		var cancellation = _sessionCancellation;
		_sessionCancellation = null;
		cancellation?.Cancel();
		_generation++;
		_sessionEvents?.Dispose();
		_sessionEvents = null;
		session.Strategy.SetChart(null);
		_logManager.Sources.Remove(session.Strategy);
		_logManager.Sources.Remove(session.Connector);
		try
		{
			await session.DisposeAsync();
		}
		finally
		{
			cancellation?.Dispose();
			if (!_isClosing)
				SetRunningState(false, "Ready");
		}
	}

	private void SetRunningState(bool running, string text)
	{
		StartButton.IsEnabled = !running;
		StopButton.IsEnabled = running;
		BeginDate.IsEnabled = !running;
		EndDate.IsEnabled = !running;
		CandleType.IsEnabled = !running;
		Status.Text = text;
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
		_sessionCancellation?.Cancel();
		try
		{
			await StopSessionAsync();
		}
		catch
		{
		}
		finally
		{
			Closing -= OnClosing;
			_uiEvents.Dispose();
			_logManager.Dispose();
			DisposeControl(MarketDepth);
			DisposeControl(OrderGrid);
			DisposeControl(MyTradeGrid);
			DisposeControl(StatisticGrid);
			DisposeControl(Monitor);
			_lifetimeCancellation.Dispose();
			_sessionGate.Dispose();
			_closeApproved = true;
			Close();
		}
	}

	private static void DisposeControl(object control)
	{
		if (control is IDisposable disposable)
			disposable.Dispose();
	}
}
