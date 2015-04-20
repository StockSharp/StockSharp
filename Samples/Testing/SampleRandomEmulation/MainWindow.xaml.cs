namespace SampleRandomEmulation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies.Reporting;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private SmaStrategy _strategy;

		private ICollection<EquityData> _curveItems;
		private HistoryEmulationConnector _connector;

		private readonly LogManager _logManager = new LogManager();

		private DateTime _startEmulationTime;

		public MainWindow()
		{
			InitializeComponent();

			_logManager.Listeners.Add(new FileLogListener("log.txt"));
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			// если процесс был запущен, то его останавливаем
			if (_connector != null)
			{
				_strategy.Stop();
				_connector.Disconnect();
				_logManager.Sources.Clear();

				_connector = null;
				return;
			}

			// создаем тестовый инструмент, на котором будет производится тестирование
			var security = new Security
			{
				Id = "RIU9@FORTS",
				Code = "RIU9",
				Name = "RTS-9.09",
				Board = ExchangeBoard.Forts,
			};

			var startTime = new DateTime(2009, 6, 1);
			var stopTime = new DateTime(2009, 9, 1);

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

			var timeFrame = TimeSpan.FromMinutes(5);

			// создаем подключение для эмуляции
			_connector = new HistoryEmulationConnector(
				new[] { security },
				new[] { portfolio })
			{ MarketTimeChangedInterval = timeFrame };

			_logManager.Sources.Add(_connector);

			_connector.NewSecurities += securities =>
			{
				if (securities.All(s => s != security))
					return;

				// отправляем данные Level1 для инструмента
				_connector.SendOutMessage(level1Info);

				_connector.RegisterTrades(new RandomWalkTradeGenerator(_connector.GetSecurityId(security)));
				_connector.RegisterMarketDepth(new TrendMarketDepthGenerator(_connector.GetSecurityId(security)) { GenerateDepthOnEachTrade = false });
			};

			// указываем даты начала и конца тестирования
			_connector.StartDate = startTime;
			_connector.StopDate = stopTime;

			var candleManager = new CandleManager(_connector);

			var series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame);
			
			// создаем торговую стратегию, скользящие средние на 80 5-минуток и 10 5-минуток
			_strategy = new SmaStrategy(series, new SimpleMovingAverage { Length = 80 }, new SimpleMovingAverage { Length = 10 })
			{
				Volume = 1,
				Security = security,
				Portfolio = portfolio,
				Connector = _connector,
			};

			// копируем параметры на визуальную панель
			ParameterGrid.Parameters.Clear();
			ParameterGrid.Parameters.AddRange(_strategy.StatisticManager.Parameters);

			_strategy.PnLChanged += () =>
			{
				var data = new EquityData
				{
					Time = _strategy.CurrentTime,
					Value = _strategy.PnL,
				};

				this.GuiAsync(() => _curveItems.Add(data));
			};

			_logManager.Sources.Add(_strategy);

			// задаем шаг ProgressBar
			var progressStep = ((stopTime - startTime).Ticks / 100).To<TimeSpan>();
			var nextTime = startTime + progressStep;

			TestingProcess.Maximum = 100;
			TestingProcess.Value = 0;

			// и подписываемся на событие изменения времени, чтобы обновить ProgressBar
			_connector.MarketTimeChanged += diff =>
			{
				if (_connector.CurrentTime < nextTime && _connector.CurrentTime < stopTime)
					return;

				var steps = (_connector.CurrentTime - startTime).Ticks / progressStep.Ticks + 1;
				nextTime = startTime + (steps * progressStep.Ticks).To<TimeSpan>();
				this.GuiAsync(() => TestingProcess.Value = steps);
			};

			_connector.StateChanged += () =>
			{
				if (_connector.State == EmulationStates.Stopped)
				{
					this.GuiAsync(() =>
					{
						Report.IsEnabled = true;

						if (_connector.IsFinished)
						{
							TestingProcess.Value = TestingProcess.Maximum;
							MessageBox.Show(LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime));
						}
						else
							MessageBox.Show(LocalizedStrings.cancelled);
					});
				}
				else if (_connector.State == EmulationStates.Started)
				{
					// запускаем стратегию когда эмулятор запустился
					_strategy.Start();
					candleManager.Start(series);
				}
			};

			if (_curveItems == null)
				_curveItems = Curve.CreateCurve(_strategy.Name, Colors.DarkGreen);
			else
				_curveItems.Clear();

			Report.IsEnabled = false;

			_startEmulationTime = DateTime.Now;

			// соединяемся с трейдером и запускаем экспорт,
			// чтобы инициализировать переданными инструментами и портфелями необходимые свойства коннектора
			_connector.Connect();
		}

		private void ReportClick(object sender, RoutedEventArgs e)
		{
			// сгерерировать отчет по прошедшему тестированию
			// Внимание! сделок и заявок может быть большое количество,
			// поэтому Excel отчет может тормозить
			new ExcelStrategyReport(_strategy, "sma.xls").Generate();

			// открыть отчет
			Process.Start("sma.xls");
		}
	}
}