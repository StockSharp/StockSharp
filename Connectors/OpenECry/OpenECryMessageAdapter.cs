namespace StockSharp.OpenECry
{
	using System;

	using Ecng.Common;

	using OEC;
	using OEC.API;
	using OEC.Data;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для OpenECry.
	/// </summary>
	public partial class OpenECryMessageAdapter : MessageAdapter<OpenECrySessionHolder>
	{
		private class InPlaceThreadPolicy : ThreadingPolicy
		{
			void IDisposable.Dispose()
			{
			}

			object ThreadingPolicy.SetTimer(int timeout, EventHandler action)
			{
				return new object();
			}

			void ThreadingPolicy.KillTimer(object timerObject)
			{
			}

			void ThreadingPolicy.Send(APIAction action)
			{
				action();
			}

			void ThreadingPolicy.Post(APIAction action)
			{
				action();
			}
		}

		class OECLogger : ILogImpl
		{
			private readonly OpenECrySessionHolder _sessionHolder;

			public OECLogger(OpenECrySessionHolder sessionHolder)
			{
				if (sessionHolder == null)
					throw new ArgumentNullException("sessionHolder");

				_sessionHolder = sessionHolder;
			}

			void ILogImpl.Start(string path)
			{
			}

			void ILogImpl.Stop()
			{
			}

			void ILogImpl.WriteLine(Severity severity, string format, params object[] args)
			{
				switch (severity)
				{
					case Severity.N_A:
					case Severity.Prf:
					case Severity.Dbg:
						_sessionHolder.AddDebugLog(format, args);
						break;
					case Severity.Inf:
						_sessionHolder.AddInfoLog(format, args);
						break;
					case Severity.Wrn:
						_sessionHolder.AddWarningLog(format, args);
						break;
					case Severity.Err:
					case Severity.Crt:
						_sessionHolder.AddErrorLog(format, args);
						break;
					default:
						throw new ArgumentOutOfRangeException("severity");
				}
			}
		}

		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="OpenECryMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public OpenECryMessageAdapter(MessageAdapterTypes type, OpenECrySessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			SessionHolder.Initialize += SessionHolderOnInitialize;
			SessionHolder.UnInitialize += SessionHolderOnUnInitialize;
		}

		private void SessionHolderOnInitialize()
		{
			if (_isSessionOwner)
			{
				SessionHolder.Session.OnLoginComplete += SessionOnLoginComplete;
				SessionHolder.Session.OnLoginFailed += SessionOnLoginFailed;
				SessionHolder.Session.OnDisconnected += SessionOnDisconnected;
				SessionHolder.Session.OnBeginEvents += SessionOnBeginEvents;
				SessionHolder.Session.OnEndEvents += SessionOnEndEvents;
				SessionHolder.Session.OnError += SessionOnError;
			}

			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
					SessionHolder.Session.OnAccountRiskLimitChanged += SessionOnAccountRiskLimitChanged;
					SessionHolder.Session.OnAccountSummaryChanged += SessionOnAccountSummaryChanged;
					SessionHolder.Session.OnAllocationBlocksChanged += SessionOnAllocationBlocksChanged;
					SessionHolder.Session.OnAvgPositionChanged += SessionOnAvgPositionChanged;
					SessionHolder.Session.OnBalanceChanged += SessionOnBalanceChanged;
					SessionHolder.Session.OnCommandUpdated += SessionOnCommandUpdated;
					SessionHolder.Session.OnCompoundPositionGroupChanged += SessionOnCompoundPositionGroupChanged;
					SessionHolder.Session.OnOrderConfirmed += SessionOnOrderConfirmed;
					SessionHolder.Session.OnOrderFilled += SessionOnOrderFilled;
					SessionHolder.Session.OnOrderStateChanged += SessionOnOrderStateChanged;
					SessionHolder.Session.OnDetailedPositionChanged += SessionOnDetailedPositionChanged;
					SessionHolder.Session.OnMarginCalculationCompleted += SessionOnMarginCalculationCompleted;
					SessionHolder.Session.OnPortfolioMarginChanged += SessionOnPortfolioMarginChanged;
					SessionHolder.Session.OnPostAllocation += SessionOnPostAllocation;
					SessionHolder.Session.OnRiskLimitDetailsReceived += SessionOnRiskLimitDetailsReceived;
					break;
				case MessageAdapterTypes.MarketData:
					SessionHolder.Session.OnBarsReceived += SessionOnBarsReceived;
					SessionHolder.Session.OnContinuousContractRuleChanged += SessionOnContinuousContractRuleChanged;
					SessionHolder.Session.OnContractChanged += SessionOnContractChanged;
					SessionHolder.Session.OnContractCreated += SessionOnContractCreated;
					SessionHolder.Session.OnContractRiskLimitChanged += SessionOnContractRiskLimitChanged;
					SessionHolder.Session.OnContractsChanged += SessionOnContractsChanged;
					SessionHolder.Session.OnCurrencyPriceChanged += SessionOnCurrencyPriceChanged;
					SessionHolder.Session.OnDOMChanged += SessionOnDomChanged;
					SessionHolder.Session.OnDealQuoteUpdated += SessionOnDealQuoteUpdated;
					SessionHolder.Session.OnHistogramReceived += SessionOnHistogramReceived;
					SessionHolder.Session.OnHistoryReceived += SessionOnHistoryReceived;
					SessionHolder.Session.OnIndexComponentsReceived += SessionOnIndexComponentsReceived;
					SessionHolder.Session.OnLoggedUserClientsChanged += SessionOnLoggedUserClientsChanged;
					SessionHolder.Session.OnNewsMessage += SessionOnNewsMessage;
					SessionHolder.Session.OnOsmAlgoListLoaded += SessionOnOsmAlgoListLoaded;
					SessionHolder.Session.OnOsmAlgoListUpdated += SessionOnOsmAlgoListUpdated;
					SessionHolder.Session.OnPitGroupsChanged += SessionOnPitGroupsChanged;
					SessionHolder.Session.OnPriceChanged += SessionOnPriceChanged;
					SessionHolder.Session.OnPriceTick += SessionOnPriceTick;
					SessionHolder.Session.OnProductCalendarUpdated += SessionOnProductCalendarUpdated;
					SessionHolder.Session.OnQuoteDetailsChanged += SessionOnQuoteDetailsChanged;
					SessionHolder.Session.OnRelationsChanged += SessionOnRelationsChanged;
					SessionHolder.Session.OnSymbolLookupReceived += SessionOnSymbolLookupReceived;
					SessionHolder.Session.OnTicksReceived += SessionOnTicksReceived;
					SessionHolder.Session.OnTradersChanged += SessionOnTradersChanged;
					SessionHolder.Session.OnUserMessage += SessionOnUserMessage;
					SessionHolder.Session.OnUserStatusChanged += SessionOnUserStatusChanged;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void SessionHolderOnUnInitialize()
		{
			if (_isSessionOwner)
			{
				SessionHolder.Session.OnLoginComplete -= SessionOnLoginComplete;
				SessionHolder.Session.OnLoginFailed -= SessionOnLoginFailed;
				SessionHolder.Session.OnDisconnected -= SessionOnDisconnected;
				SessionHolder.Session.OnBeginEvents -= SessionOnBeginEvents;
				SessionHolder.Session.OnEndEvents -= SessionOnEndEvents;
				SessionHolder.Session.OnError -= SessionOnError;
			}

			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
					SessionHolder.Session.OnAccountRiskLimitChanged -= SessionOnAccountRiskLimitChanged;
					SessionHolder.Session.OnAccountSummaryChanged -= SessionOnAccountSummaryChanged;
					SessionHolder.Session.OnAllocationBlocksChanged -= SessionOnAllocationBlocksChanged;
					SessionHolder.Session.OnAvgPositionChanged -= SessionOnAvgPositionChanged;
					SessionHolder.Session.OnBalanceChanged -= SessionOnBalanceChanged;
					SessionHolder.Session.OnCommandUpdated -= SessionOnCommandUpdated;
					SessionHolder.Session.OnCompoundPositionGroupChanged -= SessionOnCompoundPositionGroupChanged;
					SessionHolder.Session.OnOrderConfirmed -= SessionOnOrderConfirmed;
					SessionHolder.Session.OnOrderFilled -= SessionOnOrderFilled;
					SessionHolder.Session.OnOrderStateChanged -= SessionOnOrderStateChanged;
					SessionHolder.Session.OnDetailedPositionChanged -= SessionOnDetailedPositionChanged;
					SessionHolder.Session.OnMarginCalculationCompleted -= SessionOnMarginCalculationCompleted;
					SessionHolder.Session.OnPortfolioMarginChanged -= SessionOnPortfolioMarginChanged;
					SessionHolder.Session.OnPostAllocation -= SessionOnPostAllocation;
					SessionHolder.Session.OnRiskLimitDetailsReceived -= SessionOnRiskLimitDetailsReceived;
					break;
				case MessageAdapterTypes.MarketData:
					SessionHolder.Session.OnBarsReceived -= SessionOnBarsReceived;
					SessionHolder.Session.OnContinuousContractRuleChanged -= SessionOnContinuousContractRuleChanged;
					SessionHolder.Session.OnContractChanged -= SessionOnContractChanged;
					SessionHolder.Session.OnContractCreated -= SessionOnContractCreated;
					SessionHolder.Session.OnContractRiskLimitChanged -= SessionOnContractRiskLimitChanged;
					SessionHolder.Session.OnContractsChanged -= SessionOnContractsChanged;
					SessionHolder.Session.OnCurrencyPriceChanged -= SessionOnCurrencyPriceChanged;
					SessionHolder.Session.OnDOMChanged -= SessionOnDomChanged;
					SessionHolder.Session.OnDealQuoteUpdated -= SessionOnDealQuoteUpdated;
					SessionHolder.Session.OnHistogramReceived -= SessionOnHistogramReceived;
					SessionHolder.Session.OnHistoryReceived -= SessionOnHistoryReceived;
					SessionHolder.Session.OnIndexComponentsReceived -= SessionOnIndexComponentsReceived;
					SessionHolder.Session.OnLoggedUserClientsChanged -= SessionOnLoggedUserClientsChanged;
					SessionHolder.Session.OnNewsMessage -= SessionOnNewsMessage;
					SessionHolder.Session.OnOsmAlgoListLoaded -= SessionOnOsmAlgoListLoaded;
					SessionHolder.Session.OnOsmAlgoListUpdated -= SessionOnOsmAlgoListUpdated;
					SessionHolder.Session.OnPitGroupsChanged -= SessionOnPitGroupsChanged;
					SessionHolder.Session.OnPriceChanged -= SessionOnPriceChanged;
					SessionHolder.Session.OnPriceTick -= SessionOnPriceTick;
					SessionHolder.Session.OnProductCalendarUpdated -= SessionOnProductCalendarUpdated;
					SessionHolder.Session.OnQuoteDetailsChanged -= SessionOnQuoteDetailsChanged;
					SessionHolder.Session.OnRelationsChanged -= SessionOnRelationsChanged;
					SessionHolder.Session.OnSymbolLookupReceived -= SessionOnSymbolLookupReceived;
					SessionHolder.Session.OnTicksReceived -= SessionOnTicksReceived;
					SessionHolder.Session.OnTradersChanged -= SessionOnTradersChanged;
					SessionHolder.Session.OnUserMessage -= SessionOnUserMessage;
					SessionHolder.Session.OnUserStatusChanged -= SessionOnUserStatusChanged;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void SessionOnLoginComplete()
		{
			SendOutMessage(new ConnectMessage());
		}

		private void SessionOnLoginFailed(FailReason reason)
		{
			SendOutMessage(new ConnectMessage
			{
				Error = new InvalidOperationException(reason.GetDescription())
			});

			SessionHolder.Session.Dispose();
			SessionHolder.Session = null;
			_isSessionOwner = false;
		}

		private void SessionOnDisconnected(bool unexpected)
		{
			SendOutMessage(new DisconnectMessage
			{
				Error = unexpected ? new InvalidOperationException(LocalizedStrings.Str2551) : null
			});

			SessionHolder.Session = null;
			_isSessionOwner = false;
		}

		private void SessionOnError(Exception exception)
		{
			if (SessionHolder.Session != null)
			{
				if (!SessionHolder.Session.CompleteConnected)
					SessionOnDisconnected(true);
				else
					SendOutError(exception);
			}
		}

		private void SessionOnBeginEvents()
		{

		}

		private void SessionOnEndEvents()
		{
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.Initialize -= SessionHolderOnInitialize;
			SessionHolder.UnInitialize -= SessionHolderOnUnInitialize;

			base.DisposeManaged();
		}

		/// <summary>
		/// Добавить <see cref="Message"/> в исходящую очередь <see cref="IMessageAdapter"/>.
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
						SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						SendInMessage(new OrderStatusMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						break;
					case MessageAdapterTypes.MarketData:
						//SendInMessage(new SecurityLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
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
					_subscriptions.Clear();

					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;

						switch (SessionHolder.Remoting)
						{
							case OpenECryRemoting.None:
							case OpenECryRemoting.Primary:
								SessionHolder.Session = new OECClient(new InPlaceThreadPolicy())
								{
									UUID = SessionHolder.Uuid,
									EventBatchInterval = 0,
									RemoteHostingEnabled = SessionHolder.Remoting == OpenECryRemoting.Primary,
									//PriceHost = "",
									//AutoSubscribe = false
								};
								break;
							case OpenECryRemoting.Secondary:
								SessionHolder.Session = OECClient.CreateInstance(true);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						if (SessionHolder.EnableOECLogging)
						{
							if (SessionHolder.Remoting == OpenECryRemoting.Secondary)
							{
								SessionHolder.AddWarningLog(LocalizedStrings.Str2552);
							}
							else
							{
								Log.ConsoleOutput = false;
								SessionHolder.Session.SetLoggingConfig(new LoggingConfiguration { Level = LogLevel.All });
								Log.Initialize(new OECLogger(SessionHolder));
								Log.Start();
							}
						}
						
						SessionHolder.Session.Connect(SessionHolder.Address.GetHost(), SessionHolder.Address.GetPort(), SessionHolder.Login, SessionHolder.Password.To<string>(), SessionHolder.UseNativeReconnect);
					}
					else
						SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_isSessionOwner)
					{
						SessionHolder.Session.Disconnect();
					}
					else
						SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.SecurityLookup:
				{
					ProcessSecurityLookup((SecurityLookupMessage)message);
					break;
				}

				case MessageTypes.OrderRegister:
				{
					ProcessOrderRegister((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					ProcessOrderCancel((OrderCancelMessage)message);
					break;
				}

				case MessageTypes.OrderReplace:
				{
					ProcessOrderReplace((OrderReplaceMessage)message);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					ProcessPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					ProcessOrderStatusMessage();
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}

				case MessageTypes.News:
				{
					var newsMsg = (Messages.NewsMessage)message;
					SessionHolder.Session.SendMessage(SessionHolder.Session.Users[newsMsg.Source], newsMsg.Headline);
					break;
				}
			}
		}
	}
}