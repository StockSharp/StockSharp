namespace StockSharp.InteractiveBrokers
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.InteractiveBrokers.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Адаптер сообщений для InteractiveBrokers.
	/// </summary>
	public partial class InteractiveBrokersMessageAdapter : MessageAdapter
	{
		private const ServerVersions _minimumServerVersion = ServerVersions.V38;
		private const ServerVersions _clientVersion = ServerVersions.V63;
		
		private readonly SynchronizedPairSet<Tuple<MarketDataTypes, SecurityId, object>, long> _requestIds = new SynchronizedPairSet<Tuple<MarketDataTypes, SecurityId, object>, long>();
		private readonly SynchronizedDictionary<string, long> _pfRequests = new SynchronizedDictionary<string, long>();

		private IBSocket _socket;

		/// <summary>
		/// Создать <see cref="InteractiveBrokersMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public InteractiveBrokersMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Address = DefaultAddress;
			ServerLogLevel = ServerLogLevels.Detail;
			CreateAssociatedSecurity = true;
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new IBOrderCondition();
		}

		private void OnProcessTimeShift(TimeSpan timeShift)
		{
			SendOutMessage(new TimeMessage { ServerTime = DateTimeOffset.UtcNow + timeShift });
		}

		private IBSocket Session
		{
			get
			{
				var session = _socket;

				if (session == null)
					throw new InvalidOperationException(LocalizedStrings.Str2511);

				return session;
			}
		}

		private void ProcessRequest(RequestMessages message, ServerVersions minServerVersion, ServerVersions version, Action<IBSocket> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			var socket = Session;

			if (minServerVersion > socket.ServerVersion)
			{
				throw new ArgumentException(LocalizedStrings.Str2512Params
					.Put((int)minServerVersion, (int)socket.ServerVersion));
			}

			socket
				.Send((int)message)
				.Send((int)version);

			handler(socket);
		}

		///// <summary>
		///// Добавить <see cref="Message"/> в исходящую очередь <see cref="IMessageAdapter"/>.
		///// </summary>
		///// <param name="message">Сообщение.</param>
		//public override void SendOutMessage(Message message)
		//{
		//	if (message.Type == MessageTypes.Security)
		//	{
		//		var secMsg = (SecurityMessage)message;
		//		SessionHolder.Securities.TryAdd(secMsg.SecurityId, (SecurityMessage)secMsg.Clone());
		//	}

		//	base.SendOutMessage(message);
		//}

		private static string GetBoardCode(string boardCode)
		{
			return boardCode.IsEmpty() ? "IBRKS" : boardCode;
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return IsTransactionEnabled; }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return IsTransactionEnabled; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_depths.Clear();
					_secIdByTradeIds.Clear();

					if (_socket != null)
					{
						try
						{
							_socket.Dispose();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_socket = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_socket != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_socket = new IBSocket { Parent = this };
					_socket.ProcessResponse += OnProcessResponse;
					_socket.Connect(Address);

					_socket.Send((int)_clientVersion);

					_socket.ServerVersion = (ServerVersions)_socket.ReadInt();

					if (_socket.ServerVersion >= ServerVersions.V20)
					{
						var str = _socket.ReadStr();
						ConnectedTime = str.Substring(0, str.LastIndexOf(' ')).ToDateTime("yyyyMMdd HH:mm:ss");
					}

					if (_socket.ServerVersion < _minimumServerVersion)
					{
						throw new InvalidOperationException(LocalizedStrings.Str2513Params
							.Put((int)_socket.ServerVersion, (int)_minimumServerVersion));
					}

					if (_socket.ServerVersion >= ServerVersions.V3)
					{
						if (_socket.ServerVersion >= ServerVersions.V70)
						{
							if (!ExtraAuth)
							{
								_socket.Send((int)RequestMessages.StartApi);
								_socket.Send((int)ServerVersions.V1);
								_socket.Send(ClientId);
							}
						}
						else
							_socket.Send(ClientId);
					}

					_socket.StartListening(error => SendOutMessage(new ConnectMessage { Error = error }));
					
					SendOutMessage(new ConnectMessage());

					// отправляется автоматически 
					//RequestIds(1);

					SetServerLogLevel();
					SetMarketDataType();

					RequestCurrentTime();

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_socket == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					UnSubscribePosition();
					UnSubscribeAccountSummary(_pfRequests.GetAndRemove("ALL"));

					_socket.Dispose();
					_socket = null;

					SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.OrderRegister:
				{
					RegisterOrder((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;
					ProcessRequest(RequestMessages.CancelOrder, 0, ServerVersions.V1, socket => socket.Send((int)cancelMsg.OrderTransactionId));
					break;
				}

				case MessageTypes.OrderGroupCancel:
				{
					RequestGlobalCancel();
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					RequestSecurityInfo((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var pfMsg = (PortfolioLookupMessage)message;

					// отправляется автоматически
					//RequestPortfolios();

					SubscribePosition();

					_pfRequests.Add("ALL", pfMsg.TransactionId);
					SubscribeAccountSummary(pfMsg.TransactionId, "ALL", Enumerator.GetValues<AccountSummaryTag>());

					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					SubscribePortfolio(pfMsg.PortfolioName, pfMsg.IsSubscribe);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					var orderMsg = (OrderStatusMessage)message;

					RequestOpenOrders();
					RequestAllOpenOrders();
					//RequestAutoOpenOrders(ClientId == 0);
					ReqeustMyTrades(orderMsg.TransactionId, new MyTradeFilter());

					break;
				}
			}
		}

		// TODO https://www.interactivebrokers.com/en/software/api/apiguide/tables/api_message_codes.htm
		enum NotifyCodes
		{
			OrderDuplicateId = 103,
			OrderFilled = 104,
			OrderNotMatchPrev = 105,
			OrderCannotTransmitId = 106,
			OrderCannotTransmitIncomplete = 107,
			OrderPriceOutOfRange = 109,
			OrderCannotTransmit = 132,
			OrderSubmitFailed = 133,
			SecurityNoDefinition = 200,
			Rejected = 201,
			OrderCancelled = 202,
			OrderVolumeTooSmall = 481,
		}

		private bool OnProcessResponse(IBSocket socket)
		{
			var str = socket.ReadStr(false);

			if (str.IsEmpty())
			{
				socket.AddErrorLog(LocalizedStrings.Str2524);
				return false;
			}

			var message = (ResponseMessages)str.To<int>();

			socket.AddDebugLog("Msg: {0}", message);

			if (message == ResponseMessages.Error)
				return false;

			var version = (ServerVersions)socket.ReadInt();

			switch (message)
			{
				case ResponseMessages.CurrentTime:
				{
					// http://www.interactivebrokers.com/en/software/api/apiguide/java/currenttime.htm

					var time = socket.ReadLongDateTime();
					OnProcessTimeShift(TimeHelper.Now - time);

					break;
				}
				case ResponseMessages.ErrorMessage:
				{
					if (version < ServerVersions.V2)
					{
						OnProcessMarketDataError(socket.ReadStr());
					}
					else
					{
						var id = socket.ReadInt();
						var code = socket.ReadInt();
						var msg = socket.ReadStr();

						socket.AddInfoLog(() => msg);

						if (id == -1)
							break;

						switch ((NotifyCodes)code)
						{
							case NotifyCodes.OrderCancelled:
							{
								OnProcessOrderCancelled(id);
								break;
							}
							case NotifyCodes.OrderCannotTransmit:
							case NotifyCodes.OrderCannotTransmitId:
							case NotifyCodes.OrderCannotTransmitIncomplete:
							case NotifyCodes.OrderDuplicateId:
							case NotifyCodes.OrderFilled:
							case NotifyCodes.OrderNotMatchPrev:
							case NotifyCodes.OrderPriceOutOfRange:
							case NotifyCodes.OrderSubmitFailed:
							case NotifyCodes.OrderVolumeTooSmall:
							case NotifyCodes.Rejected:
							{
								OnProcessOrderError(id, msg);
								break;
							}
							case NotifyCodes.SecurityNoDefinition:
								OnProcessSecurityLookupNoFound(id);
								break;
							default:
								OnProcessMarketDataError(LocalizedStrings.Str2525Params.Put(msg, id, code));
								break;
						}
					}

					break;
				}
				case ResponseMessages.VerifyMessageApi:
				{
					/*int version =*/
					socket.ReadInt();
					/*var apiData = */
					socket.ReadStr();

					//eWrapper().verifyMessageAPI(apiData);
					break;
				}
				case ResponseMessages.VerifyCompleted:
				{
					/*int version =*/
					socket.ReadInt();
					var isSuccessfulStr = socket.ReadStr();
					var isSuccessful = "true".CompareIgnoreCase(isSuccessfulStr);
					/*var errorText = */
					socket.ReadStr();

					if (isSuccessful)
					{
						throw new NotSupportedException();
						//m_parent.startAPI();
					}

					//eWrapper().verifyCompleted(isSuccessful, errorText);
					break;
				}
				case ResponseMessages.DisplayGroupList:
				{
					/*int version =*/
					socket.ReadInt();
					/*var reqId = */
					socket.ReadInt();
					/*var groups = */
					socket.ReadStr();

					//eWrapper().displayGroupList(reqId, groups);
					break;
				}
				case ResponseMessages.DisplayGroupUpdated:
				{
					/*int version =*/
					socket.ReadInt();
					/*var reqId = */
					socket.ReadInt();
					/*var contractInfo = */
					socket.ReadStr();

					//eWrapper().displayGroupUpdated(reqId, contractInfo);
					break;
				}
				default:
				{
					if (!message.IsDefined())
						return false;

					var handled = ProcessTransactionResponse(socket, message, version);

					if (!handled)
						handled = ProcessMarketDataResponse(socket, message, version);

					if (!handled)
						throw new InvalidOperationException(LocalizedStrings.Str1622Params.Put(message));

					break;
				}
			}

			return true;
		}

		private void SetServerLogLevel()
		{
			ProcessRequest(RequestMessages.SetServerLogLevel, 0, ServerVersions.V1, socket => socket.Send((int)ServerLogLevel));
		}

		/// <summary>
		/// Returns the current system time on the server side.
		/// </summary>
		private void RequestCurrentTime()
		{
			ProcessRequest(RequestMessages.RequestCurrentTime, ServerVersions.V33, ServerVersions.V1, socket => { });
		}

		/// <summary>
		/// Returns one next valid Id...
		/// </summary>
		/// <param name="numberOfIds">Has No Effect</param>
		private void RequestIds(int numberOfIds)
		{
			ProcessRequest(RequestMessages.RequestIds, 0, ServerVersions.V1, socket => socket.Send(numberOfIds));
		}

		private void VerifyRequest(string apiName, string apiVersion)
		{
			ProcessRequest(RequestMessages.VerifyRequest, ServerVersions.V70, ServerVersions.V1, socket =>
				socket
					.Send(apiName)
					.Send(apiVersion));
		}

		private void VerifyMessage(string apiData)
		{
			ProcessRequest(RequestMessages.VerifyMessage, ServerVersions.V70, ServerVersions.V1, socket =>
				socket
					.Send(apiData));
		}

		private void QueryDisplayGroups(long transactionId)
		{
			ProcessRequest(RequestMessages.QueryDisplayGroups, ServerVersions.V70, ServerVersions.V1, socket =>
				socket
					.Send(transactionId));
		}

		private void SubscribeToGroupEvents(long transactionId, int groupId)
		{
			ProcessRequest(RequestMessages.SubscribeToGroupEvents, ServerVersions.V70, ServerVersions.V1, socket =>
				socket
					.Send(transactionId)
					.Send(groupId));
		}

		private void UpdateDisplayGroup(long transactionId, string contractInfo)
		{
			ProcessRequest(RequestMessages.UpdateDisplayGroup, ServerVersions.V70, ServerVersions.V1, socket =>
				socket
					.Send(transactionId)
					.Send(contractInfo));
		}

		private void UnSubscribeFromGroupEvents(long transactionId)
		{
			ProcessRequest(RequestMessages.UnSubscribeFromGroupEvents, ServerVersions.V70, ServerVersions.V1, socket =>
				socket
					.Send(transactionId));
		}
	}
}