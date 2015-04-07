namespace StockSharp.Transaq
{
	using System;
	using System.Globalization;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Transaq.Native;
	using StockSharp.Transaq.Native.Commands;
	using StockSharp.Transaq.Native.Responses;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для Transaq.
	/// </summary>
	public partial class TransaqMessageAdapter : MessageAdapter<TransaqSessionHolder>
	{
		private readonly SynchronizedDictionary<Type, Action<BaseResponse>> _handlerBunch = new SynchronizedDictionary<Type, Action<BaseResponse>>();
		private ApiClient _client;

		/// <summary>
		/// Создать <see cref="TransaqMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public TransaqMessageAdapter(TransaqSessionHolder sessionHolder)
			: base(sessionHolder)
		{
			AddHandler<ClientLimitsResponse>(OnClientLimitsResponse);
			AddHandler<ClientResponse>(OnClientResponse);
			AddHandler<LeverageControlResponse>(OnLeverageControlResponse);
			AddHandler<MarketOrdResponse>(OnMarketOrdResponse);
			AddHandler<OrdersResponse>(OnOrdersResponse);
			AddHandler<OvernightResponse>(OnOvernightResponse);
			AddHandler<PositionsResponse>(OnPositionsResponse);
			AddHandler<TradesResponse>(OnTradesResponse);
			AddHandler<PortfolioTPlusResponse>(OnPortfolioTPlusResponse);

			AddHandler<AllTradesResponse>(OnAllTradesResponse);
			AddHandler<CandleKindsResponse>(OnCandleKindsResponse);
			AddHandler<CandlesResponse>(OnCandlesResponse);
			AddHandler<MarketsResponse>(OnMarketsResponse);
			AddHandler<NewsBodyResponse>(OnNewsBodyResponse);
			AddHandler<NewsHeaderResponse>(OnNewsHeaderResponse);
			AddHandler<QuotationsResponse>(OnQuotationsResponse);
			AddHandler<QuotesResponse>(OnQuotesResponse);
			AddHandler<SecInfoResponse>(OnSecInfoResponse);
			AddHandler<SecuritiesResponse>(OnSecuritiesResponse);
			AddHandler<TicksResponse>(OnTicksResponse);
			AddHandler<BoardsResponse>(OnBoardsResponse);
			AddHandler<PitsResponse>(OnPitsResponse);
			AddHandler<MessagesResponse>(OnMessagesResponse);

			AddHandler<ServerStatusResponse>(OnServerStatusResponse);
			AddHandler<ConnectorVersionResponse>(OnConnectorVersionResponse);
			AddHandler<CurrentServerResponse>(OnCurrentServerResponse);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_handlerBunch.Clear();
			base.DisposeManaged();
		}

		private void AddHandler<T>(Action<T> handler)
			where T : BaseResponse
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			_handlerBunch[typeof(T)] = response => handler((T)response);
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
			if (_client != null)
				throw new InvalidOperationException(LocalizedStrings.Str1619);

			_registeredSecurityIds.Clear();
			_candleTransactions.Clear();
			_quotes.Clear();

			_orders.Clear();
			_ordersTypes.Clear();

			_client = new ApiClient(OnCallback,
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

				var handler = _handlerBunch.TryGetValue(response.GetType());

				if (handler != null)
					handler(response);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private BaseResponse SendCommand(BaseCommandMessage command, bool throwError = true)
		{
			var commandXml = XmlSerializeHelper.Serialize(command);
			var result = XmlSerializeHelper.Deserialize(_client.SendCommand(commandXml));

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
			if (_client == null)
				throw new InvalidOperationException(LocalizedStrings.Str1856);

			SendCommand(new Native.Commands.DisconnectMessage(), false);

			_client.Dispose();
			_client = null;
		}

		private void OnDisconnected(Exception error)
		{
			SendOutMessage(new Messages.DisconnectMessage { Error = error });

			SessionHolder.ConnectorVersion = null;
			SessionHolder.CurrentServer = -1;
			SessionHolder.ServerTimeDiff = null;
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
				SendOutMessage(new Messages.ConnectMessage());
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