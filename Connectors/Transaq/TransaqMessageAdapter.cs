namespace StockSharp.Transaq
{
	using System;
	using System.Globalization;
	using System.Net;

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Transaq.Native;
	using StockSharp.Transaq.Native.Commands;
	using StockSharp.Transaq.Native.Responses;

	using ConnectMessage = StockSharp.Messages.ConnectMessage;
	using DisconnectMessage = StockSharp.Messages.DisconnectMessage;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для Transaq.
	/// </summary>
	public partial class TransaqMessageAdapter : MessageAdapter<TransaqSessionHolder>
	{
		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="TransaqMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		/// <param name="type">Тип адаптера.</param>
		public TransaqMessageAdapter(MessageAdapterTypes type, TransaqSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			switch (type)
			{
				case MessageAdapterTypes.Transaction:
				{
					SessionHolder.AddHandler<ClientLimitsResponse>(OnClientLimitsResponse);
					SessionHolder.AddHandler<ClientResponse>(OnClientResponse);
					SessionHolder.AddHandler<LeverageControlResponse>(OnLeverageControlResponse);
					SessionHolder.AddHandler<MarketOrdResponse>(OnMarketOrdResponse);
					SessionHolder.AddHandler<OrdersResponse>(OnOrdersResponse);
					SessionHolder.AddHandler<OvernightResponse>(OnOvernightResponse);
					SessionHolder.AddHandler<PositionsResponse>(OnPositionsResponse);
					SessionHolder.AddHandler<TradesResponse>(OnTradesResponse);
					SessionHolder.AddHandler<PortfolioTPlusResponse>(OnPortfolioTPlusResponse);

					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					SessionHolder.AddHandler<AllTradesResponse>(OnAllTradesResponse);
					SessionHolder.AddHandler<CandleKindsResponse>(OnCandleKindsResponse);
					SessionHolder.AddHandler<CandlesResponse>(OnCandlesResponse);
					SessionHolder.AddHandler<MarketsResponse>(OnMarketsResponse);
					SessionHolder.AddHandler<NewsBodyResponse>(OnNewsBodyResponse);
					SessionHolder.AddHandler<NewsHeaderResponse>(OnNewsHeaderResponse);
					SessionHolder.AddHandler<QuotationsResponse>(OnQuotationsResponse);
					SessionHolder.AddHandler<QuotesResponse>(OnQuotesResponse);
					SessionHolder.AddHandler<SecInfoResponse>(OnSecInfoResponse);
					SessionHolder.AddHandler<SecuritiesResponse>(OnSecuritiesResponse);
					SessionHolder.AddHandler<TicksResponse>(OnTicksResponse);
					SessionHolder.AddHandler<BoardsResponse>(OnBoardsResponse);
					SessionHolder.AddHandler<PitsResponse>(OnPitsResponse);
					SessionHolder.AddHandler<MessagesResponse>(OnMessagesResponse);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		private ApiClient Session
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
					Connect();
					break;

				case MessageTypes.Disconnect:
					Disconnect();
					break;

				case InitMessage.MsgType:
					Init();
					break;

				case MessageTypes.OrderRegister:
					ProcessRegisterMessage((OrderRegisterMessage)message);
					break;

				case MessageTypes.OrderCancel:
					ProcessCancelMessage((OrderCancelMessage)message);
					break;

				case MessageTypes.OrderReplace:
					ProcessReplaceMessage((OrderReplaceMessage)message);
					break;

				//case MessageTypes.PortfolioChange:
				//{
				//	var pfMsg = (PortfolioChangeMessage)message;
				//	SendCommand(new RequestLeverageControlMessage { Client = pfMsg.PortfolioName });
				//	break;
				//}

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.ChangePassword:
				{
					var pwdMsg = (ChangePasswordMessage)message;

					SendCommand(new ChangePassMessage
					{
						OldPass = SessionHolder.Password.To<string>(),
						NewPass = pwdMsg.NewPassword.To<string>()
					});

					SendOutMessage(new ChangePasswordMessage { OriginalTransactionId = pwdMsg.TransactionId });

					break;
				}
			}
		}

		private void Connect()
		{
			_registeredSecurityIds.Clear();
			_candleTransactions.Clear();
			_quotes.Clear();

			_orders.Clear();
			_ordersTypes.Clear();

			if (SessionHolder.Session == null)
			{
				_isSessionOwner = true;

				SessionHolder.AddHandler<ServerStatusResponse>(OnServerStatusResponse);
				SessionHolder.AddHandler<ConnectorVersionResponse>(OnConnectorVersionResponse);
				SessionHolder.AddHandler<CurrentServerResponse>(OnCurrentServerResponse);

				Session = new ApiClient(OnCallback,
					SessionHolder.DllPath,
					SessionHolder.IsHFT,
					SessionHolder.ApiLogsPath,
					SessionHolder.ApiLogLevel);

				SendCommand(new Native.Commands.ConnectMessage
				{
					Login = SessionHolder.Login,
					Password = SessionHolder.Password.To<string>(),
					EndPoint = SessionHolder.Address.To<EndPoint>(),
					Proxy = SessionHolder.Proxy,
					MicexRegisters = SessionHolder.MicexRegisters,
					RqDelay = SessionHolder.MarketDataInterval == null ? (int?)null : (int)SessionHolder.MarketDataInterval.Value.TotalMilliseconds,
					Milliseconds = true,
				}, false);
			}
			else
				SendOutMessage(new ConnectMessage());
		}

		private void OnCallback(string data)
		{
			try
			{
				var response = CultureInfo.InvariantCulture.DoInCulture(() => XmlSerializeHelper.Deserialize(data));

				if (!response.IsSuccess)
				{
					SendOutError(response.Exception);
					return;
				}

				var type = response.GetType();

				SessionHolder.AddDebugLog(type.Name);

				SessionHolder.ProcessResponse(response);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private BaseResponse SendCommand(BaseCommandMessage command, bool throwError = true)
		{
			var commandXml = XmlSerializeHelper.Serialize(command);
			var result = XmlSerializeHelper.Deserialize(Session.SendCommand(commandXml));

			if (!result.IsSuccess)
			{
				SessionHolder.AddErrorLog(LocalizedStrings.Str3514Params, command.Id, result.Text);

				if (throwError)
					throw new InvalidOperationException(result.Text);
			}

			return result;
		}

		private void Disconnect()
		{
			if (!_isSessionOwner)
				return;

			SendCommand(new Native.Commands.DisconnectMessage(), false);
			
			SessionHolder.Session.Dispose();
			SessionHolder.Session = null;
		}

		private void OnDisconnected(Exception error)
		{
			SendOutMessage(new DisconnectMessage { Error = error });

			SessionHolder.ConnectorVersion = null;
			SessionHolder.CurrentServer = -1;
			SessionHolder.ServerTimeDiff = null;

			_isSessionOwner = false;
		}

		private void OnServerStatusResponse(ServerStatusResponse response)
		{
			var error = response.Connected == "error";
			var isConnected = response.Connected == "true";

			if (error)
			{
				OnDisconnected(new InvalidOperationException(response.Text));
				return;
			}

			if (isConnected)
			{
				SendOutMessage(new ConnectMessage());
				SendInMessage(new InitMessage());
			}
			else
			{
				OnDisconnected(null);
			}
		}

		private void Init()
		{
			SendCommand(new RequestConnectorVersionMessage(), false);
			SendCommand(new RequestServerIdMessage(), false);
			//SendCommand(new RequestSecuritiesInfoMessage(), false);

			var result = SendCommand(new RequestServTimeDifferenceMessage(), false);

			var diff = TimeSpan.FromSeconds(result.Diff);
			XmlSerializeHelper.GetNow = () => TimeHelper.Now + diff;
		}

		private void OnConnectorVersionResponse(ConnectorVersionResponse response)
		{
			SessionHolder.ConnectorVersion = response.Version;
		}

		private void OnCurrentServerResponse(CurrentServerResponse response)
		{
			SessionHolder.CurrentServer = response.Id;
		}
	}
}