namespace StockSharp.Samples.Strategies.HistoryIndex;

using System;
using System.Windows;
using Ecng.Common;
using Ecng.Compilation;
using Ecng.Configuration;
using Ecng.Compilation.Roslyn;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Expressions;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;
using StockSharp.Logging;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private HistoryEmulationConnector _connector;
	private ChartCandleElement _candleElement;

	private CandleSeries _candleSeries;
	private Security _security;
	private Security _indexSecurity;
	private Portfolio _portfolio;
	private readonly LogManager _logManager;

	private readonly string _pathHistory = Paths.HistoryDataPath;

	public MainWindow()
	{
		InitializeComponent();
		ConfigManager.RegisterService<ICompiler>(new RoslynCompiler());
		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener("log.txt"));
		_logManager.Listeners.Add(new GuiLogListener(Monitor));

		CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));

		DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		DatePickerEnd.SelectedDate = Paths.HistoryEndDate;
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		_security = new Security
		{
			Id = "SBER@TQBR",
			Code = "SBER",
			PriceStep = 0.01m,
			Board = ExchangeBoard.Micex,
		};
		_indexSecurity = new ExpressionIndexSecurity()
		{
			Id = "IndexInstr@TQBR",
			Code = "IndexInstr",
			Expression = "SBER@TQBR/2 + SBER@TQBR*100",
			Board = ExchangeBoard.Micex,
		};

		_portfolio = new Portfolio { Name = "test portfolio", BeginValue = 10000000 };
		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(_pathHistory),
		};
		_connector = new HistoryEmulationConnector(new[] { _security, _indexSecurity }, new[] { _portfolio })
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
		ConfigManager.RegisterService<ISecurityProvider>(_connector);

		_candleSeries = CandleSettingsEditor.DataType.ToCandleSeries(_indexSecurity);
		_candleSeries.BuildCandlesMode = MarketDataBuildModes.Build;
		_candleSeries.BuildCandlesFrom2 = DataType.Ticks;

		InitCart();

		_connector.CandleProcessing += Processing;

		_connector.Connected += Connector_Connected;
		_connector.Connect();
	}


	private void Connector_Connected()
	{
		_connector.SubscribeTrades(_security);
		_connector.SubscribeCandles(_candleSeries);
		_connector.Start();
	}

	private void Processing(CandleSeries candleSeries, ICandleMessage candle)
	{
		if (candleSeries.Security != _indexSecurity) return;
		Chart.Draw(_candleElement, candle);
	}

	private void InitCart()
	{
		Chart.ClearAreas();
		var area = new ChartArea();
		_candleElement = new ChartCandleElement();

		Chart.AddArea(area);
		Chart.AddElement(area, _candleElement);
	}
}