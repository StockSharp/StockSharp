#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleMultiConnection.SampleMultiConnectionPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleStrategies
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Storages.Csv;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly Connector Connector;
		public readonly LogManager LogManager;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly StrategiesWindow _strategiesWindow = new StrategiesWindow();

		public static MainWindow Instance { get; private set; }

		private const string _settingsFile = "connection.xml";

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put(LocalizedStrings.Str1355);

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_strategiesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			LogManager = new LogManager();
			LogManager.Listeners.Add(new FileLogListener("sample.log"));
			LogManager.Listeners.Add(new GuiLogListener(Monitor));

			var entityRegistry = new CsvEntityRegistry("Data");

			ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);
			// ecng.serialization invoke in several places IStorage obj
			ConfigManager.RegisterService(entityRegistry.Storage);

			var storageRegistry = ConfigManager.GetService<IStorageRegistry>();

			SerializationContext.DelayAction = entityRegistry.DelayAction = new DelayAction(entityRegistry.Storage, ex => ex.LogError());

			Connector = new Connector(entityRegistry, storageRegistry);
			LogManager.Sources.Add(Connector);

			InitConnector(entityRegistry);
		}

		private void InitConnector(CsvEntityRegistry entityRegistry)
		{
			// subscribe on connection successfully event
			Connector.Connected += () =>
			{
				this.GuiAsync(() => ChangeConnectStatus(true));
			};

			// subscribe on connection error event
			Connector.ConnectionError += error => this.GuiAsync(() =>
			{
				ChangeConnectStatus(false);
				MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
			});

			Connector.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

			// subscribe on error event
			Connector.Error += error =>
			{
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));
			};


			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, msg, error) =>
			{
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));
			};

			Connector.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
			//Connector.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);

			Connector.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
			//Connector.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);
			Connector.NewMyTrades += trades =>
			{
				_myTradesWindow.TradeGrid.Trades.AddRange(trades);
			};

			Connector.NewPortfolios += portfolios =>
			{
				_portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
				portfolios.ForEach(p => Connector.RegisterPortfolio(p));
			};

			Connector.PortfoliosChanged += portfolios =>
			{
				this.GuiAsync(() => _portfoliosWindow.PortfolioGrid.RefreshData());
			};

			Connector.NewPositions += positions =>
			{
				_portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);
			};

			Connector.PositionsChanged += positions =>
			{
				this.GuiAsync(() => _portfoliosWindow.PortfolioGrid.RefreshData());
			};

			// subscribe on error of order registration event
			Connector.OrdersRegisterFailed += OrdersFailed;
			Connector.StopOrdersRegisterFailed += OrdersFailed;

			// subscribe on error of order cancelling event
			Connector.OrdersCancelFailed += OrdersFailed;
			Connector.StopOrdersCancelFailed += OrdersFailed;

			// set market data provider
			_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

			try
			{
				if (File.Exists(_settingsFile))
					Connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
			}
			catch
			{
			}

			if (Connector.StorageAdapter == null)
				return;

			entityRegistry.Init();

			Connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(3);
			//Connector.StorageAdapter.Load();

			ConfigManager.RegisterService<IExchangeInfoProvider>(new ExchangeInfoProvider(entityRegistry));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_strategiesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();

			_securitiesWindow.Close();
			_strategiesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();

			Connector.Dispose();

			ConfigManager.GetService<IEntityRegistry>().DelayAction.WaitFlush();

			base.OnClosing(e);
		}

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			if (Connector.Configure(this))
				new XmlSerializer<SettingsStorage>().Serialize(Connector.Save(), _settingsFile);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				Connector.Connect();
			}
			else
			{
				Connector.Disconnect();
			}
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
					MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
			});
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowStrategiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_strategiesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}
	}
}