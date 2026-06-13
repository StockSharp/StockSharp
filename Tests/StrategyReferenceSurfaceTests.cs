namespace StockSharp.Tests;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Slippage;
using StockSharp.Algo.Strategies.Decomposed;

/// <summary>
/// Surface-completeness tests shared by BOTH strategy implementations: the same
/// combined deterministic backtest scenario, the same must-fire event list, the same
/// silence expectations and the same property/entity completeness checks are applied
/// first to the monolith <see cref="Strategy"/> (the reference - expected to pass) and
/// then to <see cref="DecomposedStrategy"/> (expected red until the migration closes
/// the gaps). A test fails whenever an expected event never fires or expected data
/// stays zero/null - "the event did not fire" and "the property stayed null" are
/// first-class failures here.
///
/// The combined scenario: SMA crossover trading on 1-minute candles, 0.2% stop
/// protection, raw tick/level1/depth subscriptions, one off-step order with connector
/// CheckSteps (register-fail path), a scripted resting order that is edited,
/// re-registered, canceled and then canceled AGAIN (cancel-fail path), a normal stop
/// after history end (cancel-on-stop) and a final Reset.
///
/// Events that the synchronous emulation environment structurally cannot drive are
/// asserted to stay SILENT (== 0) on both sides with the reason documented inline -
/// so if the environment ever starts delivering them, the test goes red and the
/// expectation must be consciously revisited.
///
/// Event-by-event 1:1 stream comparison between the two implementations (same order,
/// same payloads) lives in StrategyDecomposedFullEquivalenceTests; this suite pins
/// that each side delivers the surface AT ALL.
/// </summary>
[TestClass]
[DoNotParallelize] // full backtest per method: needs full CPU to stay within watchdogs
public class StrategyReferenceSurfaceTests : BaseTestClass
{
	private const int _longLen = 80;
	private const int _shortLen = 10;

	#region Shared expectations (identical for both implementations)

	/// <summary>
	/// Single source of truth for the compared event names, each bound via
	/// <see langword="nameof"/> to the real monolith member so a rename or removal
	/// breaks the build instead of silently passing. These names are the logical
	/// keys both counters bump and both expectation lists reference. The lone
	/// synthetic key (<see cref="PositionChangedTyped"/>) has no own member: the
	/// monolith exposes two position-changed events (the obsolete parameterless
	/// <see cref="Strategy.PositionChanged"/> and <see cref="IPositionProvider.PositionChanged"/>),
	/// and nameof on both would collide, so the typed one gets a distinct label.
	/// </summary>
	private static class Ev
	{
		public const string ProcessStateChanged = nameof(Strategy.ProcessStateChanged);
		public const string ConnectorChanged = nameof(Strategy.ConnectorChanged);
		public const string ParametersChanged = nameof(Strategy.ParametersChanged);
		public const string PropertyChanged = nameof(Strategy.PropertyChanged);
		public const string Reseted = nameof(Strategy.Reseted);
		public const string IsOnlineChanged = nameof(Strategy.IsOnlineChanged);
		public const string CurrentTimeChanged = nameof(ITimeProvider.CurrentTimeChanged);
		public const string Error = nameof(Strategy.Error);

		public const string NewOrder = nameof(ITransactionProvider.NewOrder);
		public const string OrderRegistering = nameof(Strategy.OrderRegistering);
		public const string OrderRegistered = nameof(Strategy.OrderRegistered);
		public const string OrderChanged = nameof(Strategy.OrderChanged);
		public const string OrderCanceling = nameof(Strategy.OrderCanceling);
		public const string OrderReRegistering = nameof(Strategy.OrderReRegistering);
		public const string OrderRegisterFailed = nameof(Strategy.OrderRegisterFailed);
		public const string OrderRegisterFailReceived = nameof(Strategy.OrderRegisterFailReceived);
		public const string OrderCancelFailed = nameof(Strategy.OrderCancelFailed);
		public const string OrderCancelFailReceived = nameof(Strategy.OrderCancelFailReceived);
		public const string OrderEdited = nameof(Strategy.OrderEdited);
		public const string OrderEditFailed = nameof(Strategy.OrderEditFailed);
		public const string OrderEditFailReceived = nameof(Strategy.OrderEditFailReceived);
		public const string OrderReceived = nameof(Strategy.OrderReceived);

		public const string NewMyTrade = nameof(Strategy.NewMyTrade);
		public const string OwnTradeReceived = nameof(Strategy.OwnTradeReceived);

		public const string PositionChanged = nameof(Strategy.PositionChanged);
		public const string PositionChangedTyped = "PositionChangedTyped";
		public const string NewPosition = nameof(IPositionProvider.NewPosition);
		public const string PositionReceived = nameof(Strategy.PositionReceived);
		public const string PortfolioReceived = nameof(Strategy.PortfolioReceived);

		public const string PnLChanged = nameof(Strategy.PnLChanged);
		public const string PnLReceived = nameof(Strategy.PnLReceived);
		public const string PnLReceived2 = nameof(Strategy.PnLReceived2);
		public const string CommissionChanged = nameof(Strategy.CommissionChanged);
		public const string SlippageChanged = nameof(Strategy.SlippageChanged);
		public const string LatencyChanged = nameof(Strategy.LatencyChanged);

		public const string MassOrderCanceled = nameof(ITransactionProvider.MassOrderCanceled);
		public const string MassOrderCanceled2 = nameof(ITransactionProvider.MassOrderCanceled2);
		public const string MassOrderCancelFailed = nameof(ITransactionProvider.MassOrderCancelFailed);
		public const string MassOrderCancelFailed2 = nameof(ITransactionProvider.MassOrderCancelFailed2);

		public const string CandleReceived = nameof(Strategy.CandleReceived);
		public const string TickTradeReceived = nameof(Strategy.TickTradeReceived);
		public const string Level1Received = nameof(Strategy.Level1Received);
		public const string OrderBookReceived = nameof(Strategy.OrderBookReceived);
		public const string OrderLogReceived = nameof(Strategy.OrderLogReceived);
		public const string SecurityReceived = nameof(Strategy.SecurityReceived);
		public const string BoardReceived = nameof(Strategy.BoardReceived);
		public const string NewsReceived = nameof(Strategy.NewsReceived);
		public const string DataTypeReceived = nameof(Strategy.DataTypeReceived);
		public const string SubscriptionReceived = nameof(Strategy.SubscriptionReceived);
		public const string SubscriptionStarted = nameof(Strategy.SubscriptionStarted);
		public const string SubscriptionOnline = nameof(Strategy.SubscriptionOnline);
		public const string SubscriptionStopped = nameof(Strategy.SubscriptionStopped);
		public const string SubscriptionFailed = nameof(Strategy.SubscriptionFailed);

		public const string OrderBookDrawing = nameof(Strategy.OrderBookDrawing);
		public const string OrderBookDrawingOrder = nameof(Strategy.OrderBookDrawingOrder);
		public const string OrderBookDrawingOrderFail = nameof(Strategy.OrderBookDrawingOrderFail);
	}

	/// <summary>
	/// Every event the combined scenario drives on a correct implementation. The
	/// names are the monolith surface names; the decomposed counter maps its own
	/// counterpart events onto the same names, and names without a counterpart
	/// simply never move - failing the decomposed run until the surface appears.
	/// </summary>
	private static readonly string[] _mustFire =
	[
		// Lifecycle and infra.
		Ev.ProcessStateChanged, Ev.ConnectorChanged, Ev.ParametersChanged, Ev.PropertyChanged,
		Ev.Reseted, Ev.IsOnlineChanged, Ev.CurrentTimeChanged,
		// Transactions: registration, change, cancel, re-register, edit.
		Ev.NewOrder, Ev.OrderRegistering, Ev.OrderRegistered, Ev.OrderChanged,
		Ev.OrderCanceling, Ev.OrderReRegistering,
		Ev.OrderRegisterFailed, Ev.OrderRegisterFailReceived,
		Ev.OrderCancelFailed, Ev.OrderCancelFailReceived,
		Ev.OrderEdited,
		Ev.NewMyTrade, Ev.OwnTradeReceived, Ev.OrderReceived,
		// Positions and money.
		Ev.PositionChanged, Ev.PositionChangedTyped, Ev.NewPosition, Ev.PositionReceived,
		Ev.PnLChanged, Ev.PnLReceived, Ev.PnLReceived2,
		Ev.CommissionChanged, Ev.SlippageChanged,
		// Market data relays.
		Ev.CandleReceived, Ev.TickTradeReceived, Ev.Level1Received, Ev.OrderBookReceived,
		// Subscription lifecycle.
		Ev.SubscriptionStarted, Ev.SubscriptionOnline, Ev.SubscriptionReceived,
	];

	/// <summary>
	/// Events the emulation environment structurally cannot (or a correct
	/// implementation must not) drive in this scenario.
	/// </summary>
	private static readonly (string name, string reason)[] _mustStaySilent =
	[
		(Ev.Error, "clean run: no strategy-level error sources are triggered (register/cancel fails must not raise Error)"),
		(Ev.LatencyChanged, "synchronous emulation pipeline has structurally zero latency, both latency sources stay null"),
		(Ev.OrderEditFailed, "a correct edit must SUCCEED (OrderEdited is asserted in the must-fire list); this firing means the edit path is broken"),
		(Ev.OrderEditFailReceived, "same contract as OrderEditFailed: a correct edit must succeed"),
		(Ev.SubscriptionFailed, "no failing subscription in the scenario"),
		(Ev.DataTypeReceived, "no data-type lookup subscription in the scenario"),
		(Ev.PortfolioReceived, "the emulator reports account money as Money-security position changes (PositionReceived), never as portfolio messages"),
		(Ev.MassOrderCanceled, "orders are canceled one by one; no mass-cancel API is exercised"),
		(Ev.MassOrderCanceled2, "same as MassOrderCanceled"),
		(Ev.MassOrderCancelFailed, "same as MassOrderCanceled"),
		(Ev.MassOrderCancelFailed2, "same as MassOrderCanceled"),
		(Ev.OrderLogReceived, "no order-log subscription in the scenario"),
		(Ev.NewsReceived, "no news subscription and no news data in the history package"),
		(Ev.SecurityReceived, "no security-lookup subscription in the scenario"),
		(Ev.BoardReceived, "no board-lookup subscription in the scenario"),
		(Ev.OrderBookDrawing, "presentation surface: no chart is attached"),
		(Ev.OrderBookDrawingOrder, "presentation surface: no chart is attached"),
		(Ev.OrderBookDrawingOrderFail, "presentation surface: no chart is attached"),
	];

	private static void AssertAllEventsFire(EventCounter counter, string side)
	{
		var failures = _mustFire
			.Where(name => counter.Count(name) == 0)
			.Select(name => $"Event '{name}' never fired - {side} does not deliver it in this scenario")
			.ToList();

		if (failures.Count > 0)
		{
			Fail(
				$"{side} surface incomplete: {failures.Count} expected event(s) never fired.{Environment.NewLine}" +
				failures.JoinNL() + Environment.NewLine +
				$"All counts: {counter.Dump()}");
		}
	}

	private static void AssertSilentEventsStaySilent(EventCounter counter, string side)
	{
		var failures = _mustStaySilent
			.Where(e => counter.Count(e.name) > 0)
			.Select(e => $"Event '{e.name}' fired {counter.Count(e.name)} time(s) on {side} but was expected silent ({e.reason})")
			.ToList();

		if (failures.Count > 0)
		{
			Fail(
				$"Driveability expectations violated on {side}: {failures.Count} event(s) fired unexpectedly.{Environment.NewLine}" +
				failures.JoinNL() + Environment.NewLine +
				$"All counts: {counter.Dump()}");
		}
	}

	private static void AssertPropertiesCarryData(
		string side, ProcessStates state, decimal maxAbsPosition, bool hasOrders, bool hasTrades,
		decimal pnl, decimal? commission, decimal? slippage, TimeSpan? latency, bool hasPositions, bool hasStatistics)
	{
		var failures = new List<string>();

		void Check(bool ok, string what)
		{
			if (!ok)
				failures.Add(what);
		}

		Check(state == ProcessStates.Stopped, $"ProcessState={state}, expected Stopped");
		Check(maxAbsPosition > 0, "Position never left zero - the strategy never actually held a position");
		Check(hasOrders, "Orders collection is empty");
		Check(hasTrades, "Trades collection is empty");
		Check(pnl != 0, "PnL is exactly zero - PnL accounting never produced a value");
		Check(commission is > 0, $"Commission={Fmt(commission)} - the per-trade commission rule never accumulated");
		Check(slippage is not null, "Slippage is null - the slippage manager never accumulated");
		Check(latency is null, "Latency is not null - synchronous emulation was expected to produce no latency");
		Check(hasPositions, "Positions collection is empty");
		Check(hasStatistics, "no statistic parameter carries a value");

		if (failures.Count > 0)
		{
			Fail(
				$"{side} properties carry no data: {failures.Count} violation(s).{Environment.NewLine}" +
				failures.JoinNL());
		}
	}

	private static void AssertEntitiesFullyPopulated(string side, Order[] orders, MyTrade[] trades)
	{
		IsTrue(orders.Length >= 10, $"{side} produced only {orders.Length} orders - scenario too thin");
		IsTrue(trades.Length >= 4, $"{side} produced only {trades.Length} trades - scenario too thin");

		var failures = new List<string>();

		void Check(bool ok, string what)
		{
			if (!ok)
				failures.Add(what);
		}

		for (var i = 0; i < orders.Length; i++)
		{
			var o = orders[i];

			Check(o.TransactionId != 0, $"order[{i}]: TransactionId is 0");
			Check(o.Time != default, $"order[{i}]: Time is default");
			Check(o.ServerTime != default, $"order[{i}]: ServerTime is default");
			Check(o.State is OrderStates.Done or OrderStates.Failed, $"order[{i}]: State={o.State} is not final after stop");
			Check(o.Price > 0, $"order[{i}]: Price={o.Price} is not positive");
			Check(o.Volume > 0, $"order[{i}]: Volume={o.Volume} is not positive");
			Check(o.Security is not null, $"order[{i}]: Security is null");
			Check(o.Portfolio is not null, $"order[{i}]: Portfolio is null");
			Check(!o.UserOrderId.IsEmpty(), $"order[{i}]: UserOrderId is empty (ownership stamp missing)");
			Check(!o.StrategyId.IsEmpty(), $"order[{i}]: StrategyId is empty (ownership stamp missing)");
		}

		for (var i = 0; i < trades.Length; i++)
		{
			var t = trades[i];

			Check(t.Order is not null, $"trade[{i}]: Order is null");
			Check(t.Trade is not null, $"trade[{i}]: Trade is null");
			Check(t.Trade.Price > 0, $"trade[{i}]: price={t.Trade.Price} is not positive");
			Check(t.Trade.Volume > 0, $"trade[{i}]: volume={t.Trade.Volume} is not positive");
			Check(t.Trade.ServerTime != default, $"trade[{i}]: ServerTime is default");
			Check(t.Position is not null, $"trade[{i}]: Position is null");
			Check(t.Commission is not null, $"trade[{i}]: Commission is null (commission rule must stamp every trade)");
		}

		Check(trades.Any(t => t.PnL is not null), "no trade carries PnL - realized PnL never attributed to a trade");
		Check(trades.Any(t => t.Slippage is not null), "no trade carries Slippage - slippage manager never stamped a trade");

		if (failures.Count > 0)
		{
			Fail(
				$"{side} entities are not fully populated: {failures.Count} violation(s).{Environment.NewLine}" +
				failures.JoinNL());
		}
	}

	private static string Fmt(decimal? value)
		=> value?.ToString(CultureInfo.InvariantCulture) ?? "null";

	#endregion

	#region Scenario strategies

	private sealed class ReferenceSmaStrategy : Strategy
	{
		private readonly SimpleMovingAverage _longSma = new() { Length = _longLen };
		private readonly SimpleMovingAverage _shortSma = new() { Length = _shortLen };
		private bool? _isShortLessThenLong;
		private int _candleCount;
		private bool _invalidOrderSent;
		private Order _resting;
		private Order _canceledOnce;

		public decimal MaxAbsPosition { get; private set; }

		protected override void OnStarted2(DateTime time)
		{
			base.OnStarted2(time);

			StartProtection(new Unit(), new Unit(0.2m, UnitTypes.Percent));

			SubscribeCandles(new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), Security) { MarketData = { IsFinishedOnly = true } })
				.Bind(_longSma, _shortSma, OnProcess)
				.Start();

			Subscribe(new(DataType.Ticks, Security));
			Subscribe(new(DataType.Level1, Security));
			Subscribe(new(DataType.MarketDepth, Security));
		}

		private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			_candleCount++;

			ProcessScript(candle);
			ProcessCrossover(candle, longValue, shortValue);

			MaxAbsPosition = MaxAbsPosition.Max(Position.Abs());
		}

		private void ProcessScript(ICandleMessage candle)
			=> RunScript(_candleCount, candle, Security, Portfolio,
				price => _resting = BuyLimit(price, 1m),
				changes => EditOrder(_resting, changes),
				replacement => { ReRegisterOrder(_resting, replacement); _resting = replacement; },
				() => { CancelOrder(_resting); _canceledOnce = _resting; },
				() => CancelOrder(_canceledOnce));

		private void ProcessCrossover(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			var signal = Crossover(ref _isShortLessThenLong, candle, longValue, shortValue, Security.PriceStep ?? 1);

			if (signal is not (Sides direction, decimal price))
				return;

			var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

			if (!_invalidOrderSent)
			{
				_invalidOrderSent = true;

				// Half a price step off the grid: rejected by connector CheckSteps,
				// driving the register-fail surface.
				RegisterOrder(new Order
				{
					Security = Security,
					Portfolio = Portfolio,
					Side = direction,
					Price = price + 0.005m,
					Volume = volume,
				});
			}

			if (direction == Sides.Buy)
				BuyLimit(price, volume);
			else
				SellLimit(price, volume);
		}
	}

	private sealed class DecomposedReferenceSmaStrategy : DecomposedStrategy
	{
		private readonly SimpleMovingAverage _longSma = new() { Length = _longLen };
		private readonly SimpleMovingAverage _shortSma = new() { Length = _shortLen };
		private bool? _isShortLessThenLong;
		private int _candleCount;
		private bool _invalidOrderSent;
		private Order _resting;
		private Order _canceledOnce;
		private Subscription _candleSub;
		private bool _isFormed;

		public decimal MaxAbsPosition { get; private set; }
		public override bool IsFormed => _isFormed;

		public event Action OrderRegisterFailedHook;
		public event Action CandleProcessed;

		protected override void OnStateChanged(ProcessStates state)
		{
			base.OnStateChanged(state);

			switch (state)
			{
				case ProcessStates.Started:
				{
					StartProtection(new Unit(), new Unit(0.2m, UnitTypes.Percent));

					Subscriptions.Subscribe(new(DataType.PositionChanges), true);
					Subscriptions.Subscribe(new(DataType.Transactions), true);

					_candleSub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), Security) { MarketData = { IsFinishedOnly = true } };

					Connector.CandleReceived += OnCandle;

					Subscriptions.Subscribe(_candleSub);
					Subscriptions.Subscribe(new(DataType.Ticks, Security));
					Subscriptions.Subscribe(new(DataType.Level1, Security));
					Subscriptions.Subscribe(new(DataType.MarketDepth, Security));
					break;
				}

				case ProcessStates.Stopping:
				{
					if (Connector is not null)
						Connector.CandleReceived -= OnCandle;
					break;
				}
			}
		}

		protected override void OnOrderRegisterFailed(OrderFail fail)
		{
			base.OnOrderRegisterFailed(fail);
			OrderRegisterFailedHook?.Invoke();
		}

		private void OnCandle(Subscription sub, ICandleMessage candle)
		{
			if (sub != _candleSub)
				return;

			CandleProcessed?.Invoke();
			_candleCount++;

			RunScript(_candleCount, candle, Security, Portfolio,
				price => _resting = BuyLimit(price, 1m),
				changes => EditOrder(_resting, changes),
				replacement => { ReRegisterOrder(_resting, replacement); _resting = replacement; },
				() => { CancelOrder(_resting); _canceledOnce = _resting; },
				() => CancelOrder(_canceledOnce));

			var longValue = _longSma.Process(candle);
			var shortValue = _shortSma.Process(candle);

			if (!_isFormed && _longSma.IsFormed && _shortSma.IsFormed)
			{
				_isFormed = true;
				this.Notify(nameof(IsFormed));
			}

			if (longValue.IsEmpty || shortValue.IsEmpty)
				return;

			var signal = Crossover(ref _isShortLessThenLong, candle, longValue.ToDecimal(), shortValue.ToDecimal(), Security.PriceStep ?? 1);

			if (signal is (Sides direction, decimal price))
			{
				var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

				if (!_invalidOrderSent)
				{
					_invalidOrderSent = true;
					RegisterOrder(CreateOrder(direction, price + 0.005m, volume));
				}

				if (direction == Sides.Buy)
					BuyLimit(price, volume);
				else
					SellLimit(price, volume);
			}

			MaxAbsPosition = MaxAbsPosition.Max(Position.Abs());
		}
	}

	/// <summary>
	/// Shared crossover decision used by both variants (single source of truth).
	/// </summary>
	private static (Sides side, decimal price)? Crossover(ref bool? isShortLessThenLong, ICandleMessage candle, decimal longValue, decimal shortValue, decimal priceStep)
	{
		if (candle.State != CandleStates.Finished)
			return null;

		var current = shortValue < longValue;

		if (isShortLessThenLong == null)
		{
			isShortLessThenLong = current;
			return null;
		}

		if (isShortLessThenLong == current)
			return null;

		isShortLessThenLong = current;

		var direction = current ? Sides.Sell : Sides.Buy;
		return (direction, candle.ClosePrice + (direction == Sides.Buy ? priceStep : -priceStep));
	}

	/// <summary>
	/// Shared order-maintenance script keyed by candle index: place a far resting
	/// order, edit it, re-register it, cancel it, then cancel it AGAIN to force a
	/// cancel failure. Both variants run the identical steps.
	/// </summary>
	private static void RunScript(
		int candleIndex, ICandleMessage candle, Security security, Portfolio portfolio,
		Action<decimal> buyLimit, Action<Order> edit, Action<Order> reRegister, Action cancel, Action cancelAgain)
	{
		switch (candleIndex)
		{
			case 100:
				buyLimit(candle.ClosePrice - 5000);
				break;

			case 105:
				edit(new Order
				{
					Security = security,
					Portfolio = portfolio,
					Side = Sides.Buy,
					Price = candle.ClosePrice - 4000,
					Volume = 1m,
				});
				break;

			case 110:
				reRegister(new Order
				{
					Security = security,
					Portfolio = portfolio,
					Side = Sides.Buy,
					Price = candle.ClosePrice - 3000,
					Volume = 1m,
				});
				break;

			case 115:
				cancel();
				break;

			case 120:
				cancelAgain();
				break;
		}
	}

	#endregion

	#region Event counting

	private sealed class EventCounter
	{
		private readonly Dictionary<string, int> _counts = [];

		public void Bump(string name)
		{
			_counts.TryGetValue(name, out var current);
			_counts[name] = current + 1;
		}

		public int Count(string name)
			=> _counts.TryGetValue(name, out var count) ? count : 0;

		public string Dump()
			=> _counts.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}").JoinCommaSpace();
	}

	private static EventCounter CountMonolithEvents(ReferenceSmaStrategy strategy)
	{
		var counter = new EventCounter();

		// Core lifecycle and infra.
		strategy.ProcessStateChanged += _ => counter.Bump(Ev.ProcessStateChanged);
		strategy.ConnectorChanged += () => counter.Bump(Ev.ConnectorChanged);
		strategy.ParametersChanged += () => counter.Bump(Ev.ParametersChanged);
		strategy.PropertyChanged += (_, _) => counter.Bump(Ev.PropertyChanged);
		strategy.Reseted += () => counter.Bump(Ev.Reseted);
		strategy.IsOnlineChanged += _ => counter.Bump(Ev.IsOnlineChanged);
		strategy.Error += (_, _) => counter.Bump(Ev.Error);
		((ITimeProvider)strategy).CurrentTimeChanged += _ => counter.Bump(Ev.CurrentTimeChanged);

		// Transactions.
		strategy.OrderRegistering += _ => counter.Bump(Ev.OrderRegistering);
		strategy.OrderCanceling += _ => counter.Bump(Ev.OrderCanceling);
		strategy.OrderReRegistering += (_, _) => counter.Bump(Ev.OrderReRegistering);

#pragma warning disable CS0618 // obsolete events are still public strategy surface under test
		strategy.OrderRegistered += _ => counter.Bump(Ev.OrderRegistered);
		strategy.OrderChanged += _ => counter.Bump(Ev.OrderChanged);
		strategy.OrderRegisterFailed += _ => counter.Bump(Ev.OrderRegisterFailed);
		strategy.OrderCancelFailed += _ => counter.Bump(Ev.OrderCancelFailed);
		strategy.OrderEdited += (_, _) => counter.Bump(Ev.OrderEdited);
		strategy.OrderEditFailed += (_, _) => counter.Bump(Ev.OrderEditFailed);
		strategy.NewMyTrade += _ => counter.Bump(Ev.NewMyTrade);
		strategy.PositionChanged += () => counter.Bump(Ev.PositionChanged);
		strategy.PnLReceived += _ => counter.Bump(Ev.PnLReceived);
#pragma warning restore CS0618

		var transactions = (ITransactionProvider)strategy;
		transactions.NewOrder += _ => counter.Bump(Ev.NewOrder);
		transactions.MassOrderCanceled += _ => counter.Bump(Ev.MassOrderCanceled);
		transactions.MassOrderCanceled2 += (_, _) => counter.Bump(Ev.MassOrderCanceled2);
		transactions.MassOrderCancelFailed += (_, _) => counter.Bump(Ev.MassOrderCancelFailed);
		transactions.MassOrderCancelFailed2 += (_, _, _) => counter.Bump(Ev.MassOrderCancelFailed2);

		// Positions and money.
		var positions = (IPositionProvider)strategy;
		positions.NewPosition += _ => counter.Bump(Ev.NewPosition);
		positions.PositionChanged += _ => counter.Bump(Ev.PositionChangedTyped);

		strategy.PnLChanged += () => counter.Bump(Ev.PnLChanged);
		strategy.PnLReceived2 += (_, _, _, _, _, _) => counter.Bump(Ev.PnLReceived2);
		strategy.CommissionChanged += () => counter.Bump(Ev.CommissionChanged);
		strategy.SlippageChanged += () => counter.Bump(Ev.SlippageChanged);
		strategy.LatencyChanged += () => counter.Bump(Ev.LatencyChanged);

		// Subscription-scoped relays.
		strategy.CandleReceived += (_, _) => counter.Bump(Ev.CandleReceived);
		strategy.TickTradeReceived += (_, _) => counter.Bump(Ev.TickTradeReceived);
		strategy.Level1Received += (_, _) => counter.Bump(Ev.Level1Received);
		strategy.OrderBookReceived += (_, _) => counter.Bump(Ev.OrderBookReceived);
		strategy.OrderLogReceived += (_, _) => counter.Bump(Ev.OrderLogReceived);
		strategy.SecurityReceived += (_, _) => counter.Bump(Ev.SecurityReceived);
		strategy.BoardReceived += (_, _) => counter.Bump(Ev.BoardReceived);
		strategy.NewsReceived += (_, _) => counter.Bump(Ev.NewsReceived);
		strategy.DataTypeReceived += (_, _) => counter.Bump(Ev.DataTypeReceived);
		strategy.SubscriptionReceived += (_, _) => counter.Bump(Ev.SubscriptionReceived);
		strategy.OwnTradeReceived += (_, _) => counter.Bump(Ev.OwnTradeReceived);
		strategy.OrderReceived += (_, _) => counter.Bump(Ev.OrderReceived);
		strategy.OrderRegisterFailReceived += (_, _) => counter.Bump(Ev.OrderRegisterFailReceived);
		strategy.OrderCancelFailReceived += (_, _) => counter.Bump(Ev.OrderCancelFailReceived);
		strategy.OrderEditFailReceived += (_, _) => counter.Bump(Ev.OrderEditFailReceived);
		strategy.PortfolioReceived += (_, _) => counter.Bump(Ev.PortfolioReceived);
		strategy.PositionReceived += (_, _) => counter.Bump(Ev.PositionReceived);
		strategy.SubscriptionStarted += _ => counter.Bump(Ev.SubscriptionStarted);
		strategy.SubscriptionOnline += _ => counter.Bump(Ev.SubscriptionOnline);
		strategy.SubscriptionStopped += (_, _) => counter.Bump(Ev.SubscriptionStopped);
		strategy.SubscriptionFailed += (_, _, _) => counter.Bump(Ev.SubscriptionFailed);

		// Presentation surface.
		strategy.OrderBookDrawing += (_, _, _) => counter.Bump(Ev.OrderBookDrawing);
		strategy.OrderBookDrawingOrder += (_, _, _) => counter.Bump(Ev.OrderBookDrawingOrder);
		strategy.OrderBookDrawingOrderFail += (_, _, _) => counter.Bump(Ev.OrderBookDrawingOrderFail);

		return counter;
	}

	/// <summary>
	/// Maps the decomposed implementation's counterpart events onto the SAME logical
	/// names as the monolith counter, so the identical must-fire expectations apply.
	/// Names without a decomposed counterpart never move and stay red in the
	/// must-fire assertion until the decomposed side gains the surface.
	/// </summary>
	private static EventCounter CountDecomposedEvents(DecomposedReferenceSmaStrategy strategy)
	{
		var counter = new EventCounter();

		strategy.Engine.StateChanged += _ => counter.Bump(Ev.ProcessStateChanged);
		strategy.ConnectorChanged += () => counter.Bump(Ev.ConnectorChanged);
		strategy.ParametersChanged += () => counter.Bump(Ev.ParametersChanged);
		strategy.PropertyChanged += (_, _) => counter.Bump(Ev.PropertyChanged);
		strategy.Reseted += () => counter.Bump(Ev.Reseted);
		strategy.IsOnlineChanged += _ => counter.Bump(Ev.IsOnlineChanged);
		strategy.Error += _ => counter.Bump(Ev.Error);
		((ITimeProvider)strategy).CurrentTimeChanged += _ => counter.Bump(Ev.CurrentTimeChanged);

		strategy.OrderRegistering += _ => counter.Bump(Ev.OrderRegistering);
		strategy.OrderCanceling += _ => counter.Bump(Ev.OrderCanceling);
		strategy.OrderReRegistering += (_, _) => counter.Bump(Ev.OrderReRegistering);
		strategy.OrderCancelFailed += _ => counter.Bump(Ev.OrderCancelFailed);
		strategy.OrderEdited += (_, _) => counter.Bump(Ev.OrderEdited);
		strategy.OrderEditFailed += (_, _) => counter.Bump(Ev.OrderEditFailed);
		strategy.Orders.NewOrder += _ => counter.Bump(Ev.NewOrder);
		strategy.Orders.Registered += _ => counter.Bump(Ev.OrderRegistered);
		strategy.Orders.Changed += _ => counter.Bump(Ev.OrderChanged);
		strategy.Trades.TradeAdded += _ => counter.Bump(Ev.NewMyTrade);
		strategy.PnLChanged += () => counter.Bump(Ev.PnLChanged);
		strategy.PnLReceived += _ => counter.Bump(Ev.PnLReceived);
		strategy.PnLReceived2 += (_, _, _, _, _, _) => counter.Bump(Ev.PnLReceived2);
		strategy.CommissionChanged += () => counter.Bump(Ev.CommissionChanged);
		strategy.SlippageChanged += () => counter.Bump(Ev.SlippageChanged);
		strategy.LatencyChanged += () => counter.Bump(Ev.LatencyChanged);

		var positions = (IPositionProvider)strategy;
		positions.NewPosition += _ => counter.Bump(Ev.NewPosition);
		positions.PositionChanged += _ => counter.Bump(Ev.PositionChangedTyped);
		strategy.PositionChanged += () => counter.Bump(Ev.PositionChanged);
		strategy.PositionReceived += (_, _) => counter.Bump(Ev.PositionReceived);
		strategy.OwnTradeReceived += (_, _) => counter.Bump(Ev.OwnTradeReceived);
		strategy.OrderReceived += (_, _) => counter.Bump(Ev.OrderReceived);
		strategy.OrderCancelFailReceived += (_, _) => counter.Bump(Ev.OrderCancelFailReceived);
		strategy.OrderEditFailReceived += (_, _) => counter.Bump(Ev.OrderEditFailReceived);
		strategy.CandleReceived += (_, _) => counter.Bump(Ev.CandleReceived);
		strategy.TickTradeReceived += (_, _) => counter.Bump(Ev.TickTradeReceived);
		strategy.Level1Received += (_, _) => counter.Bump(Ev.Level1Received);
		strategy.OrderBookReceived += (_, _) => counter.Bump(Ev.OrderBookReceived);
		strategy.OrderLogReceived += (_, _) => counter.Bump(Ev.OrderLogReceived);
		strategy.SecurityReceived += (_, _) => counter.Bump(Ev.SecurityReceived);
		strategy.BoardReceived += (_, _) => counter.Bump(Ev.BoardReceived);
		strategy.NewsReceived += (_, _) => counter.Bump(Ev.NewsReceived);
		strategy.DataTypeReceived += (_, _) => counter.Bump(Ev.DataTypeReceived);
		strategy.SubscriptionReceived += (_, _) => counter.Bump(Ev.SubscriptionReceived);
		strategy.SubscriptionStarted += _ => counter.Bump(Ev.SubscriptionStarted);
		strategy.SubscriptionOnline += _ => counter.Bump(Ev.SubscriptionOnline);
		strategy.SubscriptionStopped += (_, _) => counter.Bump(Ev.SubscriptionStopped);
		strategy.SubscriptionFailed += (_, _, _) => counter.Bump(Ev.SubscriptionFailed);
		strategy.PortfolioReceived += (_, _) => counter.Bump(Ev.PortfolioReceived);

		// The single register-fail hook is the decomposed side's entire
		// register-fail surface: it stands in for both monolith relays.
		strategy.OrderRegisterFailedHook += () =>
		{
			counter.Bump(Ev.OrderRegisterFailed);
			counter.Bump(Ev.OrderRegisterFailReceived);
		};

		// Everything else from the shared must-fire list (presentation drawing)
		// has no decomposed counterpart.
		return counter;
	}

	#endregion

	#region Backtest runs

	private HistoryEmulationConnector CreateScenarioConnector(Security security, Portfolio portfolio)
	{
		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

		var historyAdapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			new HistoryMarketDataManager(new TradingTimeLineGenerator())
			{
				StorageRegistry = storageRegistry
			})
		{
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddDays(2),
		};

		var connector = new HistoryEmulationConnector(
			historyAdapter, true,
			new PassThroughMessageChannel(),
			secProvider, pfProvider,
			storageRegistry.ExchangeInfoProvider);

		connector.EmulationAdapter.Settings.MatchOnTouch = true;
		connector.CheckSteps = true;

		var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
		emulator.RandomProvider = new DefaultRandomProvider(42);
		connector.EmulationAdapter.Settings.InitialOrderId = 100;
		connector.EmulationAdapter.Settings.InitialTradeId = 100;
		connector.Adapter.LatencyManager = null;
		connector.Adapter.ApplyHeartbeat(connector.EmulationAdapter, false);

		var commissionManager = new CommissionManager();
		commissionManager.Rules.Add(new CommissionTradeRule { Value = new Unit(1) });
		connector.Adapter.CommissionManager = commissionManager;
		connector.Adapter.SlippageManager = new SlippageManager(new SlippageManagerState());

		return connector;
	}

	private void EnsureHistoryData()
	{
		if (Paths.HistoryDataPath == null)
		{
			// Not a silent pass: without market data nothing below is meaningful.
			Inconclusive("HistoryDataPath is null (stocksharp.samples.historydata not installed) - the surface cannot be verified");
		}
	}

	private async Task AwaitBacktest(HistoryEmulationConnector connector)
	{
		var stoppedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		connector.Connect();
		await connector.StartAsync(CancellationToken);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}
	}

	private async Task<(ReferenceSmaStrategy strategy, EventCounter counter)> RunMonolith()
	{
		EnsureHistoryData();

		var security = new Security { Id = Paths.HistoryDefaultSecurity, PriceStep = 0.01m };
		var portfolio = Portfolio.CreateSimulator();

		using var connector = CreateScenarioConnector(security, portfolio);

		var strategy = new ReferenceSmaStrategy();

		// Attach the counter BEFORE any configuration so setup-time events
		// (ConnectorChanged, ParametersChanged, PropertyChanged) are captured too.
		var counter = CountMonolithEvents(strategy);

		strategy.Security = security;
		strategy.Portfolio = portfolio;
		strategy.Volume = 1;
		strategy.Connector = connector;
		strategy.WaitRulesOnStop = false;

		strategy.Start();
		await AwaitBacktest(connector);
		strategy.Stop();

		return (strategy, counter);
	}

	private async Task<(DecomposedReferenceSmaStrategy strategy, EventCounter counter)> RunDecomposed()
	{
		EnsureHistoryData();

		var security = new Security { Id = Paths.HistoryDefaultSecurity, PriceStep = 0.01m };
		var portfolio = Portfolio.CreateSimulator();

		using var connector = CreateScenarioConnector(security, portfolio);

		var strategy = new DecomposedReferenceSmaStrategy();

		var counter = CountDecomposedEvents(strategy);

		strategy.Security = security;
		strategy.Portfolio = portfolio;
		strategy.Volume = 1;
		strategy.Engine.UnrealizedPnLInterval = TimeSpan.FromMinutes(1);
		strategy.Connector = connector;

		await strategy.StartAsync(CancellationToken);
		await AwaitBacktest(connector);
		await strategy.StopAsync(CancellationToken);

		return (strategy, counter);
	}

	#endregion

	#region Monolith (reference) - expected to pass

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task ReferenceSurface_AllEventsFire()
	{
		var (strategy, counter) = await RunMonolith();

		// Drive the reset surface as the final lifecycle step.
		strategy.Reset();

		AssertAllEventsFire(counter, "monolith");
	}

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task ReferenceSurface_UndrivableEventsStaySilent()
	{
		var (_, counter) = await RunMonolith();

		AssertSilentEventsStaySilent(counter, "monolith");
	}

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task ReferenceSurface_PropertiesCarryData()
	{
		var (strategy, _) = await RunMonolith();

		AssertPropertiesCarryData(
			"monolith", strategy.ProcessState, strategy.MaxAbsPosition,
			strategy.Orders.Any(), strategy.MyTrades.Any(),
			strategy.PnL, strategy.Commission, strategy.Slippage, strategy.Latency,
			strategy.Positions.Any(), strategy.StatisticManager.Parameters.Any(p => p.Value is not null));
	}

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task ReferenceSurface_EntitiesFullyPopulated()
	{
		var (strategy, _) = await RunMonolith();

		AssertEntitiesFullyPopulated("monolith", [.. strategy.Orders], [.. strategy.MyTrades]);
	}

	#endregion

	#region Decomposed - the SAME checks, red until the migration closes the gaps

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task DecomposedSurface_AllEventsFire()
	{
		var (strategy, counter) = await RunDecomposed();

		strategy.Reset();

		AssertAllEventsFire(counter, "decomposed");
	}

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task DecomposedSurface_UndrivableEventsStaySilent()
	{
		var (_, counter) = await RunDecomposed();

		AssertSilentEventsStaySilent(counter, "decomposed");
	}

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task DecomposedSurface_PropertiesCarryData()
	{
		var (strategy, _) = await RunDecomposed();

		AssertPropertiesCarryData(
			"decomposed", strategy.ProcessState, strategy.MaxAbsPosition,
			strategy.Orders.Orders.Any(), strategy.Trades.MyTrades.Any(),
			strategy.PnLManager.GetPnL(), strategy.Commission, strategy.Slippage, strategy.Latency,
			strategy.PositionsList.Any(), strategy.StatisticManager.Parameters.Any(p => p.Value is not null));
	}

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public async Task DecomposedSurface_EntitiesFullyPopulated()
	{
		var (strategy, _) = await RunDecomposed();

		AssertEntitiesFullyPopulated("decomposed", [.. strategy.Orders.Orders], [.. strategy.Trades.MyTrades]);
	}

	#endregion
}
