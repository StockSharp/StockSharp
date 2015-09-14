namespace StockSharp.ETrade
{
	using System;

	using Ecng.Common;

	using StockSharp.ETrade.Native;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The messages adapter for ETrade.
	/// </summary>
	public partial class ETradeMessageAdapter : MessageAdapter
	{
		private ETradeClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="ETradeMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public ETradeMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			this.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return false; }
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new ETradeOrderCondition();
		}

		private void DisposeClient()
		{
			_client.ConnectionStateChanged -= ClientOnConnectionStateChanged;
			_client.ConnectionError -= ClientOnConnectionError;
			_client.Error -= SendOutError;

			_client.OrderRegisterResult -= ClientOnOrderRegisterResult;
			_client.OrderReRegisterResult -= ClientOnOrderRegisterResult;
			_client.OrderCancelResult -= ClientOnOrderCancelResult;
			_client.AccountsData -= ClientOnAccountsData;
			_client.PositionsData -= ClientOnPositionsData;
			_client.OrdersData -= ClientOnOrdersData;

			_client.ProductLookupResult -= ClientOnProductLookupResult;

			_client.Disconnect();
		}

		/// <summary>
		/// Send incoming message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					if (_client != null)
					{
						try
						{
							DisposeClient();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_client = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_client = new ETradeClient
					{
						ConsumerKey = ConsumerKey,
						ConsumerSecret = ConsumerSecret,
						AccessToken = AccessToken,
						Sandbox = Sandbox,
						VerificationCode = VerificationCode,
					};

					_client.ConnectionStateChanged += ClientOnConnectionStateChanged;
					_client.ConnectionError += ClientOnConnectionError;
					_client.Error += SendOutError;

					_client.OrderRegisterResult += ClientOnOrderRegisterResult;
					_client.OrderReRegisterResult += ClientOnOrderRegisterResult;
					_client.OrderCancelResult += ClientOnOrderCancelResult;
					_client.AccountsData += ClientOnAccountsData;
					_client.PositionsData += ClientOnPositionsData;
					_client.OrdersData += ClientOnOrdersData;

					_client.ProductLookupResult += ClientOnProductLookupResult;

					_client.Parent = this;
					_client.Connect();

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					DisposeClient();
					_client = null;

					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					_client.LookupSecurities(lookupMsg.SecurityId.SecurityCode, lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					_client.RegisterOrder(
						regMsg.PortfolioName,
						regMsg.SecurityId.SecurityCode,
						regMsg.Side,
						regMsg.Price,
						regMsg.Volume,
						regMsg.TransactionId,
						regMsg.TimeInForce == TimeInForce.MatchOrCancel,
						regMsg.TillDate,
						regMsg.OrderType,
						(ETradeOrderCondition)regMsg.Condition);

					break;
				}

				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (replaceMsg.OldOrderId == null)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(replaceMsg.OldTransactionId));

					SaveOrder(replaceMsg.OldTransactionId, replaceMsg.OldOrderId.Value);

					_client.ReRegisterOrder(
						replaceMsg.OldOrderId.Value,
						replaceMsg.PortfolioName,
						replaceMsg.Price,
						replaceMsg.Volume,
						replaceMsg.TransactionId,
						replaceMsg.TimeInForce == TimeInForce.MatchOrCancel,
						replaceMsg.TillDate,
						replaceMsg.OrderType,
						(ETradeOrderCondition)replaceMsg.Condition);

					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					if (cancelMsg.OrderId == null)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

					_client.CancelOrder(cancelMsg.TransactionId, cancelMsg.OrderId.Value, cancelMsg.PortfolioName);
					break;
				}
			}
		}

		/// <summary>
		/// Connection state changed callback.
		/// </summary>
		private void ClientOnConnectionStateChanged()
		{
			this.AddInfoLog(LocalizedStrings.Str3364Params, _client.IsConnected ? LocalizedStrings.Str3365 : LocalizedStrings.Str3366);
			SendOutMessage(_client.IsConnected ? (Message)new ConnectMessage() : new DisconnectMessage());
		}

		/// <summary>
		/// Connection error callback.
		/// </summary>
		/// <param name="ex">Error connection.</param>
		private void ClientOnConnectionError(Exception ex)
		{
			this.AddInfoLog(LocalizedStrings.Str3458Params.Put(ex.Message));
			SendOutMessage(new ConnectMessage { Error = ex });
		}

		/// <summary>
		/// Set own authorization mode (the default is browser uses).
		/// </summary>
		/// <param name="method">ETrade authorization method.</param>
		public void SetCustomAuthorizationMethod(Action<string> method)
		{
			_client.SetCustomAuthorizationMethod(method);
		}
	}
}
