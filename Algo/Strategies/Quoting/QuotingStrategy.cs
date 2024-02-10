namespace StockSharp.Algo.Strategies.Quoting
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Base quoting strategy class.
	/// </summary>
	public abstract class QuotingStrategy : Strategy
	{
		private Order _reRegisteringOrder;
		private Order _editingChanges;
		private bool _isCanceling;
		private bool _isRegistering;
		//private Order _manualReRegisterOrder;
		private bool _isReRegistedFailed;
		private IOrderBookMessage _filteredBook;
		private ITickTradeMessage _lastTrade;

		private IEnumerable<IMarketRule> _notificationRules;

		/// <summary>
		/// Initialize <see cref="QuotingStrategy"/>.
		/// </summary>
		/// <param name="quotingDirection">Quoting direction.</param>
		/// <param name="quotingVolume">Total quoting volume.</param>
		protected QuotingStrategy(Sides quotingDirection, decimal quotingVolume)
		{
			CheckQuotingVolume(quotingVolume);

			_quotingDirection = this.Param(nameof(QuotingDirection), quotingDirection);
			_quotingVolume = this.Param(nameof(QuotingVolume), quotingVolume);
			_timeOut = this.Param<TimeSpan>(nameof(TimeOut));
			_useLastTradePrice = this.Param(nameof(UseLastTradePrice), true);
			_isSupportAtomicReRegister = this.Param(nameof(IsSupportAtomicReRegister), true);

			DisposeOnStop = true;
		}

		/// <summary>
		/// Initialize <see cref="QuotingStrategy"/>.
		/// </summary>
		/// <param name="order">Quoting order.</param>
		protected QuotingStrategy(Order order)
			: this(order.Side, order.TransactionId == 0 ? order.Volume : order.Balance)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var isNew = order.TransactionId == 0;

			// если была передана на котирование уже зарегистрированная заявка,
			// то будем котировать именно её
			if (!isNew)
				_order = order;
		}

		private readonly StrategyParam<bool> _isSupportAtomicReRegister;

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via the method <see cref="ITransactionProvider.ReRegisterOrder"/> as a single transaction. The default is enabled.
		/// </summary>
		public bool IsSupportAtomicReRegister
		{
			get => _isSupportAtomicReRegister.Value;
			set => _isSupportAtomicReRegister.Value = value;
		}

		private readonly StrategyParam<bool> _useLastTradePrice;

		/// <summary>
		/// To use the last trade price, if the information in the order book is missed. The default is enabled.
		/// </summary>
		public bool UseLastTradePrice
		{
			get => _useLastTradePrice.Value;
			set => _useLastTradePrice.Value = value;
		}

		private readonly StrategyParam<Sides> _quotingDirection;

		/// <summary>
		/// Quoting direction.
		/// </summary>
		public Sides QuotingDirection
		{
			get => _quotingDirection.Value;
			set
			{
				if (value == QuotingDirection)
					return;

				if (Position != 0)
					throw new InvalidOperationException("Pos != 0");

				_quotingDirection.Value = value;
			}
		}

		private readonly StrategyParam<decimal> _quotingVolume;

		/// <summary>
		/// Total quoting volume.
		/// </summary>
		public virtual decimal QuotingVolume
		{
			get => _quotingVolume.Value;
			set
			{
				if (ProcessState == ProcessStates.Started)
					throw new InvalidOperationException("In process.");

				if (value == QuotingVolume)
					return;

				this.AddInfoLog(LocalizedStrings.OldVolNewVol, QuotingVolume, value);

				CheckQuotingVolume(value);

				_quotingVolume.Value = value;
			}
		}

		private static void CheckQuotingVolume(decimal quotingVolume)
		{
			if (quotingVolume <= 0)
				throw new ArgumentOutOfRangeException(nameof(quotingVolume), quotingVolume, LocalizedStrings.InvalidValue);

			//if (checkOnZero && quotingVolume == 0)
			//	throw new ArgumentOutOfRangeException("quotingVolume", quotingVolume, "Котируемый объем не может быть нулевым.");
		}

		private Order _order;

		/// <summary>
		/// The order with which the quoting strategy is currently operating.
		/// </summary>
		[Browsable(false)]
		public virtual Order Order => _order;

		/// <summary>
		/// The volume which is left to fulfill before the quoting end.
		/// </summary>
		[Browsable(false)]
		public decimal LeftVolume => QuotingVolume - Position.Abs();

		private readonly StrategyParam<TimeSpan> _timeOut;

		/// <summary>
		/// The time limit during which the quoting should be fulfilled. If the total volume of <see cref="QuotingVolume"/> will not be fulfilled by this time, the strategy will stop operating.
		/// </summary>
		/// <remarks>
		/// By default, the limit is disabled and it is equal to <see cref="TimeSpan.Zero"/>.
		/// </remarks>
		public TimeSpan TimeOut
		{
			get => _timeOut.Value;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_timeOut.Value = value;
			}
		}

		/// <summary>
		/// Is the <see cref="TimeOut"/> occurred.
		/// </summary>
		/// <param name="currentTime">Current time.</param>
		/// <returns>Check result.</returns>
		protected bool IsTimeOut(DateTimeOffset currentTime) => (TimeOut != TimeSpan.Zero && (currentTime - StartedTime) >= TimeOut);

		/// <summary>
		/// Whether the quoting can be stopped.
		/// </summary>
		/// <returns><see langword="true" /> it is possible, otherwise, <see langword="false" />.</returns>
		/// <remarks>
		/// By default, the quoting is stopped when all contracts are fulfilled and <see cref="LeftVolume"/> is equal to 0.
		/// </remarks>
		protected virtual bool NeedFinish()
		{
			return LeftVolume <= 0;
		}

		/// <summary>
		/// Should the order be quoted.
		/// </summary>
		/// <param name="currentTime">Current time.</param>
		/// <param name="currentPrice">The current price. If the value is equal to <see langword="null" /> then the order is not registered yet.</param>
		/// <param name="currentVolume">The current volume. If the value is equal to <see langword="null" /> then the order is not registered yet.</param>
		/// <param name="newVolume">New volume.</param>
		/// <returns>The price at which the order will be registered. If the value is equal to <see langword="null" /> then the quoting is not required.</returns>
		protected virtual decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume/*, Range<decimal> acceptablePriceRange*/, decimal newVolume)
		{
			//if (acceptablePriceRange == null)
			//	throw new ArgumentNullException(nameof(acceptablePriceRange));

			var bestPrice = BestPrice;

		    if (bestPrice == null)
		    {
				//this.AddWarningLog(LocalizedStrings.MarketDepthIsEmpty);
			    return null;
		    }

			if (bestPrice != currentPrice || currentVolume != newVolume)
				return bestPrice;

			return null;
		}

		/// <summary>
		/// To get the preferrable quoting price. If it is impossible to calculate the it at the moment, then <see langword="null" /> will be returned.
		/// </summary>
		protected virtual decimal? BestPrice
		{
			get
			{
				var quote = GetFilteredQuotes(QuotingDirection)?.FirstOr();

				if (quote == null)
				{
					return UseLastTradePrice
						? LastTradePrice
						: null;
				}

				return quote.Value.Price;
			}
		}

		/// <summary>
		/// Last trade price.
		/// </summary>
		protected decimal? LastTradePrice => _lastTrade?.Price;

		/// <summary>
		/// To get a new volume for an order.
		/// </summary>
		/// <returns>The new volume for an order.</returns>
		protected virtual decimal GetNewVolume()
		{
			if (Volume <= 0)
				return LeftVolume;

			var newVolume = Volume;

			if (LeftVolume > 0)
				newVolume = newVolume.Min(LeftVolume);

			return newVolume;
		}

		/// <summary>
		/// To get the order book filtered <see cref="DataType.FilteredMarketDepth"/>.
		/// </summary>
		/// <param name="side">The order book side (bids or offers).</param>
		/// <returns>The filtered order book.</returns>
		protected QuoteChange[] GetFilteredQuotes(Sides side)
		{
			var book = _filteredBook;

			if (book == null)
				return null;

			return side == Sides.Buy ? book.Bids : book.Asks;
		}

		/// <summary>
		/// To get a list of rules on which the quoting will respond.
		/// </summary>
		/// <returns>Rule list.</returns>
		protected virtual IEnumerable<IMarketRule> GetNotificationRules()
		{
			yield return this.SubscribeFilteredMarketDepth(this.GetSecurity()).WhenOrderBookReceived(this);
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			//CurrentBestPrice = default;
			_editingChanges = default;
			_isCanceling = default;
			_isRegistering = default;
			_reRegisteringOrder = default;
			//_manualReRegisterOrder = default;
			_isReRegistedFailed = default;
			_order = default;
			_filteredBook = default;
			_lastTrade = default;

			this.AddInfoLog(LocalizedStrings.QuotingForVolume, QuotingDirection, QuotingVolume);

			this.SuspendRules(() =>
			{
				var security = this.GetSecurity();

				this
					.SubscribeFilteredMarketDepth(security)
					.WhenOrderBookReceived(this)
					.Do(book => _filteredBook = book)
					.Apply(this);

				this
					.SubscribeTrades(security)
					.WhenTickTradeReceived(this)
					.Do(t => _lastTrade = t)
					.Apply(this);

				this
					.WhenStopping()
					.Do(() =>
					{
						if (LeftVolume > 0)
							this.AddWarningLog(LocalizedStrings.QuotingFinishedNotFull, LeftVolume);
					})
					.Once()
					.Apply(this);

				_notificationRules = GetNotificationRules().ToArray();
				if (!_notificationRules.IsEmpty())
				{
					_notificationRules
						.Or()
						.Do(() => ProcessQuoting(CurrentTime))
						.Apply(this);
				}

				this
					.WhenPositionChanged()
					.Do(() =>
					{
						this.AddInfoLog(LocalizedStrings.PrevPosNewPos, security, Position, LeftVolume);

						if (NeedFinish())
						{
							this.AddInfoLog(LocalizedStrings.Stopped);
							Stop();
						}
					})
					.Apply(this);

				if (TimeOut > TimeSpan.Zero)
				{
					SafeGetConnector()
						.WhenIntervalElapsed(TimeOut)
						.Do(() => ProcessTimeOut(CurrentTime))
						.Once()
						.Apply(this);
				}
			});

			if (!IsRulesSuspended)
				ProcessQuoting(time);
		}

		/// <summary>
		/// The <see cref="TimeOut"/> occurrence event handler.
		/// </summary>
		protected virtual void ProcessTimeOut(DateTimeOffset currentTime)
		{
			Stop();
		}

		/// <inheritdoc />
		protected override void OnStopping()
		{
			// an error may happen during startup. in that case rules will be null
			_notificationRules?.ForEach(r => r.Suspend(true));

			base.OnStopping();
		}

		/// <summary>
		/// To register the quoted order.
		/// </summary>
		/// <param name="order">The quoted order.</param>
		protected virtual void RegisterQuotingOrder(Order order)
		{
			AddOrderRules(order);

			//_manualReRegisterOrder = default;

			_isRegistering = true;
			RegisterOrder(order);
		}

		private void ProcessRegisteredOrder(Order o)
		{
			if (o == _order)
			{
				_isRegistering = false;
				this.AddInfoLog(LocalizedStrings.OrderAcceptedByExchange, o.TransactionId);
			}
			else if (o == _reRegisteringOrder)
			{
				this.AddInfoLog(LocalizedStrings.OrderReplacedByNew, _order.TransactionId, _reRegisteringOrder.TransactionId);

				Rules.RemoveRulesByToken(_order, null);

				_order = _reRegisteringOrder;
				_reRegisteringOrder = null;
			}
			else
				this.AddWarningLog(LocalizedStrings.OrderOutOfDate, o.TransactionId);

			ProcessQuoting(o.ServerTime);
		}

		private void AddOrderRules(Order order)
		{
			var regRule = order
				.WhenRegistered(this)
				.Do(ProcessRegisteredOrder)
				.Once()
				.Apply(this);

			var regFailRule = order
				.WhenRegisterFailed(this)
				.Do(fail =>
				{
					var o = fail.Order;

					this.AddErrorLog(LocalizedStrings.ErrorRegOrder, o.TransactionId, fail.Error.Message);

					var canProcess = false;

					if (o == _order)
					{
						_order = null;
						_isRegistering = false;
						canProcess = true;
					}
					else if (o == _reRegisteringOrder)
					{
						_reRegisteringOrder = null;
						_isReRegistedFailed = true;
					}
					else
						this.AddWarningLog(LocalizedStrings.OrderOutOfDate, o.TransactionId);

					if (canProcess)
						ProcessQuoting(fail.ServerTime);
				})
				.Once()
				.Apply(this);

			regRule.Exclusive(regFailRule);

			var matchedRule = order
				.WhenMatched(this)
				.Do((r, o) =>
				{
					this.AddInfoLog(LocalizedStrings.OrderMatchedRemainBalance, o.TransactionId, LeftVolume);

					Rules.RemoveRulesByToken(o, r);

					// исполнилась заявка, которая сейчас в процессе регистрации
					if (o == _reRegisteringOrder)
					{
						ProcessRegisteredOrder(o);
					}

					// http://stocksharp.com/forum/yaf_postst1708_MarketQuotingStrategy---Obiem-zaiavki-nie-mozhiet-byt--nulievym.aspx
					if (NeedFinish())
					{
						this.AddInfoLog(LocalizedStrings.Stopped);
						Stop();
					}
					else
					{
						if (_order == o)
						{
							//_manualReRegisterOrder = default;
							_isCanceling = default;
							_order = default;
							_reRegisteringOrder = default;
							_isReRegistedFailed = default;
							_editingChanges = default;

							ProcessQuoting(o.ServerTime);
						}
						else
							this.AddWarningLog(LocalizedStrings.OrderOutOfDate, o.TransactionId);
					}
				})
				.Once()
				.Apply(this);

			regFailRule.Exclusive(matchedRule);
		}

		/// <summary>
		/// To initiate the quoting.
		/// </summary>
		/// <param name="currentTime">Current time.</param>
		protected virtual void ProcessQuoting(DateTimeOffset currentTime)
		{
			if (ProcessState != ProcessStates.Started)
			{
				this.AddWarningLog(LocalizedStrings.StrategyInState, ProcessState);
				return;
			}

			if (_order != null)
			{
				if (_isCanceling)
				{
					this.AddDebugLog(LocalizedStrings.OrderNRegistering, _order.TransactionId);
					return;
				}
				else if (_isRegistering)
				{
					this.AddDebugLog(LocalizedStrings.OrderNReplacing, _order.TransactionId);
					return;
				}
				else if (_editingChanges != null)
				{
					this.AddDebugLog(LocalizedStrings.OrderReplacingInto, _order.TransactionId, _editingChanges.TransactionId);
					return;
				}
				else if (_reRegisteringOrder != null)
				{
					this.AddDebugLog(LocalizedStrings.OrderReplacingInto, _order.TransactionId, _reRegisteringOrder.TransactionId);
					return;
				}
				else if (_isReRegistedFailed)
				{
					this.AddDebugLog(LocalizedStrings.ReplacingNotCompleteWaitingFor, _order.TransactionId);
					return;
				}

				if (ProcessState != ProcessStates.Started)
					return;
			}

			//// pyhta4og: При тестировании на истории - нормальная ситуация что нет стакана или последней сделки в начале тестирования.
			////http://stocksharp.com/forum/yaf_postst779_Oshibka-zashchitnykh-stratieghii---kolliektsiia-kotirovok-pusta.aspx
			//var priceRange = GetAcceptablePriceRange();
			//if (priceRange == null)
			//{
			//	this.AddWarningLog(LocalizedStrings.MarketDepthIsEmpty);
			//	return;
			//}

			var newVolume = GetNewVolume();

			var newPrice = NeedQuoting(currentTime, _order?.Price, _order?.Balance, newVolume);

			if (newPrice == null)
				return;

			newPrice = Security.ShrinkPrice(newPrice.Value);

			this.AddInfoLog(LocalizedStrings.CurrPriceBestPrice, _order?.Price ?? (object)"NULL", newPrice);

			var bidPrice = _filteredBook?.Bids?.FirstOr()?.Price ?? this.GetSecurityValue<decimal?>(Level1Fields.BestBidPrice);
			var askPrice = _filteredBook?.Asks?.FirstOr()?.Price ?? this.GetSecurityValue<decimal?>(Level1Fields.BestAskPrice);

			this.AddInfoLog(LocalizedStrings.BestBidAsk, bidPrice ?? (object)"NULL", askPrice ?? (object)"NULL");

			if (_order == null)
			{
				//if (_manualReRegisterOrder != null)
				//	return;

				if (!this.IsFormedAndOnlineAndAllowTrading())
					return;

				_order = this.CreateOrder(QuotingDirection, newPrice.Value, newVolume);
				RegisterQuotingOrder(_order);
			}
			else
			{
				this.AddInfoLog("Quoting order {0} to {1} with price {2} volume {3}.", _order.TransactionId, _order.Side, _order.Price, _order.Volume);

				if (IsSupportAtomicReRegister && IsOrderReplaceable(_order) == true)
				{
					if (newPrice == 0)
					{
						this.AddWarningLog(LocalizedStrings.CannotChangePriceToZero);
						return;
					}

					var newOrder = _order.ReRegisterClone(newPrice, _order.Balance);

					if (IsOrderEditable(_order) == true)
					{
						_editingChanges = newOrder;

						var editRule = _order
							.WhenEdited(this)
							.Do(o =>
							{
								this.AddDebugLog("Order {0} edited.", o.TransactionId);

								_editingChanges = null;
								ProcessQuoting(o.ServerTime);
							})
							.Once()
							.Apply(this);

						var editFailRule = _order
							.WhenEditFailed(this)
							.Do(fail =>
							{
								var canProcess = false;

								this.AddErrorLog("Order {0} edit failed: {1}", fail.Order.TransactionId, fail.Error.Message);

								if (fail.Order == _order)
								{
									canProcess = true;
								}
								else
									this.AddWarningLog(LocalizedStrings.OrderOutOfDate, fail.Order.TransactionId);

								_editingChanges = null;

								if (canProcess)
									ProcessQuoting(fail.ServerTime);
							})
							.Once()
							.Apply(this);

						editRule.Exclusive(editFailRule);

						EditOrder(_order, newOrder);
					}
					else
					{
						AddOrderRules(newOrder);

						_reRegisteringOrder = newOrder;
						_isReRegistedFailed = false;

						ReRegisterOrder(_order, newOrder);
					}

					this.AddInfoLog("Requoting registered for order {0} with price {1} and volume {2}.", _order.TransactionId, newOrder.Price, newOrder.Volume);
				}
				else
				{
					this.AddInfoLog(LocalizedStrings.CancellingOrderN, _order.TransactionId);

					_order
						.WhenCanceled(this)
						.Do((r, o) =>
						{
							this.AddInfoLog(LocalizedStrings.OrderCancelledAt, o.TransactionId, o.ServerTime);

							Rules.RemoveRulesByToken(o, r);

							if (_order == o)
							{
								//_manualReRegisterOrder = _order;
								_order = null;
								_isCanceling = false;
								ProcessQuoting(o.ServerTime);
							}
							else
								this.AddWarningLog(LocalizedStrings.OrderOutOfDate, o.TransactionId);
						})
						.Once()
						.Apply(this);

					_order
						.WhenCancelFailed(this)
						.Do((r, f) =>
						{
							this.AddInfoLog(LocalizedStrings.ErrorCancellingOrder, f.Order.TransactionId, f.Error);
							_isCanceling = false;
						})
						.Once()
						.Apply(this);

					_isCanceling = true;
					CancelOrder(_order);
				}
			}
		}
	}
}