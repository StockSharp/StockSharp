namespace SampleHistoryTestingParallel
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;

	using Ookii.Dialogs.Wpf;

	using Ecng.Collections;
	using Ecng.Xaml;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Testing;
	using StockSharp.Algo.Testing;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private DateTime _startEmulationTime;

		public MainWindow()
		{
			InitializeComponent();

			HistoryPath.Text = Path.GetFullPath(@"..\..\..\HistoryData\");
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!HistoryPath.Text.IsEmpty())
				dlg.SelectedPath = HistoryPath.Text;

			if (dlg.ShowDialog() == true)
			{
				HistoryPath.Text = dlg.SelectedPath;
			}
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			if (HistoryPath.Text.IsEmpty() || !Directory.Exists(HistoryPath.Text))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			if (Math.Abs(TestingProcess.Value - 0) > double.Epsilon)
			{
				MessageBox.Show(this, LocalizedStrings.Str3015);
				return;
			}

			var logManager = new LogManager();
			var fileLogListener = new FileLogListener("sample.log");
			logManager.Listeners.Add(fileLogListener);

			// создаем длины скользящих средник
			var periods = new[]
			{
				new Tuple<int, int, Color>(80, 10, Colors.DarkGreen),
				new Tuple<int, int, Color>(70, 8, Colors.Red),
				new Tuple<int, int, Color>(60, 6, Colors.DarkBlue)
			};

			// хранилище, через которое будет производиться доступ к тиковой и котировочной базе
			var storageRegistry = new StorageRegistry
			{
				// изменяем путь, используемый по умолчанию
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Text)
			};

			var timeFrame = TimeSpan.FromMinutes(5);

			// создаем тестовый инструмент, на котором будет производится тестирование
			var security = new Security
			{
				Id = "RIZ2@FORTS", // по идентификатору инструмента будет искаться папка с историческими маркет данными
				Code = "RIZ2",
				Name = "RTS-12.12",
				Board = ExchangeBoard.Forts,
			};

			var startTime = new DateTime(2012, 10, 1);
			var stopTime = new DateTime(2012, 10, 31);

			var level1Info = new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = startTime,
			}
			.TryAdd(Level1Fields.PriceStep, 10m)
			.TryAdd(Level1Fields.StepPrice, 6m)
			.TryAdd(Level1Fields.MinPrice, 10m)
			.TryAdd(Level1Fields.MaxPrice, 1000000m)
			.TryAdd(Level1Fields.MarginBuy, 10000m)
			.TryAdd(Level1Fields.MarginSell, 10000m);

			// тестовый портфель
			var portfolio = new Portfolio
			{
				Name = "test account",
				BeginValue = 1000000,
			};

			// создаем подключение для эмуляции
			var batchEmulation = new BatchEmulation(new[] { security }, new[] { portfolio }, storageRegistry)
			{
				// инициализируем настройки (инструмент в истории обновляется раз в секунду)
				EmulationSettings =
				{
					MarketTimeChangedInterval = timeFrame,
					StartTime = startTime,
					StopTime = stopTime,

					// кол-во одновременно тестируемых стратегий
					BatchSize = periods.Length,
				}
			};

			// и подписываемся на событие изменения прогресса тестирования, чтобы обновить ProgressBar
			batchEmulation.ProgressChanged += (curr, total) => this.GuiAsync(() => TestingProcess.Value = total);

			batchEmulation.StateChanged += (oldState, newState) =>
			{
				if (batchEmulation.State != EmulationStates.Stopped)
					return;

				this.GuiAsync(() =>
				{
					if (batchEmulation.IsFinished)
					{
						TestingProcess.Value = TestingProcess.Maximum;
						MessageBox.Show(LocalizedStrings.Str3024 + (DateTime.Now - _startEmulationTime));
					}
					else
						MessageBox.Show(LocalizedStrings.cancelled);
				});
			};

			// получаем подключение для эмуляции
			var connector = batchEmulation.EmulationConnector;

			logManager.Sources.Add(connector);

			// подписываемся на получение данных после получения инструмента
			connector.NewSecurities += securities =>
			{
				if (securities.All(s => s != security))
					return;

				// отправляем данные Level1 для инструмента
				connector.MarketDataAdapter.SendOutMessage(level1Info);

				connector.RegisterMarketDepth(new TrendMarketDepthGenerator(connector.GetSecurityId(security))
				{
					// стакан для инструмента в истории обновляется раз в секунду
					Interval = TimeSpan.FromSeconds(1),
				});
			};

			TestingProcess.Maximum = 100;
			TestingProcess.Value = 0;

			_startEmulationTime = DateTime.Now;

			var strategies = periods
				.Select(period =>
				{
					var series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame);

					// создаем торговую стратегию
					var strategy = new SmaStrategy(series, new SimpleMovingAverage { Length = period.Item1 }, new SimpleMovingAverage { Length = period.Item2 })
					{
						Volume = 1,
						Security = security,
						Portfolio = portfolio,
						Connector = connector,

						// по-умолчанию интервал равен 1 минут,
						// что для истории в диапазон от нескольких месяцев излишне
						UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>()
					};

					strategy.SetCandleManager(new CandleManager(connector));

					var curveItems = Curve.CreateCurve(LocalizedStrings.Str3026Params.Put(period.Item1, period.Item2), period.Item3);
					strategy.PnLChanged += () =>
					{
						var data = new EquityData
						{
							Time = strategy.CurrentTime,
							Value = strategy.PnL,
						};

						this.GuiAsync(() => curveItems.Add(data));
					};

					Stat.AddStrategies(new[] { strategy });

					return strategy;
				})
				.ToEx(periods.Length);

			// запускаем эмуляцию
			batchEmulation.Start(strategies);
		}
	}
}