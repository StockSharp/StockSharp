namespace StockSharp.AlfaDirect
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.AlfaDirect.Native;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для AlfaDirect.
	/// </summary>
	public partial class AlfaDirectMessageAdapter : MessageAdapter<AlfaDirectSessionHolder>
	{
		private readonly AlfaDirectSessionHolder _sessionHolder;
		private AlfaDirectSessionHolder.IAlfaSession _session;
		private bool _sessionOwner;
		private bool _adapterIsActive;
		private bool _subscribed;

		/// <summary>
		/// Создать <see cref="AlfaDirectMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public AlfaDirectMessageAdapter(MessageAdapterTypes type, AlfaDirectSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			if (sessionHolder == null)
				throw new ArgumentNullException("sessionHolder");

			_sessionHolder = sessionHolder;
			_sessionHolder.Initialize += OnSessionInitialize;
			_sessionHolder.UnInitialize += OnSessionUnInitialize;

			Platform = Platforms.x86;

			OnSessionInitialize();
		}

		/// <summary>Освободить занятые ресурсы.</summary>
		protected override void DisposeManaged()
		{
			_sessionHolder.Initialize -= OnSessionInitialize;
			_sessionHolder.UnInitialize -= OnSessionUnInitialize;

			base.DisposeManaged();
		}

		private AlfaWrapper Wrapper
		{
			get { return _sessionHolder.Wrapper; }
		}

		private void OnSessionInitialize()
		{
			_alfaIds.Clear();
			_localIds.Clear();

			if (Wrapper == null)
				return;

			if (_adapterIsActive)
				SubscribeWrapper();
		}

		private void OnSessionUnInitialize()
		{
			UnsubscribeWrapper();
		}

		private void SubscribeWrapper()
		{
			if (_subscribed)
			{
				SessionHolder.AddWarningLog("SubscribeWrapper: already subscribed");
				return;
			}

			if (_sessionOwner)
			{
				Wrapper.Connected += OnWrapperConnected;
				Wrapper.Disconnected += OnWrapperDisconnected;
				Wrapper.ConnectionError += OnConnectionError;
				Wrapper.Error += SendOutError;
			}

			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					Wrapper.ProcessOrder += OnProcessOrders;
					Wrapper.ProcessOrderConfirmed += OnProcessOrderConfirmed;
					Wrapper.ProcessOrderFailed += OnProcessOrderFailed;
					Wrapper.ProcessPositions += OnProcessPositions;
					Wrapper.ProcessMyTrades += OnProcessMyTrades;
					Wrapper.ProcessNews += OnProcessNews;
					
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					Wrapper.ProcessSecurities += OnProcessSecurities;
					Wrapper.ProcessLevel1 += OnProcessLevel1;
					Wrapper.ProcessQuotes += OnProcessQuotes;
					Wrapper.ProcessTrades += OnProcessTrades;
					Wrapper.ProcessCandles += OnProcessCandles;

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			_subscribed = true;
		}

		private void UnsubscribeWrapper()
		{
			if (!_subscribed)
			{
				SessionHolder.AddWarningLog("UnsubscribeWrapper: not subscribed");
				return;
			}

			if (_sessionOwner)
			{
				Wrapper.Connected -= OnWrapperConnected;
				Wrapper.Disconnected -= OnWrapperDisconnected;
				Wrapper.ConnectionError -= OnConnectionError;
			}

			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					Wrapper.ProcessOrder -= OnProcessOrders;
					Wrapper.ProcessOrderConfirmed -= OnProcessOrderConfirmed;
					Wrapper.ProcessOrderFailed -= OnProcessOrderFailed;
					Wrapper.ProcessPositions -= OnProcessPositions;
					Wrapper.ProcessMyTrades -= OnProcessMyTrades;
					Wrapper.ProcessNews -= OnProcessNews;

					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					Wrapper.ProcessSecurities += OnProcessSecurities;
					Wrapper.ProcessLevel1 -= OnProcessLevel1;
					Wrapper.ProcessQuotes -= OnProcessQuotes;
					Wrapper.ProcessTrades -= OnProcessTrades;
					Wrapper.ProcessCandles -= OnProcessCandles;

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			_subscribed = false;
		}

		/// <summary>
		/// Добавить <see cref="StockSharp.Messages.Message"/> в выходную очередь <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public override void SendOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					base.SendOutMessage(message);

					var connectMsg = (ConnectMessage)message;

					if (connectMsg.Error == null)
					{
						switch (Type)
						{
							case MessageAdapterTypes.Transaction:
								SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
								SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
								break;
							case MessageAdapterTypes.MarketData:
								SendInMessage(new SecurityLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					return;
				}

				case MessageTypes.Disconnect:
				{
					var connectMsg = (DisconnectMessage)message;

					if (connectMsg.Error == null)
					{
						switch (Type)
						{
							case MessageAdapterTypes.Transaction:
								Wrapper.StopExportOrders();
								Wrapper.StopExportPortfolios();
								Wrapper.StopExportMyTrades();
								break;
						}
					}

					base.SendOutMessage(message);

					return;
				}
			}

			base.SendOutMessage(message);
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
					_adapterIsActive = true;

					if (_session == null)
					{
						_sessionOwner = _sessionHolder.Wrapper == null;
						_session = _sessionHolder.GetSession(SessionHolder);
					}

					SubscribeWrapper();

					if (Wrapper.IsConnected || !_sessionOwner)
						SendOutMessage(new ConnectMessage());

					else if (!Wrapper.IsConnecting)
						Wrapper.Connect(SessionHolder.Login, SessionHolder.Password.To<string>());

					break;
				}

				case MessageTypes.Disconnect:
				{
					UnsubscribeWrapper();
					_adapterIsActive = false;
					SendOutMessage(new DisconnectMessage());

					if (_session != null)
					{
						_session.Dispose();
						_session = null;
					}

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					// чтобы не было дедлока, RegisterOrder должен использовать только асинхронные
					// вызовы AlfaDirect, как CreateLimitOrder(... timeout=-1)
					lock (_localIds.SyncRoot)
					{
						var alfaTransactionId = Wrapper.RegisterOrder(regMsg);
						_localIds.Add(regMsg.TransactionId, alfaTransactionId);
					}

					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					if (cancelMsg.OrderId == 0)
						throw new InvalidOperationException(LocalizedStrings.Str2252Params.Put(cancelMsg.OrderTransactionId));

					Wrapper.CancelOrder(cancelMsg.OrderId);
					break;
				}

				case MessageTypes.OrderGroupCancel:
				{
					var groupMsg = (OrderGroupCancelMessage)message;
					Wrapper.CancelOrders(groupMsg.IsStop, groupMsg.PortfolioName, groupMsg.Side, groupMsg.SecurityId, groupMsg.SecurityType);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;

					if (pfMsg.IsSubscribe)
						Wrapper.StartExportPortfolios();
					else
						SessionHolder.AddWarningLog("ignore portfolios unsubscribe");

					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					Wrapper.LookupSecurities(lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var lookupMsg = (PortfolioLookupMessage)message;
					Wrapper.LookupPortfolios(lookupMsg.TransactionId);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					Wrapper.LookupOrders();
					break;
				}
			}
		}

		private void OnWrapperDisconnected()
		{
			SessionHolder.AddInfoLog(LocalizedStrings.Str2254);
			SendOutMessage(new DisconnectMessage());
		}

		private void OnWrapperConnected()
		{
			SessionHolder.AddInfoLog(LocalizedStrings.Str2255);
			SendOutMessage(new ConnectMessage());
		}

		private void OnConnectionError(Exception ex)
		{
			SessionHolder.AddInfoLog(LocalizedStrings.Str3458Params.Put(ex.Message));
			SendOutMessage(new ConnectMessage { Error = ex });
		}
	}
}