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
	public partial class SmartComMessageAdapter : MessageAdapter
	{
		private ISmartComWrapper _wrapper;

		/// <summary>
		/// Создать <see cref="SmartComMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public SmartComMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Version = SmartComVersions.V3;

			SecurityClassInfo.Add("OPT", RefTuple.Create(SecurityTypes.Option, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("OPTM", RefTuple.Create(SecurityTypes.Option, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("FUT", RefTuple.Create(SecurityTypes.Future, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("FXMFD", RefTuple.Create(SecurityTypes.Currency, ExchangeBoard.Mfd.Code));
			SecurityClassInfo.Add("WFMFD", RefTuple.Create(SecurityTypes.Index, ExchangeBoard.Mfd.Code));
			SecurityClassInfo.Add("WIMFD", RefTuple.Create(SecurityTypes.Index, ExchangeBoard.Mfd.Code));
			SecurityClassInfo.Add("EQBR", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.MicexEqbr.Code));
			SecurityClassInfo.Add("TQBR", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.MicexTqbr.Code));
			SecurityClassInfo.Add("TQNE", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.MicexTqne.Code));
			SecurityClassInfo.Add("EQNE", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.MicexEqne.Code));
			SecurityClassInfo.Add("EQOB", RefTuple.Create(SecurityTypes.Bond, ExchangeBoard.MicexEqob.Code));
			SecurityClassInfo.Add("EQNB", RefTuple.Create(SecurityTypes.Bond, ExchangeBoard.MicexEqnb.Code));
			SecurityClassInfo.Add("EQDB", RefTuple.Create(SecurityTypes.Bond, ExchangeBoard.MicexEqdb.Code));
			SecurityClassInfo.Add("EQOS", RefTuple.Create(SecurityTypes.Bond, ExchangeBoard.MicexEqos.Code));
			SecurityClassInfo.Add("TQOB", RefTuple.Create(SecurityTypes.Bond, ExchangeBoard.MicexTqob.Code));
			SecurityClassInfo.Add("TQLV", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.MicexTqlv.Code));

			PortfolioBoardCodes = new Dictionary<string, string>
			{
			    { "EQ", ExchangeBoard.MicexEqbr.Code },
			    { "FOB", ExchangeBoard.MicexFbcb.Code },
			    { "RTS_FUT", ExchangeBoard.Forts.Code },
			};

			UpdatePlatform();

			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.RemoveSupportedMessage(MessageTypes.OrderStatus);
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new SmartComOrderCondition();
		}

		private void UpdatePlatform()
		{
			Platform = Version == SmartComVersions.V3 ? Platforms.AnyCPU : Platforms.x86;
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
					_tempDepths.Clear();
					_candleTransactions.Clear();
					_bestQuotes.Clear();

					_lookupSecuritiesId = 0;
					_lookupPortfoliosId = 0;

					//_smartOrderIds.Clear();
					//_smartIdOrders.Clear();

					if (_wrapper != null)
					{
						try
						{
							DisposeWrapper();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						try
						{
							_wrapper.Disconnect();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_wrapper = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_wrapper != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					switch (Version)
					{
						case SmartComVersions.V2:
							_wrapper = new SmartCom2Wrapper();
							break;
						case SmartComVersions.V3:
							_wrapper = (Environment.Is64BitProcess
								? (ISmartComWrapper)new SmartCom3Wrapper64
								{
									ClientSettings = ClientSettings,
									ServerSettings = ServerSettings,
								}
								: new SmartCom3Wrapper32
								{
									ClientSettings = ClientSettings,
									ServerSettings = ServerSettings,
								});

							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					_wrapper.NewPortfolio += OnNewPortfolio;
					_wrapper.PortfolioChanged += OnPortfolioChanged;
					_wrapper.PositionChanged += OnPositionChanged;
					_wrapper.NewMyTrade += OnNewMyTrade;
					_wrapper.NewOrder += OnNewOrder;
					_wrapper.OrderFailed += OnOrderFailed;
					_wrapper.OrderCancelFailed += OnOrderCancelFailed;
					_wrapper.OrderChanged += OnOrderChanged;
					_wrapper.OrderReRegisterFailed += OnOrderReRegisterFailed;
					_wrapper.OrderReRegistered += OnOrderReRegistered;

					_wrapper.NewSecurity += OnNewSecurity;
					_wrapper.SecurityChanged += OnSecurityChanged;
					_wrapper.QuoteChanged += OnQuoteChanged;
					_wrapper.NewTrade += OnNewTrade;
					_wrapper.NewHistoryTrade += OnNewHistoryTrade;
					_wrapper.NewBar += OnNewBar;

					_wrapper.Connected += OnConnected;
					_wrapper.Disconnected += OnDisconnected;

					_wrapper.Connect(Address.GetHost(), (short)Address.GetPort(), Login, Password.To<string>());

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_wrapper == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					_wrapper.Disconnect();

					break;
				}

				case MessageTypes.OrderRegister:
					ProcessRegisterMessage((OrderRegisterMessage)message);
					break;

				case MessageTypes.OrderCancel:
					ProcessCancelMessage((OrderCancelMessage)message);
					break;

				case MessageTypes.OrderGroupCancel:
					_wrapper.CancelAllOrders();
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
			DisposeWrapper();

			SendOutMessage(new DisconnectMessage { Error = error });

			_wrapper = null;
		}

		private void DisposeWrapper()
		{
			_wrapper.NewPortfolio -= OnNewPortfolio;
			_wrapper.PortfolioChanged -= OnPortfolioChanged;
			_wrapper.PositionChanged -= OnPositionChanged;
			_wrapper.NewMyTrade -= OnNewMyTrade;
			_wrapper.NewOrder -= OnNewOrder;
			_wrapper.OrderFailed -= OnOrderFailed;
			_wrapper.OrderCancelFailed -= OnOrderCancelFailed;
			_wrapper.OrderChanged -= OnOrderChanged;
			_wrapper.OrderReRegisterFailed -= OnOrderReRegisterFailed;
			_wrapper.OrderReRegistered -= OnOrderReRegistered;

			_wrapper.NewSecurity -= OnNewSecurity;
			_wrapper.SecurityChanged -= OnSecurityChanged;
			_wrapper.QuoteChanged -= OnQuoteChanged;
			_wrapper.NewTrade -= OnNewTrade;
			_wrapper.NewHistoryTrade -= OnNewHistoryTrade;
			_wrapper.NewBar -= OnNewBar;

			_wrapper.Connected -= OnConnected;
			_wrapper.Disconnected -= OnDisconnected;
		}
	}
}