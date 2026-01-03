namespace StockSharp.Samples.Testing.Optimization;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

using Ecng.Xaml;
using Ecng.Common;
using Ecng.Serialization;
using Ecng.Compilation;
using Ecng.Configuration;
using Ecng.Compilation.Roslyn;
using Ecng.ComponentModel;
using Ecng.Collections;
using Ecng.Logging;
using Ecng.IO;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Algo.Commissions;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Localization;
using StockSharp.Configuration;
using StockSharp.Xaml;

public partial class MainWindow
{
	private DateTime _startEmulationTime;

	private BaseOptimizer _optimizer;

	public MainWindow()
	{
		InitializeComponent();

		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
		HistoryPath.Folder = Paths.HistoryDataPath;
		GeneticSettings.SelectedObject = new GeneticSettings();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.ApplyDefaultTheme();
	}

	private void StartBtnClick(object sender, RoutedEventArgs e)
	{
		if (_optimizer != null)
		{
			_optimizer.Resume();
			return;
		}

		OptimizeTypeGrid.IsEnabled = false;

		var folder = HistoryPath.Folder;

		if (folder.IsEmpty() || !Directory.Exists(folder))
		{
			MessageBox.Show(this, LocalizedStrings.WrongPath);
			return;
		}

		TestingProcess.Value = 0;
		TestingProcessText.Text = string.Empty;

		Stat.Clear();
		Stat.ClearColumns();
		Stat.CreateColumns(new History.SmaStrategy());

		var logManager = new LogManager();
		var fileLogListener = new FileLogListener("sample.log");
		logManager.Listeners.Add(fileLogListener);

		// storage to historical data
		var storageRegistry = new StorageRegistry
		{
			// set historical path
			DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, folder)
		};

		// create test security
		var security = new Security
		{
			Id = Paths.HistoryDefaultSecurity, // sec id has the same name as folder with historical data
			PriceStep = 0.01m,
		};

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		// test portfolio
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		if (BruteForce.IsChecked == true)
			_optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);
		else
			_optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry, Paths.FileSystem);

		var settings = _optimizer.EmulationSettings;

		// set max possible iteration to 100
		settings.MaxIterations = 100;

		// 1 cent commission for trade
		settings.CommissionRules =
		[
			new CommissionTradeRule { Value = 0.01m },
		];

		// count of parallel testing strategies
		// if not set, then CPU count * 2
		//_optimizer.EmulationSettings.BatchSize = 1;

		// settings caching mode non security optimized param
		_optimizer.AdapterCache = new();

		var ids = new SynchronizedSet<Guid>();

		// handle single iteration progress
		_optimizer.SingleProgressChanged += (s, a, p) =>
		{
			if (ids.TryAdd(s.Id))
				this.GuiAsync(() => Stat.AddStrategy(s));
			else
				this.GuiAsync(() => Stat.UpdateProgress(s, p));
		};

		// handle historical time for update ProgressBar
		_optimizer.TotalProgressChanged += (progress, duration, remaining) => this.GuiAsync(() =>
		{
			TestingProcess.Value = progress;

			var remainingSeconds = remaining == TimeSpan.MaxValue ? "unk" : ((int)remaining.TotalSeconds).To<string>();
			TestingProcessText.Text = $"{progress}% | {(int)duration.TotalSeconds} sec left | {remainingSeconds} sec rem";
		});

		_optimizer.StateChanged += (oldState, newState) =>
		{
			this.GuiAsync(() =>
			{
				switch (newState)
				{
					case ChannelStates.Stopping:
					case ChannelStates.Starting:
					case ChannelStates.Suspending:
						SetIsEnabled(false, false, false, false);
						break;
					case ChannelStates.Stopped:
						SetIsEnabled(true, false, false, true);

						if (!_optimizer.IsCancelled)
						{
							TestingProcess.Value = TestingProcess.Maximum;
							MessageBox.Show(this, LocalizedStrings.CompletedIn.Put(DateTime.UtcNow - _startEmulationTime));
						}
						else
							MessageBox.Show(this, LocalizedStrings.Cancelled);

						_optimizer = null;

						break;
					case ChannelStates.Started:
						SetIsEnabled(false, true, true, false);
						break;
					case ChannelStates.Suspended:
						SetIsEnabled(true, false, true, false);
						break;
					default:
						throw new ArgumentOutOfRangeException(newState.ToString());
				}
			});
		};

		_startEmulationTime = DateTime.UtcNow;

		// Create base strategy with optimization ranges configured
		var strategy = new History.SmaStrategy
		{
			Volume = 1,
			Security = security,
			Portfolio = portfolio,

			// by default interval is 1 min,
			// it is excessively for time range with several months
			UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>(),
		};

		// Configure optimization ranges on parameters
		var longParam = (StrategyParam<int>)strategy.Parameters[nameof(strategy.LongSma)];
		var shortParam = (StrategyParam<int>)strategy.Parameters[nameof(strategy.ShortSma)];
		var tfParam = (StrategyParam<TimeSpan?>)strategy.Parameters[nameof(strategy.CandleTimeFrame)];

		longParam.SetOptimize(50, 100, 5);
		shortParam.SetOptimize(20, 40, 1);
		tfParam.SetOptimize(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5));

		var optimizeParams = new IStrategyParam[] { longParam, shortParam, tfParam };

		if (_optimizer is BruteForceOptimizer btOptimizer)
		{
			var isRandomMode = RandomMode.IsChecked == true;
			var randomCount = isRandomMode ? int.Parse(RandomCount.Text) : 0;

			IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> strategies;
			int totalCount;

			if (isRandomMode)
			{
				// Random mode
				strategies = strategy.ToBruteForceRandom(optimizeParams, randomCount, out _, out totalCount);
			}
			else
			{
				// Step-based mode
				strategies = strategy.ToBruteForce(optimizeParams, out _, out totalCount);
			}

			// start emulation
			btOptimizer.Start(startTime, stopTime, strategies, totalCount);
		}
		else
		{
			var go = (GeneticOptimizer)_optimizer;
			go.Settings.Apply((GeneticSettings)GeneticSettings.SelectedObject);

			// Convert parameters to genetic format
			var geneticParams = strategy.ToGeneticParameters([
				(tfParam, new[] { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) }), // explicit values for timeframe
				(longParam, null), // use range from SetOptimize
				(shortParam, null), // use range from SetOptimize
			]);

			go.Start(startTime, stopTime, strategy, geneticParams);
		}
	}

	private void SetIsEnabled(bool canStart, bool canSuspend, bool canStop, bool canType)
	{
		this.GuiAsync(() =>
		{
			StopBtn.IsEnabled = canStop;
			StartBtn.IsEnabled = canStart;
			PauseBtn.IsEnabled = canSuspend;
			OptimizeTypeGrid.IsEnabled = canType;
		});
	}

	private void StopBtnClick(object sender, RoutedEventArgs e)
	{
		_optimizer.Stop();
	}

	private void PauseBtnClick(object sender, RoutedEventArgs e)
	{
		_optimizer.Suspend();
	}
}
