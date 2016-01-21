using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Localization;
using StockSharp.Studio.Core.Commands;
using StockSharp.Terminal.Interfaces;
using StockSharp.Terminal.Properties;

namespace StockSharp.Terminal.Services
{
	public class ConnectorService
	{
		#region Events
		//-------------------------------------------------------------------

		public delegate void ChangeConnectStatusDelegate(bool isConnected);

		/// <summary>
		/// 
		/// </summary>
		public event ChangeConnectStatusDelegate ChangeConnectStatusEvent;

		private void OnChangeConnectStatusEvent(bool isConnected)
		{
			IsConnected = isConnected;

			if (ChangeConnectStatusEvent != null)
				ChangeConnectStatusEvent(isConnected);
		}


		public delegate void ErrorDelegate(string message, string caption);

		/// <summary>
		/// 
		/// </summary>
		public event ErrorDelegate ErrorEvent;

		public void OnErrorEvent(string message, string caption)
		{
			if (ErrorEvent != null)
				ErrorEvent(message, caption);
		}

		//-------------------------------------------------------------------
		#endregion Events

		#region Fields
		//-------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		private readonly Connector _connector;

		/// <summary>
		/// 
		/// </summary>
		public const string SETTINGS_FILE = "connection.xml";

		//-------------------------------------------------------------------
		#endregion Fields
			
		/// <summary>
		/// 
		/// </summary>
		public bool IsConnected { get; set; }
        
		public ConnectorService()
		{
			var entityRegistry = ConfigManager.GetService<IEntityRegistry>();
			var storageRegistry = ConfigManager.GetService<IStorageRegistry>();

			_connector = new Connector(entityRegistry, storageRegistry);
        }

		#region Public methods
		//-------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		public void InitConnector()
		{
			#region Subscribe on events
			//-------------------------------------------------------------------

			_connector.Connected += () =>
			{
				OnChangeConnectStatusEvent(true);
			};

			_connector.Disconnected += () =>
			{
				OnChangeConnectStatusEvent(false);
			};

			_connector.ConnectionError += error =>
			{
				OnChangeConnectStatusEvent(false);
				OnErrorEvent(error.ToString(), LocalizedStrings.Str2959);
			};

			_connector.Error += error =>
			{
				OnErrorEvent(error.ToString(), LocalizedStrings.Str2955);
			};

			_connector.MarketDataSubscriptionFailed += (security, type, error) =>
			{
				OnErrorEvent(error.ToString(), LocalizedStrings.Str2956Params.Put(type, security));
			};

			_connector.OrdersRegisterFailed += OrdersFailed;
			_connector.StopOrdersRegisterFailed += OrdersFailed;

			_connector.OrdersCancelFailed += OrdersFailed;
			_connector.StopOrdersCancelFailed += OrdersFailed;

            //-------------------------------------------------------------------
            #endregion Subscribe on events

            //TODO: передать объект securities в NewSecuritiesCommand
            _connector.NewSecurities += securities => new NewSecuritiesCommand().Process(this);

            //_connector.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
            _connector.NewTrades += trades => new NewTradesCommand(trades).Process(this);

            //_connector.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
            _connector.NewOrders += orders =>
            {
                foreach (var item in orders)
                {
                    new OrderCommand(item, OrderActions.Registering).Process(this);
                    //new RegisterOrderCommand(item).Process(this);
                }
            };

            //_connector.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);
            //_connector.NewStopOrders += orders => new CancelOrderCommand(orders);

            //_connector.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
            _connector.NewMyTrades += trades => new NewMyTradesCommand(trades).Process(this);

            //_connector.NewPortfolios += portfolios => _portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
            _connector.NewPortfolios += portfolios =>
            {
                foreach (var item in portfolios)
                {
                    new PortfolioCommand(item, true).Process(this);
                }
            };

            //_connector.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);
            _connector.NewPositions += positions =>
            {
                foreach (var item in positions)
                {
                    //new PositionCommand(item.
                }
            };

            // set market data provider
            //_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

            try
			{
				if (File.Exists(SETTINGS_FILE))
					_connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(SETTINGS_FILE));
			}
			catch (Exception ex)
			{
				OnErrorEvent(ex.ToString(), "Ошибка при чтении файла " + SETTINGS_FILE);
				return;
			}

			if (_connector.StorageAdapter == null)
				return;

            if (!File.Exists("StockSharp.db"))
                File.WriteAllBytes("StockSharp.db", Resources.StockSharp);

            _connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(3);
			_connector.StorageAdapter.Load();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Connect()
		{
			_connector.Connect();
        }

		/// <summary>
		/// 
		/// </summary>
		public void Disconnect()
		{
			_connector.Disconnect();
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="window"></param>
		public void Configure(Window window)
		{
			_connector.Configure(window);
		}

		/// <summary>
		/// 
		/// </summary>
		public SettingsStorage Save()
		{
			return _connector.Save();
        }


		public Connector GetConnector()
		{
			return _connector;
		}

		//-------------------------------------------------------------------
		#endregion Public methods

		#region Private methods
		//-------------------------------------------------------------------

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fails"></param>
		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			foreach (var fail in fails)
			{
				OnErrorEvent(fail.Error.ToString(), LocalizedStrings.Str2960);
			}
		}

		//-------------------------------------------------------------------
		#endregion Private methods
	}
}
