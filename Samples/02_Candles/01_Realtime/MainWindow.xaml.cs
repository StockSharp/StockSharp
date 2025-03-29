namespace StockSharp.Samples.Candles.Realtime;

using System;
using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Configuration;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;

public partial class MainWindow
{
	private Subscription _subscription;
	private ChartCandleElement _candleElement;

	private readonly Connector _connector = new();
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
		CandleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();
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
		_connector.CandleReceived += Connector_CandleReceived;
		_connector.Connect();
	}

	private void SecurityPicker_SecuritySelected(Security security)
	{
		if (security == null) return;

		if (_subscription != null) _connector.UnSubscribe(_subscription);
		//----------------------------------Candles will be built from Trades-----------------------------------------------
		//----------------------------------TimeFrameCandle-----------------------------------------------------------------
		//_subscription = new(DataType.TimeFrame(TimeSpan.FromMinutes(5)), security)
		//{
		//	MarketData =
		//	{
		//		BuildMode = MarketDataBuildModes.Build,
		//		BuildFrom = DataType.Ticks,
		//		IsCalcVolumeProfile = true,
		//	}
		//};
		//----------------------------------VolumeCandle---------------------------------------------------------------------
		//_subscription = new(DataType.Volume(100), security)
		//{
		//	MarketData =
		//	{
		//		BuildMode = MarketDataBuildModes.Build,
		//		BuildFrom = DataType.Ticks,
		//	}
		//};
		//----------------------------------TickCandle------------------------------------------------------------------------
		//_subscription = new(DataType.Tick(100), security)
		//{
		//	MarketData =
		//	{
		//		BuildMode = MarketDataBuildModes.Build,
		//		BuildFrom = DataType.Ticks,
		//	}
		//};
		//----------------------------------RangeCandle-----------------------------------------------------------------------
		//_subscription = new(DataType.Range(0.1m), security)
		//{
		//	MarketData =
		//	{
		//		BuildMode = MarketDataBuildModes.Build,
		//		BuildFrom = DataType.Ticks,
		//	}
		//};
		//----------------------------------RenkoCandle-----------------------------------------------------------------------
		//_subscription = new(DataType.Renko(0.1m), security)
		//{
		//	MarketData =
		//	{
		//		BuildMode = MarketDataBuildModes.Build,
		//		BuildFrom = DataType.Ticks,
		//	}
		//};
		//----------------------------------PnFCandle--------------------------------------------------------------------------
		//_subscription = new(DataType.PnF(new() { BoxSize = 0.1m, ReversalAmount = 1 }), security)
		//{
		//	MarketData =
		//	{
		//		BuildMode = MarketDataBuildModes.Build,
		//		BuildFrom = DataType.Ticks,
		//	}
		//};
		//----------------------------------Candles will be load and built the missing data from trades------------------------
		_subscription = new(CandleDataTypeEdit.DataType, security)
		{
			MarketData =
			{
				BuildMode = MarketDataBuildModes.LoadAndBuild,
				From = DateTime.Today.Subtract(TimeSpan.FromDays(30)),
			}
		};

		//-----------------Chart--------------------------------
		Chart.ClearAreas();

		var area = new ChartArea();
		_candleElement = new ChartCandleElement();

		Chart.AddArea(area);
		Chart.AddElement(area, _candleElement, _subscription);

		_connector.Subscribe(_subscription);
	}

	private void Connector_CandleReceived(Subscription subscription, ICandleMessage candle)
	{
		Chart.Draw(_candleElement, candle);
	}
}