using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Configuration;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;

using System;
using System.IO;
using System.Windows;

namespace HydraFIX_get_marketdata;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private readonly Connector _connector = new();
	private Subscription _subscription;
	private ChartCandleElement _candleElement;

	private const string _connectorFile = "ConnectorFile.json";

	public MainWindow()
	{
		InitializeComponent();

		// registering all connectors
		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));

		if (File.Exists(_connectorFile))
		{
			_connector.Load(_connectorFile.Deserialize<SettingsStorage>());
		}

		CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));

		DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		DatePickerEnd.SelectedDate = Paths.HistoryEndDate;
	}

	private void Setting_Click(object sender, RoutedEventArgs e)
	{
		if (_connector.Configure(this))
		{
			_connector.Save().Serialize(_connectorFile);
		}
	}

	private void Connect_Click(object sender, RoutedEventArgs e)
	{
		SecurityPicker.SecurityProvider = _connector;
		SecurityPicker.MarketDataProvider = _connector;

		_connector.CandleProcessing += Connector_CandleSeriesProcessing;
		_connector.Connected += Connector_Connected;
		_connector.Connect();
	}

	private void Connector_Connected()
	{
		// try lookup all securities
		_connector.LookupSecurities(StockSharp.Messages.Extensions.LookupAllCriteriaMessage);
	}

	private void SecurityPicker_SecuritySelected(Security security)
	{
		if (security == null) return;
		if (_subscription != null) _connector.UnSubscribe(_subscription);

		_subscription = new(CandleSettingsEditor.DataType.ToCandleSeries(security))
		{
			MarketData =
			{
				From = DatePickerBegin.SelectedDate,
				To = DatePickerEnd.SelectedDate,
			}
		};

		if (BuildFromTicks.IsChecked == true)
		{
			_subscription.MarketData.BuildMode = MarketDataBuildModes.Build;
			_subscription.MarketData.BuildFrom = DataType.Ticks;
		}

		//------------------------------------------Chart----------------------------------------------------------------------------------------
		Chart.ClearAreas();

		var area = new ChartArea();
		_candleElement = new ChartCandleElement();

		Chart.AddArea(area);
		_candleElement = new ChartCandleElement();

		Chart.AddElement(area, _candleElement, _subscription.CandleSeries);
		_connector.Subscribe(_subscription);
	}

	private void Connector_CandleSeriesProcessing(CandleSeries candleSeries,ICandleMessage candle)
	{
		Chart.Draw(_candleElement, candle);
	}
}
