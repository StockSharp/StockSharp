using System;
using System.Windows;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;

namespace HistoryEmulation;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private HistoryEmulationConnector _connector;
	private ChartCandleElement _candleElement;
	private ChartTradeElement _tradeElement;
	private CandleSeries _candleSeries;
	private Security _security;
	private Portfolio _portfolio;
	private readonly LogManager _logManager;

	private readonly string _pathHistory = Paths.HistoryDataPath;

	public MainWindow()
	{
		InitializeComponent();

		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener("log.txt"));
		_logManager.Listeners.Add(new GuiLogListener(Monitor));

		DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

		CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		_security = new Security
		{
			Id = "SBER@TQBR",
			Code = "SBER",
			PriceStep = 0.01m,
			Board = ExchangeBoard.Micex
		};
		_portfolio = new Portfolio { Name = "test account", BeginValue = 1000000 };
		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(_pathHistory),
		};

		_connector = new HistoryEmulationConnector(new[] { _security }, new[] { _portfolio })
		{
			HistoryMessageAdapter =
			{
				StorageRegistry = storageRegistry,
				StorageFormat = StorageFormats.Binary,
				StartDate = DatePickerBegin.SelectedDate.Value.ChangeKind(DateTimeKind.Utc),
				StopDate = DatePickerEnd.SelectedDate.Value.ChangeKind(DateTimeKind.Utc),
			},
			LogLevel = LogLevels.Info,
		};
		_logManager.Sources.Add(_connector);

		_candleSeries = CandleSettingsEditor.DataType.ToCandleSeries(_security);

		// ready-to-use candles much faster than compression on fly mode
		// turn off compression to boost optimizer (!!! make sure you have candles)
		//_candleSeries.BuildCandlesMode = MarketDataBuildModes.Build;
		//_candleSeries.BuildCandlesFrom2 = DataType.Ticks;

		InitChart();

		_connector.CandleProcessing += Connector_CandleSeriesProcessing;
		//-------------------------------------------------
		_connector.OrderBookReceived += (s, b) => MarketDepthControl.UpdateDepth(b);

		_connector.Connected += Connector_Connected;
		_connector.Connect();
	}

	private void InitChart()
	{
		//-----------------Chart--------------------------------
		Chart.ClearAreas();

		var area = new ChartArea();
		_candleElement = new ChartCandleElement();
		_tradeElement = new ChartTradeElement { FullTitle = "Trade" };

		Chart.AddArea(area);
		Chart.AddElement(area, _candleElement);
		Chart.AddElement(area, _tradeElement);
	}

	private void Connector_Connected()
	{
		// uncomment in case has order book history
		// (will degradate performance)
		//_connector.SubscribeMarketDepth(_security);

		_connector.SubscribeCandles(_candleSeries);
		_connector.Start();
	}

	private void Connector_CandleSeriesProcessing(CandleSeries candleSeries, ICandleMessage candle)
	{
		Chart.Draw(_candleElement, candle);
	}
}

