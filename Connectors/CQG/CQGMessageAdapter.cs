namespace StockSharp.CQG
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;

	using global::CQG;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// CQG message adapter.
	/// </summary>
	public partial class CQGMessageAdapter : MessageAdapter
	{
		private readonly SynchronizedDictionary<long, CQGOrder> _orders = new SynchronizedDictionary<long, CQGOrder>();
		private readonly SynchronizedDictionary<string, CQGAccount> _accounts = new SynchronizedDictionary<string, CQGAccount>();
		private readonly SynchronizedDictionary<string, CQGInstrument> _instruments = new SynchronizedDictionary<string, CQGInstrument>();
		private CQGCEL _session;

		/// <summary>
		/// Initializes a new instance of the <see cref="CQGMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public CQGMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new CQGOrderCondition();
		}

		private void SessionOnDataError(object cqgError, string errorDescription)
		{
			SendOutError(errorDescription);
		}

		private void SessionOnCelStarted()
		{
			SendOutMessage(new ConnectMessage());
		}

		private void DisposeSession()
		{
			_session.AccountChanged -= SessionOnAccountChanged;
			_session.AlgorithmicOrderPlaced -= SessionOnAlgorithmicOrderPlaced;
			_session.AlgorithmicOrderRegistrationComplete -= SessionOnAlgorithmicOrderRegistrationComplete;
			_session.OrderChanged -= SessionOnOrderChanged;
			_session.PositionsStatementResolved -= SessionOnPositionsStatementResolved;

			_session.InstrumentDOMChanged -= SessionOnInstrumentDomChanged;
			_session.InstrumentChanged -= SessionOnInstrumentChanged;
			_session.TicksAdded -= SessionOnTicksAdded;
			_session.IncorrectSymbol -= SessionOnIncorrectSymbol;
			_session.InstrumentSubscribed -= SessionOnInstrumentSubscribed;
			_session.ConstantVolumeBarsAdded -= SessionOnConstantVolumeBarsAdded;
			_session.ConstantVolumeBarsUpdated -= SessionOnConstantVolumeBarsUpdated;
			_session.PointAndFigureBarsAdded -= SessionOnPointAndFigureBarsAdded;
			_session.PointAndFigureBarsUpdated -= SessionOnPointAndFigureBarsUpdated;
			_session.TimedBarsAdded -= SessionOnTimedBarsAdded;
			_session.TimedBarsUpdated -= SessionOnTimedBarsUpdated;
			_session.TFlowBarsAdded -= SessionOnFlowBarsAdded;
			_session.TFlowBarsUpdated -= SessionOnFlowBarsUpdated;

			_session.DataError -= SessionOnDataError;
			_session.CELStarted -= SessionOnCelStarted;

			_session.Shutdown();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					if (_session != null)
					{
						try
						{
							DisposeSession();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_session = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

				case MessageTypes.Connect:
				{
					if (_session != null)
						throw new InvalidOperationException(LocalizedStrings.Str1619);

					_session = new CQGCELClass();
					_session.CELStarted += SessionOnCelStarted;
					_session.DataError += SessionOnDataError;

					_session.AccountChanged += SessionOnAccountChanged;
					_session.AlgorithmicOrderPlaced += SessionOnAlgorithmicOrderPlaced;
					_session.AlgorithmicOrderRegistrationComplete += SessionOnAlgorithmicOrderRegistrationComplete;
					_session.OrderChanged += SessionOnOrderChanged;
					_session.PositionsStatementResolved += SessionOnPositionsStatementResolved;

					_session.InstrumentDOMChanged += SessionOnInstrumentDomChanged;
					_session.InstrumentChanged += SessionOnInstrumentChanged;
					_session.TicksAdded += SessionOnTicksAdded;
					_session.IncorrectSymbol += SessionOnIncorrectSymbol;
					_session.InstrumentSubscribed += SessionOnInstrumentSubscribed;
					_session.ConstantVolumeBarsAdded += SessionOnConstantVolumeBarsAdded;
					_session.ConstantVolumeBarsUpdated += SessionOnConstantVolumeBarsUpdated;
					_session.PointAndFigureBarsAdded += SessionOnPointAndFigureBarsAdded;
					_session.PointAndFigureBarsUpdated += SessionOnPointAndFigureBarsUpdated;
					_session.TimedBarsAdded += SessionOnTimedBarsAdded;
					_session.TimedBarsUpdated += SessionOnTimedBarsUpdated;
					_session.TFlowBarsAdded += SessionOnFlowBarsAdded;
					_session.TFlowBarsUpdated += SessionOnFlowBarsUpdated;

					_session.Startup();

					break;
				}

				case MessageTypes.Disconnect:
				{
					if (_session == null)
						throw new InvalidOperationException(LocalizedStrings.Str1856);

					DisposeSession();
					_session = null;

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
							var instrument = _instruments.TryGetValue(mdMsg.SecurityId.SecurityCode);
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
						{
							SendOutMarketDataNotSupported(mdMsg.TransactionId);
							return;
						}
					}

					var reply = (MarketDataMessage)mdMsg.Clone();
					reply.OriginalTransactionId = mdMsg.TransactionId;
					SendOutMessage(reply);

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					var instrument = _instruments.TryGetValue(regMsg.SecurityId.SecurityCode);

					if (instrument == null)
						throw new InvalidOperationException(LocalizedStrings.Str3792Params.Put(regMsg.SecurityId.SecurityCode));

					var account = _accounts.TryGetValue(regMsg.PortfolioName);

					if (account == null)
						throw new InvalidOperationException(LocalizedStrings.Str3793Params.Put(regMsg.PortfolioName));

					var stopPrice = regMsg.OrderType == OrderTypes.Conditional
						? ((CQGOrderCondition)regMsg.Condition).StopPrice
						: null;

					var order = _session.CreateOrder(regMsg.OrderType.ToCQG(stopPrice), instrument, account, (int)regMsg.Volume, regMsg.Side.ToCQG(), (double)regMsg.Price, (double)(stopPrice ?? 0));
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