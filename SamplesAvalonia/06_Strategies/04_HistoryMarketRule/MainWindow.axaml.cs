namespace StockSharp.Samples.Strategies.HistoryMarketRule.Avalonia;

using System;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;

using MonitorControl = StockSharp.Xaml.Windows.Avalonia.Monitor;

public partial class MainWindow : Window
{
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _sessionGate = new(1, 1);
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly LogManager _logManager;
	private readonly DatePicker _beginDate;
	private readonly DatePicker _endDate;
	private readonly Button _startButton;
	private readonly Button _stopButton;
	private readonly ProgressBar _progress;
	private readonly TextBlock _status;
	private readonly MonitorControl _monitor;
	private HistoryStrategySession _session;
	private EventSubscription _sessionEvents;
	private int _generation;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();

		_beginDate = this.FindControl<DatePicker>(nameof(DatePickerBegin));
		_endDate = this.FindControl<DatePicker>(nameof(DatePickerEnd));
		_startButton = this.FindControl<Button>(nameof(StartButton));
		_stopButton = this.FindControl<Button>(nameof(StopButton));
		_progress = this.FindControl<ProgressBar>(nameof(Progress));
		_status = this.FindControl<TextBlock>(nameof(Status));
		_monitor = this.FindControl<MonitorControl>(nameof(LogMonitor));

		_beginDate.SelectedDate = Paths.HistoryBeginDate;
		_endDate.SelectedDate = Paths.HistoryEndDate;
		_logManager = new LogManager();
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
				// SimpleOrderRulesStrategy, SimpleRulesStrategy, SimpleRulesUntilStrategy and
				// SimpleTradeRulesStrategy are drop-in alternatives.
				(security, portfolio, connector) =>
				{
					var strategy = new SimpleCandleRulesStrategy();
					try
					{
						strategy.Security = security;
						strategy.Portfolio = portfolio;
						strategy.Connector = connector;
						strategy.LogLevel = LogLevels.Debug;
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
				_progress.Value = 0;
				AttachSessionEvents(session, generation);
				_logManager.Sources.Add(session.Strategy);
				SetRunningState(true, "Starting");
				await session.StartAsync(false, _lifetimeCancellation.Token);
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

	private void AttachSessionEvents(HistoryStrategySession session, int generation)
	{
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

		_sessionEvents = new EventSubscription(
			() =>
			{
				session.Connector.ProgressChanged += progressChanged;
				session.Connector.StateChanged2 += stateChanged;
			},
			() =>
			{
				session.Connector.ProgressChanged -= progressChanged;
				session.Connector.StateChanged2 -= stateChanged;
			});
		_sessionEvents.Attach();
	}

	private bool IsCurrent(HistoryStrategySession session, int generation)
		=> !_isClosing && ReferenceEquals(_session, session) && _generation == generation;

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
		try
		{
			_logManager.Sources.Remove(session.Strategy);
		}
		catch
		{
			// Source removal must never skip owned session cleanup.
		}

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

	private void SetRunningState(bool isRunning, string status)
	{
		_startButton.IsEnabled = !isRunning;
		_stopButton.IsEnabled = isRunning;
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
				try
				{
					((IDisposable)_monitor).Dispose();
				}
				catch
				{
					// The application must still finish its closing handshake.
				}
				_lifetimeCancellation.Dispose();
				_sessionGate.Dispose();
				_closeApproved = true;
				Close();
			}
		}
	}
}
