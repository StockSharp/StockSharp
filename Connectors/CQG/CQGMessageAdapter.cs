namespace StockSharp.CQG
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;

	using global::CQG;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщений для CQG.
	/// </summary>
	public partial class CQGMessageAdapter : MessageAdapter<CQGSessionHolder>
	{
		private readonly SynchronizedDictionary<long, CQGOrder> _orders = new SynchronizedDictionary<long, CQGOrder>();
		private readonly SynchronizedDictionary<string, CQGAccount> _accounts = new SynchronizedDictionary<string, CQGAccount>();
		private bool _isSessionOwner;

		/// <summary>
		/// Создать <see cref="CQGMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public CQGMessageAdapter(MessageAdapterTypes type, CQGSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			SessionHolder.Initialize += OnSessionInitialize;
			SessionHolder.UnInitialize += OnSessionUnInitialize;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			base.DisposeManaged();

			SessionHolder.Initialize -= OnSessionInitialize;
			SessionHolder.UnInitialize -= OnSessionUnInitialize;
		}

		private void OnSessionInitialize()
		{
			switch (Type)
			{
				case MessageAdapterTypes.Transaction:
				{
					SessionHolder.Session.AccountChanged += SessionOnAccountChanged;
					SessionHolder.Session.AlgorithmicOrderPlaced += SessionOnAlgorithmicOrderPlaced;
					SessionHolder.Session.AlgorithmicOrderRegistrationComplete += SessionOnAlgorithmicOrderRegistrationComplete;
					SessionHolder.Session.OrderChanged += SessionOnOrderChanged;
					SessionHolder.Session.PositionsStatementResolved += SessionOnPositionsStatementResolved;
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					SessionHolder.Session.InstrumentDOMChanged += SessionOnInstrumentDomChanged;
					SessionHolder.Session.InstrumentChanged += SessionOnInstrumentChanged;
					SessionHolder.Session.TicksAdded += SessionOnTicksAdded;
					SessionHolder.Session.IncorrectSymbol += SessionOnIncorrectSymbol;
					SessionHolder.Session.InstrumentSubscribed += SessionOnInstrumentSubscribed;
					SessionHolder.Session.ConstantVolumeBarsAdded += SessionOnConstantVolumeBarsAdded;
					SessionHolder.Session.ConstantVolumeBarsUpdated += SessionOnConstantVolumeBarsUpdated;
					SessionHolder.Session.PointAndFigureBarsAdded += SessionOnPointAndFigureBarsAdded;
					SessionHolder.Session.PointAndFigureBarsUpdated += SessionOnPointAndFigureBarsUpdated;
					SessionHolder.Session.TimedBarsAdded += SessionOnTimedBarsAdded;
					SessionHolder.Session.TimedBarsUpdated += SessionOnTimedBarsUpdated;
					SessionHolder.Session.TFlowBarsAdded += SessionOnFlowBarsAdded;
					SessionHolder.Session.TFlowBarsUpdated += SessionOnFlowBarsUpdated;
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
					SessionHolder.Session.AccountChanged -= SessionOnAccountChanged;
					SessionHolder.Session.AlgorithmicOrderPlaced -= SessionOnAlgorithmicOrderPlaced;
					SessionHolder.Session.AlgorithmicOrderRegistrationComplete -= SessionOnAlgorithmicOrderRegistrationComplete;
					SessionHolder.Session.OrderChanged -= SessionOnOrderChanged;
					SessionHolder.Session.PositionsStatementResolved -= SessionOnPositionsStatementResolved;
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					SessionHolder.Session.InstrumentDOMChanged -= SessionOnInstrumentDomChanged;
					SessionHolder.Session.InstrumentChanged -= SessionOnInstrumentChanged;
					SessionHolder.Session.TicksAdded -= SessionOnTicksAdded;
					SessionHolder.Session.IncorrectSymbol -= SessionOnIncorrectSymbol;
					SessionHolder.Session.InstrumentSubscribed -= SessionOnInstrumentSubscribed;
					SessionHolder.Session.ConstantVolumeBarsAdded -= SessionOnConstantVolumeBarsAdded;
					SessionHolder.Session.ConstantVolumeBarsUpdated -= SessionOnConstantVolumeBarsUpdated;
					SessionHolder.Session.PointAndFigureBarsAdded -= SessionOnPointAndFigureBarsAdded;
					SessionHolder.Session.PointAndFigureBarsUpdated -= SessionOnPointAndFigureBarsUpdated;
					SessionHolder.Session.TimedBarsAdded -= SessionOnTimedBarsAdded;
					SessionHolder.Session.TimedBarsUpdated -= SessionOnTimedBarsUpdated;
					SessionHolder.Session.TFlowBarsAdded -= SessionOnFlowBarsAdded;
					SessionHolder.Session.TFlowBarsUpdated -= SessionOnFlowBarsUpdated;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void SessionOnDataError(object cqgError, string errorDescription)
		{
			SendOutError(errorDescription);
		}

		private void SessionOnCelStarted()
		{
			SendOutMessage(new ConnectMessage());
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
					if (SessionHolder.Session == null)
					{
						_isSessionOwner = true;

						SessionHolder.Session = new CQGCELClass();
						SessionHolder.Session.CELStarted += SessionOnCelStarted;
						SessionHolder.Session.DataError += SessionOnDataError;
						SessionHolder.Session.Startup();
					}
					else
					{
						SendOutMessage(new ConnectMessage());
					}

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_isSessionOwner)
					{
						SessionHolder.Session.Shutdown();
						SessionHolder.Session.DataError -= SessionOnDataError;
						SessionHolder.Session.CELStarted -= SessionOnCelStarted;
						SessionHolder.Session = null;
					}
					else
						SendOutMessage(new DisconnectMessage());

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					switch (mdMsg.DataType)
					{
						case MarketDataTypes.Level1:
						{
							var instrument = SessionHolder.Instruments.TryGetValue(mdMsg.SecurityId.SecurityCode);
							//SessionHolder.Session.CreateInstrumentRequest().;

							break;
						}
						case MarketDataTypes.MarketDepth:
							break;
						case MarketDataTypes.Trades:
							break;
						case MarketDataTypes.OrderLog:
							break;
						case MarketDataTypes.CandleTimeFrame:
							break;
						default:
							throw new ArgumentOutOfRangeException("message", mdMsg.DataType, LocalizedStrings.Str1618);
					}

					var reply = (MarketDataMessage)mdMsg.Clone();
					reply.OriginalTransactionId = mdMsg.TransactionId;
					SendOutMessage(reply);

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					var instrument = SessionHolder.Instruments.TryGetValue(regMsg.SecurityId.SecurityCode);

					if (instrument == null)
						throw new InvalidOperationException(LocalizedStrings.Str3792Params.Put(regMsg.SecurityId.SecurityCode));

					var account = _accounts.TryGetValue(regMsg.PortfolioName);

					if (account == null)
						throw new InvalidOperationException(LocalizedStrings.Str3793Params.Put(regMsg.PortfolioName));

					var stopPrice = regMsg.OrderType == OrderTypes.Conditional
						? ((CQGOrderCondition)regMsg.Condition).StopPrice
						: null;

					var order = SessionHolder.Session.CreateOrder(regMsg.OrderType.ToCQG(stopPrice), instrument, account, (int)regMsg.Volume, regMsg.Side.ToCQG(), (double)regMsg.Price, (double)(stopPrice ?? 0));
					_orders.Add(regMsg.TransactionId, order);
					order.Place();
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;
					var order = _orders.TryGetValue(cancelMsg.OrderTransactionId);

					if (order == null)
						throw new InvalidOperationException(LocalizedStrings.Str3794Params.Put(cancelMsg.OrderTransactionId));
					else
						order.Cancel();

					break;
				}

				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;
					var order = _orders.TryGetValue(replaceMsg.OldTransactionId);

					if (order == null)
					{
						throw new InvalidOperationException(LocalizedStrings.Str3794Params.Put(replaceMsg.OldTransactionId));
					}
					else
					{
						var modify = order.PrepareModify();
						modify.Properties[eOrderProperty.opLimitPrice].Value = replaceMsg.Price;
						modify.Properties[eOrderProperty.opQuantity].Value = replaceMsg.Volume;
						order.Modify(modify);
					}

					break;
				}
			}
		}
	}
}