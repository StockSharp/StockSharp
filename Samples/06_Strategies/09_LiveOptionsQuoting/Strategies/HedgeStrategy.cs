namespace StockSharp.Samples.Strategies.LiveOptionsQuoting;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Derivatives;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Localization;

/// <summary>
/// Base hedge strategy for derivatives.
/// </summary>
public abstract class HedgeStrategy : Strategy
{
	private readonly Dictionary<Security, QuotingProcessor> _quotingProcessors = [];
	private readonly Dictionary<Order, QuotingProcessor> _activeOrders = [];
	private readonly HashSet<Order> _pendingOrders = [];
	private bool _isRebalancing;

	/// <summary>
	/// Initializes a new instance of <see cref="HedgeStrategy"/>.
	/// </summary>
	/// <param name="blackScholes">The Black-Scholes model for derivatives.</param>
	protected HedgeStrategy(BasketBlackScholes blackScholes)
	{
		BlackScholes = blackScholes ?? throw new ArgumentNullException(nameof(blackScholes));

		_useQuoting = Param(nameof(UseQuoting), true)
			.SetDisplay("Use Quoting", "Whether to use quoting for orders instead of direct market orders", "Hedging");

		_priceOffset = Param(nameof(PriceOffset), new Unit())
			.SetDisplay("Price Offset", "The price offset from the market price when placing orders", "Hedging");

		_hedgingThreshold = Param(nameof(HedgingThreshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Hedging Threshold", "Minimum position change required to trigger rehedging", "Hedging");

		_monitoringInterval = Param(nameof(MonitoringInterval), TimeSpan.FromMinutes(5))
			.SetNotNegative()
			.SetDisplay("Monitoring Interval", "Interval between portfolio delta recalculations", "Hedging");
	}

	/// <summary>
	/// Portfolio model for calculating the values of Greeks by the Black-Scholes formula.
	/// </summary>
	protected BasketBlackScholes BlackScholes { get; }

	private readonly StrategyParam<bool> _useQuoting;

	/// <summary>
	/// Whether to use quoting for orders instead of direct market orders.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.QuotingKey,
		Description = LocalizedStrings.UseQuotingDescKey,
		GroupName = LocalizedStrings.HedgingKey,
		Order = 0)]
	public bool UseQuoting
	{
		get => _useQuoting.Value;
		set => _useQuoting.Value = value;
	}

	private readonly StrategyParam<Unit> _priceOffset;

	/// <summary>
	/// The price offset from the market price when placing orders.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceOffsetKey,
		Description = LocalizedStrings.PriceOffsetForOrderKey,
		GroupName = LocalizedStrings.HedgingKey,
		Order = 1)]
	public Unit PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	private readonly StrategyParam<decimal> _hedgingThreshold;

	/// <summary>
	/// Minimum position change required to trigger rehedging.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = "Hedging Threshold",
		Description = "Minimum position change required to trigger rehedging",
		GroupName = LocalizedStrings.HedgingKey,
		Order = 2)]
	public decimal HedgingThreshold
	{
		get => _hedgingThreshold.Value;
		set => _hedgingThreshold.Value = value;
	}

	private readonly StrategyParam<TimeSpan> _monitoringInterval;

	/// <summary>
	/// Interval between portfolio delta recalculations.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = "Monitoring Interval",
		Description = "Interval between portfolio delta recalculations",
		GroupName = LocalizedStrings.HedgingKey,
		Order = 3)]
	public TimeSpan MonitoringInterval
	{
		get => _monitoringInterval.Value;
		set => _monitoringInterval.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		BlackScholes.InnerModels.Clear();

		// Initialize underlying asset
		if (BlackScholes.UnderlyingAsset == null)
		{
			BlackScholes.UnderlyingAsset = Security;
			LogInfo("Underlying asset set to {0}", Security.Id);
		}

		// Find all option securities related to our underlying asset
		foreach (var security in Connector.Securities)
		{
			if (security.Type == SecurityTypes.Option && security.GetAsset(this) == BlackScholes.UnderlyingAsset)
			{
				BlackScholes.InnerModels.Add(new BlackScholes(security, this, this, BlackScholes.ExchangeInfoProvider));
				LogInfo("Added option model for {0}", security.Id);
			}
		}

		// Setup periodic rebalancing
		if (MonitoringInterval > TimeSpan.Zero)
		{
			this.WhenIntervalElapsed(MonitoringInterval)
				.Do(() =>
				{
					if (IsFormedAndOnlineAndAllowTrading())
					{
						LogInfo("Periodic rebalancing triggered");
						RehedgePositions(CurrentTime);
					}
				})
				.Apply(this);
		}

		// Subscribe to position changes for options and underlying asset
		this.WhenPositionChanged()
			.Do(() =>
			{
				if (IsFormedAndOnlineAndAllowTrading() && !_isRebalancing)
				{
					LogInfo("Position change detected - checking if rehedging is needed");
					RehedgePositions(CurrentTime);
				}
			})
			.Apply(this);

		// Initial rebalancing
		if (IsFormedAndOnlineAndAllowTrading())
		{
			RehedgePositions(time);
		}
	}

	/// <summary>
	/// Get orders required to rehedge the portfolio.
	/// </summary>
	/// <param name="currentTime">Current time for calculations.</param>
	/// <returns>Enumeration of orders needed for rehedging.</returns>
	protected abstract IEnumerable<Order> GetReHedgeOrders(DateTimeOffset currentTime);

	/// <summary>
	/// Rehedge positions based on current market conditions.
	/// </summary>
	/// <param name="currentTime">Current time for calculations.</param>
	protected virtual void RehedgePositions(DateTimeOffset currentTime)
	{
		// Skip if already rebalancing
		if (_isRebalancing || !IsFormedAndOnlineAndAllowTrading())
			return;

		try
		{
			_isRebalancing = true;
			LogInfo("Starting portfolio rebalancing");

			// Get required rehedging orders
			var orders = GetReHedgeOrders(currentTime).ToList();

			if (orders.Count == 0)
			{
				LogInfo("No rehedging needed");
				return;
			}

			LogInfo("Creating {0} rehedging orders", orders.Count);

			// Process each order
			foreach (var order in orders)
			{
				if (UseQuoting)
				{
					// Create quoting processor for this order
					CreateQuotingProcessor(order);
				}
				else
				{
					// Direct order registration with monitoring
					RegisterRehedgeOrder(order);
				}
			}
		}
		finally
		{
			_isRebalancing = false;
		}
	}

	/// <summary>
	/// Create quoting processor for an order.
	/// </summary>
	/// <param name="order">Order to be quoted.</param>
	protected virtual void CreateQuotingProcessor(Order order)
	{
		// Create appropriate quoting behavior
		var behavior = new MarketQuotingBehavior(
			PriceOffset,
			new Unit(0.1m, UnitTypes.Percent), // Default price deviation
			MarketPriceTypes.Following
		);

		// Create and configure the quoting processor
		var processor = new QuotingProcessor(
			behavior,
			order.Security,
			order.Portfolio,
			order.Side,
			order.Volume,
			order.Volume, // No splitting
			TimeSpan.Zero, // No timeout
			this, // Strategy implements ISubscriptionProvider
			this, // Strategy implements IMarketRuleContainer
			this, // Strategy implements ITransactionProvider
			this, // Strategy implements ITimeProvider
			this, // Strategy implements IMarketDataProvider
			IsFormedAndOnlineAndAllowTrading,
			true, // Use order book
			true  // Use last trade if no order book
		)
		{
			Parent = this
		};

		// Set up event handlers
		processor.OrderRegistered += o =>
		{
			_activeOrders[o] = processor;
			LogInfo("Rehedge order {0} registered at price {1}", o.TransactionId, o.Price);
		};

		processor.OrderFailed += fail =>
		{
			LogError("Rehedge order failed: {0}", fail.Error.Message);
		};

		processor.OwnTrade += trade =>
		{
			LogInfo("Rehedge trade executed: {0} {1} @ {2}",
				trade.Order.Side, trade.Trade.Volume, trade.Trade.Price);
		};

		processor.Finished += success =>
		{
			if (success)
				LogInfo("Rehedging completed successfully");
			else
				LogWarning("Rehedging finished with incomplete volume");

			// Remove processor
			_quotingProcessors.Remove(order.Security);
			processor.Dispose();
		};

		// Store and start the processor
		_quotingProcessors[order.Security] = processor;
		processor.Start();

		LogInfo("Started quoting for {0} {1} {2} shares",
			order.Security.Id, order.Side, order.Volume);
	}

	/// <summary>
	/// Register a rehedge order directly.
	/// </summary>
	/// <param name="order">Order to register.</param>
	protected virtual void RegisterRehedgeOrder(Order order)
	{
		if (_pendingOrders.Contains(order))
			return;

		_pendingOrders.Add(order);

		// Setup order monitoring rules
		order
			.WhenRegistered(this)
			.Do(o =>
			{
				LogInfo("Rehedge order {0} registered at price {1}", o.TransactionId, o.Price);
			})
			.Once()
			.Apply(this);

		order
			.WhenRegisterFailed(this)
			.Do(fail =>
			{
				LogError("Rehedge order failed: {0}", fail.Error.Message);
				_pendingOrders.Remove(order);
			})
			.Once()
			.Apply(this);

		var completionRule = order
			.WhenMatched(this)
			.Or(order.WhenCanceled(this))
			.Do((rule, o) =>
			{
				if (o.State == OrderStates.Done)
					LogInfo("Rehedge order {0} executed", o.TransactionId);
				else
					LogInfo("Rehedge order {0} canceled", o.TransactionId);

				_pendingOrders.Remove(order);
			})
			.Once()
			.Apply(this);

		// Register the order
		RegisterOrder(order);
		LogInfo("Registering rehedge order {0} {1} {2} @ {3}",
			order.Security.Id, order.Side, order.Volume, order.Price);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		// Dispose all quoting processors
		foreach (var processor in _quotingProcessors.Values)
		{
			processor.Dispose();
		}

		_quotingProcessors.Clear();
		_activeOrders.Clear();
		_pendingOrders.Clear();

		base.OnStopped();
	}
}