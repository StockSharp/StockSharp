#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 3:22 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

using StockSharp.Terminal.Layout;
using StockSharp.Terminal.Controls;
using StockSharp.Terminal.Logics;

using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock;

using StockSharp.Algo;
using StockSharp.Logging;
using StockSharp.Algo.Storages;

using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Common;
using Ecng.Xaml;

using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Configuration;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		#region Fields
		//-------------------------------------------------------------------

		private int _countWorkArea = 2;

		private bool _isConnected;
		public readonly Connector Connector;

		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly StopOrderWindow _stopOrdersWindow = new StopOrderWindow();
		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();

		private const string _settingsFile = "connection.xml";

		//-------------------------------------------------------------------
		#endregion Fields

		#region Properties
		//-------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		public LayoutManager LayoutManager { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public static MainWindow Instance { get; private set; }

		//-------------------------------------------------------------------
		#endregion Properties

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			LayoutManager = new LayoutManager(DockingManager);			
			DockingManager.DocumentClosed += DockingManager_DocumentClosed;

			Title = Title.Put("Multi connection");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			var logManager = new LogManager();
			logManager.Listeners.Add(new FileLogListener("sample.log"));

			var entityRegistry = ConfigManager.GetService<IEntityRegistry>();
			var storageRegistry = ConfigManager.GetService<IStorageRegistry>();

			Connector = new Connector(entityRegistry, storageRegistry);
			logManager.Sources.Add(Connector);

			InitConnector();
		}
		
		#region Events
		//-------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnAddDocument_Click(object sender, RoutedEventArgs e)
		{
			var newWorkArea = new LayoutDocument()
			{
				Title = "Work area #" + ++_countWorkArea,
				Content = new WorkAreaControl()
			};
			
			LayoutDocuments.Children.Add(newWorkArea);

			var offset = LayoutDocuments.Children.Count - 1;
			LayoutDocuments.SelectedContentIndex = (offset < 0) ? 0 : offset;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DockingManager_DocumentClosed(object sender, DocumentClosedEventArgs e)
		{
			var manager = (DockingManager)sender;

			if (LayoutDocuments.Children.Count == 0 &&
				manager.FloatingWindows.ToList().Count == 0)
				_countWorkArea = 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (NewControlComboBox.SelectedIndex != -1)
			{
				var workArea = (WorkAreaControl)DockingManager.ActiveContent;
				workArea.AddControl(((ComboBoxItem)NewControlComboBox.SelectedItem).Content.ToString());
				NewControlComboBox.SelectedIndex = -1;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DockingManager_OnActiveContentChanged(object sender, EventArgs e)
		{
			DockingManager.ActiveContent.DoIfElse<WorkAreaControl>(editor =>
			{
				var element = (DockingManager)sender;

			}, () =>
			{
				var element = (DockingManager)sender;

			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			Connector.Configure(this);

			new XmlSerializer<SettingsStorage>().Serialize(Connector.Save(), _settingsFile);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
				Connector.Connect();
			else
				Connector.Disconnect();
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

		private void ShowStopOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
		}

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		//-------------------------------------------------------------------
		#endregion Events

		#region Приватные методы
		//-------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		private void InitConnector()
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
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, type, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

			Connector.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
			Connector.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);

			Connector.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
			Connector.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);
			Connector.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);

			Connector.NewPortfolios += portfolios => _portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
			Connector.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

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

			Connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(3);
			Connector.StorageAdapter.Load();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fails"></param>
		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
					MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str2960);
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="isConnected"></param>
		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="window"></param>
		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		//-------------------------------------------------------------------
		#endregion Приватные методы
	}
}