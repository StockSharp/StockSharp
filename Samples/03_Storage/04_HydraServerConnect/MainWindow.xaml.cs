namespace StockSharp.Samples.Storage.HydraServerConnect;

using System;
using System.IO;
using System.Windows;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Configuration;
using Ecng.Xaml;

using StockSharp.Configuration;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;
using StockSharp.Localization;

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
		else
		{
			var adapter = new Fix.FixMessageAdapter(_connector.TransactionIdGenerator)
			{
				Address = RemoteMarketDataDrive.DefaultAddress,
				TargetCompId = RemoteMarketDataDrive.DefaultTargetCompId,

				SenderCompId = "hydra_user",

				//
				// required for non anonymous access
				//
				//Password = "hydra_user".To<SecureString>()

				//
				// uncomment to enable binary mode
				//
				//IsBinaryEnabled = true,
			};

			// turning off the support of the transactional messages
			adapter.ChangeSupported(false, false);

			_connector.Adapter.InnerAdapters.Add(adapter);

			_connector.Save().Serialize(_connectorFile);
		}

		CandleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();

		DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

		SecurityPicker.SecurityProvider = _connector;
		SecurityPicker.MarketDataProvider = _connector;

		_connector.ConnectionError += Connector_ConnectionError;
		_connector.CandleReceived += Connector_CandleReceived;
	}

	private void Connector_ConnectionError(Exception error)
	{
		this.GuiAsync(() =>
		{
			MessageBox.Show(this.GetWindow(), error.ToString(), LocalizedStrings.ErrorConnection);
		});
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
		_connector.Connect();
	}

	private void SecurityPicker_SecuritySelected(Security security)
	{
		if (security == null) return;
		if (_subscription != null) _connector.UnSubscribe(_subscription);

		_subscription = new(CandleDataTypeEdit.DataType, security)
		{
			From = DatePickerBegin.SelectedDate,
			To = DatePickerEnd.SelectedDate,
		};

		if (BuildFromTicks.IsChecked == true)
		{
			_subscription.MarketData.BuildMode = MarketDataBuildModes.Build;
			_subscription.MarketData.BuildFrom = DataType.Ticks;
		}

		//------------------------------------------Chart----------------------------------------------------------------------------------------
		Chart.ClearAreas();

		var area = new ChartArea();
		Chart.AddArea(area);

		_candleElement = new ChartCandleElement();
		Chart.AddElement(area, _candleElement, _subscription);

		_connector.Subscribe(_subscription);
	}

	private void Connector_CandleReceived(Subscription subscription, ICandleMessage candle)
	{
		Chart.Draw(_candleElement, candle);
	}
}
