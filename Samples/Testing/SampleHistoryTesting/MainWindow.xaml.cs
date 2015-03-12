namespace SampleHistoryTesting
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Collections.Generic;

	using Ecng.Xaml;
	using Ecng.Common;
	using Ecng.Collections;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		// вспомогательный класс для настроек тестирования
		internal sealed class EmulationInfo
		{
			public bool UseMarketDepth { get; set; }
			public TimeSpan? UseCandleTimeFrame { get; set; }
			public Color CurveColor { get; set; }
			public string StrategyName { get; set; }
			public bool UseOrderLog { get; set; }
		}

		private readonly List<HistoryEmulationConnector> _connectors = new List<HistoryEmulationConnector>();
		private readonly BufferedChart _bufferedChart;
		
		private DateTime _startEmulationTime;
		private ChartCandleElement _candlesElem;
		private ChartTradeElement _tradesElem;
		private ChartIndicatorElement _shortElem;
		private SimpleMovingAverage _shortMa;
		private ChartIndicatorElement _longElem;
		private SimpleMovingAverage _longMa;
		private ChartArea _area;

		public MainWindow()
		{
			InitializeComponent();

			_bufferedChart = new BufferedChart(Chart);

			HistoryPath.Text = @"..\..\..\HistoryData\".ToFullPath();

			From.Value = new DateTime(2012, 10, 1);
			To.Value = new DateTime(2012, 10, 25);
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!HistoryPath.Text.IsEmpty())
				dlg.SelectedPath = HistoryPath.Text;

			if (dlg.ShowDialog(this) == true)
			{
				HistoryPath.Text = dlg.SelectedPath;
			}
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			InitChart();

			if (HistoryPath.Text.IsEmpty() || !Directory.Exists(HistoryPath.Text))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			if (_connectors.Any(t => t.State != EmulationStates.Stopped))
			{
				MessageBox.Show(this, LocalizedStrings.Str3015);
				return;
			}

			var secIdParts = SecId.Text.Split('@');

			if (secIdParts.Length != 2)
			{
				MessageBox.Show(this, LocalizedStrings.Str3016);
				return;
			}

			var timeFrame = TimeSpan.FromMinutes(5);

			// создаем настройки для тестирования
			var settings = new[]
			{
				Tuple.Create(
					TicksCheckBox, 
					TicksTestingProcess, 
					TicksParameterGrid,
					// тест только на тиках
					new EmulationInfo {CurveColor = Colors.DarkGreen, StrategyName = LocalizedStrings.Str3017}),

				Tuple.Create(
					TicksAndDepthsCheckBox, 
					TicksAndDepthsTestingProcess, 
					TicksAndDepthsParameterGrid,
					// тест на тиках + стаканы
					new EmulationInfo {UseMarketDepth = true, CurveColor = Colors.Red, StrategyName = LocalizedStrings.Str3018}),

				Tuple.Create(
					CandlesCheckBox, 
					CandlesTestingProcess, 
					CandlesParameterGrid,
					// тест на свечах
					new EmulationInfo {UseCandleTimeFrame = timeFrame, CurveColor = Colors.DarkBlue, StrategyName = LocalizedStrings.Str3019}),
				
				Tuple.Create(
					CandlesAndDepthsCheckBox, 
					CandlesAndDepthsTestingProcess, 
					CandlesAndDepthsParameterGrid,
					// тест на свечах + стаканы
					new EmulationInfo {UseMarketDepth = true, UseCandleTimeFrame = timeFrame, CurveColor = Colors.Cyan, StrategyName = LocalizedStrings.Str3020}),
			
				Tuple.Create(
					OrderLogCheckBox, 
					OrderLogTestingProcess, 
					OrderLogParameterGrid,
					// тест на логе заявок
					new EmulationInfo {UseOrderLog = true, CurveColor = Colors.CornflowerBlue, StrategyName = LocalizedStrings.Str3021})
			};

			// хранилище, через которое будет производиться доступ к тиковой и котировочной базе
			var storageRegistry = new StorageRegistry
			{
				// изменяем путь, используемый по умолчанию
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Text)
			};

			var startTime = (DateTime)From.Value;
			var stopTime = (DateTime)To.Value;

			// ОЛ необходимо загружать с 18.45 пред дня, чтобы стаканы строились правильно
			if (OrderLogCheckBox.IsChecked == true)
				startTime = startTime.Subtract(TimeSpan.FromDays(1)).AddHours(18).AddMinutes(45).AddTicks(1);

			// задаем шаг ProgressBar
			var progressStep = ((stopTime - startTime).Ticks / 100).To<TimeSpan>();

			// в реальности период может быть другим, и это зависит от объема данных,
			// хранящихся по пути HistoryPath, 
			TicksTestingProcess.Maximum = TicksAndDepthsTestingProcess.Maximum = CandlesTestingProcess.Maximum = 100;
			TicksTestingProcess.Value = TicksAndDepthsTestingProcess.Value = CandlesTestingProcess.Value = 0;

			var logManager = new LogManager();
			var fileLogListener = new FileLogListener("sample.log");
			logManager.Listeners.Add(fileLogListener);
			//logManager.Listeners.Add(new DebugLogListener());	// чтобы смотреть логи в отладчике - работает медленно.

			var generateDepths = GenDepthsCheckBox.IsChecked == true;
			var maxDepth = MaxDepth.Text.To<int>();
			var maxVolume = MaxVolume.Text.To<int>();

			var secCode = secIdParts[0];
			var board = ExchangeBoard.GetOrCreateBoard(secIdParts[1]);

			foreach (var set in settings)
			{
				if (set.Item1.IsChecked == false)
					continue;

				var progressBar = set.Item2;
				var statistic = set.Item3;
				var emulationInfo = set.Item4;

				// создаем тестовый инструмент, на котором будет производится тестирование
				var security = new Security
				{
					Id = SecId.Text, // по идентификатору инструмента будет искаться папка с историческими маркет данными
					Code = secCode,
					Board = board,
				};

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
				// инициализируем настройки (инструмент в истории обновляется раз в секунду)
				var connector = new HistoryEmulationConnector(
					new[] { security },
					new[] { portfolio })
				{
					StorageRegistry = storageRegistry,

					MarketEmulator =
					{
						Settings =
						{
							// использовать свечи
							UseCandlesTimeFrame =  emulationInfo.UseCandleTimeFrame,

							// сведение сделки в эмуляторе если цена коснулась нашей лимитной заявки. 
							// Если выключено - требуется "прохождение цены сквозь уровень"
							// (более "суровый" режим тестирования.)
							MatchOnTouch = false,
						}
					},

					//UseExternalCandleSource = true,
					CreateDepthFromOrdersLog = emulationInfo.UseOrderLog,
					CreateTradesFromOrdersLog = emulationInfo.UseOrderLog,
				};

				connector.MarketDataAdapter.SessionHolder.MarketTimeChangedInterval = timeFrame;

				((ILogSource)connector).LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info;

				logManager.Sources.Add(connector);

				connector.NewSecurities += securities =>
				{
					//подписываемся на получение данных после получения инструмента

					if (securities.All(s => s != security))
						return;

					// отправляем данные Level1 для инструмента
					connector.MarketDataAdapter.SendOutMessage(level1Info);

					// тест подразумевает наличие стаканов
					if (emulationInfo.UseMarketDepth)
					{
						connector.RegisterMarketDepth(security);

						if (
								// если выбрана генерация стаканов вместо реальных стаканов
								generateDepths ||
								// для свечей генерируем стаканы всегда
								emulationInfo.UseCandleTimeFrame != TimeSpan.Zero
							)
						{
							// если история по стаканам отсутствует, но стаканы необходимы для стратегии,
							// то их можно сгенерировать на основании цен последних сделок или свечек.
							connector.RegisterMarketDepth(new TrendMarketDepthGenerator(connector.GetSecurityId(security))
							{
								Interval = TimeSpan.FromSeconds(1), // стакан для инструмента в истории обновляется раз в секунду
								MaxAsksDepth = maxDepth,
								MaxBidsDepth = maxDepth,
								UseTradeVolume = true,
								MaxVolume = maxVolume,
								MinSpreadStepCount = 2,  // минимальный генерируемый спред - 2 минимальных шага цены
								MaxSpreadStepCount = 5, // не генерировать спрэд между лучшим бид и аск больше чем 5 минимальных шагов цены - нужно чтобы при генерации из свечей не получалось слишком широкого спреда.
								MaxPriceStepCount = 3	// максимальное количество шагов между ценами,
							});
						}
					}
					else if (emulationInfo.UseOrderLog)
					{
						connector.RegisterOrderLog(security);
					}
				};

				// соединяемся с трейдером и запускаем экспорт,
				// чтобы инициализировать переданными инструментами и портфелями необходимые свойства EmulationTrader
				connector.Connect();
				connector.StartExport();

				var candleManager = new CandleManager(connector);
				var series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame);

				_shortMa = new SimpleMovingAverage { Length = 10 };
				_shortElem = new ChartIndicatorElement
				{
					Color = Colors.Coral,
					ShowAxisMarker = false,
					FullTitle = _shortMa.ToString()
				};
				_bufferedChart.AddElement(_area, _shortElem);

				_longMa = new SimpleMovingAverage { Length = 80 };
				_longElem = new ChartIndicatorElement
				{
					ShowAxisMarker = false,
					FullTitle = _longMa.ToString()
				};
				_bufferedChart.AddElement(_area, _longElem);

				// создаем торговую стратегию, скользящие средние на 80 5-минуток и 10 5-минуток
				var strategy = new SmaStrategy(_bufferedChart, _candlesElem, _tradesElem, _shortMa, _shortElem, _longMa, _longElem, series)
				{
					Volume = 1,
					Portfolio = portfolio,
					Security = security,
					Connector = connector,
					LogLevel = DebugLogCheckBox.IsChecked == true ? LogLevels.Debug : LogLevels.Info,

					// по-умолчанию интервал равен 1 минут,
					// что для истории в диапазон от нескольких месяцев излишне
					UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>()
				};

				// комиссия в 1 копейку за сделку
				connector.MarketEmulator.SendInMessage(new CommissionRuleMessage
				{
					Rule = new CommissionPerTradeRule { Value = 0.01m }
				});

				logManager.Sources.Add(strategy);

				// копируем параметры на визуальную панель
				statistic.Parameters.Clear();
				statistic.Parameters.AddRange(strategy.StatisticManager.Parameters);

				var pnlCurve = Curve.CreateCurve("P&L " + emulationInfo.StrategyName, emulationInfo.CurveColor, EquityCurveChartStyles.Area);
				var unrealizedPnLCurve = Curve.CreateCurve(LocalizedStrings.PnLUnreal + emulationInfo.StrategyName, Colors.Black);
				var commissionCurve = Curve.CreateCurve(LocalizedStrings.Str159 + " " + emulationInfo.StrategyName, Colors.Red, EquityCurveChartStyles.DashedLine);
				var posItems = PositionCurve.CreateCurve(emulationInfo.StrategyName, emulationInfo.CurveColor);
				strategy.PnLChanged += () =>
				{
					var pnl = new EquityData
					{
						Time = strategy.CurrentTime,
						Value = strategy.PnL - strategy.Commission ?? 0
					};

					var unrealizedPnL = new EquityData
					{
						Time = strategy.CurrentTime,
						Value = strategy.PnLManager.UnrealizedPnL
					};

					var commission = new EquityData
					{
						Time = strategy.CurrentTime,
						Value = strategy.Commission ?? 0
					};

					pnlCurve.Add(pnl);
					unrealizedPnLCurve.Add(unrealizedPnL);
					commissionCurve.Add(commission);
				};

				strategy.PositionChanged += () => posItems.Add(new EquityData { Time = strategy.CurrentTime, Value = strategy.Position });

				var nextTime = startTime + progressStep;

				// и подписываемся на событие изменения времени, чтобы обновить ProgressBar
				connector.MarketTimeChanged += d =>
				{
					if (connector.CurrentTime < nextTime && connector.CurrentTime < stopTime)
						return;

					var steps = (connector.CurrentTime - startTime).Ticks / progressStep.Ticks + 1;
					nextTime = startTime + (steps * progressStep.Ticks).To<TimeSpan>();
					this.GuiAsync(() => progressBar.Value = steps);
				};

				connector.StateChanged += () =>
				{
					if (connector.State == EmulationStates.Stopped)
					{
						candleManager.Stop(series);
						strategy.Stop();

						logManager.Dispose();
						_connectors.Clear();

						SetIsEnabled(false);

						this.GuiAsync(() =>
						{
							if (connector.IsFinished)
							{
								progressBar.Value = progressBar.Maximum;
								MessageBox.Show(LocalizedStrings.Str3024 + (DateTime.Now - _startEmulationTime));
							}
							else
								MessageBox.Show(LocalizedStrings.cancelled);
						});
					}
					else if (connector.State == EmulationStates.Started)
					{
						SetIsEnabled(true);

						// запускаем стратегию когда эмулятор запустился
						strategy.Start();
						candleManager.Start(series);
					}
				};

				if (ShowDepth.IsChecked == true)
				{
					MarketDepth.UpdateFormat(security);

					connector.NewMessage += (message, dir) =>
					{
						var quoteMsg = message as QuoteChangeMessage;

						if (quoteMsg != null)
							MarketDepth.UpdateDepth(quoteMsg);
					};
				}

				_connectors.Add(connector);
			}

			_startEmulationTime = DateTime.Now;

			// запускаем эмуляцию
			foreach (var connector in _connectors)
			{
				// указываем даты начала и конца тестирования
				connector.Start(startTime, stopTime);
			}

			TabControl.Items.Cast<TabItem>().First(i => i.Visibility == Visibility.Visible).IsSelected = true;
		}

		private void CheckBoxClick(object sender, RoutedEventArgs e)
		{
			var isEnabled = TicksCheckBox.IsChecked == true ||
			                TicksAndDepthsCheckBox.IsChecked == true ||
			                CandlesCheckBox.IsChecked == true ||
			                CandlesAndDepthsCheckBox.IsChecked == true ||
			                OrderLogCheckBox.IsChecked == true;

			StartBtn.IsEnabled = isEnabled;
			TabControl.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
		}

		private void StopBtnClick(object sender, RoutedEventArgs e)
		{
			foreach (var connector in _connectors)
			{
				connector.Stop();
			}
		}

		private void InitChart()
		{
			_bufferedChart.ClearAreas();
			Curve.Clear();
			PositionCurve.Clear();

			_area = new ChartArea();
			_bufferedChart.AddArea(_area);

			_candlesElem = new ChartCandleElement { ShowAxisMarker = false };
			_bufferedChart.AddElement(_area, _candlesElem);

			_tradesElem = new ChartTradeElement { FullTitle = "Сделки" };
			_bufferedChart.AddElement(_area, _tradesElem);
		}

		private void SetIsEnabled(bool started)
		{
			this.GuiAsync(() =>
			{
				StopBtn.IsEnabled = started;
				StartBtn.IsEnabled = !started;
				TicksCheckBox.IsEnabled = TicksAndDepthsCheckBox.IsEnabled = CandlesCheckBox.IsEnabled
					= CandlesAndDepthsCheckBox.IsEnabled = OrderLogCheckBox.IsEnabled = !started;

				_bufferedChart.IsAutoRange = started;
			});
		}
	}
}