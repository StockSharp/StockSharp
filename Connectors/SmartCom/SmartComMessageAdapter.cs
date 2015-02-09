namespace StockSharp.SmartCom
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для SmartCOM.
	/// </summary>
	public partial class SmartComMessageAdapter : MessageAdapter<SmartComSessionHolder>
	{
		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="SmartComMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		/// <param name="type">Тип адаптера.</param>
		public SmartComMessageAdapter(MessageAdapterTypes type, SmartComSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			SessionHolder.Initialize += OnSessionInitialize;
			SessionHolder.UnInitialize += OnSessionUnInitialize;
			SessionHolder.VersionChanged += OnSessionVersionChanged;

			PortfolioBoardCodes = new Dictionary<string, string>
			{
			    { "EQ", ExchangeBoard.MicexEqbr.Code },
			    { "FOB", ExchangeBoard.MicexFbcb.Code },
			    { "RTS_FUT", ExchangeBoard.Forts.Code },
			};

			OnSessionVersionChanged();
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.Initialize -= OnSessionInitialize;
			SessionHolder.UnInitialize -= OnSessionUnInitialize;
			SessionHolder.VersionChanged -= OnSessionVersionChanged;

			base.DisposeManaged();
		}

		private void OnSessionInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					Session.NewPortfolio += OnNewPortfolio;
					Session.PortfolioChanged += OnPortfolioChanged;
					Session.PositionChanged += OnPositionChanged;
					Session.NewMyTrade += OnNewMyTrade;
					Session.NewOrder += OnNewOrder;
					Session.OrderFailed += OnOrderFailed;
					Session.OrderCancelFailed += OnOrderCancelFailed;
					Session.OrderChanged += OnOrderChanged;
					Session.OrderReRegisterFailed += OnOrderReRegisterFailed;
					Session.OrderReRegistered += OnOrderReRegistered;

					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					Session.NewSecurity += OnNewSecurity;
					Session.SecurityChanged += OnSecurityChanged;
					Session.QuoteChanged += OnQuoteChanged;
					Session.NewTrade += OnNewTrade;
					Session.NewHistoryTrade += OnNewHistoryTrade;
					Session.NewBar += OnNewBar;

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
					Session.NewPortfolio -= OnNewPortfolio;
					Session.PortfolioChanged -= OnPortfolioChanged;
					Session.PositionChanged -= OnPositionChanged;
					Session.NewMyTrade -= OnNewMyTrade;
					Session.NewOrder -= OnNewOrder;
					Session.OrderFailed -= OnOrderFailed;
					Session.OrderCancelFailed -= OnOrderCancelFailed;
					Session.OrderChanged -= OnOrderChanged;
					Session.OrderReRegisterFailed -= OnOrderReRegisterFailed;
					Session.OrderReRegistered -= OnOrderReRegistered;

					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					Session.NewSecurity -= OnNewSecurity;
					Session.SecurityChanged -= OnSecurityChanged;
					Session.QuoteChanged -= OnQuoteChanged;
					Session.NewTrade -= OnNewTrade;
					Session.NewHistoryTrade -= OnNewHistoryTrade;
					Session.NewBar -= OnNewBar;

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void OnSessionVersionChanged()
		{
			Platform = SessionHolder.Version == SmartComVersions.V3 ? Platforms.AnyCPU : Platforms.x86;
		}

		private ISmartComWrapper Session
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

				Session.Connected += OnConnected;
				Session.Disconnected += OnDisconnected;
			}
		}

		/// <summary>
		/// Добавить <see cref="StockSharp.Messages.Message"/> в выходную очередь <see cref="IMessageAdapter"/>.
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
						break;
					case MessageAdapterTypes.MarketData:
						SendInMessage(new SecurityLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });
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
					_tempDepths.Clear();
					_candleTransactions.Clear();
					_bestQuotes.Clear();

					_lookupSecuritiesId = 0;
					_lookupPortfoliosId = 0;

					//_smartOrderIds.Clear();
					//_smartIdOrders.Clear();

					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;

						switch (SessionHolder.Version)
						{
							case SmartComVersions.V2:
								Session = new SmartCom2Wrapper();
								break;
							case SmartComVersions.V3:
								Session = (Environment.Is64BitProcess
									? (ISmartComWrapper)new SmartCom3Wrapper64
									{
										ClientSettings = SessionHolder.ClientSettings,
										ServerSettings = SessionHolder.ServerSettings,
									}
									: new SmartCom3Wrapper32
									{
										ClientSettings = SessionHolder.ClientSettings,
										ServerSettings = SessionHolder.ServerSettings,
									});

								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						Session.Connect(SessionHolder.Address.GetHost(), (short)SessionHolder.Address.GetPort(), SessionHolder.Login, SessionHolder.Password.To<string>());
					}
					else
						SendOutMessage(new ConnectMessage());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_isSessionOwner)
						Session.Disconnect();
					else
						SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.OrderRegister:
					ProcessRegisterMessage((OrderRegisterMessage)message);
					break;

				case MessageTypes.OrderCancel:
					ProcessCancelMessage((OrderCancelMessage)message);
					break;

				case MessageTypes.OrderGroupCancel:
					Session.CancelAllOrders();
					break;

				case MessageTypes.OrderReplace:
					ProcessReplaceMessage((OrderReplaceMessage)message);
					break;

				case MessageTypes.Portfolio:
					ProcessPortfolioMessage((PortfolioMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					ProcessPortolioLookupMessage((PortfolioLookupMessage)message);
					break;

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.SecurityLookup:
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;
			}
		}

		private void OnConnected()
		{
			SendOutMessage(new ConnectMessage());
		}

		private void OnDisconnected(Exception error)
		{
			SendOutMessage(new DisconnectMessage { Error = error });

			Session.Connected -= OnConnected;
			Session.Disconnected -= OnDisconnected;

			SessionHolder.Session = null;
			_isSessionOwner = false;
		}
	}
}