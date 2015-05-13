namespace StockSharp.ETrade.Native
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Localization;

	partial class ETradeClient : BaseLogReceiver
	{
		internal readonly ETradeDispatcher Dispatcher;
		internal readonly ETradeApi Api;

		private readonly ETradeAccountsModule _accountsModule;
		private readonly ETradeMarketModule _marketModule;
		private readonly ETradeOrderModule _orderModule;

		private readonly List<string> _portfolioNames = new List<string>();

		internal ETradeConnection Connection {get; private set;}

		public Security[] SandboxSecurities { get; set; }

		public string ConsumerKey
		{
			get { return Api.ConsumerKey; }
			set { Api.ConsumerKey = value; }
		}

		public SecureString ConsumerSecret
		{
			get { return Api.ConsumerSecret; }
			set { Api.ConsumerSecret = value; }
		}

		public OAuthToken AccessToken
		{
			get { return Connection.AccessToken; }
			set { Connection.AccessToken = value; }
		}

		public string VerificationCode { get; set; }

		public bool Sandbox
		{
			get { return Api.Sandbox; }
			set { Api.Sandbox = value; }
		}

		public bool IsConnected { get { return Connection.IsConnected; }}
		public bool IsExportStarted { get; private set; }

		public event Action ConnectionStateChanged;
		public event Action<Exception> ConnectionError;
		public event Action<Exception> Error;
		public event Action<long, List<ProductInfo>, Exception> ProductLookupResult;
		public event Action<long, PlaceEquityOrderResponse2, Exception> OrderRegisterResult;
		public event Action<long, PlaceEquityOrderResponse2, Exception> OrderReRegisterResult;
		public event Action<long, long, CancelOrderResponse2, Exception> OrderCancelResult;
		public event Action<List<AccountInfo>, Exception> AccountsData;
		public event Action<string, List<PositionInfo>, Exception> PositionsData;
		public event Action<string, List<Order>, Exception> OrdersData;
		public event Action ExportStarted;
		public event Action ExportStopped;

		public ETradeClient()
		{
			Api = new ETradeApi(this);
			Dispatcher = new ETradeDispatcher(DispatcherErrorHandler);
			Connection = new ETradeConnection(this);
			Connection.ConnectionStateChanged += OnConnectionStateChanged;

			_accountsModule = new ETradeAccountsModule(this);
			_marketModule   = new ETradeMarketModule(this);
			_orderModule    = new ETradeOrderModule(this);
		}

		private void DispatcherErrorHandler(Exception exception)
		{
			this.AddErrorLog(LocalizedStrings.Str3355Params, Dispatcher.GetThreadName(), exception);
			RaiseError(exception);
		}

		public void SetCustomAuthorizationMethod(Action<string> method)
		{
			Connection.AuthorizationAction = method;
		}

		public void LookupSecurities(string name, long transactionId)
		{
			if (!IsConnected)
				throw new InvalidOperationException(LocalizedStrings.Str3356);

			if (name.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str3357);

			_marketModule.ExecuteUserRequest(new ETradeProductLookupRequest(name), response =>
			{
				ProductLookupResult.SafeInvoke(transactionId, response.Data, response.Exception);
				_orderModule.ResetOrderUpdateSettings(null, true);
			});
		}

		public void Connect()
		{
			Dispatcher.OnRequestThreadAsync(() =>
			{
				try
				{
					Connection.Connect();
				}
				catch (Exception e)
				{
					ConnectionError.SafeInvoke(e);
				}
			});
		}

		public void Disconnect()
		{
			Dispatcher.OnRequestThreadAsync(() =>
			{
				_accountsModule.Stop();
				_marketModule.Stop();
				_orderModule.Stop();

				Connection.Disconnect();
			});
		}

		private void OnConnectionStateChanged()
		{
			ConnectionStateChanged.SafeInvoke();
			if (Connection.IsConnected)
			{
				Dispatcher.OnResponseThreadAsync(() =>
				{
					RaiseHardcodedDataEvents();
					StartExport();
				});
			}
		}

		public void StartExport()
		{
			Dispatcher.OnResponseThreadAsync(() =>
			{
				IsExportStarted = true;
				ExportStarted.SafeInvoke();

				RaiseHardcodedDataEvents();

				_accountsModule.HandleClientState();
				_marketModule.HandleClientState();
				_orderModule.HandleClientState();
			});
		}

		public void StopExport()
		{
			Dispatcher.OnResponseThreadAsync(() =>
			{
				IsExportStarted = false;
				ExportStopped.SafeInvoke();
			});
		}

		private void RaiseHardcodedDataEvents()
		{
			if (!Sandbox || SandboxSecurities == null)
				return;

			var list = SandboxSecurities.Select(sec => new ProductInfo
			{
				companyName = sec.Name, 
				exchange = sec.Board.Exchange.Name, 
				securityType = "EQ", 
				symbol = sec.Code
			}).ToList();

			ProductLookupResult.SafeInvoke(0, list, null);
		}

		private void RaiseError(Exception exception)
		{
			Error.SafeInvoke(exception);
		}
	}
}
