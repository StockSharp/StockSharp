namespace StockSharp.LMAX
{
	using System;

	using Com.Lmax.Api;
	using Com.Lmax.Api.Account;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для LMAX.
	/// </summary>
	public partial class LmaxMessageAdapter : MessageAdapter
	{
		private LmaxApi _api;
		private ISession _session;

		/// <summary>
		/// Создать <see cref="LmaxMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public LmaxMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			HeartbeatInterval = TimeSpan.FromSeconds(60);
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для получения маркет-данных, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsMarketDataEnabled
		{
			get { return true; }
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для отправки транзакций, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsTransactionEnabled
		{
			get { return true; }
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new LmaxOrderCondition();
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
					if (_session != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					if (_api != null)
						throw new InvalidOperationException(LocalizedStrings.Str3378);

					_isDownloadSecurityFromSite = IsDownloadSecurityFromSite;
					_isHistoricalSubscribed = false;

					_api = new LmaxApi("https://{0}api.lmaxtrader.com".Put(IsDemo ? "test" : string.Empty));
					_api.Login(new LoginRequest(Login, Password.To<string>(), IsDemo ? ProductType.CFD_DEMO : ProductType.CFD_LIVE), OnLoginOk, OnLoginFailure);

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_session == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_session.Stop();
					_session.Logout(OnLogoutSuccess, OnLogoutFailure);

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
					var timeMsg = (TimeMessage)message;
					_session.RequestHeartbeat(new HeartbeatRequest(timeMsg.TransactionId), () => { }, CreateErrorHandler("RequestHeartbeat"));
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
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return true; }
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
					.Thread(() => _session.Start())
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