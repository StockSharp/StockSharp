namespace StockSharp.Samples.Testing.Optimization.Avalonia;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Platform.Storage;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Compilation.Roslyn;
using Ecng.Configuration;
using Ecng.Serialization;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Configuration;
using StockSharp.Samples.Avalonia;

public partial class MainWindow : Window
{
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _runGate = new(1, 1);
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly ObservableCollection<OptimizationIterationSnapshot> _rows = [];
	private readonly Dictionary<Guid, int> _rowIndexes = [];
	private OptimizationRun _run;
	private CancellationTokenSource _runCancellation;
	private EventSubscription _runEvents;
	private Task _runTask = Task.CompletedTask;
	private int _generation;
	private int _knownTotal;
	private bool _running;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();
		EnsureCompiler();

		HistoryPath.Text = Paths.HistoryDataPath;
		BeginDate.SelectedDate = Paths.HistoryBeginDate;
		EndDate.SelectedDate = Paths.HistoryEndDate;
		GeneticSettingsEditor.SelectedObject = new GeneticSettings();
		UpdateOptionState();
		Closing += OnClosing;
	}

	private static void EnsureCompiler()
	{
		if (ConfigManager.TryGetService<ICompiler>() is null)
			ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
	}

	private async void OnBrowseClick(object sender, RoutedEventArgs e)
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

	private void OnOptimizerTypeChanged(object sender, RoutedEventArgs e)
		=> UpdateOptionState();

	private void OnRandomModeChanged(object sender, RoutedEventArgs e)
		=> UpdateOptionState();

	private async void OnStartClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await StartRunAsync();
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

	private async Task StartRunAsync()
	{
		await _runGate.WaitAsync(_lifetimeCancellation.Token);
		OptimizationRun createdRun = null;
		CancellationTokenSource createdCancellation = null;
		var launched = false;
		try
		{
			if (_run is not null || _isClosing)
				return;

			var begin = (BeginDate.SelectedDate ?? throw new InvalidOperationException("Select a begin date."))
				.Date.ChangeKind(DateTimeKind.Utc);
			var end = (EndDate.SelectedDate ?? throw new InvalidOperationException("Select an end date."))
				.Date.ChangeKind(DateTimeKind.Utc);
			var mode = Genetic.IsChecked == true ? OptimizationMode.Genetic : OptimizationMode.BruteForce;
			var geneticSettings = new GeneticSettings();
			geneticSettings.Apply((GeneticSettings)GeneticSettingsEditor.GetSelectedObject());
			var run = createdRun = OptimizationRun.Create(HistoryPath.Text, mode, geneticSettings, begin, end);
			var cancellation = createdCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);

			_run = run;
			_runCancellation = cancellation;
			var generation = ++_generation;
			AttachRunEvents(run, generation);
			_rows.Clear();
			_rowIndexes.Clear();
			Statistics.Clear();
			Statistics.ClearColumns();
			_knownTotal = 0;
			Progress.Value = 0;
			ProgressText.Text = "0 / 0 iterations";
			SetRunningState(true, "Starting real historical optimization...");

			var randomMode = mode == OptimizationMode.BruteForce && RandomMode.IsChecked == true;
			var randomCount = decimal.ToInt32(RandomCount.Value ?? 10m);
			_runTask = ExecuteRunAsync(run, cancellation, randomMode, randomCount, generation);
			launched = true;
		}
		catch
		{
			if (!launched)
			{
				_run = null;
				_runCancellation = null;
				_generation++;
				_runEvents?.Dispose();
				_runEvents = null;
				if (createdRun is not null)
					await createdRun.DisposeAsync();
				createdCancellation?.Dispose();
				SetRunningState(false, "Ready");
			}
			throw;
		}
		finally
		{
			_runGate.Release();
		}

		await _runTask;
	}

	private async Task ExecuteRunAsync(
		OptimizationRun run,
		CancellationTokenSource cancellation,
		bool randomMode,
		int randomCount,
		int generation)
	{
		var startedAt = DateTime.UtcNow;
		var outcome = "Completed";
		try
		{
			await run.RunAsync(randomMode, randomCount, cancellation.Token);
			outcome = $"Completed in {DateTime.UtcNow - startedAt:g}";
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
			outcome = "Cancelled";
		}
		catch (Exception error)
		{
			outcome = $"Optimization failed: {error.Message}";
		}
		finally
		{
			await FinishRunAsync(run, cancellation, generation, outcome);
		}
	}

	private void AttachRunEvents(OptimizationRun run, int generation)
	{
		Action<Strategy, OptimizationIterationSnapshot> iterationProgress = (strategy, snapshot) => _uiEvents.Dispatch(() =>
		{
			if (IsCurrent(run, generation))
				UpdateResult(strategy, snapshot);
		});
		Action<int> totalKnown = total => _uiEvents.Dispatch(() =>
		{
			if (!IsCurrent(run, generation))
				return;
			_knownTotal = total;
			ProgressText.Text = $"0 / {total} iterations";
		});
		Action<int, int> completedChanged = (completed, total) => _uiEvents.Dispatch(() =>
		{
			if (!IsCurrent(run, generation))
				return;
			_knownTotal = total;
			Progress.Value = total == 0 ? 0 : Math.Min(100, completed * 100d / total);
			ProgressText.Text = $"{completed} / {total} iterations";
			Status.Text = run.IsPaused ? "Optimization suspended" : "Optimizing historical strategies...";
		});

		_runEvents = new EventSubscription(
			() =>
			{
				run.IterationProgress += iterationProgress;
				run.TotalIterationsKnown += totalKnown;
				run.CompletedCountChanged += completedChanged;
			},
			() =>
			{
				run.IterationProgress -= iterationProgress;
				run.TotalIterationsKnown -= totalKnown;
				run.CompletedCountChanged -= completedChanged;
			});
		_runEvents.Attach();
	}

	private bool IsCurrent(OptimizationRun run, int generation)
		=> !_isClosing && ReferenceEquals(_run, run) && _generation == generation;

	private void UpdateResult(Strategy strategy, OptimizationIterationSnapshot snapshot)
	{
		if (_rowIndexes.TryGetValue(snapshot.StrategyId, out var index))
			_rows[index] = snapshot;
		else
		{
			if (_rowIndexes.Count == 0)
				Statistics.CreateColumns(strategy);
			Statistics.AddStrategy(strategy);
			_rowIndexes.Add(snapshot.StrategyId, _rows.Count);
			_rows.Add(snapshot);
		}

		Statistics.UpdateProgress(strategy, snapshot.Progress);
		if (snapshot.IsCompleted)
			Statistics.UpdatePnL(strategy, snapshot.CurrentTime, snapshot.PnL);
	}

	private async Task FinishRunAsync(
		OptimizationRun run,
		CancellationTokenSource cancellation,
		int generation,
		string outcome)
	{
		await _runGate.WaitAsync();
		try
		{
			if (!ReferenceEquals(_run, run))
				return;

			_run = null;
			_runCancellation = null;
			_generation++;
			_runEvents?.Dispose();
			_runEvents = null;
			try
			{
				await run.DisposeAsync();
			}
			finally
			{
				cancellation.Dispose();
				if (!_isClosing)
				{
					if (outcome.StartsWith("Completed", StringComparison.Ordinal))
						Progress.Value = 100;
					SetRunningState(false, outcome);
				}
			}
		}
		finally
		{
			_runGate.Release();
		}
	}

	private void OnStopClick(object sender, RoutedEventArgs e)
	{
		Status.Text = "Cancelling optimization...";
		_runCancellation?.Cancel();
	}

	private async void OnPauseClick(object sender, RoutedEventArgs e)
	{
		var run = _run;
		if (run is null)
			return;

		PauseButton.IsEnabled = false;
		try
		{
			if (run.IsPaused)
				await run.ResumeAsync();
			else
				await run.PauseAsync();

			if (ReferenceEquals(_run, run))
			{
				PauseButton.Content = run.IsPaused ? "Continue" : "Pause";
				Status.Text = run.IsPaused ? "Optimization suspended" : "Optimizing historical strategies...";
				ProgressText.Text = $"{_rows.Count(row => row.IsCompleted)} / {_knownTotal} iterations";
			}
		}
		catch (Exception error)
		{
			if (!_isClosing)
				Status.Text = $"Pause failed: {error.Message}";
		}
		finally
		{
			if (ReferenceEquals(_run, run))
				PauseButton.IsEnabled = true;
		}
	}

	private void SetRunningState(bool running, string status)
	{
		_running = running;
		StartButton.IsEnabled = !running;
		PauseButton.IsEnabled = running;
		PauseButton.Content = "Pause";
		StopButton.IsEnabled = running;
		HistoryPath.IsEnabled = !running;
		BrowseButton.IsEnabled = !running;
		BeginDate.IsEnabled = !running;
		EndDate.IsEnabled = !running;
		OptimizationOptions.IsEnabled = !running;
		Status.Text = status;
		UpdateOptionState();
	}

	private void UpdateOptionState()
	{
		var canEdit = !_running;
		var isBruteForce = BruteForce.IsChecked == true;
		RandomMode.IsEnabled = canEdit && isBruteForce;
		RandomCount.IsEnabled = canEdit && isBruteForce && RandomMode.IsChecked == true;
		GeneticSettingsEditor.IsEnabled = canEdit && !isBruteForce;
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
		_runCancellation?.Cancel();
		try
		{
			try
			{
				await _runTask;
			}
			catch
			{
			}

			await _runGate.WaitAsync();
			try
			{
				if (_run is not null)
					await _run.DisposeAsync();
				_run = null;
				_runEvents?.Dispose();
				_runEvents = null;
				_runCancellation?.Dispose();
				_runCancellation = null;
			}
			finally
			{
				_runGate.Release();
			}
		}
		finally
		{
			Closing -= OnClosing;
			Statistics.Dispose();
			_uiEvents.Dispose();
			_lifetimeCancellation.Dispose();
			_runGate.Dispose();
			_closeApproved = true;
			Close();
		}
	}
}
