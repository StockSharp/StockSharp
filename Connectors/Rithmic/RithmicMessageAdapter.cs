#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rithmic.Rithmic
File: RithmicMessageAdapter.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rithmic
{
	using System;
	using System.Linq;

	using com.omnesys.omne.om;
	using com.omnesys.rapi;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The message adapter for Rithmic.
	/// </summary>
	public partial class RithmicMessageAdapter : MessageAdapter
	{
		private readonly SynchronizedDictionary<ConnectionId, bool?> _connStates = new SynchronizedDictionary<ConnectionId, bool?>();
		private RithmicClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="RithmicMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public RithmicMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMessage(MessageTypes.ChangePassword);
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new RithmicOrderCondition();
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired => false;

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup => true;

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_accounts.Clear();
					_quotes.Clear();

					if (_client != null)
					{
						try
						{
							_client.Session.logout();
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

					Connect();
					
					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_client.Session.logout();

					break;
				}

				case MessageTypes.OrderRegister:
					ProcessRegisterMessage((OrderRegisterMessage)message);
					break;

				case MessageTypes.OrderReplace:
					ProcessReplaceMessage((OrderReplaceMessage)message);
					break;

				case MessageTypes.OrderCancel:
					ProcessCancelMessage((OrderCancelMessage)message);
					break;

				case MessageTypes.OrderGroupCancel:
					ProcessGroupCancelMessage((OrderGroupCancelMessage)message);
					break;

				case MessageTypes.OrderStatus:
					ProcessOrderStatusMessage();
					break;

				case MessageTypes.SecurityLookup:
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					_client.Session.getAccounts();
					break;

				case MessageTypes.Portfolio:
					ProcessPortfolioMessage((PortfolioMessage)message);
					break;

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.ChangePassword:
					var newPassword = ((ChangePasswordMessage)message).NewPassword;
					_client.Session.changePassword(Password.To<string>(), newPassword.To<string>());
					break;
			}
		}

		private void Connect()
		{
			if (UserName.IsEmpty() || Password.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str3456);

			_client = new RithmicClient(this, AdminConnectionPoint, CertFile, DomainServerAddress, DomainName,
				LicenseServerAddress, LocalBrokerAddress, LoggerAddress, LogFileName);

			_client.OrderInfo += SessionHolderOnOrderInfo;
			_client.OrderBust += SessionHolderOnOrderBust;
			_client.OrderCancel += SessionHolderOnOrderCancel;
			_client.OrderCancelFailure += SessionHolderOnOrderCancelFailure;
			_client.OrderFailure += SessionHolderOnOrderFailure;
			_client.OrderFill += SessionHolderOnOrderFill;
			_client.OrderLineUpdate += SessionHolderOnOrderLineUpdate;
			_client.OrderModify += SessionHolderOnOrderModify;
			_client.OrderModifyFailure += SessionHolderOnOrderModifyFailure;
			_client.OrderReject += SessionHolderOnOrderReject;
			_client.OrderReport += SessionHolderOnOrderReport;
			_client.OrderStatus += SessionHolderOnOrderStatus;
			_client.OrderReplay += SessionHolderOnOrderReplay;

			_client.Execution += SessionHolderOnExecution;

			_client.AccountPnL += SessionHolderOnAccountPnL;
			_client.AccountPnLUpdate += SessionHolderOnAccountPnLUpdate;
			_client.AccountRms += SessionHolderOnAccountRms;
			_client.Accounts += SessionHolderOnAccounts;
			_client.AccountSodUpdate += SessionHolderOnAccountSodUpdate;

			_client.TransactionError += SendOutError;

			_client.SecurityBinaryContracts += SessionHolderOnSecurityBinaryContracts;
			_client.SecurityInstrumentByUnderlying += SessionHolderOnSecurityInstrumentByUnderlying;
			_client.SecurityOptions += SessionHolderOnSecurityOptions;
			_client.SecurityRefData += SessionHolderOnSecurityRefData;

			_client.Exchanges += SessionHolderOnExchanges;

			_client.BidQuote += SessionHolderOnBidQuote;
			_client.BestBidQuote += SessionHolderOnBestBidQuote;
			_client.AskQuote += SessionHolderOnAskQuote;
			_client.BestAskQuote += SessionHolderOnBestAskQuote;
			_client.EndQuote += SessionHolderOnEndQuote;
			_client.Level1 += SessionHolderOnLevel1;
			_client.OrderBook += SessionHolderOnOrderBook;
			_client.SettlementPrice += SessionHolderOnSettlementPrice;
			_client.TradeCondition += SessionHolderOnTradeCondition;
			_client.TradePrint += SessionHolderOnTradePrint;
			_client.TradeVolume += SessionHolderOnTradeVolume;
			_client.TradeReplay += SessionHolderOnTradeReplay;

			_client.TimeBar += SessionHolderOnTimeBar;
			_client.TimeBarReplay += SessionHolderOnTimeBarReplay;

			_client.MarketDataError += SendOutError;

			_client.Alert += SessionHolderOnAlert;
			_client.PasswordChange += SessionHolderOnPasswordChange;

			_connStates[ConnectionId.TradingSystem] = TransactionConnectionPoint.IsEmpty() ? (bool?)null : false;
			_connStates[ConnectionId.MarketData] = MarketDataConnectionPoint.IsEmpty() ? (bool?)null : false;
			_connStates[ConnectionId.PnL] = PositionConnectionPoint.IsEmpty() ? (bool?)null : false;
			_connStates[ConnectionId.History] = HistoricalConnectionPoint.IsEmpty() ? (bool?)null : false;

			_client.Session.login(_client.Callbacks,
				UserName, Password.To<string>(),
				MarketDataConnectionPoint,
				TransactionalUserName.IsEmpty() ? UserName : TransactionalUserName,
				(TransactionalPassword.IsEmpty() ? Password : TransactionalPassword).To<string>(),
				TransactionConnectionPoint,
				PositionConnectionPoint,
				HistoricalConnectionPoint);
		}

		private void SessionHolderOnPasswordChange(PasswordChangeInfo info)
		{
			try
			{
				SendOutMessage(new ChangePasswordMessage
				{
					Error = info.RpCode == 0 ? null : new InvalidOperationException(OMErrors.OMgetErrorDesc(info.RpCode))
				});
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private void SessionHolderOnAlert(AlertInfo info)
		{
			try
			{
				if (!_connStates.ContainsKey(info.ConnectionId))
				{
					this.AddErrorLog("Received alert for unexpected connection id ({0}):\n{1}",
						info.ConnectionId, info.DumpableToString());

					return;
				}

				this.AddInfoLog("{0}: {1} - '{2}'", info.AlertType, info.ConnectionId, info.Message);

				switch (info.AlertType)
				{
					case AlertType.ConnectionOpened:
					case AlertType.TradingEnabled:
						break;

					case AlertType.LoginComplete:
					{
						var dict = _connStates;

						bool canProcess;

						lock (dict.SyncRoot)
						{
							dict[info.ConnectionId] = true;
							canProcess = dict.Values.All(connected => connected == null || connected == true);
						}

						if (canProcess)
							SendOutMessage(new ConnectMessage());

						break;
					}

					case AlertType.ConnectionClosed:
					{
						var dict = _connStates;

						bool canProcess;

						lock (dict.SyncRoot)
						{
							dict[info.ConnectionId] = false;
							canProcess = dict.Values.All(connected => connected == null || connected == false);
						}

						if (canProcess)
						{
							SendOutMessage(new DisconnectMessage());
							_client.Session.shutdown();
						}

						break;
					}

					case AlertType.LoginFailed:
					case AlertType.ServiceError:
					case AlertType.ForcedLogout:
					{
						this.AddErrorLog(info.AlertType.ToString());

						var dict = _connStates;

						bool canProcess;

						lock (dict.SyncRoot)
						{
							dict[info.ConnectionId] = false;
							canProcess = dict.Values.All(connected => connected == null || connected == false);
						}

						if (canProcess)
							SendOutMessage(new ConnectMessage { Error = new InvalidOperationException(LocalizedStrings.Str3458Params.Put(info.Message)) });

						break;
					}

					case AlertType.ShutdownSignal:
					{
						_client.OrderInfo -= SessionHolderOnOrderInfo;
						_client.OrderBust -= SessionHolderOnOrderBust;
						_client.OrderCancel -= SessionHolderOnOrderCancel;
						_client.OrderCancelFailure -= SessionHolderOnOrderCancelFailure;
						_client.OrderFailure -= SessionHolderOnOrderFailure;
						_client.OrderFill -= SessionHolderOnOrderFill;
						_client.OrderLineUpdate -= SessionHolderOnOrderLineUpdate;
						_client.OrderModify -= SessionHolderOnOrderModify;
						_client.OrderModifyFailure -= SessionHolderOnOrderModifyFailure;
						_client.OrderReject -= SessionHolderOnOrderReject;
						_client.OrderReport -= SessionHolderOnOrderReport;
						_client.OrderStatus -= SessionHolderOnOrderStatus;
						_client.OrderReplay -= SessionHolderOnOrderReplay;

						_client.Execution -= SessionHolderOnExecution;

						_client.AccountPnL -= SessionHolderOnAccountPnL;
						_client.AccountPnLUpdate -= SessionHolderOnAccountPnLUpdate;
						_client.AccountRms -= SessionHolderOnAccountRms;
						_client.Accounts -= SessionHolderOnAccounts;
						_client.AccountSodUpdate -= SessionHolderOnAccountSodUpdate;

						_client.TransactionError -= SendOutError;

						_client.SecurityBinaryContracts -= SessionHolderOnSecurityBinaryContracts;
						_client.SecurityInstrumentByUnderlying -= SessionHolderOnSecurityInstrumentByUnderlying;
						_client.SecurityOptions -= SessionHolderOnSecurityOptions;
						_client.SecurityRefData -= SessionHolderOnSecurityRefData;

						_client.Exchanges -= SessionHolderOnExchanges;

						_client.BidQuote -= SessionHolderOnBidQuote;
						_client.BestBidQuote -= SessionHolderOnBestBidQuote;
						_client.AskQuote -= SessionHolderOnAskQuote;
						_client.BestAskQuote -= SessionHolderOnBestAskQuote;
						_client.EndQuote -= SessionHolderOnEndQuote;
						_client.Level1 -= SessionHolderOnLevel1;
						_client.OrderBook -= SessionHolderOnOrderBook;
						_client.SettlementPrice -= SessionHolderOnSettlementPrice;
						_client.TradeCondition -= SessionHolderOnTradeCondition;
						_client.TradePrint -= SessionHolderOnTradePrint;
						_client.TradeVolume -= SessionHolderOnTradeVolume;
						_client.TradeReplay -= SessionHolderOnTradeReplay;

						_client.TimeBar -= SessionHolderOnTimeBar;
						_client.TimeBarReplay -= SessionHolderOnTimeBarReplay;

						_client.MarketDataError -= SendOutError;

						_client.Alert -= SessionHolderOnAlert;
						_client.PasswordChange -= SessionHolderOnPasswordChange;

						_client = null;
						break;
					}

					case AlertType.ConnectionBroken:
					case AlertType.TradingDisabled:
					case AlertType.QuietHeartbeat:
					{
						this.AddErrorLog(info.AlertType.ToString());
						break;
					}

					default:
						this.AddWarningLog("Unhandled alert: {0}:{1}:{2}", info.ConnectionId, info.AlertType, info.Message);
						break;
				}
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private bool ProcessErrorCode(int code)
		{
			if (code == 0)
				return true;

			SendOutError(OMErrors.OMgetErrorDesc(code));
			return false;
		}
	}
}