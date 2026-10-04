namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.IO;
using Ecng.Logging;
using Ecng.Serialization;
using Ecng.Xaml;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Xaml;

public partial class StrategiesWindow : Window, IDisposable
{
	private readonly Connector _connector;
	private readonly PortfolioDataSource _portfolios;
	private readonly Func<string, Security> _resolveSecurity;
	private readonly Func<string, Portfolio> _resolvePortfolio;
	private readonly LogManager _logManager;
	private readonly IFileSystem _fileSystem;
	private readonly string _strategiesDirectory;
	private readonly StrategiesDashboard _dashboard;
	private readonly List<Strategy> _strategies = [];
	private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private int _generation = 1;
	private bool _loaded;
	private bool _disposed;

	public StrategiesWindow(
		Connector connector,
		PortfolioDataSource portfolios,
		Func<string, Security> resolveSecurity,
		Func<string, Portfolio> resolvePortfolio,
		LogManager logManager,
		IFileSystem fileSystem,
		string dataPath)
	{
		_connector = connector ?? throw new ArgumentNullException(nameof(connector));
		_portfolios = portfolios ?? throw new ArgumentNullException(nameof(portfolios));
		_resolveSecurity = resolveSecurity ?? throw new ArgumentNullException(nameof(resolveSecurity));
		_resolvePortfolio = resolvePortfolio ?? throw new ArgumentNullException(nameof(resolvePortfolio));
		_logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		_strategiesDirectory = Path.Combine(dataPath ?? throw new ArgumentNullException(nameof(dataPath)), "Strategies");

		InitializeComponent();
		_dashboard = this.FindControl<StrategiesDashboard>(nameof(StrategiesDashboard));
		_dashboard.SecurityProvider = _connector;
		_dashboard.Portfolios = _portfolios;
	}

	public async Task LoadStrategiesAsync(CancellationToken cancellationToken)
	{
		await _lifecycleGate.WaitAsync(cancellationToken);
		try
		{
			if (_loaded || _disposed)
				return;

			_fileSystem.CreateDirectory(_strategiesDirectory);
			foreach (var fileName in _strategiesDirectory.EnumerateConfigs(_fileSystem))
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					var storage = await fileName.DeserializeAsync<SettingsStorage>(_fileSystem, cancellationToken);
					var strategy = storage is null
						? null
						: await TerminalStrategyPersistence.LoadAsync(storage, _resolveSecurity, _resolvePortfolio, cancellationToken);
					if (strategy is not null)
						AddStrategy(strategy);
				}
				catch (Exception error)
				{
					error.LogError();
				}
			}

			_loaded = true;
		}
		finally
		{
			_lifecycleGate.Release();
		}
	}


	private async void OnAddQuotingClick(object sender, RoutedEventArgs e)
	{
		var generation = Volatile.Read(ref _generation);
		Strategy quoting = null;
		try
		{
			quoting = new StockSharp.Samples.Strategies.LiveTerminal.MarketQuotingProcessorStrategy();
			using var window = new StrategyEditWindow(_connector, _portfolios)
			{
				Strategy = quoting,
			};
			if (!await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token) || !IsCurrent(generation))
				return;

			AddStrategy(quoting);
			var added = quoting;
			quoting = null;
			await SaveStrategyAsync(added, _lifetimeCancellation.Token);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			error.LogError();
		}
		finally
		{
			quoting?.Dispose();
		}
	}

	private void AddStrategy(Strategy strategy)
	{
		strategy.Connector = _connector;
		strategy.DisposeOnStop = false;
		var item = new StrategiesDashboardItem(strategy);
		item.SettingsCommand = new DelegateCommand(
			_ => EditStrategyAsync(strategy, Volatile.Read(ref _generation)),
			_ => strategy.ProcessState == ProcessStates.Stopped && !_disposed);
		_dashboard.Items.Add(item);
		_strategies.Add(strategy);
		_logManager.Sources.Add(strategy);
	}

	private async void EditStrategyAsync(Strategy strategy, int generation)
	{
		if (!IsCurrent(strategy, generation))
			return;

		try
		{
			using var edited = await strategy.CloneAsync(_lifetimeCancellation.Token);
			using var window = new StrategyEditWindow(_connector, _portfolios)
			{
				Strategy = edited,
			};
			if (!await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token) || !IsCurrent(strategy, generation))
				return;

			var id = strategy.Id;
			await strategy.ApplyAsync(edited, _lifetimeCancellation.Token);
			strategy.Id = id;
			await SaveStrategyAsync(strategy, _lifetimeCancellation.Token);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			error.LogError();
		}
	}

	private async Task SaveStrategyAsync(Strategy strategy, CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		var storage = await strategy.SaveEntireAsync(false, cancellationToken);
		await storage.SerializeAsync(_fileSystem, Path.Combine(_strategiesDirectory, $"{strategy.Id}{Paths.DefaultSettingsExt}"), true, cancellationToken);
	}

	public async Task StopAllAsync()
	{
		await _lifecycleGate.WaitAsync();
		try
		{
			foreach (var strategy in _strategies.ToArray())
			{
				if (strategy.ProcessState != ProcessStates.Stopped)
					await strategy.StopAsync();
			}
		}
		finally
		{
			_lifecycleGate.Release();
		}
	}

	private bool IsCurrent(int generation)
		=> !_disposed && generation == Volatile.Read(ref _generation);

	private bool IsCurrent(Strategy strategy, int generation)
		=> IsCurrent(generation) && _strategies.Contains(strategy);

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		Interlocked.Increment(ref _generation);
		_lifetimeCancellation.Cancel();
		_dashboard.Dispose();
		foreach (var strategy in _strategies.ToArray())
		{
			try
			{
				_logManager.Sources.Remove(strategy);
			}
			catch
			{
			}
			strategy.Dispose();
		}
		_strategies.Clear();
		_portfolios.Dispose();
		_lifetimeCancellation.Dispose();
		_lifecycleGate.Dispose();
	}
}
