namespace StockSharp.Quik.Native
{
	using System;
	using System.Runtime.InteropServices;
	using System.Text;

	using Ecng.Common;
	using Ecng.Interop;

	class Api : NativeLibrary
	{
		public Api(string dllPath)
			: base(dllPath)
		{
			_connect = GetHandler<ConnectHandler>("_TRANS2QUIK_CONNECT@16");
			_disconnect = GetHandler<DisconnectHandler>("_TRANS2QUIK_DISCONNECT@12");
			_isDllConnected = GetHandler<IsDllConnectedHandler>("_TRANS2QUIK_IS_DLL_CONNECTED@12");
			_isQuikConnected = GetHandler<IsQuikConnectedHandler>("_TRANS2QUIK_IS_QUIK_CONNECTED@12");
			_sendSyncTransaction = GetHandler<SendSyncTransactionHandler>("_TRANS2QUIK_SEND_SYNC_TRANSACTION@36");
			_sendAsyncTransaction = GetHandler<SendAsyncTransactionHandler>("_TRANS2QUIK_SEND_ASYNC_TRANSACTION@16");
			_setConnectionStatusCallback = GetHandler<SetConnectionStatusCallbackHandler>("_TRANS2QUIK_SET_CONNECTION_STATUS_CALLBACK@16");
			_setTransactionsReply = GetHandler<SetTransactionsReplyHandler>("_TRANS2QUIK_SET_TRANSACTIONS_REPLY_CALLBACK@16");

			if (DllVersion >= "1.1.0.0".To<Version>())
			{
				_subscribeOrders = GetHandler<SubscribeOrdersHandler>("_TRANS2QUIK_SUBSCRIBE_ORDERS@8");
				_subscribeTrades = GetHandler<SubscribeTradesHandler>("_TRANS2QUIK_SUBSCRIBE_TRADES@8");
				_unSubscribeOrders = GetHandler<UnSubscribeOrdersHandler>("_TRANS2QUIK_UNSUBSCRIBE_ORDERS@0");
				_unSubscribeTrades = GetHandler<UnSubscribeTradesHandler>("_TRANS2QUIK_UNSUBSCRIBE_TRADES@0");
				_startOrders = GetHandler<StartOrdersHandler>("_TRANS2QUIK_START_ORDERS@4");
				_startTrades = GetHandler<StartTradesHandler>("_TRANS2QUIK_START_TRADES@4");

				_getOrderQuantity = GetHandler<GetOrderQuantityHandler>("_TRANS2QUIK_ORDER_QTY@4");
				_getOrderDate = GetHandler<GetOrderDateHandler>("_TRANS2QUIK_ORDER_DATE@4");
				_getOrderTime = GetHandler<GetOrderTimeHandler>("_TRANS2QUIK_ORDER_TIME@4");
				_getOrderActivationTime = GetHandler<GetOrderActivationTimeHandler>("_TRANS2QUIK_ORDER_ACTIVATION_TIME@4");
				_getOrderWithDrawTime = GetHandler<GetOrderWithDrawTimeHandler>("_TRANS2QUIK_ORDER_WITHDRAW_TIME@4");
				_getOrderExpiry = GetHandler<GetOrderExpiryHandler>("_TRANS2QUIK_ORDER_EXPIRY@4");
				_getOrderAccrued = GetHandler<GetOrderAccruedHandler>("_TRANS2QUIK_ORDER_ACCRUED_INT@4");
				_getOrderYield = GetHandler<GetOrderYieldHandler>("_TRANS2QUIK_ORDER_YIELD@4");
				_getOrderId = GetHandler<GetOrderIdHandler>("_TRANS2QUIK_ORDER_UID@4");
				_getOrderUserId = GetHandler<GetOrderUserIdHandler>("_TRANS2QUIK_ORDER_USERID@4");
				_getOrderAccount = GetHandler<GetOrderAccountHandler>("_TRANS2QUIK_ORDER_ACCOUNT@4");
				_getOrderBrokerRef = GetHandler<GetOrderBrokerRefHandler>("_TRANS2QUIK_ORDER_BROKERREF@4");
				_getOrderClientCode = GetHandler<GetOrderClientCodeHandler>("_TRANS2QUIK_ORDER_CLIENT_CODE@4");
				_getOrderFirmId = GetHandler<GetOrderFirmIdHandler>("_TRANS2QUIK_ORDER_FIRMID@4");

				_getTradeDate = GetHandler<GetTradeDateHandler>("_TRANS2QUIK_TRADE_DATE@4");
				_getTradeTime = GetHandler<GetTradeTimeHandler>("_TRANS2QUIK_TRADE_DATE@4");
				_getTradeIsMarginal = GetHandler<GetTradeIsMarginalHandler>("_TRANS2QUIK_TRADE_IS_MARGINAL@4");
				_getTradeAccrued = GetHandler<GetTradeAccruedHandler>("_TRANS2QUIK_TRADE_ACCRUED_INT@4");
				_getTradeYeild = GetHandler<GetTradeYeildHandler>("_TRANS2QUIK_TRADE_YIELD@4");
				_getTradeTsCommission = GetHandler<GetTradeTsCommissionHandler>("_TRANS2QUIK_TRADE_TS_COMMISSION@4");
				_getTradeClearingCentreCommission = GetHandler<GetTradeClearingCentreCommissionHandler>("_TRANS2QUIK_TRADE_CLEARING_CENTER_COMMISSION@4");
				_getTradeExchangeCommission = GetHandler<GetTradeExchangeCommissionHandler>("_TRANS2QUIK_TRADE_EXCHANGE_COMMISSION@4");
				_getTradeTradeSystemCommission = GetHandler<GetTradeTradeSystemCommissionHandler>("_TRANS2QUIK_TRADE_TRADING_SYSTEM_COMMISSION@4");
				_getTradePrice = GetHandler<GetTradePriceHandler>("_TRANS2QUIK_TRADE_PRICE2@4");
				_getTradeRepoRate = GetHandler<GetTradeRepoRateHandler>("_TRANS2QUIK_TRADE_REPO_RATE@4");
				_getTradeRepoValue = GetHandler<GetTradeRepoValueHandler>("_TRANS2QUIK_TRADE_REPO_VALUE@4");
				_getTradeRepo2Value = GetHandler<GetTradeRepo2ValueHandler>("_TRANS2QUIK_TRADE_REPO2_VALUE@4");
				_getTradeAccrued2 = GetHandler<GetTradeAccrued2Handler>("_TRANS2QUIK_TRADE_ACCRUED_INT2@4");
				_getTradeRepoTerm = GetHandler<GetTradeRepoTermHandler>("_TRANS2QUIK_TRADE_REPO_TERM@4");
				_getTradeStartDiscount = GetHandler<GetTradeStartDiscountHandler>("_TRANS2QUIK_TRADE_START_DISCOUNT@4");
				_getTradeLowerDiscount = GetHandler<GetTradeLowerDiscountHandler>("_TRANS2QUIK_TRADE_LOWER_DISCOUNT@4");
				_getTradeUpperDiscount = GetHandler<GetTradeUpperDiscountHandler>("_TRANS2QUIK_TRADE_UPPER_DISCOUNT@4");
				_getTradeBlockSecurities = GetHandler<GetTradeBlockSecuritiesHandler>("_TRANS2QUIK_TRADE_BLOCK_SECURITIES@4");
				_getTradeCurrency = GetHandler<GetTradeCurrencyHandler>("_TRANS2QUIK_TRADE_CURRENCY@4");
				_getTradeSettlementDate = GetHandler<GetTradeSettlementDateHandler>("_TRANS2QUIK_TRADE_SETTLE_DATE@4");
				_getTradeSettlementCode = GetHandler<GetTradeSettlementCodeHandler>("_TRANS2QUIK_TRADE_SETTLE_CODE@4");
				_getTradeSettlementCurrency = GetHandler<GetTradeSettlementCurrencyHandler>("_TRANS2QUIK_TRADE_SETTLE_CURRENCY@4");
				_getTradeAccount = GetHandler<GetTradeAccountHandler>("_TRANS2QUIK_TRADE_ACCOUNT@4");
				_getTradeBrokerRef = GetHandler<GetTradeBrokerRefHandler>("_TRANS2QUIK_TRADE_BROKERREF@4");
				_getTradeClientCode = GetHandler<GetTradeClientCodeHandler>("_TRANS2QUIK_TRADE_CLIENT_CODE@4");
				_getTradeUserId = GetHandler<GetTradeUserIdHandler>("_TRANS2QUIK_TRADE_USERID@4");
				_getTradeFirmId = GetHandler<GetTradeFirmIdHandler>("_TRANS2QUIK_TRADE_FIRMID@4");
				_getTradePartnerFirmId = GetHandler<GetTradePartnerFirmIdHandler>("_TRANS2QUIK_TRADE_PARTNER_FIRMID@4");
				_getTradeExchangeCode = GetHandler<GetTradeExchangeCodeHandler>("_TRANS2QUIK_TRADE_EXCHANGE_CODE@4");
				_getTradeStatiodId = GetHandler<GetTradeStatiodIdHandler>("_TRANS2QUIK_TRADE_STATION_ID@4");
			}

			if (DllVersion >= "1.2".To<Version>())
			{
				_getOrderVisibleQuantity = GetHandler<GetOrderVisibleQuantityHandler>("_TRANS2QUIK_ORDER_VISIBLE_QTY@4");
				_getOrderDateTime = GetHandler<GetOrderDateTimeHandler>("_TRANS2QUIK_ORDER_DATE_TIME@8");
				_getOrderFiletime = GetHandler<GetOrderFiletimeHandler>("_TRANS2QUIK_ORDER_FILETIME@4");
				_getOrderPeriod = GetHandler<GetOrderPeriodHandler>("_TRANS2QUIK_ORDER_PERIOD@4");
				_getOrderWithDrawFileTime = GetHandler<GetOrderWithDrawFileTimeHandler>("_TRANS2QUIK_ORDER_WITHDRAW_FILETIME@4");

				_getTradePeriod = GetHandler<GetTradePeriodHandler>("_TRANS2QUIK_TRADE_PERIOD@4");
				_getTradeFileTime = GetHandler<GetTradeFileTimeHandler>("_TRANS2QUIK_TRADE_FILETIME@4");
				_getTradeDateTime = GetHandler<GetTradeDateTimeHandler>("_TRANS2QUIK_TRADE_DATE_TIME@8");
			}
		}

		#region Connect

		public delegate int ConnectHandler(string lpcstrConnectionParamsString, ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly ConnectHandler _connect;

		public ConnectHandler Connect
		{
			get
			{
				ThrowIfDisposed();
				return _connect;
			}
		}

		#endregion

		#region Disconnect

		public delegate int DisconnectHandler(ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly DisconnectHandler _disconnect;

		public DisconnectHandler Disconnect
		{
			get
			{
				ThrowIfDisposed();
				return _disconnect;
			}
		}

		#endregion

		#region IsDllConnected

		public delegate int IsDllConnectedHandler(ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly IsDllConnectedHandler _isDllConnected;

		public IsDllConnectedHandler IsDllConnected
		{
			get
			{
				ThrowIfDisposed();
				return _isDllConnected;
			}
		}

		#endregion

		#region IsQuikConnected

		public delegate int IsQuikConnectedHandler(ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly IsQuikConnectedHandler _isQuikConnected;

		public IsQuikConnectedHandler IsQuikConnected
		{
			get
			{
				ThrowIfDisposed();
				return _isQuikConnected;
			}
		}

		#endregion

		#region SendSyncTransaction

		public delegate int SendSyncTransactionHandler(string lpstTransactionString, out long pnReplyCode, out uint pdwTransId, out double pdOrderNum, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrResultMessage, uint dwResultMessageSize, ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly SendSyncTransactionHandler _sendSyncTransaction;

		public SendSyncTransactionHandler SendSyncTransaction
		{
			get
			{
				ThrowIfDisposed();
				return _sendSyncTransaction;
			}
		}

		#endregion

		#region SendAsyncTransaction

		public delegate int SendAsyncTransactionHandler(string lpstTransactionString, ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly SendAsyncTransactionHandler _sendAsyncTransaction;

		public SendAsyncTransactionHandler SendAsyncTransaction
		{
			get
			{
				ThrowIfDisposed();
				return _sendAsyncTransaction;
			}
		}

		#endregion

		#region SetConnectionStatusCallback

		public delegate void ConnectionStatusCallback(int connectionEvent, int extendedErrorCode, [MarshalAs(UnmanagedType.AnsiBStr)]string infoMessage);

		public delegate int SetConnectionStatusCallbackHandler(ConnectionStatusCallback pfConnectionStatusCallback, ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly SetConnectionStatusCallbackHandler _setConnectionStatusCallback;

		public SetConnectionStatusCallbackHandler SetConnectionStatusCallback
		{
			get
			{
				ThrowIfDisposed();
				return _setConnectionStatusCallback;
			}
		}

		#endregion

		#region SetTransactionsReply

		public delegate void TransactionReplyCallback(int transactionResult, int transactionExtendedErrorCode, int transactionReplyCode, uint transId, double orderNum, [MarshalAs(UnmanagedType.LPStr)]string transactionReplyMessage);

		public delegate int SetTransactionsReplyHandler(TransactionReplyCallback pfTransactionReplyCallback, ref long pnExtendedErrorCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrErrorMessage, uint dwErrorMessageSize);

		private readonly SetTransactionsReplyHandler _setTransactionsReply;

		public SetTransactionsReplyHandler SetTransactionsReply
		{
			get
			{
				ThrowIfDisposed();
				return _setTransactionsReply;
			}
		}

		#endregion

		#region SubscribeOrders

		public delegate int SubscribeOrdersHandler([MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrClassCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrSeccodes);

		private readonly SubscribeOrdersHandler _subscribeOrders;

		public SubscribeOrdersHandler SubscribeOrders
		{
			get
			{
				ThrowIfDisposed();
				return _subscribeOrders;
			}
		}

		#endregion

		#region SubscribeTrades

		public delegate int SubscribeTradesHandler([MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrClassCode, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpstrSeccodes);

		private readonly SubscribeTradesHandler _subscribeTrades;

		public SubscribeTradesHandler SubscribeTrades
		{
			get
			{
				ThrowIfDisposed();
				return _subscribeTrades;
			}
		}

		#endregion

		#region UnSubscribeOrders

		public delegate int UnSubscribeOrdersHandler();

		private readonly UnSubscribeOrdersHandler _unSubscribeOrders;

		public UnSubscribeOrdersHandler UnSubscribeOrders
		{
			get
			{
				ThrowIfDisposed();
				return _unSubscribeOrders;
			}
		}

		#endregion

		#region UnSubscribeTrades

		public delegate int UnSubscribeTradesHandler();

		private readonly UnSubscribeTradesHandler _unSubscribeTrades;

		public UnSubscribeTradesHandler UnSubscribeTrades
		{
			get
			{
				ThrowIfDisposed();
				return _unSubscribeTrades;
			}
		}

		#endregion

		#region StartOrders

		public delegate void OrderStatusCallback(int mode, uint transId, double orderNum, [MarshalAs(UnmanagedType.LPStr)]string classCode, [MarshalAs(UnmanagedType.LPStr)]string secCode, double price, int balance, double volume, int sell, int status, int orderDescriptor);

		public delegate int StartOrdersHandler(OrderStatusCallback pfnOrderStatusCallback);

		private readonly StartOrdersHandler _startOrders;

		public StartOrdersHandler StartOrders
		{
			get
			{
				ThrowIfDisposed();
				return _startOrders;
			}
		}

		#endregion

		#region StartTrades

		public delegate void TradeStatusCallback(int mode, double tradeNum, double orderNum, [MarshalAs(UnmanagedType.LPStr)]string classCode, [MarshalAs(UnmanagedType.LPStr)]string secCode, double price, int balance, double volume, int sell, int tradeDescriptor);

		public delegate int StartTradesHandler(TradeStatusCallback pfnTradesStatusCallback);

		private readonly StartTradesHandler _startTrades;

		public StartTradesHandler StartTrades
		{
			get
			{
				ThrowIfDisposed();
				return _startTrades;
			}
		}

		#endregion

		#region GetOrderQuantity

		public delegate int GetOrderQuantityHandler(int orderDescriptor);

		private readonly GetOrderQuantityHandler _getOrderQuantity;

		public GetOrderQuantityHandler GetOrderQuantity
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderQuantity;
			}
		}

		#endregion

		#region GetOrderVisibleQuantity

		public delegate int GetOrderVisibleQuantityHandler(int orderDescriptor);

		private readonly GetOrderVisibleQuantityHandler _getOrderVisibleQuantity;

		public GetOrderVisibleQuantityHandler GetOrderVisibleQuantity
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderVisibleQuantity;
			}
		}

		#endregion

		#region GetOrderDate

		public delegate int GetOrderDateHandler(int orderDescriptor);

		private readonly GetOrderDateHandler _getOrderDate;

		public GetOrderDateHandler GetOrderDate
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderDate;
			}
		}

		#endregion

		#region GetOrderTime

		public delegate int GetOrderTimeHandler(int orderDescriptor);
		
		private readonly GetOrderTimeHandler _getOrderTime;

		public GetOrderTimeHandler GetOrderTime
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderTime;
			}
		}

		#endregion

		#region GetOrderDateTime

		public delegate int GetOrderDateTimeHandler(int orderDescriptor, int timeType);

		private readonly GetOrderDateTimeHandler _getOrderDateTime;

		public GetOrderDateTimeHandler GetOrderDateTime
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderDateTime;
			}
		}

		#endregion

		#region GetOrderFiletime

		public delegate int GetOrderFiletimeHandler(int orderDescriptor);

		private readonly GetOrderFiletimeHandler _getOrderFiletime;

		public GetOrderFiletimeHandler GetOrderFiletime
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderFiletime;
			}
		}

		#endregion

		#region GetOrderPeriod

		public delegate int GetOrderPeriodHandler(int orderDescriptor);

		private readonly GetOrderPeriodHandler _getOrderPeriod;

		public GetOrderPeriodHandler GetOrderPeriod
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderPeriod;
			}
		}

		#endregion

		#region GetOrderActivationTime

		public delegate int GetOrderActivationTimeHandler(int orderDescriptor);

		private readonly GetOrderActivationTimeHandler _getOrderActivationTime;

		public GetOrderActivationTimeHandler GetOrderActivationTime
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderActivationTime;
			}
		}

		#endregion

		#region GetOrderWithDrawTime

		public delegate int GetOrderWithDrawTimeHandler(int orderDescriptor);

		private readonly GetOrderWithDrawTimeHandler _getOrderWithDrawTime;

		public GetOrderWithDrawTimeHandler GetOrderWithDrawTime
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderWithDrawTime;
			}
		}

		#endregion

		#region GetOrderWithDrawFileTime

		public delegate int GetOrderWithDrawFileTimeHandler(int orderDescriptor);

		private readonly GetOrderWithDrawFileTimeHandler _getOrderWithDrawFileTime;

		public GetOrderWithDrawFileTimeHandler GetOrderWithDrawFileTime
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderWithDrawFileTime;
			}
		}

		#endregion

		#region GetOrderExpiry

		public delegate int GetOrderExpiryHandler(int orderDescriptor);

		private readonly GetOrderExpiryHandler _getOrderExpiry;

		public GetOrderExpiryHandler GetOrderExpiry
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderExpiry;
			}
		}

		#endregion

		#region GetOrderAccrued

		public delegate double GetOrderAccruedHandler(int orderDescriptor);

		private readonly GetOrderAccruedHandler _getOrderAccrued;

		public GetOrderAccruedHandler GetOrderAccrued
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderAccrued;
			}
		}

		#endregion

		#region GetOrderYield

		public delegate double GetOrderYieldHandler(int orderDescriptor);

		private readonly GetOrderYieldHandler _getOrderYield;

		public GetOrderYieldHandler GetOrderYield
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderYield;
			}
		}

		#endregion

		#region GetOrderId

		public delegate int GetOrderIdHandler(int orderDescriptor);

		private readonly GetOrderIdHandler _getOrderId;

		public GetOrderIdHandler GetOrderId
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderId;
			}
		}

		#endregion

		#region GetOrderUserId

		public delegate string GetOrderUserIdHandler(int orderDescriptor);

		private readonly GetOrderUserIdHandler _getOrderUserId;

		public GetOrderUserIdHandler GetOrderUserId
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderUserId;
			}
		}

		#endregion

		#region GetOrderAccount

		public delegate string GetOrderAccountHandler(int orderDescriptor);

		private readonly GetOrderAccountHandler _getOrderAccount;

		public GetOrderAccountHandler GetOrderAccount
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderAccount;
			}
		}

		#endregion

		#region GetOrderBrokerRef

		public delegate string GetOrderBrokerRefHandler(int orderDescriptor);

		private readonly GetOrderBrokerRefHandler _getOrderBrokerRef;

		public GetOrderBrokerRefHandler GetOrderBrokerRef
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderBrokerRef;
			}
		}

		#endregion

		#region GetOrderClientCode

		public delegate string GetOrderClientCodeHandler(int orderDescriptor);

		private readonly GetOrderClientCodeHandler _getOrderClientCode;

		public GetOrderClientCodeHandler GetOrderClientCode
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderClientCode;
			}
		}

		#endregion

		#region GetOrderFirmId

		public delegate string GetOrderFirmIdHandler(int orderDescriptor);

		private readonly GetOrderFirmIdHandler _getOrderFirmId;

		public GetOrderFirmIdHandler GetOrderFirmId
		{
			get
			{
				ThrowIfDisposed();
				return _getOrderFirmId;
			}
		}

		#endregion

		#region GetTradeDate

		public delegate int GetTradeDateHandler(int tradeDescriptor);

		private readonly GetTradeDateHandler _getTradeDate;

		public GetTradeDateHandler GetTradeDate
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeDate;
			}
		}

		#endregion

		#region GetTradeTime

		public delegate int GetTradeTimeHandler(int tradeDescriptor);

		private readonly GetTradeTimeHandler _getTradeTime;

		public GetTradeTimeHandler GetTradeTime
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeTime;
			}
		}

		#endregion

		#region GetTradePeriod

		public delegate int GetTradePeriodHandler(int tradeDescriptor);

		private readonly GetTradePeriodHandler _getTradePeriod;

		public GetTradePeriodHandler GetTradePeriod
		{
			get
			{
				ThrowIfDisposed();
				return _getTradePeriod;
			}
		}

		#endregion

		#region GetTradeFileTime

		public delegate int GetTradeFileTimeHandler(int tradeDescriptor);

		private readonly GetTradeFileTimeHandler _getTradeFileTime;

		public GetTradeFileTimeHandler GetTradeFileTime
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeFileTime;
			}
		}

		#endregion

		#region GetTradeDateTime

		public delegate int GetTradeDateTimeHandler(int tradeDescriptor, int timeType);

		private readonly GetTradeDateTimeHandler _getTradeDateTime;

		public GetTradeDateTimeHandler GetTradeDateTime
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeDateTime;
			}
		}

		#endregion

		#region GetTradeIsMarginal

		public delegate int GetTradeIsMarginalHandler(int tradeDescriptor);

		private readonly GetTradeIsMarginalHandler _getTradeIsMarginal;

		public GetTradeIsMarginalHandler GetTradeIsMarginal
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeIsMarginal;
			}
		}

		#endregion

		#region GetTradeAccrued

		public delegate double GetTradeAccruedHandler(int tradeDescriptor);

		private readonly GetTradeAccruedHandler _getTradeAccrued;

		public GetTradeAccruedHandler GetTradeAccrued
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeAccrued;
			}
		}

		#endregion

		#region GetTradeYeild

		public delegate double GetTradeYeildHandler(int tradeDescriptor);

		private readonly GetTradeYeildHandler _getTradeYeild;

		public GetTradeYeildHandler GetTradeYeild
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeYeild;
			}
		}

		#endregion

		#region GetTradeTsCommission

		public delegate double GetTradeTsCommissionHandler(int tradeDescriptor);

		private readonly GetTradeTsCommissionHandler _getTradeTsCommission;

		public GetTradeTsCommissionHandler GetTradeTsCommission
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeTsCommission;
			}
		}

		#endregion

		#region GetTradeClearingCentreCommission

		public delegate double GetTradeClearingCentreCommissionHandler(int tradeDescriptor);

		private readonly GetTradeClearingCentreCommissionHandler _getTradeClearingCentreCommission;

		public GetTradeClearingCentreCommissionHandler GetTradeClearingCentreCommission
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeClearingCentreCommission;
			}
		}

		#endregion

		#region GetTradeExchangeCommission

		public delegate double GetTradeExchangeCommissionHandler(int tradeDescriptor);

		private readonly GetTradeExchangeCommissionHandler _getTradeExchangeCommission;

		public GetTradeExchangeCommissionHandler GetTradeExchangeCommission
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeExchangeCommission;
			}
		}

		#endregion

		#region GetTradeTradeSystemCommission

		public delegate double GetTradeTradeSystemCommissionHandler(int tradeDescriptor);

		private readonly GetTradeTradeSystemCommissionHandler _getTradeTradeSystemCommission;

		public GetTradeTradeSystemCommissionHandler GetTradeTradeSystemCommission
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeTradeSystemCommission;
			}
		}

		#endregion

		#region GetTradePrice

		public delegate double GetTradePriceHandler(int tradeDescriptor);

		private readonly GetTradePriceHandler _getTradePrice;

		public GetTradePriceHandler GetTradePrice
		{
			get
			{
				ThrowIfDisposed();
				return _getTradePrice;
			}
		}

		#endregion

		#region GetTradeRepoRate

		public delegate double GetTradeRepoRateHandler(int tradeDescriptor);

		private readonly GetTradeRepoRateHandler _getTradeRepoRate;

		public GetTradeRepoRateHandler GetTradeRepoRate
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeRepoRate;
			}
		}

		#endregion

		#region GetTradeRepoValue

		public delegate double GetTradeRepoValueHandler(int tradeDescriptor);

		private readonly GetTradeRepoValueHandler _getTradeRepoValue;

		public GetTradeRepoValueHandler GetTradeRepoValue
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeRepoValue;
			}
		}

		#endregion

		#region GetTradeRepo2Value

		public delegate double GetTradeRepo2ValueHandler(int tradeDescriptor);

		private readonly GetTradeRepo2ValueHandler _getTradeRepo2Value;

		public GetTradeRepo2ValueHandler GetTradeRepo2Value
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeRepo2Value;
			}
		}

		#endregion

		#region GetTradeAccrued2

		public delegate double GetTradeAccrued2Handler(int tradeDescriptor);

		private readonly GetTradeAccrued2Handler _getTradeAccrued2;

		public GetTradeAccrued2Handler GetTradeAccrued2
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeAccrued2;
			}
		}

		#endregion

		#region GetTradeRepoTerm

		public delegate int GetTradeRepoTermHandler(int tradeDescriptor);

		private readonly GetTradeRepoTermHandler _getTradeRepoTerm;

		public GetTradeRepoTermHandler GetTradeRepoTerm
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeRepoTerm;
			}
		}

		#endregion

		#region GetTradeStartDiscount

		public delegate double GetTradeStartDiscountHandler(int tradeDescriptor);

		private readonly GetTradeStartDiscountHandler _getTradeStartDiscount;

		public GetTradeStartDiscountHandler GetTradeStartDiscount
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeStartDiscount;
			}
		}

		#endregion

		#region GetTradeLowerDiscount

		public delegate double GetTradeLowerDiscountHandler(int tradeDescriptor);

		private readonly GetTradeLowerDiscountHandler _getTradeLowerDiscount;

		public GetTradeLowerDiscountHandler GetTradeLowerDiscount
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeLowerDiscount;
			}
		}

		#endregion

		#region GetTradeUpperDiscount

		public delegate double GetTradeUpperDiscountHandler(int tradeDescriptor);

		private readonly GetTradeUpperDiscountHandler _getTradeUpperDiscount;

		public GetTradeUpperDiscountHandler GetTradeUpperDiscount
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeUpperDiscount;
			}
		}

		#endregion

		#region GetTradeBlockSecurities

		public delegate int GetTradeBlockSecuritiesHandler(int tradeDescriptor);

		private readonly GetTradeBlockSecuritiesHandler _getTradeBlockSecurities;

		public GetTradeBlockSecuritiesHandler GetTradeBlockSecurities
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeBlockSecurities;
			}
		}

		#endregion

		#region GetTradeCurrency

		public delegate string GetTradeCurrencyHandler(int tradeDescriptor);

		private readonly GetTradeCurrencyHandler _getTradeCurrency;

		public GetTradeCurrencyHandler GetTradeCurrency
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeCurrency;
			}
		}

		#endregion

		#region GetTradeSettlementDate

		public delegate int GetTradeSettlementDateHandler(int tradeDescriptor);

		private readonly GetTradeSettlementDateHandler _getTradeSettlementDate;

		public GetTradeSettlementDateHandler GetTradeSettlementDate
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeSettlementDate;
			}
		}

		#endregion

		#region GetTradeSettlementCode

		public delegate string GetTradeSettlementCodeHandler(int tradeDescriptor);

		private readonly GetTradeSettlementCodeHandler _getTradeSettlementCode;

		public GetTradeSettlementCodeHandler GetTradeSettlementCode
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeSettlementCode;
			}
		}

		#endregion

		#region GetTradeSettlementCurrency

		public delegate string GetTradeSettlementCurrencyHandler(int tradeDescriptor);

		private readonly GetTradeSettlementCurrencyHandler _getTradeSettlementCurrency;

		public GetTradeSettlementCurrencyHandler GetTradeSettlementCurrency
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeSettlementCurrency;
			}
		}

		#endregion

		#region GetTradeAccount

		public delegate string GetTradeAccountHandler(int tradeDescriptor);

		private readonly GetTradeAccountHandler _getTradeAccount;

		public GetTradeAccountHandler GetTradeAccount
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeAccount;
			}
		}

		#endregion

		#region GetTradeBrokerRef

		public delegate string GetTradeBrokerRefHandler(int tradeDescriptor);

		private readonly GetTradeBrokerRefHandler _getTradeBrokerRef;

		public GetTradeBrokerRefHandler GetTradeBrokerRef
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeBrokerRef;
			}
		}

		#endregion

		#region GetTradeClientCode

		public delegate string GetTradeClientCodeHandler(int tradeDescriptor);

		private readonly GetTradeClientCodeHandler _getTradeClientCode;

		public GetTradeClientCodeHandler GetTradeClientCode
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeClientCode;
			}
		}

		#endregion

		#region GetTradeUserId

		public delegate string GetTradeUserIdHandler(int tradeDescriptor);

		private readonly GetTradeUserIdHandler _getTradeUserId;

		public GetTradeUserIdHandler GetTradeUserId
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeUserId;
			}
		}

		#endregion

		#region GetTradeFirmId

		public delegate string GetTradeFirmIdHandler(int tradeDescriptor);

		private readonly GetTradeFirmIdHandler _getTradeFirmId;

		public GetTradeFirmIdHandler GetTradeFirmId
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeFirmId;
			}
		}

		#endregion

		#region GetTradePartnerFirmId

		public delegate string GetTradePartnerFirmIdHandler(int tradeDescriptor);

		private readonly GetTradePartnerFirmIdHandler _getTradePartnerFirmId;

		public GetTradePartnerFirmIdHandler GetTradePartnerFirmId
		{
			get
			{
				ThrowIfDisposed();
				return _getTradePartnerFirmId;
			}
		}

		#endregion

		#region GetTradeExchangeCode

		public delegate string GetTradeExchangeCodeHandler(int tradeDescriptor);

		private readonly GetTradeExchangeCodeHandler _getTradeExchangeCode;

		public GetTradeExchangeCodeHandler GetTradeExchangeCode
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeExchangeCode;
			}
		}

		#endregion

		#region GetTradeStatiodId

		public delegate string GetTradeStatiodIdHandler(int tradeDescriptor);

		private readonly GetTradeStatiodIdHandler _getTradeStatiodId;

		public GetTradeStatiodIdHandler GetTradeStatiodId
		{
			get
			{
				ThrowIfDisposed();
				return _getTradeStatiodId;
			}
		}

		#endregion
	}
}