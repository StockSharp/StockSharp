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

		private OECClient _client;

		/// <summary>
		/// Создать <see cref="OpenECryMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public OpenECryMessageAdapter(OpenECrySessionHolder sessionHolder)
			: base(sessionHolder)
		{
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

			_client.OnLoginComplete -= SessionOnLoginComplete;
			_client.OnLoginFailed -= SessionOnLoginFailed;
			_client.OnDisconnected -= SessionOnDisconnected;
			_client.OnBeginEvents -= SessionOnBeginEvents;
			_client.OnEndEvents -= SessionOnEndEvents;
			_client.OnError -= SessionOnError;

			_client.OnAccountRiskLimitChanged -= SessionOnAccountRiskLimitChanged;
			_client.OnAccountSummaryChanged -= SessionOnAccountSummaryChanged;
			_client.OnAllocationBlocksChanged -= SessionOnAllocationBlocksChanged;
			_client.OnAvgPositionChanged -= SessionOnAvgPositionChanged;
			_client.OnBalanceChanged -= SessionOnBalanceChanged;
			_client.OnCommandUpdated -= SessionOnCommandUpdated;
			_client.OnCompoundPositionGroupChanged -= SessionOnCompoundPositionGroupChanged;
			_client.OnOrderConfirmed -= SessionOnOrderConfirmed;
			_client.OnOrderFilled -= SessionOnOrderFilled;
			_client.OnOrderStateChanged -= SessionOnOrderStateChanged;
			_client.OnDetailedPositionChanged -= SessionOnDetailedPositionChanged;
			_client.OnMarginCalculationCompleted -= SessionOnMarginCalculationCompleted;
			_client.OnPortfolioMarginChanged -= SessionOnPortfolioMarginChanged;
			_client.OnPostAllocation -= SessionOnPostAllocation;
			_client.OnRiskLimitDetailsReceived -= SessionOnRiskLimitDetailsReceived;

			_client.OnBarsReceived -= SessionOnBarsReceived;
			_client.OnContinuousContractRuleChanged -= SessionOnContinuousContractRuleChanged;
			_client.OnContractChanged -= SessionOnContractChanged;
			_client.OnContractCreated -= SessionOnContractCreated;
			_client.OnContractRiskLimitChanged -= SessionOnContractRiskLimitChanged;
			_client.OnContractsChanged -= SessionOnContractsChanged;
			_client.OnCurrencyPriceChanged -= SessionOnCurrencyPriceChanged;
			_client.OnDOMChanged -= SessionOnDomChanged;
			_client.OnDealQuoteUpdated -= SessionOnDealQuoteUpdated;
			_client.OnHistogramReceived -= SessionOnHistogramReceived;
			_client.OnHistoryReceived -= SessionOnHistoryReceived;
			_client.OnIndexComponentsReceived -= SessionOnIndexComponentsReceived;
			_client.OnLoggedUserClientsChanged -= SessionOnLoggedUserClientsChanged;
			_client.OnNewsMessage -= SessionOnNewsMessage;
			_client.OnOsmAlgoListLoaded -= SessionOnOsmAlgoListLoaded;
			_client.OnOsmAlgoListUpdated -= SessionOnOsmAlgoListUpdated;
			_client.OnPitGroupsChanged -= SessionOnPitGroupsChanged;
			_client.OnPriceChanged -= SessionOnPriceChanged;
			_client.OnPriceTick -= SessionOnPriceTick;
			_client.OnProductCalendarUpdated -= SessionOnProductCalendarUpdated;
			_client.OnQuoteDetailsChanged -= SessionOnQuoteDetailsChanged;
			_client.OnRelationsChanged -= SessionOnRelationsChanged;
			_client.OnSymbolLookupReceived -= SessionOnSymbolLookupReceived;
			_client.OnTicksReceived -= SessionOnTicksReceived;
			_client.OnTradersChanged -= SessionOnTradersChanged;
			_client.OnUserMessage -= SessionOnUserMessage;
			_client.OnUserStatusChanged -= SessionOnUserStatusChanged;

			_client.Dispose();
			_client = null;
		}

		private void SessionOnDisconnected(bool unexpected)
		{
			SendOutMessage(new DisconnectMessage
			{
				Error = unexpected ? new InvalidOperationException(LocalizedStrings.Str2551) : null
			});

			_client = null;
		}

		private void SessionOnError(Exception exception)
		{
			if (_client != null)
			{
				if (!_client.CompleteConnected)
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
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (_client != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_subscriptions.Clear();

					switch (SessionHolder.Remoting)
					{
						case OpenECryRemoting.None:
						case OpenECryRemoting.Primary:
							_client = new OECClient(new InPlaceThreadPolicy())
							{
								UUID = SessionHolder.Uuid,
								EventBatchInterval = 0,
								RemoteHostingEnabled = SessionHolder.Remoting == OpenECryRemoting.Primary,
								//PriceHost = "",
								//AutoSubscribe = false
							};
							break;
						case OpenECryRemoting.Secondary:
							_client = OECClient.CreateInstance(true);
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
							_client.SetLoggingConfig(new LoggingConfiguration { Level = LogLevel.All });
							Log.Initialize(new OECLogger(SessionHolder));
							Log.Start();
						}
					}

					_client.OnLoginComplete += SessionOnLoginComplete;
					_client.OnLoginFailed += SessionOnLoginFailed;
					_client.OnDisconnected += SessionOnDisconnected;
					_client.OnBeginEvents += SessionOnBeginEvents;
					_client.OnEndEvents += SessionOnEndEvents;
					_client.OnError += SessionOnError;

					_client.OnAccountRiskLimitChanged += SessionOnAccountRiskLimitChanged;
					_client.OnAccountSummaryChanged += SessionOnAccountSummaryChanged;
					_client.OnAllocationBlocksChanged += SessionOnAllocationBlocksChanged;
					_client.OnAvgPositionChanged += SessionOnAvgPositionChanged;
					_client.OnBalanceChanged += SessionOnBalanceChanged;
					_client.OnCommandUpdated += SessionOnCommandUpdated;
					_client.OnCompoundPositionGroupChanged += SessionOnCompoundPositionGroupChanged;
					_client.OnOrderConfirmed += SessionOnOrderConfirmed;
					_client.OnOrderFilled += SessionOnOrderFilled;
					_client.OnOrderStateChanged += SessionOnOrderStateChanged;
					_client.OnDetailedPositionChanged += SessionOnDetailedPositionChanged;
					_client.OnMarginCalculationCompleted += SessionOnMarginCalculationCompleted;
					_client.OnPortfolioMarginChanged += SessionOnPortfolioMarginChanged;
					_client.OnPostAllocation += SessionOnPostAllocation;
					_client.OnRiskLimitDetailsReceived += SessionOnRiskLimitDetailsReceived;

					_client.OnBarsReceived += SessionOnBarsReceived;
					_client.OnContinuousContractRuleChanged += SessionOnContinuousContractRuleChanged;
					_client.OnContractChanged += SessionOnContractChanged;
					_client.OnContractCreated += SessionOnContractCreated;
					_client.OnContractRiskLimitChanged += SessionOnContractRiskLimitChanged;
					_client.OnContractsChanged += SessionOnContractsChanged;
					_client.OnCurrencyPriceChanged += SessionOnCurrencyPriceChanged;
					_client.OnDOMChanged += SessionOnDomChanged;
					_client.OnDealQuoteUpdated += SessionOnDealQuoteUpdated;
					_client.OnHistogramReceived += SessionOnHistogramReceived;
					_client.OnHistoryReceived += SessionOnHistoryReceived;
					_client.OnIndexComponentsReceived += SessionOnIndexComponentsReceived;
					_client.OnLoggedUserClientsChanged += SessionOnLoggedUserClientsChanged;
					_client.OnNewsMessage += SessionOnNewsMessage;
					_client.OnOsmAlgoListLoaded += SessionOnOsmAlgoListLoaded;
					_client.OnOsmAlgoListUpdated += SessionOnOsmAlgoListUpdated;
					_client.OnPitGroupsChanged += SessionOnPitGroupsChanged;
					_client.OnPriceChanged += SessionOnPriceChanged;
					_client.OnPriceTick += SessionOnPriceTick;
					_client.OnProductCalendarUpdated += SessionOnProductCalendarUpdated;
					_client.OnQuoteDetailsChanged += SessionOnQuoteDetailsChanged;
					_client.OnRelationsChanged += SessionOnRelationsChanged;
					_client.OnSymbolLookupReceived += SessionOnSymbolLookupReceived;
					_client.OnTicksReceived += SessionOnTicksReceived;
					_client.OnTradersChanged += SessionOnTradersChanged;
					_client.OnUserMessage += SessionOnUserMessage;
					_client.OnUserStatusChanged += SessionOnUserStatusChanged;

					_client.Connect(SessionHolder.Address.GetHost(), SessionHolder.Address.GetPort(), SessionHolder.Login, SessionHolder.Password.To<string>(), SessionHolder.UseNativeReconnect);

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_client == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_client.Disconnect();
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
					_client.SendMessage(_client.Users[newsMsg.Source], newsMsg.Headline);
					break;
				}
			}
		}
	}
}