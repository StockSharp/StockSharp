#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.LMAX.LMAX
File: LmaxMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.LMAX
{
	using System;

	using Com.Lmax.Api;
	using Com.Lmax.Api.Account;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The messages adapter for LMAX.
	/// </summary>
	public partial class LmaxMessageAdapter : MessageAdapter
	{
		private LmaxApi _api;
		private ISession _session;

		/// <summary>
		/// Initializes a new instance of the <see cref="LmaxMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public LmaxMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromMinutes(10);

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new LmaxOrderCondition();
		}

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
					_isHistoricalSubscribed = false;

					if (_session != null)
					{
						try
						{
							_session.Stop();
							_session.Logout(OnLogoutSuccess, OnLogoutFailure);
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_session = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_session != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					if (_api != null)
						throw new InvalidOperationException(LocalizedStrings.Str3378);

					_isDownloadSecurityFromSite = IsDownloadSecurityFromSite;

					_api = new LmaxApi(IsDemo ? "https://web-order.london-demo.lmax.com" : "https://api.lmaxtrader.com");
					_api.Login(new LoginRequest(Login, Password.To<string>(), IsDemo ? ProductType.CFD_DEMO : ProductType.CFD_LIVE), OnLoginOk, OnLoginFailure);

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_session == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_session.Stop();
					_session.Logout(OnLogoutSuccess, OnLogoutFailure);
					_session = null;

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookupMessage();
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
					break;
				}

				case MessageTypes.Time:
				{
					_session.RequestHeartbeat(new HeartbeatRequest(TransactionIdGenerator.GetNextId().To<string>()), () => { }, CreateErrorHandler("RequestHeartbeat"));
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegisterMessage((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessOrderCancelMessage((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}
			}
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return false; }
		}

		/// <summary>
		/// Convert the instruction code to numeric transaction id.
		/// </summary>
		/// <param name="instructionId">Instruction.</param>
		/// <returns>Converted instruction. If the instruction cannot be converter, the <see langword="null" /> will be returned.</returns>
		private static long? TryParseTransactionId(string instructionId)
		{
			long transactionId;

			if (!long.TryParse(instructionId, out transactionId))
				return null;

			return transactionId;
		}

		private OnFailure CreateErrorHandler(string methodName)
		{
			return failure => SendOutError(ToException(failure, methodName));
		}

		private static Exception ToException(FailureResponse failure, string methodName)
		{
			if (!failure.IsSystemFailure)
			{
				return new InvalidOperationException(LocalizedStrings.Str3379Params.Put(methodName, failure.Message, failure.Description));
			}
			else
			{
				var e = failure.Exception;
				return e ?? new InvalidOperationException(LocalizedStrings.Str3380Params.Put(methodName, failure.Message, failure.Description));
			}
		}

		private void SendError<TMessage>(Exception error)
			where TMessage : BaseConnectionMessage, new()
		{
			DisposeApi();
			SendOutMessage(new TMessage { Error = error });
		}

		private void DisposeApi()
		{
			if (_session != null)
			{
				_session.HeartbeatReceived -= OnSessionHeartbeatReceived;
				_session.EventStreamSessionDisconnected -= OnSessionEventStreamSessionDisconnected;

				_session.InstructionRejected -= OnSessionInstructionRejected;
				_session.AccountStateUpdated -= OnSessionAccountStateUpdated;
				_session.OrderChanged -= OnSessionOrderChanged;
				_session.PositionChanged -= OnSessionPositionChanged;
				_session.OrderExecuted -= OnSessionOrderExecuted;

				_session.EventStreamFailed -= OnSessionEventStreamFailed;
				_session.HistoricMarketDataReceived -= OnSessionHistoricMarketDataReceived;
				_session.MarketDataChanged -= OnSessionMarketDataChanged;
				_session.OrderBookStatusChanged -= OnSessionOrderBookStatusChanged;

				_session = null;
			}

			_api = null;
		}

		private void OnLoginFailure(FailureResponse failureResponse)
		{
			SendError<ConnectMessage>(ToException(failureResponse, "Login"));
		}

		private void OnLoginOk(ISession session)
		{
			try
			{
				_session = session;

				_session.HeartbeatReceived += OnSessionHeartbeatReceived;
				_session.EventStreamSessionDisconnected += OnSessionEventStreamSessionDisconnected;

				_session.InstructionRejected += OnSessionInstructionRejected;
				_session.AccountStateUpdated += OnSessionAccountStateUpdated;
				_session.OrderChanged += OnSessionOrderChanged;
				_session.PositionChanged += OnSessionPositionChanged;
				_session.OrderExecuted += OnSessionOrderExecuted;

				_session.EventStreamFailed += OnSessionEventStreamFailed;
				_session.HistoricMarketDataReceived += OnSessionHistoricMarketDataReceived;
				_session.MarketDataChanged += OnSessionMarketDataChanged;
				_session.OrderBookStatusChanged += OnSessionOrderBookStatusChanged;

				SendOutMessage(new ConnectMessage());
			}
			catch (Exception ex)
			{
				SendError<ConnectMessage>(ex);
			}

			try
			{
				_session.Subscribe(new HeartbeatSubscriptionRequest(), () => { }, CreateErrorHandler("HeartbeatSubscriptionRequest"));
				
				ThreadingHelper
					.Thread(() =>
					{
						try
						{
							_session.Start();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}
					})
					.Background(true)
					.Name("LMAX session thread")
					.Launch();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private void OnLogoutFailure(FailureResponse failureResponse)
		{
			SendError<DisconnectMessage>(ToException(failureResponse, "Logout"));
		}

		private void OnLogoutSuccess()
		{
			DisposeApi();
			SendOutMessage(new DisconnectMessage());
		}

		private void OnSessionHeartbeatReceived(string token)
		{
			// just receive. do not resend
			//SendOutMessage(new TimeMessage { OriginalTransactionId = token, IsBack = true });
		}

		private void OnSessionEventStreamSessionDisconnected()
		{
			SendError<DisconnectMessage>(new InvalidOperationException(LocalizedStrings.Str3381));
		}
	}
}