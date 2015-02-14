namespace StockSharp.LMAX
{
	using System;
	using System.Collections.Generic;

	using Com.Lmax.Api;
	using Com.Lmax.Api.Account;
	using Com.Lmax.Api.MarketData;
	using Com.Lmax.Api.Order;
	using Com.Lmax.Api.Position;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для LMAX.
	/// </summary>
	public partial class LmaxMessageAdapter : MessageAdapter<LmaxSessionHolder>
	{
		private LmaxApi _api;
		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="LmaxMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public LmaxMessageAdapter(MessageAdapterTypes type, LmaxSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			SessionHolder.Initialize += OnSessionInitialize;
			SessionHolder.UnInitialize += OnSessionUnInitialize;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.Initialize -= OnSessionInitialize;
			SessionHolder.UnInitialize -= OnSessionUnInitialize;

			base.DisposeManaged();
		}

		private void OnSessionInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					Session.InstructionRejected += OnSessionInstructionRejected;
					Session.AccountStateUpdated += OnSessionAccountStateUpdated;
					Session.OrderChanged += OnSessionOrderChanged;
					Session.PositionChanged += OnSessionPositionChanged;
					Session.OrderExecuted += OnSessionOrderExecuted;

					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					Session.EventStreamFailed += OnSessionEventStreamFailed;
					Session.HistoricMarketDataReceived += OnSessionHistoricMarketDataReceived;
					Session.MarketDataChanged += OnSessionMarketDataChanged;
					Session.OrderBookStatusChanged += OnSessionOrderBookStatusChanged;

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void OnSessionUnInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					Session.InstructionRejected -= OnSessionInstructionRejected;
					Session.AccountStateUpdated -= OnSessionAccountStateUpdated;
					Session.OrderChanged -= OnSessionOrderChanged;
					Session.PositionChanged -= OnSessionPositionChanged;
					Session.OrderExecuted -= OnSessionOrderExecuted;

					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					Session.EventStreamFailed += OnSessionEventStreamFailed;
					Session.HistoricMarketDataReceived += OnSessionHistoricMarketDataReceived;
					Session.MarketDataChanged += OnSessionMarketDataChanged;
					Session.OrderBookStatusChanged += OnSessionOrderBookStatusChanged;

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private ISession Session
		{
			get
			{
				if (SessionHolder.Session == null)
					throw new InvalidOperationException(LocalizedStrings.Str1856);

				return SessionHolder.Session;
			}
			set
			{
				if (SessionHolder.Session != null)
					throw new InvalidOperationException(LocalizedStrings.Str1619);

				SessionHolder.Session = value;

				Session.HeartbeatReceived += OnSessionHeartbeatReceived;
				Session.EventStreamSessionDisconnected += OnSessionEventStreamSessionDisconnected;
			}
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;

						if (_api != null)
							throw new InvalidOperationException(LocalizedStrings.Str3378);

						_api = new LmaxApi("https://{0}api.lmaxtrader.com".Put(SessionHolder.IsDemo ? "test" : string.Empty));
						_api.Login(new LoginRequest(SessionHolder.Login, SessionHolder.Password.To<string>(), SessionHolder.IsDemo ? ProductType.CFD_DEMO : ProductType.CFD_LIVE), OnLoginOk, OnLoginFailure);
					}
					else
						SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (!_isSessionOwner)
					{
						SendOutMessage(new DisconnectMessage());
						break;
					}

					Session.Stop();
					Session.Logout(OnLogoutSuccess, OnLogoutFailure);

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					Session.Subscribe(new AccountSubscriptionRequest(), () => { }, CreateErrorHandler("AccountSubscriptionRequest"));
					Session.Subscribe(new PositionSubscriptionRequest(), () => { }, CreateErrorHandler("PositionSubscriptionRequest"));
					break;
				}

				case MessageTypes.OrderStatus:
				{
					Session.Subscribe(new ExecutionSubscriptionRequest(), () => { }, CreateErrorHandler("ExecutionSubscriptionRequest"));
					Session.Subscribe(new OrderSubscriptionRequest(), () => { }, CreateErrorHandler("OrderSubscriptionRequest"));
					break;
				}

				case MessageTypes.Time:
				{
					var timeMsg = (TimeMessage)message;
					Session.RequestHeartbeat(new HeartbeatRequest(timeMsg.TransactionId), () => { }, CreateErrorHandler("RequestHeartbeat"));
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegisterMessage((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;
					Session.CancelOrder(new CancelOrderRequest(cancelMsg.TransactionId.To<string>(), (long)cancelMsg.SecurityId.Native, cancelMsg.OrderTransactionId.To<string>()), id => { }, CreateErrorHandler("CancelOrder"));
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					ProcessSecurityLookupMessage(lookupMsg);
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					ProcessMarketDataMessage(mdMsg);
					break;
				}
			}
		}

		/// <summary>
		/// Добавить <see cref="Message"/> в выходную очередь <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public override void SendOutMessage(Message message)
		{
			base.SendOutMessage(message);

			var connectMsg = message as ConnectMessage;

			if (connectMsg != null && connectMsg.Error == null)
			{
				switch (Type)
				{
					case MessageAdapterTypes.Transaction:
					{
						SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						break;
					}
					case MessageAdapterTypes.MarketData:
					{
						if (SessionHolder.IsDownloadSecurityFromSite)
						{
							SendInMessage(new SecurityLookupMessage
							{
								TransactionId = TransactionIdGenerator.GetNextId(),
								ExtensionInfo = new Dictionary<object, object> { { "FromSite", true } }
							});
						}

						Session.Subscribe(new HistoricMarketDataSubscriptionRequest(), () => { }, CreateErrorHandler("HistoricMarketDataSubscriptionRequest"));
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Привести инструкцию к номеру транзакции.
		/// </summary>
		/// <param name="instructionId">Инструкция.</param>
		/// <returns>Номер транзакции. Если инструкцию невозможно привести к числу, то будет возвращено null.</returns>
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
			if (SessionHolder.Session != null)
			{
				Session.HeartbeatReceived -= OnSessionHeartbeatReceived;
				Session.EventStreamSessionDisconnected -= OnSessionEventStreamSessionDisconnected;

				SessionHolder.Session = null;
			}

			_isSessionOwner = false;
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
				Session = session;
				SendOutMessage(new ConnectMessage());
			}
			catch (Exception ex)
			{
				SendError<ConnectMessage>(ex);
			}

			try
			{
				Session.Subscribe(new HeartbeatSubscriptionRequest(), () => { }, CreateErrorHandler("HeartbeatSubscriptionRequest"));
				
				ThreadingHelper
					.Thread(() => Session.Start())
					.Background(true)
					.Name("LMAX Export thread")
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
			//SendMessage(new TimeMessage { HeartbeatId = token });
		}

		private void OnSessionEventStreamSessionDisconnected()
		{
			SendError<DisconnectMessage>(new InvalidOperationException(LocalizedStrings.Str3381));
		}
	}
}