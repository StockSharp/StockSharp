namespace StockSharp.Samples.Testing.History.Avalonia;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Platform.Storage;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Drawing;
using Ecng.IO;
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

using DrawingColor = System.Drawing.Color;

public partial class MainWindow : Window
{
	private sealed class FeedPresentation
	{
		public FeedPresentation(
			CheckBox enabled,
			ProgressBar progress,
			TabItem tab,
			IChart chart,
			EquityCurveChart equity,
			StatisticParameterGrid statistics,
			EquityCurveChart position,
			HistoryFeedDefinition definition)
		{
			Enabled = enabled;
			Progress = progress;
			Tab = tab;
			Chart = chart;
			Equity = equity;
			Statistics = statistics;
			Position = position;
			Definition = definition;
		}

		public CheckBox Enabled { get; }

		public ProgressBar Progress { get; }

		public TabItem Tab { get; }

		public IChart Chart { get; }

		public EquityCurveChart Equity { get; }

		public StatisticParameterGrid Statistics { get; }

		public EquityCurveChart Position { get; }

		public HistoryFeedDefinition Definition { get; }

		public IChartBandElement PnlCurve { get; set; }

		public IChartBandElement RealizedPnlCurve { get; set; }

		public IChartBandElement UnrealizedPnlCurve { get; set; }

		public IChartBandElement CommissionCurve { get; set; }

		public IChartBandElement PositionCurve { get; set; }
	}

	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _sessionGate = new(1, 1);
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly List<EventSubscription> _runEvents = [];
	private readonly LogManager _logManager;
	private readonly FeedPresentation[] _presentations;
	private HistoryTestingSession _session;
	private DateTime _startedAt;
	private int _generation;
	private bool _isPaused;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();
		EnableCompactTransactionMarkers(
			CandlesChart,
			TicksChart,
			TicksAndDepthsChart,
			DepthsChart,
			CandlesAndDepthsChart,
			OrderLogChart,
			LastTradeChart,
			SpreadChart);

		HistoryPath.Text = Paths.HistoryDataPath;
		SecurityId.Text = Paths.HistoryDefaultSecurity;
		BeginDate.SelectedDate = Paths.HistoryBeginDate;
		EndDate.SelectedDate = Paths.HistoryEndDate;
		CandleType.DataType = TimeSpan.FromMinutes(1).TimeFrame();

		_presentations =
		[
			new(
				CandlesCheckBox,
				CandlesProgress,
				CandlesTab,
				CandlesChart,
				CandlesEquity,
				CandlesStatistics,
				CandlesPosition,
				new("Candles", DrawingColor.DarkBlue) { CandleType = CandleType.DataType }),
			new(
				TicksCheckBox,
				TicksProgress,
				TicksTab,
				TicksChart,
				TicksEquity,
				TicksStatistics,
				TicksPosition,
				new("Ticks", DrawingColor.DarkGreen) { UseTicks = true }),
			new(
				TicksAndDepthsCheckBox,
				TicksAndDepthsProgress,
				TicksAndDepthsTab,
				TicksAndDepthsChart,
				TicksAndDepthsEquity,
				TicksAndDepthsStatistics,
				TicksAndDepthsPosition,
				new("Ticks + Depths", DrawingColor.Red) { UseTicks = true, UseMarketDepth = true }),
			new(
				DepthsCheckBox,
				DepthsProgress,
				DepthsTab,
				DepthsChart,
				DepthsEquity,
				DepthsStatistics,
				DepthsPosition,
				new("Market Depths", DrawingColor.OrangeRed) { UseMarketDepth = true }),
			new(
				CandlesAndDepthsCheckBox,
				CandlesAndDepthsProgress,
				CandlesAndDepthsTab,
				CandlesAndDepthsChart,
				CandlesAndDepthsEquity,
				CandlesAndDepthsStatistics,
				CandlesAndDepthsPosition,
				new("Candles + Depths", DrawingColor.Cyan)
				{
					CandleType = CandleType.DataType,
					UseMarketDepth = true,
				}),
			new(
				OrderLogCheckBox,
				OrderLogProgress,
				OrderLogTab,
				OrderLogChart,
				OrderLogEquity,
				OrderLogStatistics,
				OrderLogPosition,
				new("Order Log", DrawingColor.CornflowerBlue) { UseOrderLog = true }),
			new(
				LastTradeCheckBox,
				LastTradeProgress,
				LastTradeTab,
				LastTradeChart,
				LastTradeEquity,
				LastTradeStatistics,
				LastTradePosition,
				new("Level1 Last Trade", DrawingColor.Aquamarine)
				{
					UseLevel1 = true,
					BuildField = Level1Fields.LastTradePrice,
				}),
			new(
				SpreadCheckBox,
				SpreadProgress,
				SpreadTab,
				SpreadChart,
				SpreadEquity,
				SpreadStatistics,
				SpreadPosition,
				new("Level1 Spread", DrawingColor.Aquamarine)
				{
					UseLevel1 = true,
					BuildField = Level1Fields.SpreadMiddle,
				}),
		];

		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener("history-testing.log"));
		Closing += OnClosing;
		UpdateModeVisibility();
		UpdateGeneratorControls();
	}

	private static void EnableCompactTransactionMarkers(params object[] charts)
	{
		foreach (var chart in charts)
		{
			var property = chart.GetType().GetProperty("CompactTransactionMarkers");
			if (property?.CanWrite == true && property.PropertyType == typeof(bool))
				property.SetValue(chart, true);
		}
	}

	private async void OnBrowseClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select StockSharp history folder",
				AllowMultiple = false,
			});
			var path = folders.FirstOrDefault()?.TryGetLocalPath();
			if (!string.IsNullOrWhiteSpace(path))
				HistoryPath.Text = path;
		}
		catch (Exception error)
		{
			if (!_isClosing)
				Status.Text = $"Folder selection failed: {error.Message}";
		}
	}

	private void OnModeChanged(object sender, RoutedEventArgs e)
		=> UpdateModeVisibility();

	private void OnGeneratorChanged(object sender, RoutedEventArgs e)
		=> UpdateGeneratorControls();

	private void UpdateModeVisibility()
	{
		foreach (var presentation in _presentations)
			presentation.Tab.IsVisible = presentation.Enabled.IsChecked == true;

		var firstVisible = _presentations.FirstOrDefault(presentation => presentation.Tab.IsVisible);
		ModesTab.IsVisible = firstVisible is not null;
		if (firstVisible is not null &&
			ModesTab.SelectedItem is TabItem selected &&
			!selected.IsVisible)
		{
			ModesTab.SelectedItem = firstVisible.Tab;
		}
		else if (firstVisible is not null && ModesTab.SelectedItem is null)
		{
			ModesTab.SelectedItem = firstVisible.Tab;
		}

		if (_session is null && !_isClosing)
			StartButton.IsEnabled = firstVisible is not null;
	}

	private void UpdateGeneratorControls()
	{
		var enabled = _session is null && GenerateDepths.IsChecked == true;
		MaxDepth.IsEnabled = enabled;
		MaxVolume.IsEnabled = enabled;
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

	private async void OnPauseClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await TogglePauseAsync();
		}
		catch (Exception error)
		{
			if (!_isClosing)
				Status.Text = $"Pause/resume failed: {error.Message}";
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

			var selectedPresentations = _presentations
				.Where(presentation => presentation.Enabled.IsChecked == true)
				.ToArray();
			if (selectedPresentations.Length == 0)
				throw new InvalidOperationException("Select at least one history feed.");
			if (string.IsNullOrWhiteSpace(HistoryPath.Text) || !Directory.Exists(HistoryPath.Text))
				throw new DirectoryNotFoundException("Select an existing StockSharp history folder.");
			if (string.IsNullOrWhiteSpace(SecurityId.Text))
				throw new InvalidOperationException("Enter a security identifier.");

			var startDate = (BeginDate.SelectedDate
				?? throw new InvalidOperationException("Select a begin date.")).Date.ChangeKind(DateTimeKind.Utc);
			var stopDate = (EndDate.SelectedDate
				?? throw new InvalidOperationException("Select an end date.")).Date.ChangeKind(DateTimeKind.Utc);
			if (stopDate < startDate)
				throw new InvalidOperationException("The end date must not precede the begin date.");

			var maxDepth = ParsePositiveInt(MaxDepth.Text, "maximum market depth");
			var maxVolume = ParsePositiveInt(MaxVolume.Text, "maximum generated volume");
			foreach (var presentation in selectedPresentations)
			{
				if (ReferenceEquals(presentation.Enabled, CandlesCheckBox) ||
					ReferenceEquals(presentation.Enabled, CandlesAndDepthsCheckBox))
				{
					presentation.Definition.CandleType = CandleType.DataType;
				}
			}

			var session = HistoryTestingSession.Create(
				new HistoryTestingOptions
				{
					HistoryPath = HistoryPath.Text,
					SecurityId = SecurityId.Text,
					StartDate = startDate,
					StopDate = stopDate,
					DebugLog = DebugLog.IsChecked == true,
					GenerateDepths = GenerateDepths.IsChecked == true,
					MaxDepth = maxDepth,
					MaxVolume = maxVolume,
					UseServerSideStops = ServerStops.IsChecked == true,
				},
				selectedPresentations.Select(presentation => presentation.Definition).ToArray());

			_session = session;
			_startedAt = DateTime.UtcNow;
			var generation = ++_generation;
			try
			{
				foreach (var presentation in selectedPresentations)
				{
					var run = session.Runs.Single(item => ReferenceEquals(item.Definition, presentation.Definition));
					PreparePresentation(presentation, run.Strategy);
					AttachRunEvents(session, run, presentation, generation);
					_logManager.Sources.Add(run.Connector);
					_logManager.Sources.Add(run.Strategy);
				}

				SetRunningState(true, $"Starting {session.Runs.Count} historical feed(s)...");
				await session.StartAsync(_lifetimeCancellation.Token);
				if (IsCurrent(session, generation))
					Status.Text = $"Running {session.Runs.Count} historical feed(s)";
			}
			catch
			{
				await TeardownSessionCoreAsync(session, false);
				throw;
			}
		}
		finally
		{
			_sessionGate.Release();
		}
	}

	private void PreparePresentation(FeedPresentation presentation, Strategy strategy)
	{
		presentation.Chart.ClearAreas();
		presentation.Equity.Clear();
		presentation.Position.Clear();
		presentation.Statistics.Parameters.Clear();
		presentation.Progress.Value = 0;
		presentation.Chart.IsInteracted = false;
		presentation.Chart.IsAutoRange = true;

		presentation.PnlCurve = presentation.Equity.CreateCurve(
			$"P&L {presentation.Definition.Name}",
			DrawingColor.Green,
			DrawingColor.Red,
			DrawStyles.Area);
		presentation.RealizedPnlCurve = presentation.Equity.CreateCurve(
			$"realized {presentation.Definition.Name}",
			DrawingColor.Black,
			DrawStyles.Line);
		presentation.UnrealizedPnlCurve = presentation.Equity.CreateCurve(
			$"unrealized {presentation.Definition.Name}",
			DrawingColor.DarkGray,
			DrawStyles.Line);
		presentation.CommissionCurve = presentation.Equity.CreateCurve(
			$"commission {presentation.Definition.Name}",
			DrawingColor.Red,
			DrawStyles.DashedLine);
		presentation.PositionCurve = presentation.Position.CreateCurve(
			presentation.Definition.Name,
			presentation.Definition.CurveColor,
			DrawStyles.Line);

		strategy.SetChart(presentation.Chart);
		presentation.Statistics.Parameters.AddRange(strategy.StatisticManager.Parameters);
	}

	private void AttachRunEvents(
		HistoryTestingSession session,
		HistoryFeedRun run,
		FeedPresentation presentation,
		int generation)
	{
		Action<int> progressChanged = progress => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(session, generation))
				presentation.Progress.Value = progress;
		});
		Action<ChannelStates> stateChanged = state => _uiEvents.Dispatch(() =>
		{
			if (!IsCurrent(session, generation))
				return;

			Status.Text = $"{presentation.Definition.Name}: {state}";
			if (state == ChannelStates.Stopped)
			{
				presentation.Chart.IsAutoRange = false;
				if (run.Connector.IsFinished)
					presentation.Progress.Value = presentation.Progress.Maximum;
				_ = CompleteStoppedRunAsync(session, run, generation);
			}
		});
		Action<Subscription, Portfolio, DateTime, decimal, decimal?, decimal?> pnlReceived =
			(_, _, time, realized, unrealized, commission) => _uiEvents.Dispatch(() =>
			{
				if (!IsCurrent(session, generation))
					return;

				var data = presentation.Equity.CreateData();
				data.Group(time)
					.Add(presentation.PnlCurve, realized - (commission ?? 0m))
					.Add(presentation.RealizedPnlCurve, realized)
					.Add(presentation.UnrealizedPnlCurve, unrealized ?? 0m)
					.Add(presentation.CommissionCurve, commission ?? 0m);
				presentation.Equity.Draw(data);
			});
		Action<Subscription, Position> positionReceived = (_, position) => _uiEvents.Dispatch(() =>
		{
			if (!IsCurrent(session, generation))
				return;

			var data = presentation.Position.CreateData();
			data.Group(position.LocalTime).Add(presentation.PositionCurve, position.CurrentValue);
			presentation.Position.Draw(data);
		});

		var events = new EventSubscription(
			() =>
			{
				run.Connector.ProgressChanged += progressChanged;
				run.Connector.StateChanged2 += stateChanged;
				run.Strategy.PnLReceived2 += pnlReceived;
				run.Strategy.PositionReceived += positionReceived;
			},
			() =>
			{
				run.Connector.ProgressChanged -= progressChanged;
				run.Connector.StateChanged2 -= stateChanged;
				run.Strategy.PnLReceived2 -= pnlReceived;
				run.Strategy.PositionReceived -= positionReceived;
			});
		events.Attach();
		_runEvents.Add(events);
	}

	private bool IsCurrent(HistoryTestingSession session, int generation)
		=> !_isClosing && ReferenceEquals(_session, session) && _generation == generation;

	private async Task CompleteStoppedRunAsync(
		HistoryTestingSession session,
		HistoryFeedRun run,
		int generation)
	{
		try
		{
			await session.StopStrategyAsync(run, CancellationToken.None);
		}
		catch (Exception error)
		{
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrent(session, generation))
					Status.Text = $"{run.Definition.Name} strategy stop failed: {error.Message}";
			});
		}

		if (!session.Runs.All(item => item.Connector.State == ChannelStates.Stopped))
			return;

		try
		{
			await StopSessionAsync(session, true);
		}
		catch (Exception error)
		{
			_uiEvents.Dispatch(() =>
			{
				if (!_isClosing)
					Status.Text = $"Completed-session cleanup failed: {error.Message}";
			});
		}
	}

	private async Task TogglePauseAsync()
	{
		await _sessionGate.WaitAsync(_lifetimeCancellation.Token);
		try
		{
			var session = _session;
			if (session is null || _isClosing)
				return;

			PauseButton.IsEnabled = false;
			if (_isPaused)
			{
				await session.ResumeAsync();
				_isPaused = false;
				PauseButton.Content = "Pause";
				Status.Text = "Running";
			}
			else
			{
				await session.SuspendAsync();
				_isPaused = true;
				PauseButton.Content = "Resume";
				Status.Text = "Suspended";
			}
		}
		finally
		{
			if (_session is not null && !_isClosing)
				PauseButton.IsEnabled = true;
			_sessionGate.Release();
		}
	}

	private async Task StopSessionAsync(
		HistoryTestingSession expected = null,
		bool completed = false)
	{
		await _sessionGate.WaitAsync();
		try
		{
			var session = _session;
			if (session is null || expected is not null && !ReferenceEquals(expected, session))
				return;

			await TeardownSessionCoreAsync(session, completed);
		}
		finally
		{
			_sessionGate.Release();
		}
	}

	private async Task TeardownSessionCoreAsync(HistoryTestingSession session, bool completed)
	{
		_session = null;
		_generation++;
		foreach (var events in _runEvents)
			events.Dispose();
		_runEvents.Clear();

		foreach (var run in session.Runs)
		{
			run.Strategy.SetChart(null);
			TryRemoveLogSource(run.Strategy);
			TryRemoveLogSource(run.Connector);
		}

		try
		{
			await session.DisposeAsync();
		}
		finally
		{
			if (!_isClosing)
			{
				var status = completed
					? $"Completed {session.Runs.Count} feed(s) in {DateTime.UtcNow - _startedAt:g}"
					: "Ready";
				SetRunningState(false, status);
			}
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
			// Session disposal remains authoritative even if a listener rejects source removal.
		}
	}

	private void SetRunningState(bool running, string status)
	{
		StartButton.IsEnabled = !running && _presentations.Any(presentation => presentation.Enabled.IsChecked == true);
		PauseButton.IsEnabled = running;
		StopButton.IsEnabled = running;
		HistoryPath.IsEnabled = !running;
		SecurityId.IsEnabled = !running;
		BeginDate.IsEnabled = !running;
		EndDate.IsEnabled = !running;
		CandleType.IsEnabled = !running;
		DebugLog.IsEnabled = !running;
		ServerStops.IsEnabled = !running;
		GenerateDepths.IsEnabled = !running;
		foreach (var presentation in _presentations)
			presentation.Enabled.IsEnabled = !running;

		_isPaused = false;
		PauseButton.Content = "Pause";
		Status.Text = status;
		UpdateGeneratorControls();
	}

	private static int ParsePositiveInt(string text, string fieldName)
	{
		if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) || value <= 0)
			throw new InvalidOperationException($"Enter a positive {fieldName}.");
		return value;
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
			// Continue the close handshake after every owned dependency had a disposal attempt.
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
				// Continue releasing UI controls.
			}

			foreach (var presentation in _presentations)
			{
				DisposeControl(presentation.Chart);
				DisposeControl(presentation.Equity);
				DisposeControl(presentation.Statistics);
				DisposeControl(presentation.Position);
			}

			_lifetimeCancellation.Dispose();
			_sessionGate.Dispose();
			_closeApproved = true;
			Close();
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
			// A single control must not prevent the rest of the window from closing.
		}
	}
}
