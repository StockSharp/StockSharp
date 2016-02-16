#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Transaq
File: TransaqMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	public partial class TransaqMessageAdapter : MessageAdapter
	{
		private readonly SynchronizedDictionary<Type, Action<BaseResponse>> _handlerBunch = new SynchronizedDictionary<Type, Action<BaseResponse>>();
		private ApiClient _client;
		private bool _isInitialized;

		/// <summary>
		/// Создать <see cref="TransaqMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public TransaqMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			AddHandler<ClientLimitsResponse>(OnClientLimitsResponse);
			AddHandler<ClientResponse>(OnClientResponse);
			AddHandler<UnitedPortfolioResponse>(OnUnitedPortfolioResponse);
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

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.RemoveSupportedMessage(MessageTypes.SecurityLookup);
			this.RemoveSupportedMessage(MessageTypes.OrderStatus);
			this.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
			this.AddSupportedMessage(MessageTypes.ChangePassword);
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
				throw new ArgumentNullException(nameof(handler));

			_handlerBunch[typeof(T)] = response => handler((T)response);
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new TransaqOrderCondition();
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			if (_client != null && !_isInitialized && message.Type != MessageTypes.Reset && message.Type != MessageTypes.Connect && message.Type != MessageTypes.Disconnect)
			{
				Init();
				_isInitialized = true;
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_isInitialized = false;

					_registeredSecurityIds.Clear();
					_candleTransactions.Clear();
					_quotes.Clear();

					_orders.Clear();
					_ordersTypes.Clear();

					if (_client != null)
					{
						try
						{
							_client.Dispose();
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
					Connect();
					break;

				case MessageTypes.Disconnect:
					Disconnect();
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

				case MessageTypes.PortfolioChange:
				{
					var pfMsg = (PortfolioChangeMessage)message;
					//SendCommand(new RequestLeverageControlMessage { Client = pfMsg.PortfolioName });
					SendCommand(new RequestUnitedPortfolioMessage { Client = pfMsg.PortfolioName });
					break;
				}

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.ChangePassword:
				{
					var pwdMsg = (ChangePasswordMessage)message;

					SendCommand(new ChangePassMessage
					{
						OldPass = Password.To<string>(),
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

			_client = new ApiClient(OnCallback,
					DllPath,
					OverrideDll,
					IsHFT,
					ApiLogsPath,
					ApiLogLevel);

			SendCommand(new Native.Commands.ConnectMessage
			{
				Login = Login,
				Password = Password.To<string>(),
				EndPoint = Address.To<EndPoint>(),
				Proxy = Proxy,
				MicexRegisters = MicexRegisters,
				RqDelay = MarketDataInterval == null ? (int?)null : ((int)MarketDataInterval.Value.TotalMilliseconds).Max(100),
				Milliseconds = true,
				Utc = true,
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

				this.AddDebugLog(type.Name);

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
				this.AddErrorLog(LocalizedStrings.Str3514Params, command.Id, result.Text);

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

			ConnectorVersion = null;
			CurrentServer = -1;
			ServerTimeDiff = null;
		}

		private void OnServerStatusResponse(ServerStatusResponse response)
		{
			var error = response.Connected == "error";

			if (error)
			{
				OnDisconnected(new InvalidOperationException(response.Text));
				return;
			}

			var isConnected = response.Connected == "true";

			if (isConnected)
			{
				SendOutMessage(new Messages.ConnectMessage());
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
			ConnectorVersion = response.Version;
		}

		private void OnCurrentServerResponse(CurrentServerResponse response)
		{
			CurrentServer = response.Id;
		}
	}
}