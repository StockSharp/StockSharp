using System;
using System.Windows;
using System.IO;

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

namespace Getting_realtime_candles;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
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
		CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));
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
		_connector.CandleProcessing += Connector_CandleSeriesProcessing;
		_connector.Connect();
	}

	private void SecurityPicker_SecuritySelected(Security security)
	{
		if (security == null) return;

		if (_subscription != null) _connector.UnSubscribe(_subscription);
		//----------------------------------Candles will be built from Trades-----------------------------------------------
		//----------------------------------TimeFrameCandle-----------------------------------------------------------------
		// _candleSeries = new CandleSeries(typeof(TimeFrameCandle), security, TimeSpan.FromMinutes(5))
		// {
		// 	BuildCandlesMode = MarketDataBuildModes.Build,
		// 	BuildCandlesFrom = MarketDataTypes.Trades,
		//  IsCalcVolumeProfile = true,
		// };
		//----------------------------------VolumeCandle---------------------------------------------------------------------
		//_candleSeries = new CandleSeries(typeof(VolumeCandle), security, 100m)
		// {
		// 	BuildCandlesMode = MarketDataBuildModes.Build,
		// 	BuildCandlesFrom = MarketDataTypes.Trades,
		// };
		//----------------------------------TickCandle------------------------------------------------------------------------
		// _candleSeries = new CandleSeries(typeof(TickCandle), security, 100)
		// {
		// 	BuildCandlesMode = MarketDataBuildModes.Build,
		// 	BuildCandlesFrom = MarketDataTypes.Trades,
		// };
		//----------------------------------RangeCandle-----------------------------------------------------------------------
		// _candleSeries = new CandleSeries(typeof(RangeCandle), security, new Unit(0.1m))
		// {
		// 	BuildCandlesMode = MarketDataBuildModes.Build,
		// 	BuildCandlesFrom = MarketDataTypes.Trades,
		// };
		//----------------------------------RenkoCandle-----------------------------------------------------------------------
		// _candleSeries = new CandleSeries(typeof(RenkoCandle), security, new Unit(0.1m))
		// {
		// 	BuildCandlesMode = MarketDataBuildModes.Build,
		// 	BuildCandlesFrom = MarketDataTypes.Trades,
		// };
		//----------------------------------PnFCandle--------------------------------------------------------------------------
		// _candleSeries = new CandleSeries(typeof(PnFCandle), security, new PnFArg() { BoxSize = 0.1m, ReversalAmount =1})
		// {
		//   BuildCandlesMode = MarketDataBuildModes.Build,
		//   BuildCandlesFrom = MarketDataTypes.Trades,
		// };
		//----------------------------------Candles will be load and built the missing data from trades------------------------
		_subscription = new(CandleSettingsEditor.DataType.ToCandleSeries(security))
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
		Chart.AddElement(area, _candleElement, _subscription.CandleSeries);

		_connector.Subscribe(_subscription);
	}

	private void Connector_CandleSeriesProcessing(CandleSeries candleSeries, ICandleMessage candle)
	{
		Chart.Draw(_candleElement, candle);
	}
}