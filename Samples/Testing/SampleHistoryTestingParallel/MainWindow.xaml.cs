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

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Configuration;

	public partial class MainWindow
	{
		private DateTime _startEmulationTime;

		private BatchEmulation _batchEmulation;

		public MainWindow()
		{
			InitializeComponent();

			HistoryPath.Folder = Paths.HistoryDataPath;
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			if (_batchEmulation != null)
			{
				_batchEmulation.Resume();
				return;
			}

			if (HistoryPath.Folder.IsEmpty() || !Directory.Exists(HistoryPath.Folder))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			TestingProcess.Value = 0;
			Stat.Clear();

			var logManager = new LogManager();
			var fileLogListener = new FileLogListener("sample.log");
			logManager.Listeners.Add(fileLogListener);

			// SMA periods
			var periods = new List<(int longMa, int shortMa, Color color)>();

			for (var l = 100; l >= 50; l -= 1)
			{
				for (var s = 40; s >= 5; s -= 1)
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

			// create backtesting connector
			_batchEmulation = new BatchEmulation(new[] { security }, new[] { portfolio }, storageRegistry)
			{
				EmulationSettings =
				{
					MarketTimeChangedInterval = timeFrame,
					StartTime = startTime,
					StopTime = stopTime,

					// count of parallel testing strategies
					// if not set, then CPU count * 2
					//BatchSize = 3,
				},

				AdapterCache = new(),
			};

			// handle historical time for update ProgressBar
			_batchEmulation.TotalProgressChanged += (progress, duration, remaining) => this.GuiAsync(() =>
			{
				TestingProcess.Value = progress;
				TestingProcessText.Text = $"{progress}% | {(int)duration.TotalSeconds} sec left | {(int)remaining.TotalSeconds} sec rem";
			});

			_batchEmulation.StateChanged += (oldState, newState) =>
			{
				if (newState == ChannelStates.Stopped)
					_batchEmulation = null;

				this.GuiAsync(() =>
				{
					switch (newState)
					{
						case ChannelStates.Stopping:
						case ChannelStates.Starting:
						case ChannelStates.Suspending:
							SetIsEnabled(false, false, false);
							break;
						case ChannelStates.Stopped:
							SetIsEnabled(true, false, false);

							if (!_batchEmulation.IsCancelled)
							{
								TestingProcess.Value = TestingProcess.Maximum;
								MessageBox.Show(this, LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime));
							}
							else
								MessageBox.Show(this, LocalizedStrings.cancelled);

							break;
						case ChannelStates.Started:
							SetIsEnabled(false, true, true);
							break;
						case ChannelStates.Suspended:
							SetIsEnabled(true, false, true);
							break;
						default:
							throw new ArgumentOutOfRangeException(newState.ToString());
					}
				});
			};

			_startEmulationTime = DateTime.Now;

			var strategies = periods
				.Select(period =>
				{
					// create strategy based SMA
					var strategy = new SampleHistoryTesting.SmaStrategy
					{
						Subscription = new(new CandleSeries(typeof(TimeFrameCandle), security, timeFrame)) { DisableEntity = true },

						ShortSma = { Length = period.shortMa },
						LongSma = { Length = period.longMa },

						Volume = 1,
						Security = security,
						Portfolio = portfolio,
						//Connector = connector,

						// by default interval is 1 min,
						// it is excessively for time range with several months
						UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>(),

						Name = $"L={period.longMa} S={period.shortMa}",
					};

					this.GuiSync(() => Stat.AddStrategy(strategy));

					return strategy;
				});

			// start emulation
			_batchEmulation.Start(strategies, periods.Count);
		}

		private void SetIsEnabled(bool canStart, bool canSuspend, bool canStop)
		{
			this.GuiAsync(() =>
			{
				StopBtn.IsEnabled = canStop;
				StartBtn.IsEnabled = canStart;
				PauseBtn.IsEnabled = canSuspend;
			});
		}

		private void StopBtnClick(object sender, RoutedEventArgs e)
		{
			_batchEmulation.Stop();
		}

		private void PauseBtnClick(object sender, RoutedEventArgs e)
		{
			_batchEmulation.Suspend();
		}
	}
}