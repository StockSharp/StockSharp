namespace StockSharp.InteractiveBrokers
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.InteractiveBrokers.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для InteractiveBrokers.
	/// </summary>
	public partial class InteractiveBrokersMessageAdapter : MessageAdapter<InteractiveBrokersSessionHolder>
	{
		private const ServerVersions _minimumServerVersion = ServerVersions.V38;
		private const ServerVersions _clientVersion = ServerVersions.V63;
		
		private readonly SynchronizedPairSet<Tuple<MarketDataTypes, SecurityId, object>, long> _requestIds = new SynchronizedPairSet<Tuple<MarketDataTypes, SecurityId, object>, long>();
		private readonly SynchronizedDictionary<string, long> _pfRequests = new SynchronizedDictionary<string, long>();

		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="InteractiveBrokersMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public InteractiveBrokersMessageAdapter(MessageAdapterTypes type, InteractiveBrokersSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
					SessionHolder.ProcessResponse += OnProcessTransactionResponse;
					SessionHolder.ProcessOrderError += OnProcessOrderError;
					SessionHolder.ProcessOrderCancelled += OnProcessOrderCancelled;
					break;
				case MessageAdapterTypes.MarketData:
					SessionHolder.ProcessResponse += OnProcessMarketDataResponse;
					SessionHolder.ProcessMarketDataError += OnProcessMarketDataError;
					SessionHolder.ProcessSecurityLookupNoFound += OnProcessSecurityLookupNoFound;
					break;
				default:
					throw new ArgumentOutOfRangeException("type");
			}

			SessionHolder.ProcessTimeShift += OnProcessTimeShift;
		}

		private void OnProcessTimeShift(TimeSpan timeShift)
		{
			if (_isSessionOwner)
				SendOutMessage(new TimeMessage { ServerTime = DateTimeOffset.UtcNow + timeShift });
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.ProcessTimeShift -= OnProcessTimeShift;

			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
					SessionHolder.ProcessResponse -= OnProcessTransactionResponse;
					SessionHolder.ProcessOrderError -= OnProcessOrderError;
					SessionHolder.ProcessOrderCancelled -= OnProcessOrderCancelled;
					break;
				case MessageAdapterTypes.MarketData:
					SessionHolder.ProcessResponse -= OnProcessMarketDataResponse;
					SessionHolder.ProcessMarketDataError -= OnProcessMarketDataError;
					SessionHolder.ProcessSecurityLookupNoFound -= OnProcessSecurityLookupNoFound;
					break;
			}

			base.DisposeManaged();
		}

		private IBSocket Session
		{
			get
			{
				var session = SessionHolder.Session;

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
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					_depths.Clear();
					_secIdByTradeIds.Clear();

					if (SessionHolder.Session == null)
					{
						var socket = new IBSocket { Parent = SessionHolder };

						socket.Connect(SessionHolder.Address);

						socket.Send((int)_clientVersion);

						socket.ServerVersion = (ServerVersions)socket.ReadInt();

						if (socket.ServerVersion >= ServerVersions.V20)
						{
							var str = socket.ReadStr();
							SessionHolder.ConnectedTime = str.Substring(0, str.LastIndexOf(' ')).ToDateTime("yyyyMMdd HH:mm:ss");
						}

						if (socket.ServerVersion < _minimumServerVersion)
						{
							throw new InvalidOperationException(LocalizedStrings.Str2513Params
								.Put((int)socket.ServerVersion, (int)_minimumServerVersion));
						}

						if (socket.ServerVersion >= ServerVersions.V3)
						{
							if (socket.ServerVersion >= ServerVersions.V70)
							{
								if (!SessionHolder.ExtraAuth)
								{
									socket.Send((int)RequestMessages.StartApi);
									socket.Send((int)ServerVersions.V1);
									socket.Send(SessionHolder.ClientId);
								}
							}
							else
								socket.Send(SessionHolder.ClientId);
						}

						socket.StartListening(error => SendOutMessage(new ConnectMessage { Error = error }));

						_isSessionOwner = true;

						SessionHolder.Session = socket;
					}
					
					SendOutMessage(new ConnectMessage());

					if (_isSessionOwner)
					{
						// отправляется автоматически 
						//RequestIds(1);

						SetServerLogLevel();
						SetMarketDataType();

						RequestCurrentTime();
					}

					switch (Type)
					{
						case MessageAdapterTypes.Transaction:
							SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
							SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
							break;
						case MessageAdapterTypes.MarketData:
							//RequestScannerParameters();
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				}

				case MessageTypes.Disconnect:
				{
					switch (Type)
					{
						case MessageAdapterTypes.Transaction:
							UnSubscribePosition();
							UnSubscribeAccountSummary(_pfRequests.GetAndRemove("ALL"));
							break;
						case MessageAdapterTypes.MarketData:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					if (_isSessionOwner)
					{
						SessionHolder.Session.Dispose();
						SessionHolder.Session = null;
						_isSessionOwner = false;
					}

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
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.Level1:
						{
							var key = Tuple.Create(mdMsg.DataType, mdMsg.SecurityId, (object)null);

							if (mdMsg.IsSubscribe)
							{
								_requestIds.Add(key, mdMsg.TransactionId);
								SubscribeMarketData(mdMsg, SessionHolder.Fields, false, false);
							}
							else
								UnSubscribeMarketData(_requestIds[key]);

							break;
						}
						case MarketDataTypes.MarketDepth:
						{
							var key = Tuple.Create(mdMsg.DataType, mdMsg.SecurityId, (object)null);

							if (mdMsg.IsSubscribe)
							{
								_requestIds.Add(key, mdMsg.TransactionId);
								SubscribeMarketDepth(mdMsg);
							}
							else
								UnSubscriveMarketDepth(_requestIds[key]);

							break;
						}
						case MarketDataTypes.Trades:
							break;
						case MarketDataTypes.News:
						{
							if (mdMsg.IsSubscribe)
								SubscribeNewsBulletins(true);
							else
								UnSubscribeNewsBulletins();

							break;
						}
						case MarketDataTypes.CandleTimeFrame:
						{
							var key = Tuple.Create(mdMsg.DataType, mdMsg.SecurityId, (object)Tuple.Create(mdMsg.Arg, mdMsg.To == DateTimeOffset.MaxValue));

							if (mdMsg.IsSubscribe)
							{
								_requestIds.Add(key, mdMsg.TransactionId);

								if (mdMsg.To == DateTimeOffset.MaxValue)
									SubscribeRealTimeCandles(mdMsg);
								else
									SubscribeHistoricalCandles(mdMsg, CandleDataTypes.Trades);
							}
							else
							{
								var requestId = _requestIds[key];

								if (mdMsg.To == DateTimeOffset.MaxValue)
									UnSubscribeRealTimeCandles(mdMsg, requestId);
								else
								{
									ProcessRequest(RequestMessages.UnSubscribeHistoricalData, 0, ServerVersions.V1,
										socket => socket.Send(requestId));
								}
							}

							break;
						}
						case ExtendedMarketDataTypes.Scanner:
						{
							var scannerMsg = (ScannerMarketDataMessage)mdMsg;

							var key = Tuple.Create(mdMsg.DataType, mdMsg.SecurityId, (object)scannerMsg.Filter);

							if (mdMsg.IsSubscribe)
							{
								_requestIds.Add(key, mdMsg.TransactionId);
								SubscribeScanner(scannerMsg);
							}
							else
								UnSubscribeScanner(_requestIds[key]);

							break;
						}
						case ExtendedMarketDataTypes.FundamentalReport:
						{
							var reportMsg = (FundamentalReportMarketDataMessage)mdMsg;

							var key = Tuple.Create(mdMsg.DataType, mdMsg.SecurityId, (object)reportMsg.Report);

							if (reportMsg.IsSubscribe)
							{
								_requestIds.Add(key, mdMsg.TransactionId);
								SubscribeFundamentalReport(reportMsg);
							}
							else
								UnSubscribeFundamentalReport(_requestIds[key]);

							break;
						}
						case ExtendedMarketDataTypes.OptionCalc:
						{
							var optionMsg = (OptionCalcMarketDataMessage)mdMsg;

							var key = Tuple.Create(mdMsg.DataType, mdMsg.SecurityId, (object)Tuple.Create(optionMsg.OptionPrice, optionMsg.ImpliedVolatility, optionMsg.AssetPrice));

							if (optionMsg.IsSubscribe)
							{
								_requestIds.Add(key, mdMsg.TransactionId);

								SubscribeCalculateOptionPrice(optionMsg);
								SubscribeCalculateImpliedVolatility(optionMsg);
							}
							else
							{
								var requestId = _requestIds[key];

								UnSubscribeCalculateOptionPrice(requestId);
								UnSubscribeCalculateImpliedVolatility(requestId);
							}

							break;
						}
						default:
							throw new ArgumentOutOfRangeException("message", mdMsg.DataType, LocalizedStrings.Str1618);
					}

					var reply = (MarketDataMessage)mdMsg.Clone();
					reply.OriginalTransactionId = mdMsg.OriginalTransactionId;
					SendOutMessage(reply);

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

		private void SetServerLogLevel()
		{
			ProcessRequest(RequestMessages.SetServerLogLevel, 0, ServerVersions.V1, socket => socket.Send((int)SessionHolder.ServerLogLevel));
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