namespace StockSharp.Samples.Strategies.HistoryIndex.Avalonia;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Configuration;
using Ecng.Logging;

using StockSharp.Charting;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;

public partial class MainWindow : Window
{
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _sessionGate = new(1, 1);
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly LogManager _logManager;
	private HistoryIndexSession _session;
	private CancellationTokenSource _sessionCancellation;
	private EventSubscription _sessionEvents;
	private IChartCandleElement _candleElement;
	private int _generation;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();
		EnsureCompiler();

		CandleType.DataType = TimeSpan.FromMinutes(5).TimeFrame();
		BeginDate.SelectedDate = Paths.HistoryBeginDate;
		EndDate.SelectedDate = Paths.HistoryEndDate;
		Expression.Text = $"{Paths.HistoryDefaultSecurity}/2 + {Paths.HistoryDefaultSecurity}*100";

		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener("history-index.log"));
		_logManager.Listeners.Add(new GuiLogListener(Monitor));
		Closing += OnClosing;
	}

	private static void EnsureCompiler()
	{
		if (ConfigManager.TryGetService<ICompiler>() is null)
			ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
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
			var session = HistoryIndexSession.Create(start, stop, CandleType.DataType, Expression.Text);
			var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
			_session = session;
			_sessionCancellation = cancellation;
			try
			{
				var generation = ++_generation;
				PrepareChart(session);
				AttachEvents(session, generation);
				_logManager.Sources.Add(session.Connector);
				SetRunningState(true, "Compiling expression and starting replay...");
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

	private void PrepareChart(HistoryIndexSession session)
	{
		var chart = (IChart)Chart;
		foreach (var area in chart.Areas.ToArray())
			chart.RemoveArea(area);
		var newArea = chart.CreateArea();
		newArea.Title = $"{session.IndexSecurity.Id}: {session.IndexSecurity.Expression}";
		chart.AddArea(newArea);
		Chart.ActiveArea = newArea;
		_candleElement = chart.CreateCandleElement();
		_candleElement.FullTitle = session.IndexSecurity.Expression;
		chart.AddElement(newArea, _candleElement);
		Chart.IsAutoRange = true;
		Progress.Value = 0;
	}

	private void AttachEvents(HistoryIndexSession session, int generation)
	{
		Action<Subscription, ICandleMessage> candleReceived = (subscription, candle) =>
		{
			if (!ReferenceEquals(subscription, session.IndexSubscription))
				return;
			var snapshot = candle is ICloneable cloneable ? (ICandleMessage)cloneable.Clone() : candle;
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					((IChart)Chart).Draw(_candleElement, snapshot);
			});
		};
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

		_sessionEvents = new EventSubscription(
			() =>
			{
				session.Connector.CandleReceived += candleReceived;
				session.Connector.ProgressChanged += progressChanged;
				session.Connector.StateChanged2 += stateChanged;
			},
			() =>
			{
				session.Connector.CandleReceived -= candleReceived;
				session.Connector.ProgressChanged -= progressChanged;
				session.Connector.StateChanged2 -= stateChanged;
			});
		_sessionEvents.Attach();
	}

	private bool IsCurrent(HistoryIndexSession session, int generation)
		=> !_isClosing && ReferenceEquals(_session, session) && _generation == generation;

	private async Task CompleteStoppedSessionAsync(HistoryIndexSession session)
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

	private async Task StopSessionAsync(HistoryIndexSession expected = null)
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

	private async Task TeardownCoreAsync(HistoryIndexSession session)
	{
		_session = null;
		var cancellation = _sessionCancellation;
		_sessionCancellation = null;
		cancellation?.Cancel();
		_generation++;
		_sessionEvents?.Dispose();
		_sessionEvents = null;
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
		Expression.IsEnabled = !running;
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
			if (Monitor is IDisposable disposable)
				disposable.Dispose();
			_lifetimeCancellation.Dispose();
			_sessionGate.Dispose();
			_closeApproved = true;
			Close();
		}
	}
}
