namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	// ISubscriptionProvider surface.
	//
	// Most of this subsystem is already satisfied by the decomposed Strategy:
	//  - the value/lifecycle events (SubscriptionReceived, Level1Received, OrderBookReceived,
	//    TickTradeReceived, OrderLogReceived, SecurityReceived, BoardReceived, NewsReceived,
	//    CandleReceived, OwnTradeReceived, OrderReceived, OrderRegisterFailReceived,
	//    OrderCancelFailReceived, OrderEditFailReceived, PortfolioReceived, PositionReceived,
	//    DataTypeReceived, SubscriptionOnline, SubscriptionStarted, SubscriptionStopped,
	//    SubscriptionFailed) are declared and fan-out from the connector handlers in Strategy.cs;
	//  - PortfolioLookup is declared in the IPositionProvider region of Strategy.cs;
	//  - subscription tracking (add / remove / suspend / resume / by-id) lives in SubscriptionRegistry,
	//    exposed via the Subscriptions property and wired to the connector in the Strategy constructor.
	//
	// This file adds only the members that the decomposed Strategy does not yet expose:
	//  - the public Subscribe/UnSubscribe entry points (with the monolith's live-trading history pre-roll),
	//  - the SecurityLookup/BoardLookup/OrderLookup/DataTypeLookup global lookup accessors,
	//  - the explicit ISubscriptionProvider.Subscriptions projection (the public Subscriptions property is
	//    the SubscriptionRegistry, not the IEnumerable<Subscription> the interface declares).

	private Subscription _orderLookup;

	/// <summary>
	/// Build a global lookup subscription stamped with this strategy's identifier.
	/// </summary>
	/// <typeparam name="TLookupMessage">Lookup message type.</typeparam>
	/// <returns><see cref="Subscription"/>.</returns>
	private Subscription ToSubscription<TLookupMessage>()
		where TLookupMessage : IStrategyIdMessage, ISubscriptionMessage, new()
		=> new(new TLookupMessage
		{
			StrategyId = EnsureGetId(),
		});

	/// <inheritdoc />
	public Subscription OrderLookup => _orderLookup ??= ToSubscription<OrderStatusMessage>();

	// The monolith never assigns these — they are always null (the interface documents them as optional).
	// Reproduced as explicit-interface auto-properties so consumers see the same (null) values.
	Subscription ISubscriptionProvider.SecurityLookup { get; }
	Subscription ISubscriptionProvider.BoardLookup { get; }
	Subscription ISubscriptionProvider.DataTypeLookup { get; }

	// The public Subscriptions property is the SubscriptionRegistry; ISubscriptionProvider.Subscriptions
	// is the flat enumerable of tracked subscriptions, served straight from the registry.
	IEnumerable<Subscription> ISubscriptionProvider.Subscriptions => Subscriptions.Subscriptions;

	/// <summary>
	/// Optional history pre-roll applied to market-data subscriptions during live trading, so the strategy
	/// is warmed up with the configured amount of history before "now". Returns <see cref="TimeSpan.Zero"/>
	/// by default; subsystems that own a history setting (e.g. HistorySize / HistoryCalculated) and a
	/// backtesting flag override the wiring points below to feed the monolith's behaviour.
	/// </summary>
	/// <returns>History span to subtract from <see cref="IStrategyHost.CurrentTime"/>, or zero to disable.</returns>
	private TimeSpan GetSubscribeHistoryPreroll()
	{
		// Skip the pre-roll while backtesting: the emulation connector already replays history.
		if (IsBacktestingForSubscribe)
			return TimeSpan.Zero;

		var history = HistorySizeForSubscribe ?? TimeSpan.Zero;
		var calculated = HistoryCalculatedForSubscribe;

		if (calculated is TimeSpan calc && history < calc)
			history = calc;

		return history > TimeSpan.Zero ? history : TimeSpan.Zero;
	}

	// Wiring points for the history pre-roll, fed from the ported settings (HistorySize / HistoryCalculated)
	// and the backtesting flag, so Subscribe keeps the exact monolith warm-up shape.

	/// <summary>
	/// Whether the strategy currently runs in backtesting (history emulation) mode.
	/// </summary>
	protected virtual bool IsBacktestingForSubscribe => IsBacktesting;

	/// <summary>
	/// Configured live-trading warm-up history span, if any.
	/// </summary>
	protected virtual TimeSpan? HistorySizeForSubscribe => HistorySize;

	/// <summary>
	/// Code-calculated warm-up history span, if any.
	/// </summary>
	protected virtual TimeSpan? HistoryCalculatedForSubscribe => HistoryCalculated;

	/// <inheritdoc />
	public void Subscribe(Subscription subscription)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		var history = GetSubscribeHistoryPreroll();

		if (history > TimeSpan.Zero && subscription.From is null)
		{
			var dataType = subscription.DataType;

			if (dataType.IsMarketData && dataType.IsSecurityRequired)
				subscription.From = ((IStrategyHost)this).CurrentTime - history;
		}

		Subscriptions.Subscribe(subscription, isGlobal: false);
	}

	/// <inheritdoc />
	public void UnSubscribe(Subscription subscription)
	{
		ArgumentNullException.ThrowIfNull(subscription);

		Subscriptions.UnSubscribe(subscription);
	}
}