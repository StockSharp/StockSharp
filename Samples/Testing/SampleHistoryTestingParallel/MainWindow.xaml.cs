#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleHistoryTestingParallel.SampleHistoryTestingParallelPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleHistoryTestingParallel
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;
	using System.Collections.Generic;

	using Ecng.Xaml;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Optimization;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Configuration;
	using StockSharp.Algo;

	public partial class MainWindow
	{
		private DateTime _startEmulationTime;

		private BaseOptimizer _optimizer;

		public MainWindow()
		{
			InitializeComponent();

			HistoryPath.Folder = Paths.HistoryDataPath;
			GeneticSettings.SelectedObject = new GeneticSettings();
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			if (_optimizer != null)
			{
				_optimizer.Resume();
				return;
			}

			OptimizeTypeGrid.IsEnabled = false;

			if (HistoryPath.Folder.IsEmpty() || !Directory.Exists(HistoryPath.Folder))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			TestingProcess.Value = 0;
			TestingProcessText.Text = string.Empty;
			Stat.Clear();

			var logManager = new LogManager();
			var fileLogListener = new FileLogListener("sample.log");
			logManager.Listeners.Add(fileLogListener);

			(int min, int max) longRange = new(50, 100);
			(int min, int max) shortRange = new(5, 40);

			// SMA periods
			var periods = new List<(int longMa, int shortMa, Color color)>();

			for (var l = longRange.max; l >= longRange.min; l -= 1)
			{
				for (var s = shortRange.max; s >= shortRange.min; s -= 1)
				{
					periods.Add((l, s, Color.FromRgb((byte)RandomGen.GetInt(255), (byte)RandomGen.GetInt(255), (byte)RandomGen.GetInt(255))));
				}
			}

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Folder)
			};

			var timeFrame = TimeSpan.FromMinutes(1);

			// create test security
			var security = new Security
			{
				Id = "SBER@TQBR", // sec id has the same name as folder with historical data
				Code = "SBER",
				Name = "SBER",
				Board = ExchangeBoard.Micex,
				PriceStep = 0.01m,
			};

			var startTime = Paths.HistoryBeginDate;
			var stopTime = Paths.HistoryEndDate;

			// test portfolio
			var portfolio = Portfolio.CreateSimulator();

			var secProvider = new CollectionSecurityProvider(new[] { security });
			var pfProvider = new CollectionPortfolioProvider(new[] { portfolio });

			if (BruteForce.IsChecked == true)
				_optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);
			else
				_optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry);

			_optimizer.EmulationSettings.MarketTimeChangedInterval = timeFrame;
			_optimizer.EmulationSettings.StartTime = startTime;
			_optimizer.EmulationSettings.StopTime = stopTime;

			// count of parallel testing strategies
			// if not set, then CPU count * 2
			//_optimizer.EmulationSettings.BatchSize = 3;

			// settings caching mode non security optimized param
			_optimizer.AdapterCache = new();

			// handle single iteration progress
			_optimizer.SingleProgressChanged += (s, a, p) =>
			{
				if (p != 100)
					return;

				this.GuiAsync(() => Stat.AddStrategy(s));
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
								MessageBox.Show(this, LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime));
							}
							else
								MessageBox.Show(this, LocalizedStrings.cancelled);

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

			_startEmulationTime = DateTime.Now;

			// set max possible iteration to 100
			_optimizer.EmulationSettings.MaxIterations = 100;

			if (_optimizer is BruteForceOptimizer btOptimizer)
			{
				var strategies = periods
					.Select(period =>
					{
						// create strategy based SMA
						var strategy = new SampleHistoryTesting.SmaStrategy
						{
							ShortSma = period.shortMa,
							LongSma = period.longMa,

							Volume = 1,
							Security = security,
							Portfolio = portfolio,
							//Connector = connector,

							// by default interval is 1 min,
							// it is excessively for time range with several months
							UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>(),

							Name = $"L={period.longMa} S={period.shortMa}",
						};

						return ((Strategy)strategy, new IStrategyParam[]
						{
							strategy.Parameters.GetByName(nameof(strategy.ShortSma)),
							strategy.Parameters.GetByName(nameof(strategy.LongSma)),
						});
					});

				// start emulation
				btOptimizer.Start(strategies, periods.Count);
			}
			else
			{
				var strategy = new SampleHistoryTesting.SmaStrategy
				{
					Volume = 1,
					Security = security,
					Portfolio = portfolio,
					//Connector = connector,

					// by default interval is 1 min,
					// it is excessively for time range with several months
					UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>(),
				};

				var go = (GeneticOptimizer)_optimizer;
				go.Settings.Apply((GeneticSettings)GeneticSettings.SelectedObject);
				go.Start(strategy, new (IStrategyParam, object, object, int, object)[]
				{
					(strategy.Parameters.GetByName(nameof(strategy.ShortSma)), shortRange.min, shortRange.max, 0, null),
					(strategy.Parameters.GetByName(nameof(strategy.LongSma)), longRange.min, longRange.max, 0, null),
				}, s => s.PnL);
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
}