namespace StockSharp.Algo.Strategies.Protective
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base strategy of the position protection.
	/// </summary>
	[Obsolete("Use ProtectiveController class.")]
	public abstract class ProtectiveStrategy : QuotingStrategy, IProtectiveStrategy
	{
		private readonly bool _isUpTrend;
		private bool _quotingStarted;
		private ProtectiveProcessor _processor;

		/// <summary>
		/// Initialize <see cref="ProtectiveStrategy"/>.
		/// </summary>
		/// <param name="protectiveSide">Protected position side.</param>
		/// <param name="protectivePrice">Protected position price.</param>
		/// <param name="protectiveVolume">The protected position volume.</param>
		/// <param name="protectiveLevel">The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from <paramref name="protectivePrice" /> is specified.</param>
		/// <param name="isUpTrend">To track price increase or falling.</param>
		protected ProtectiveStrategy(Sides protectiveSide, decimal protectivePrice, decimal protectiveVolume, Unit protectiveLevel, bool isUpTrend)
			: base(protectiveSide.Invert(), protectiveVolume)
		{
			_protectiveLevel = this.Param(nameof(ProtectiveLevel), new Unit());
			_useQuoting = this.Param<bool>(nameof(UseQuoting));
			_useMarketOrders = this.Param<bool>(nameof(UseMarketOrders));
			_bestPriceOffset = this.Param(nameof(BestPriceOffset), new Unit());
			_priceOffset = this.Param(nameof(PriceOffset), new Unit());
			_isTrailing = this.Param<bool>(nameof(IsTrailing));

			_isUpTrend = isUpTrend;
		
			ProtectiveSide = protectiveSide;
			ProtectivePrice = protectivePrice;
			ProtectiveLevel = protectiveLevel;
		}

		/// <inheritdoc />
		public decimal ProtectivePrice { get; }

		/// <inheritdoc />
		public Sides ProtectiveSide { get; }

		/// <inheritdoc />
		public decimal ProtectiveVolume
		{
			get => LeftVolume;
			set
			{
				QuotingVolume = value - Position.Abs();
				ProtectiveVolumeChanged?.Invoke();
			}
		}

		/// <inheritdoc />
		public event Action ProtectiveVolumeChanged;

		private readonly StrategyParam<Unit> _protectiveLevel;

		/// <summary>
		/// The protective level. If the <see cref="Unit.Type"/> type is equal to <see cref="UnitTypes.Limit"/>, then the given price is specified. Otherwise, the shift value from the protected trade <see cref="Trade"/> is specified.
		/// </summary>
		public Unit ProtectiveLevel
		{
			get => _protectiveLevel.Value;
			set
			{
				if (ProcessState == ProcessStates.Started)
					throw new InvalidOperationException(LocalizedStrings.NotStoppedBefore);

				_protectiveLevel.Value = value;
			}
		}

		private readonly StrategyParam<bool> _isTrailing;

		/// <summary>
		/// Whether to use a trailing technique. For the <see cref="TakeProfitStrategy"/> with profit increasing the level of profit taking is automatically increased. For the <see cref="StopLossStrategy"/> with profit increasing the level of loss protection is automatically increased. The default is disabled.
		/// </summary>
		public bool IsTrailing
		{
			get => _isTrailing.Value;
			set
			{
				if (value && ProtectiveLevel.Type == UnitTypes.Limit)
					throw new ArgumentException(LocalizedStrings.TrailingCannotUse);

				_isTrailing.Value = value;
			}
		}

		/// <summary>
		/// Whether the protective strategy is activated.
		/// </summary>
		public bool IsActivated { get; private set; }

		/// <summary>
		/// The protective strategy activation event.
		/// </summary>
		public event Action Activated;

		private readonly StrategyParam<bool> _useQuoting;

		/// <summary>
		/// Whether to quote the registered order by the market price. The default mode is disabled.
		/// </summary>
		public bool UseQuoting
		{
			get => _useQuoting.Value;
			set => _useQuoting.Value = value;
		}

		private readonly StrategyParam<bool> _useMarketOrders;

		/// <summary>
		/// Whether to use <see cref="OrderTypes.Market"/> for protection.
		/// </summary>
		/// <remarks>
		/// By default, orders <see cref="OrderTypes.Limit"/> are used.
		/// </remarks>
		public bool UseMarketOrders
		{
			get => _useMarketOrders.Value;
			set => _useMarketOrders.Value = value;
		}

		private readonly StrategyParam<Unit> _bestPriceOffset;

		/// <summary>
		/// The shift from the best price, on which the quoted order can be changed.
		/// </summary>
		public Unit BestPriceOffset
		{
			get => _bestPriceOffset.Value;
			set => _bestPriceOffset.Value = value;
		}

		private readonly StrategyParam<Unit> _priceOffset;

		/// <summary>
		/// The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).
		/// </summary>
		public Unit PriceOffset
		{
			get => _priceOffset.Value;
			set => _priceOffset.Value = value;
		}

		/// <inheritdoc />
		public override decimal QuotingVolume
		{
			set
			{
				if (UseQuoting)
				{
					foreach (var childStrategy in ChildStrategies.OfType<QuotingStrategy>())
					{
						childStrategy.QuotingVolume = value;
					}
				}
				else
				{
					base.QuotingVolume = value;
				}
			}
		}

		/// <inheritdoc />
		protected override IEnumerable<IMarketRule> GetNotificationRules()
		{
			foreach(var r in base.GetNotificationRules())
				yield return r;

#pragma warning disable CS0618 // Type or member is obsolete
			yield return this.GetSecurity().WhenNewTrade(this);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			this.AddInfoLog("Position protection {0}/{1} with volume {2}. Level={3}, Trailing={4}, Market orders={5}, Quoting={6}, Slippage={7}",
				ProtectiveSide, ProtectivePrice, ProtectiveVolume, ProtectiveLevel, IsTrailing, UseMarketOrders, UseQuoting, PriceOffset);

			this.SubscribeTrades(this.GetSecurity());
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			_quotingStarted = default;
			_processor = default;

			base.OnReseted();
		}

		/// <inheritdoc />
		protected override void ProcessTimeOut(DateTimeOffset currentTime)
		{
			ProcessQuoting(currentTime);
		}

		/// <inheritdoc />
		protected override void RegisterQuotingOrder(Order order)
		{
			if (UseMarketOrders)
				order.Type = OrderTypes.Market;

			base.RegisterQuotingOrder(order);
		}

		/// <inheritdoc />
		protected override decimal? NeedQuoting(DateTimeOffset currentTime, decimal? currentPrice, decimal? currentVolume, decimal newVolume)
		{
			if (IsActivated)
			{
				// отдельное котирование работает со своей логикой перестановки
				//if (UseQuoting)

				// после активации не меняем заявку
				// если нужно менять заявку, нужно использовать котирование
				return null;
			}

			var ltp = LastTradePrice;
			var bestPrice = UseLastTradePrice && ltp != null ? ltp : BestPrice;

			_processor ??= new(ProtectiveSide, ProtectivePrice, _isUpTrend, IsTrailing, ProtectiveLevel, UseMarketOrders, PriceOffset, TimeOut, this);
			
			var price = _processor.GetActivationPrice(bestPrice, currentTime);

			if (price is null)
				return null;

			RaiseActivated(price.Value);

			if (!UseQuoting)
				return price;

			this.AddInfoLog(LocalizedStrings.Reregistering);

			_quotingStarted = true;

			var quoting = CreateQuoting();

			quoting.Name = Name + "_" + quoting.Name;

			quoting
				.WhenStopped()
				.Do(Stop)
				.Once()
				.Apply(this);

			ChildStrategies.Add(quoting);

			//Если включен режим котирования, то после запуска стратегии котирования
			//нет необходимости выставлять заявку в базовой стратегии.
			return null;
		}

		/// <inheritdoc />
		protected override void ProcessQuoting(DateTimeOffset currentTime)
		{
			if (UseQuoting && _quotingStarted)
				return;

			base.ProcessQuoting(currentTime);
		}

		/// <summary>
		/// To create a quoting strategy for the protective order (its fulfilment is guaranteed).
		/// </summary>
		/// <returns>The strategy of quoting.</returns>
		protected virtual QuotingStrategy CreateQuoting()
		{
			return new MarketQuotingStrategy(QuotingDirection, ProtectiveVolume)
			{
				BestPriceOffset = BestPriceOffset,
				PriceOffset = PriceOffset,
			};
		}

		private void RaiseActivated(decimal price)
		{
			this.AddInfoLog(LocalizedStrings.ProtectionActivated, price == 0 ? LocalizedStrings.Market : price.To<string>());

			IsActivated = true;
			Activated?.Invoke();
		}
	}
}