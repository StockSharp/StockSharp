namespace StockSharp.Tests;

using System.Text;
using System.Text.RegularExpressions;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Slippage;
using StockSharp.Algo.Strategies.Decomposed;

/// <summary>
/// Strict full-stream equivalence tests between the monolith <see cref="Strategy"/>
/// and <see cref="DecomposedStrategy"/>.
///
/// Both implementations run the SAME shared SMA-crossover logic (one class, not two
/// hand-synchronized copies) inside a REAL deterministic <see cref="HistoryEmulationConnector"/>
/// backtest, each on its own identically configured connector. Every externally observable
/// event of each side is recorded into a normalized ordered stream and the two streams are
/// compared 1:1 (same order, same payloads) - first per event family for diagnosability,
/// then as a single interleaved master stream for cross-family ordering.
///
/// The comparison deliberately has NO relaxations: where the implementations genuinely
/// diverge the test stays red until the divergence is fixed (TDD). The core families
/// cannot pass vacuously: the monolith reference run must produce a substantial amount
/// of data (candles, orders, trades, states, protective orders in the protection
/// scenario) before any comparison is considered meaningful, and missing history data
/// yields Inconclusive, not silent pass. Families a scenario does not inherently drive
/// are still cross-checked 1:1, and dedicated scenarios drive the rest: a forced
/// register-fail (off-step price + CheckSteps), a mid-run stop with resting orders
/// (cancel flow + live unsubscribe), a foreign connector-level order (attachment
/// semantics), a scripted edit/re-register/cancel flow, a LongOnly+comments run
/// (trading-mode gating and order stamping) and a raw-data run (tick/level1/depth
/// subscription relays).
///
/// Normalization (NOT a relaxation - removal of non-behavioral noise only):
/// - transaction/order ids are replaced by per-side ordinals and digit runs inside
///   error messages by '#' (ids are generator-dependent: a plain Connector seeds them
///   from wall clock, this harness's emulation chain uses per-run incremental ones);
/// - LocalTime fields and latency VALUES are excluded (wall clock); in the fully
///   synchronous emulation pipeline latency is structurally zero, so in-run latency
///   events cannot be driven by this harness - the only LatencyChanged raise comes
///   from the monolith's Reset storm;
/// The compared window is configure..Reset: recorders attach before the connector is
/// assigned, both sides are Reset after the stop, and a synthetic "Final" record pins
/// the end-of-run property state (position, max position, PnL split, commission,
/// slippage, latency, collection counts) of each side for 1:1 comparison. Infra
/// streams (PropertyChanged, ParametersChanged, ConnectorChanged, Reseted,
/// CurrentTimeChanged, drawing surface) are recorded and compared too - the
/// decomposed side has no equivalents, so those families stay red until it does.
/// </summary>
[TestClass]
[DoNotParallelize] // two full backtests per method: needs full CPU so the watchdogs stay meaningful
public class StrategyDecomposedFullEquivalenceTests : BaseTestClass
{
	private const int _longLen = 80;
	private const int _shortLen = 10;
	private const decimal _volume = 1m;
	private const int _minCandles = 100;
	private const int _minOrders = 4;
	private const int _minTrades = 2;

	#region Shared strategy logic

	/// <summary>
	/// The single source of truth for the SMA crossover decision. Both strategy variants
	/// delegate here, so the equivalence comparison exercises ONLY the implementation
	/// plumbing (subscriptions, orders, trades, positions, PnL), never two diverging
	/// copies of the algorithm. Indicator VALUES are produced by each side's canonical
	/// path (monolith: Bind pipeline; decomposed: direct Process) - those paths were
	/// already pinned as producing identical crossover signals for finished candles by
	/// StrategyDecomposedEquivalenceTests.
	/// </summary>
	private sealed class SmaCrossoverLogic
	{
		private bool? _isShortLessThenLong;

		public (Sides side, decimal price)? ProcessValues(ICandleMessage candle, decimal longValue, decimal shortValue, decimal priceStep)
		{
			if (candle.State != CandleStates.Finished)
				return null;

			var isShortLessThenLong = shortValue < longValue;

			if (_isShortLessThenLong == null)
			{
				_isShortLessThenLong = isShortLessThenLong;
				return null;
			}

			if (_isShortLessThenLong == isShortLessThenLong)
				return null;

			_isShortLessThenLong = isShortLessThenLong;

			var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;
			var price = candle.ClosePrice + (direction == Sides.Buy ? priceStep : -priceStep);

			return (direction, price);
		}

		public static decimal CalcVolume(decimal position, decimal defaultVolume)
			=> position == 0 ? defaultVolume : position.Abs().Min(defaultVolume) * 2;
	}

	private static Subscription CreateCandleSubscription(Security security, DataType buildFrom, Level1Fields? buildField)
		=> new(TimeSpan.FromMinutes(1).TimeFrame(), security)
		{
			MarketData =
			{
				IsFinishedOnly = true,
				BuildFrom = buildFrom,
				BuildMode = buildFrom is null ? MarketDataBuildModes.LoadAndBuild : MarketDataBuildModes.Build,
				BuildField = buildField,
			}
		};

	private sealed class EquivalenceScenario
	{
		public DataType BuildFrom { get; init; }
		public Level1Fields? BuildField { get; init; }
		public bool UseProtection { get; init; }

		/// <summary>
		/// Register one off-step-priced order on the first signal with connector
		/// CheckSteps enabled, so the register-fail path is deterministically driven.
		/// </summary>
		public bool ForceRegisterFail { get; init; }

		/// <summary>
		/// Request strategy stop after this many processed candles (0 = run to the end),
		/// so cancel-on-stop and live unsubscribe flows execute while data still streams.
		/// </summary>
		public int StopAfterCandles { get; init; }

		/// <summary>
		/// Register a far-from-market order directly on the connector before replay,
		/// so foreign-order attachment semantics are driven.
		/// </summary>
		public bool RegisterForeignOrder { get; init; }

		/// <summary>
		/// Run the scripted edit/re-register/cancel flow instead of trading the SMA
		/// signals, so order-maintenance surfaces are driven without market noise.
		/// </summary>
		public bool UseEditFlow { get; init; }

		/// <summary>
		/// Set TradingMode=LongOnly and CommentMode=Id on both sides, so trading-mode
		/// gating and comment stamping are driven (sell signals must be blocked).
		/// </summary>
		public bool LongOnlyWithComments { get; init; }

		/// <summary>
		/// Additionally subscribe raw ticks/level1/depth through each strategy, so the
		/// subscription-scoped market-data relay surface is driven.
		/// </summary>
		public bool SubscribeRawData { get; init; }

		/// <summary>
		/// History window length; shorter for high-volume scenarios.
		/// </summary>
		public int Days { get; init; } = 7;

		public int MinOrders { get; init; } = _minOrders;
		public int MinTrades { get; init; } = _minTrades;
	}

	/// <summary>
	/// The order-maintenance operations both variants expose under identical
	/// signatures, so the scripted flow below is written once.
	/// </summary>
	private interface ITradeOps
	{
		Security Security { get; }
		Portfolio Portfolio { get; }
		Order BuyLimit(decimal price, decimal? volume);
		void EditOrder(Order order, Order changes);
		void ReRegisterOrder(Order oldOrder, Order newOrder);
		void CancelOrder(Order order);
	}

	/// <summary>
	/// Deterministic scripted flow keyed by candle index: place a far resting order,
	/// edit it, re-register it, cancel it. Far below the market so it never fills.
	/// </summary>
	private sealed class EditFlowScript(ITradeOps ops)
	{
		private Order _resting;

		public void OnCandle(int candleIndex, ICandleMessage candle)
		{
			switch (candleIndex)
			{
				case 100:
					_resting = ops.BuyLimit(candle.ClosePrice - 5000, 1m);
					break;

				case 105:
					ops.EditOrder(_resting, new Order
					{
						Security = ops.Security,
						Portfolio = ops.Portfolio,
						Side = Sides.Buy,
						Price = candle.ClosePrice - 4000,
						Volume = 1m,
					});
					break;

				case 110:
				{
					var replacement = new Order
					{
						Security = ops.Security,
						Portfolio = ops.Portfolio,
						Side = Sides.Buy,
						Price = candle.ClosePrice - 3000,
						Volume = 1m,
					};

					ops.ReRegisterOrder(_resting, replacement);
					_resting = replacement;
					break;
				}

				case 115:
					ops.CancelOrder(_resting);
					break;
			}
		}
	}

	#endregion

	#region Strategy variants

	private sealed class MonolithSmaVariant : Strategy, ITradeOps
	{
		private readonly SmaCrossoverLogic _logic = new();
		private EditFlowScript _editFlow;
		private int _candleCount;
		private bool _invalidOrderSent;
		private bool _stopRequested;

		public EquivalenceScenario Scenario { get; set; }
		public decimal MaxAbsPosition { get; private set; }

		public event Action<ICandleMessage> CandleProcessed;
		public event Action<Sides, decimal, decimal> SignalProduced;

		Order ITradeOps.BuyLimit(decimal price, decimal? volume) => this.BuyLimit(price, volume);
		void ITradeOps.EditOrder(Order order, Order changes) => EditOrder(order, changes);
		void ITradeOps.ReRegisterOrder(Order oldOrder, Order newOrder) => ReRegisterOrder(oldOrder, newOrder);
		void ITradeOps.CancelOrder(Order order) => CancelOrder(order);

		protected override void OnStarted2(DateTime time)
		{
			base.OnStarted2(time);

			if (Scenario.UseProtection)
				StartProtection(new Unit(), new Unit(0.2m, UnitTypes.Percent));

			if (Scenario.UseEditFlow)
				_editFlow = new(this);

			var longSma = new SimpleMovingAverage { Length = _longLen };
			var shortSma = new SimpleMovingAverage { Length = _shortLen };

			// The canonical monolith integration path: the high-level subscription
			// handler also wires the protective price trigger (Strategy_HighLevel
			// tryActivateProtection), which a low-level Subscribe would bypass.
			SubscribeCandles(CreateCandleSubscription(Security, Scenario.BuildFrom, Scenario.BuildField))
				.Bind(longSma, shortSma, OnProcess)
				.Start();

			if (Scenario.SubscribeRawData)
			{
				Subscribe(new(DataType.Ticks, Security));
				Subscribe(new(DataType.Level1, Security));
				Subscribe(new(DataType.MarketDepth, Security));
			}
		}

		private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			CandleProcessed?.Invoke(candle);
			_candleCount++;
			MaxAbsPosition = MaxAbsPosition.Max(Position.Abs());

			_editFlow?.OnCandle(_candleCount, candle);

			var signal = _logic.ProcessValues(candle, longValue, shortValue, Security.PriceStep ?? 1);

			if (signal is (Sides side, decimal price))
			{
				var volume = SmaCrossoverLogic.CalcVolume(Position, Volume);

				SignalProduced?.Invoke(side, price, volume);

				if (!Scenario.UseEditFlow)
				{
					if (Scenario.ForceRegisterFail && !_invalidOrderSent)
					{
						_invalidOrderSent = true;

						// Half a price step off the grid: rejected by connector CheckSteps.
						RegisterOrder(new Order
						{
							Security = Security,
							Portfolio = Portfolio,
							Side = side,
							Price = price + 0.005m,
							Volume = volume,
						});
					}

					if (side == Sides.Buy)
						BuyLimit(price, volume);
					else
						SellLimit(price, volume);
				}
			}

			if (Scenario.StopAfterCandles > 0 && !_stopRequested && _candleCount >= Scenario.StopAfterCandles)
			{
				_stopRequested = true;
				Stop();
			}
		}
	}

	private sealed class DecomposedSmaVariant : DecomposedStrategy, ITradeOps
	{
		private readonly SmaCrossoverLogic _logic = new();
		private readonly SimpleMovingAverage _longSma = new() { Length = _longLen };
		private readonly SimpleMovingAverage _shortSma = new() { Length = _shortLen };
		private EditFlowScript _editFlow;
		private Subscription _candleSub;
		private int _candleCount;
		private bool _invalidOrderSent;
		private bool _stopRequested;
		private bool _isFormed;

		public EquivalenceScenario Scenario { get; set; }
		public decimal MaxAbsPosition { get; private set; }
		public override bool IsFormed => _isFormed;

		public event Action<ICandleMessage> CandleProcessed;
		public event Action<Sides, decimal, decimal> SignalProduced;
		public event Action<ProcessStates> StateChangedHook;
		public event Action<OrderFail> OrderRegisterFailedHook;

		Order ITradeOps.BuyLimit(decimal price, decimal? volume) => BuyLimit(price, volume);
		void ITradeOps.EditOrder(Order order, Order changes) => EditOrder(order, changes);
		void ITradeOps.ReRegisterOrder(Order oldOrder, Order newOrder) => ReRegisterOrder(oldOrder, newOrder);
		void ITradeOps.CancelOrder(Order order) => CancelOrder(order);

		protected override void OnStateChanged(ProcessStates state)
		{
			base.OnStateChanged(state);

			switch (state)
			{
				case ProcessStates.Started:
				{
					if (Scenario.UseProtection)
						StartProtection(new Unit(), new Unit(0.2m, UnitTypes.Percent));

					if (Scenario.UseEditFlow)
						_editFlow = new(this);

					// The decomposed design has no automatic lookup subscriptions
					// (the monolith subscribes PortfolioLookup/OrderLookup in OnStarted2),
					// so the variant subscribes the equivalents itself in the same order.
					Subscriptions.Subscribe(new(DataType.PositionChanges), true);
					Subscriptions.Subscribe(new(DataType.Transactions), true);

					_candleSub = CreateCandleSubscription(Security, Scenario.BuildFrom, Scenario.BuildField);

					Connector.CandleReceived += OnCandle;

					Subscriptions.Subscribe(_candleSub);

					if (Scenario.SubscribeRawData)
					{
						Subscriptions.Subscribe(new(DataType.Ticks, Security));
						Subscriptions.Subscribe(new(DataType.Level1, Security));
						Subscriptions.Subscribe(new(DataType.MarketDepth, Security));
					}
					break;
				}

				case ProcessStates.Stopping:
				{
					if (Connector is not null)
						Connector.CandleReceived -= OnCandle;
					break;
				}
			}

			// Raised after the per-state work, mirroring the monolith which raises
			// ProcessStateChanged after OnStarted2/OnStopping complete.
			StateChangedHook?.Invoke(state);
		}

		protected override void OnOrderRegisterFailed(OrderFail fail)
		{
			base.OnOrderRegisterFailed(fail);
			OrderRegisterFailedHook?.Invoke(fail);
		}

		private void OnCandle(Subscription sub, ICandleMessage candle)
		{
			if (sub != _candleSub)
				return;

			var longValue = _longSma.Process(candle);
			var shortValue = _shortSma.Process(candle);

			if (!_isFormed && _longSma.IsFormed && _shortSma.IsFormed)
			{
				_isFormed = true;
				this.Notify(nameof(IsFormed));
			}

			CandleProcessed?.Invoke(candle);
			_candleCount++;
			MaxAbsPosition = MaxAbsPosition.Max(Position.Abs());

			_editFlow?.OnCandle(_candleCount, candle);

			if (longValue.IsEmpty || shortValue.IsEmpty)
				return;

			var signal = _logic.ProcessValues(candle, longValue.ToDecimal(), shortValue.ToDecimal(), Security.PriceStep ?? 1);

			if (signal is (Sides side, decimal price))
			{
				var volume = SmaCrossoverLogic.CalcVolume(Position, Volume);

				SignalProduced?.Invoke(side, price, volume);

				if (!Scenario.UseEditFlow)
				{
					if (Scenario.ForceRegisterFail && !_invalidOrderSent)
					{
						_invalidOrderSent = true;

						// Half a price step off the grid: rejected by connector CheckSteps.
						RegisterOrder(CreateOrder(side, price + 0.005m, volume));
					}

					if (side == Sides.Buy)
						BuyLimit(price, volume);
					else
						SellLimit(price, volume);
				}
			}

			if (Scenario.StopAfterCandles > 0 && !_stopRequested && _candleCount >= Scenario.StopAfterCandles)
			{
				_stopRequested = true;
				_ = StopAsync();
			}
		}
	}

	#endregion

	#region Event recording

	private sealed class SideRecorder
	{
		private readonly Dictionary<Order, int> _orderOrdinals = [];

		public List<(string kind, string payload)> Master { get; } = [];

		public void Add(string kind, string payload = "")
			=> Master.Add((kind, payload));

		public string Ord(Order order)
		{
			if (!_orderOrdinals.TryGetValue(order, out var ordinal))
			{
				ordinal = _orderOrdinals.Count + 1;
				_orderOrdinals.Add(order, ordinal);
			}

			return $"#{ordinal}";
		}

		public List<string> Family(string kind)
			=> [.. Master.Where(r => r.kind == kind).Select(r => r.payload)];

		public List<string> FormatMaster()
			=> [.. Master.Select(r => $"{r.kind}|{r.payload}")];

		public string FamilyCounts()
			=> Master.GroupBy(r => r.kind).Select(g => $"{g.Key}={g.Count()}").JoinCommaSpace();
	}

	private static string Fmt(decimal? value)
		=> value?.ToString(CultureInfo.InvariantCulture) ?? "null";

	private static string FmtTime(DateTimeOffset time)
		=> time.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

	private static string SetFlag(string value)
		=> value.IsEmpty() ? "null" : "set";

	/// <summary>
	/// Error texts can embed raw transaction/order ids (e.g. OrderAlreadyTransId),
	/// which are generator-dependent; digit runs are normalized so the payloads stay
	/// side- and run-independent while the message shape is still compared.
	/// </summary>
	private static string NormalizeError(Exception error)
	{
		if (error is null)
			return "null";

		var message = Regex.Replace(
			error.Message,
			@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
			"#");

		return Regex.Replace(message, "[0-9]+", "#");
	}

	private static string FormatOrderCore(Order order, string ordinal)
		=> $"{ordinal} side={order.Side} type={(order.Type is null ? "null" : order.Type.ToString())} price={Fmt(order.Price)} vol={Fmt(order.Volume)} tif={(order.TimeInForce is null ? "null" : order.TimeInForce.ToString())} expiry={(order.ExpiryDate is null ? "null" : "set")} cond={(order.Condition is null ? "null" : order.Condition.GetType().Name)} uid={SetFlag(order.UserOrderId)} sid={SetFlag(order.StrategyId)} comment={SetFlag(order.Comment)}";

	private static string FormatOrderState(Order order, string ordinal)
		=> $"{ordinal} state={order.State} balance={Fmt(order.Balance)} time={FmtTime(order.Time)}";

	private static string FormatTrade(MyTrade trade, string orderOrdinal)
		=> $"order={orderOrdinal} price={Fmt(trade.Trade.Price)} vol={Fmt(trade.Trade.Volume)} pos={Fmt(trade.Position)} pnl={Fmt(trade.PnL)} slippage={Fmt(trade.Slippage)} commission={Fmt(trade.Commission)} time={FmtTime(trade.Trade.ServerTime)}";

	private static string FormatCandle(ICandleMessage candle)
		=> $"open={FmtTime(candle.OpenTime)} o={Fmt(candle.OpenPrice)} h={Fmt(candle.HighPrice)} l={Fmt(candle.LowPrice)} c={Fmt(candle.ClosePrice)} vol={Fmt(candle.TotalVolume)} state={candle.State}";

	private static string FormatPosition(Position position)
		=> $"sec={position.Security?.Id} value={Fmt(position.CurrentValue)} avg={Fmt(position.AveragePrice)} realized={Fmt(position.RealizedPnL)} time={FmtTime(position.ServerTime)}";

	private static string FormatPnL(IPnLManager manager)
		=> $"real={Fmt(manager.RealizedPnL)} unreal={Fmt(manager.UnrealizedPnL)} total={Fmt(manager.GetPnL())}";

	private static string FormatSubscription(Subscription sub)
	{
		var md = sub.SubscriptionMessage as MarketDataMessage;
		var strategyId = sub.SubscriptionMessage is IStrategyIdMessage sid ? SetFlag(sid.StrategyId) : "n/a";

		return $"{sub.DataType} strategyId={strategyId}" + (md is null
			? string.Empty
			: $" finishedOnly={md.IsFinishedOnly} buildMode={md.BuildMode} buildFrom={md.BuildFrom?.ToString() ?? "null"} buildField={md.BuildField?.ToString() ?? "null"}");
	}

	private static void RecordConnector(HistoryEmulationConnector connector, SideRecorder rec)
	{
		connector.SubscriptionStarted += sub => rec.Add("Sub", FormatSubscription(sub));
		connector.SubscriptionOnline += sub => rec.Add("SubOnline", sub.DataType.ToString());
		connector.SubscriptionFailed += (sub, err, isSubscribe) => rec.Add("SubFail", $"{sub.DataType} subscribe={isSubscribe} error={NormalizeError(err)}");
		connector.SubscriptionStopped += (sub, err) => rec.Add("SubStop", $"{sub.DataType} error={(err is null ? "null" : NormalizeError(err))}");

		// Connector-boundary streams: prove the data reached/left the connector
		// identically on both sides, so any divergence above is inside the strategy layer.
		connector.OrderReceived += (sub, order) => rec.Add("ConnOrder", $"{rec.Ord(order)} sub={sub.DataType} state={order.State} balance={Fmt(order.Balance)}");
		connector.OwnTradeReceived += (sub, trade) => rec.Add("ConnTrade", $"order={rec.Ord(trade.Order)} sub={sub.DataType} price={Fmt(trade.Trade.Price)} vol={Fmt(trade.Trade.Volume)}");
		connector.OrderRegisterFailReceived += (sub, fail) => rec.Add("ConnOrderFail", $"{rec.Ord(fail.Order)} sub={sub.DataType} error={NormalizeError(fail.Error)}");
		connector.OrderCancelFailReceived += (sub, fail) => rec.Add("ConnCancelFail", $"{rec.Ord(fail.Order)} sub={sub.DataType} error={NormalizeError(fail.Error)}");
		connector.OrderEditFailReceived += (sub, fail) => rec.Add("ConnEditFail", $"{rec.Ord(fail.Order)} sub={sub.DataType} error={NormalizeError(fail.Error)}");
		connector.PositionReceived += (sub, pos) => rec.Add("ConnPos", FormatPosition(pos));
		connector.PortfolioReceived += (sub, pf) => rec.Add("Pf", $"begin={Fmt(pf.BeginValue)} current={Fmt(pf.CurrentValue)}");
	}

	private static void RecordMonolith(MonolithSmaVariant strategy, SideRecorder rec)
	{
		strategy.ProcessStateChanged += s => rec.Add("State", s.ProcessState.ToString());
		strategy.CandleProcessed += candle => rec.Add("Candle", FormatCandle(candle));
		strategy.SignalProduced += (side, price, volume) => rec.Add("Signal", $"side={side} price={Fmt(price)} vol={Fmt(volume)}");

		strategy.ConnectorChanged += () => rec.Add("ConnectorChanged");
		strategy.ParametersChanged += () => rec.Add("ParametersChanged");
		strategy.PropertyChanged += (_, e) => rec.Add("PropertyChanged", e.PropertyName);
		strategy.Reseted += () => rec.Add("Reseted");
		((ITimeProvider)strategy).CurrentTimeChanged += _ => rec.Add("CurrentTimeChanged");

		strategy.OrderRegistering += order => rec.Add("OrderRegistering", FormatOrderCore(order, rec.Ord(order)));
		strategy.OrderReRegistering += (oldOrder, newOrder) => rec.Add("OrderReRegistering", $"old={rec.Ord(oldOrder)} new={rec.Ord(newOrder)} price={Fmt(newOrder.Price)}");

#pragma warning disable CS0618 // obsolete strategy events are still the strategy-level contract surface under test
		((ITransactionProvider)strategy).NewOrder += order => rec.Add("NewOrder", FormatOrderCore(order, rec.Ord(order)));

		strategy.OrderRegistered += order => rec.Add("OrderReg", FormatOrderState(order, rec.Ord(order)));
		strategy.OrderChanged += order => rec.Add("OrderChg", FormatOrderState(order, rec.Ord(order)));
		strategy.NewMyTrade += trade => rec.Add("Trade", FormatTrade(trade, rec.Ord(trade.Order)));

		strategy.OrderEdited += (transId, order) => rec.Add("OrderEdit", $"{rec.Ord(order)} price={Fmt(order.Price)} vol={Fmt(order.Volume)}");
		strategy.OrderEditFailed += (transId, fail) => rec.Add("OrderEditFail", $"{rec.Ord(fail.Order)} error={NormalizeError(fail.Error)}");

		strategy.PositionChanged += () => rec.Add("StratPos", Fmt(strategy.Position));
		strategy.PnLReceived += sub => rec.Add("PnLReceived", sub.DataType.ToString());
		strategy.OrderCancelFailed += fail => rec.Add("OrderCancelFail", $"{rec.Ord(fail.Order)} error={NormalizeError(fail.Error)}");
#pragma warning restore CS0618

		strategy.PnLReceived2 += (sub, pf, time, realized, unrealized, commission) =>
			rec.Add("PnLReceived2", $"real={Fmt(realized)} unreal={Fmt(unrealized)} commission={Fmt(commission)} time={FmtTime(time)}");

		var transactions = (ITransactionProvider)strategy;
		transactions.MassOrderCanceled += _ => rec.Add("MassOrderCanceled");
		transactions.MassOrderCanceled2 += (_, _) => rec.Add("MassOrderCanceled2");
		transactions.MassOrderCancelFailed += (_, _) => rec.Add("MassOrderCancelFailed");
		transactions.MassOrderCancelFailed2 += (_, _, _) => rec.Add("MassOrderCancelFailed2");
		transactions.LookupPortfoliosResult += (_, _, _) => rec.Add("LookupPortfoliosResult");
		transactions.LookupPortfoliosResult2 += (_, _, _, _) => rec.Add("LookupPortfoliosResult2");

		strategy.OrderRegisterFailReceived += (sub, fail) => rec.Add("OrderFail", $"{rec.Ord(fail.Order)} error={NormalizeError(fail.Error)}");
		strategy.OrderCanceling += order => rec.Add("OrderCancel", $"{rec.Ord(order)} state={order.State}");

		strategy.SubscriptionStarted += sub => rec.Add("StratSubStarted", FormatSubscription(sub));
		strategy.SubscriptionOnline += sub => rec.Add("StratSubOnline", sub.DataType.ToString());
		strategy.SubscriptionStopped += (sub, err) => rec.Add("StratSubStopped", $"{sub.DataType} error={(err is null ? "null" : NormalizeError(err))}");
		strategy.SubscriptionFailed += (sub, err, isSubscribe) => rec.Add("StratSubFailed", $"{sub.DataType} subscribe={isSubscribe} error={NormalizeError(err)}");
		strategy.SubscriptionReceived += (sub, arg) => rec.Add("SubscriptionReceived", $"{sub.DataType} arg={arg?.GetType().Name ?? "null"}");

		strategy.TickTradeReceived += (sub, tick) => rec.Add("RelayTick", $"price={Fmt(tick.Price)} vol={Fmt(tick.Volume)} time={FmtTime(tick.ServerTime)}");
		strategy.Level1Received += (sub, l1) => rec.Add("RelayL1", $"last={Fmt(l1.TryGetDecimal(Level1Fields.LastTradePrice))} bid={Fmt(l1.TryGetDecimal(Level1Fields.BestBidPrice))} ask={Fmt(l1.TryGetDecimal(Level1Fields.BestAskPrice))} time={FmtTime(l1.ServerTime)}");
		strategy.OrderBookReceived += (sub, book) => rec.Add("RelayDepth", $"bid={Fmt(book.GetBestBid()?.Price)} ask={Fmt(book.GetBestAsk()?.Price)} time={FmtTime(book.ServerTime)}");
		strategy.OrderLogReceived += (sub, _) => rec.Add("RelayOrderLog", sub.DataType.ToString());
		strategy.SecurityReceived += (sub, sec) => rec.Add("RelaySecurity", sec.Id);
		strategy.BoardReceived += (sub, board) => rec.Add("RelayBoard", board.Code);
		strategy.NewsReceived += (sub, _) => rec.Add("RelayNews", sub.DataType.ToString());
		strategy.DataTypeReceived += (sub, dt) => rec.Add("RelayDataType", dt.ToString());

		strategy.OrderBookDrawing += (_, _, _) => rec.Add("Drawing");
		strategy.OrderBookDrawingOrder += (_, _, _) => rec.Add("DrawingOrder");
		strategy.OrderBookDrawingOrderFail += (_, _, _) => rec.Add("DrawingOrderFail");

		((IPositionProvider)strategy).NewPosition += pos => rec.Add("PosEvt", $"new {FormatPosition(pos)}");
		((IPositionProvider)strategy).PositionChanged += pos => rec.Add("PosEvt", $"chg {FormatPosition(pos)}");
		strategy.PositionReceived += (sub, pos) => rec.Add("AccPos", FormatPosition(pos));

		strategy.PnLChanged += () => rec.Add("PnL", FormatPnL(strategy.PnLManager));
		strategy.CommissionChanged += () => rec.Add("Commission", Fmt(strategy.Commission));
		strategy.SlippageChanged += () => rec.Add("Slippage", Fmt(strategy.Slippage));
		strategy.LatencyChanged += () => rec.Add("Latency");

		strategy.IsOnlineChanged += s => rec.Add("Online", s.IsOnline.ToString());
		strategy.Error += (s, error) => rec.Add("Error", NormalizeError(error));
	}

	private static void RecordDecomposed(DecomposedSmaVariant strategy, SideRecorder rec)
	{
		strategy.StateChangedHook += s => rec.Add("State", s.ToString());
		strategy.CandleProcessed += candle => rec.Add("Candle", FormatCandle(candle));
		strategy.SignalProduced += (side, price, volume) => rec.Add("Signal", $"side={side} price={Fmt(price)} vol={Fmt(volume)}");

		strategy.ConnectorChanged += () => rec.Add("ConnectorChanged");
		strategy.PropertyChanged += (_, e) => rec.Add("PropertyChanged", e.PropertyName);
		strategy.Reseted += () => rec.Add("Reseted");
		((ITimeProvider)strategy).CurrentTimeChanged += _ => rec.Add("CurrentTimeChanged");

		strategy.OrderRegistering += order => rec.Add("OrderRegistering", FormatOrderCore(order, rec.Ord(order)));
		strategy.OrderReRegistering += (oldOrder, newOrder) => rec.Add("OrderReRegistering", $"old={rec.Ord(oldOrder)} new={rec.Ord(newOrder)} price={Fmt(newOrder.Price)}");
		strategy.OrderCanceling += order => rec.Add("OrderCancel", $"{rec.Ord(order)} state={order.State}");

		strategy.Orders.NewOrder += order => rec.Add("NewOrder", FormatOrderCore(order, rec.Ord(order)));
		strategy.Orders.Registered += order => rec.Add("OrderReg", FormatOrderState(order, rec.Ord(order)));
		strategy.Orders.Changed += order => rec.Add("OrderChg", FormatOrderState(order, rec.Ord(order)));
		strategy.Trades.TradeAdded += trade => rec.Add("Trade", FormatTrade(trade, rec.Ord(trade.Order)));

		strategy.OrderEdited += (transId, order) => rec.Add("OrderEdit", $"{rec.Ord(order)} price={Fmt(order.Price)} vol={Fmt(order.Volume)}");
		strategy.OrderEditFailed += (transId, fail) => rec.Add("OrderEditFail", $"{rec.Ord(fail.Order)} error={NormalizeError(fail.Error)}");
		strategy.OrderRegisterFailReceived += (sub, fail) => rec.Add("OrderFail", $"{rec.Ord(fail.Order)} error={NormalizeError(fail.Error)}");
		strategy.OrderCancelFailed += fail => rec.Add("OrderCancelFail", $"{rec.Ord(fail.Order)} error={NormalizeError(fail.Error)}");

		strategy.SubscriptionStarted += sub => rec.Add("StratSubStarted", FormatSubscription(sub));
		strategy.SubscriptionOnline += sub => rec.Add("StratSubOnline", sub.DataType.ToString());
		strategy.SubscriptionStopped += (sub, err) => rec.Add("StratSubStopped", $"{sub.DataType} error={(err is null ? "null" : NormalizeError(err))}");
		strategy.SubscriptionFailed += (sub, err, isSubscribe) => rec.Add("StratSubFailed", $"{sub.DataType} subscribe={isSubscribe} error={NormalizeError(err)}");
		strategy.SubscriptionReceived += (sub, arg) => rec.Add("SubscriptionReceived", $"{sub.DataType} arg={arg?.GetType().Name ?? "null"}");

		strategy.TickTradeReceived += (sub, tick) => rec.Add("RelayTick", $"price={Fmt(tick.Price)} vol={Fmt(tick.Volume)} time={FmtTime(tick.ServerTime)}");
		strategy.Level1Received += (sub, l1) => rec.Add("RelayL1", $"last={Fmt(l1.TryGetDecimal(Level1Fields.LastTradePrice))} bid={Fmt(l1.TryGetDecimal(Level1Fields.BestBidPrice))} ask={Fmt(l1.TryGetDecimal(Level1Fields.BestAskPrice))} time={FmtTime(l1.ServerTime)}");
		strategy.OrderBookReceived += (sub, book) => rec.Add("RelayDepth", $"bid={Fmt(book.GetBestBid()?.Price)} ask={Fmt(book.GetBestAsk()?.Price)} time={FmtTime(book.ServerTime)}");
		strategy.OrderLogReceived += (sub, _) => rec.Add("RelayOrderLog", sub.DataType.ToString());
		strategy.SecurityReceived += (sub, sec) => rec.Add("RelaySecurity", sec.Id);
		strategy.BoardReceived += (sub, board) => rec.Add("RelayBoard", board.Code);
		strategy.NewsReceived += (sub, _) => rec.Add("RelayNews", sub.DataType.ToString());
		strategy.DataTypeReceived += (sub, dt) => rec.Add("RelayDataType", dt.ToString());

		// No decomposed equivalents exist for: mass-cancel and lookup-result relays,
		// ParametersChanged, and the drawing surface; those families stay red whenever
		// the scenario drives them on the monolith.

		var positions = (IPositionProvider)strategy;
		positions.NewPosition += pos => rec.Add("PosEvt", $"new {FormatPosition(pos)}");
		positions.PositionChanged += pos => rec.Add("PosEvt", $"chg {FormatPosition(pos)}");
		strategy.PositionChanged += () => rec.Add("StratPos", Fmt(strategy.Position));
		strategy.PositionReceived += (sub, pos) => rec.Add("AccPos", FormatPosition(pos));

		strategy.PnLChanged += () => rec.Add("PnL", FormatPnL(strategy.PnLManager));
		strategy.PnLReceived += sub => rec.Add("PnLReceived", sub.DataType.ToString());
		strategy.PnLReceived2 += (sub, pf, time, realized, unrealized, commission) =>
			rec.Add("PnLReceived2", $"real={Fmt(realized)} unreal={Fmt(unrealized)} commission={Fmt(commission)} time={FmtTime(time)}");
		strategy.CommissionChanged += () => rec.Add("Commission", Fmt(strategy.Commission));
		strategy.SlippageChanged += () => rec.Add("Slippage", Fmt(strategy.Slippage));
		strategy.LatencyChanged += () => rec.Add("Latency");

		strategy.IsOnlineChanged += s => rec.Add("Online", s.IsOnline.ToString());
		strategy.Error += error => rec.Add("Error", NormalizeError(error));
	}

	#endregion

	#region Backtest infrastructure

	private static HistoryEmulationConnector CreateDeterministicConnector(
		Security security, Portfolio portfolio, DateTime startTime, DateTime stopTime)
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
			StartDate = startTime,
			StopDate = stopTime,
		};

		var connector = new HistoryEmulationConnector(
			historyAdapter, true,
			new PassThroughMessageChannel(),
			secProvider, pfProvider,
			storageRegistry.ExchangeInfoProvider);

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		// Determinism scaffolding (see BacktestWithSameRandomSeedProducesIdenticalMessages):
		// fixed random seed, fixed initial order/trade ids, no wall-clock latency.
		var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
		emulator.RandomProvider = new DefaultRandomProvider(42);
		connector.EmulationAdapter.Settings.InitialOrderId = 100;
		connector.EmulationAdapter.Settings.InitialTradeId = 100;
		connector.Adapter.LatencyManager = null;

		// Pin the absence of the wall-clock heartbeat timer instead of relying on
		// HistoryEmulationConnector's internal applyHeartbeat=false default.
		connector.Adapter.ApplyHeartbeat(connector.EmulationAdapter, false);

		// Deterministic non-zero commission so the Commission stream cannot pass
		// as a vacuous empty-vs-empty comparison.
		var commissionManager = new CommissionManager();
		commissionManager.Rules.Add(new CommissionTradeRule { Value = new Unit(1) });
		connector.Adapter.CommissionManager = commissionManager;

		// HistoryEmulationConnector nulls the slippage manager by default; restore it
		// so the Slippage stream is driven (fill price vs best price, deterministic).
		connector.Adapter.SlippageManager = new SlippageManager(new SlippageManagerState());

		return connector;
	}

	private async Task<SideRecorder> RunSide(bool decomposed, EquivalenceScenario scenario, DateTime startTime, DateTime stopTime)
	{
		var security = new Security { Id = Paths.HistoryDefaultSecurity, PriceStep = 0.01m };
		var portfolio = Portfolio.CreateSimulator();

		using var connector = CreateDeterministicConnector(security, portfolio, startTime, stopTime);

		if (scenario.ForceRegisterFail)
			connector.CheckSteps = true;

		var rec = new SideRecorder();
		RecordConnector(connector, rec);

		// RunContinuationsAsynchronously: StateChanged2 fires synchronously on the
		// replay thread; without it the rest of the test (including the second
		// backtest) would continue inline inside the connector's State setter.
		var stoppedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		void RegisterForeignOrder()
		{
			if (!scenario.RegisterForeignOrder)
				return;

			// A connector-level order that does not belong to the strategy: rests far
			// below the market for the whole run, exercising attachment semantics.
			connector.RegisterOrder(new Order
			{
				Security = security,
				Portfolio = portfolio,
				Side = Sides.Buy,
				Price = 1000m,
				Volume = 1m,
			});
		}

		if (decomposed)
		{
			var strategy = new DecomposedSmaVariant
			{
				Security = security,
				Portfolio = portfolio,
				Volume = _volume,
				Scenario = scenario,
			};

			if (scenario.LongOnlyWithComments)
			{
				strategy.TradingMode = StrategyTradingModes.LongOnly;
				strategy.CommentMode = StrategyCommentModes.Id;
			}

			// Align the unrealized-refresh interval with the monolith default (1 min);
			// the engine's own default is 1 second, which is scenario config, not the
			// notification-surface divergence these tests pin.
			strategy.Engine.UnrealizedPnLInterval = TimeSpan.FromMinutes(1);

			RecordDecomposed(strategy, rec);

			strategy.Connector = connector;

			await strategy.StartAsync(CancellationToken);
			connector.Connect();
			RegisterForeignOrder();
			await connector.StartAsync(CancellationToken);

			await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

			if (!stoppedTcs.Task.IsCompleted)
			{
				connector.Disconnect();
				Fail("Decomposed backtest did not complete in time");
			}

			await strategy.StopAsync(CancellationToken);

			rec.Add("Final",
				$"state={strategy.ProcessState} maxAbsPos={Fmt(strategy.MaxAbsPosition)} pos={Fmt(strategy.Position)} " +
				$"pnl=[{FormatPnL(strategy.PnLManager)}] commission={Fmt(strategy.Commission)} slippage={Fmt(strategy.Slippage)} " +
				$"latency={(strategy.Latency is null ? "null" : "set")} orders={strategy.Orders.Orders.Count()} trades={strategy.Trades.MyTrades.Count()}");

			// Drive the reset surface as the final lifecycle step (the monolith raises
			// a notification storm on Reset, the decomposed side is silent).
			strategy.Reset();
		}
		else
		{
			var strategy = new MonolithSmaVariant
			{
				Security = security,
				Portfolio = portfolio,
				Volume = _volume,
				Scenario = scenario,
			};

			if (scenario.LongOnlyWithComments)
			{
				strategy.TradingMode = StrategyTradingModes.LongOnly;
				strategy.CommentMode = StrategyCommentModes.Id;
			}

			RecordMonolith(strategy, rec);

			strategy.Connector = connector;
			strategy.WaitRulesOnStop = false;

			strategy.Start();
			connector.Connect();
			RegisterForeignOrder();
			await connector.StartAsync(CancellationToken);

			await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

			if (!stoppedTcs.Task.IsCompleted)
			{
				connector.Disconnect();
				Fail("Monolith backtest did not complete in time");
			}

			strategy.Stop();

			rec.Add("Final",
				$"state={strategy.ProcessState} maxAbsPos={Fmt(strategy.MaxAbsPosition)} pos={Fmt(strategy.Position)} " +
				$"pnl=[{FormatPnL(strategy.PnLManager)}] commission={Fmt(strategy.Commission)} slippage={Fmt(strategy.Slippage)} " +
				$"latency={(strategy.Latency is null ? "null" : "set")} orders={strategy.Orders.Count()} trades={strategy.MyTrades.Count()}");

			// Drive the reset surface as the final lifecycle step (the monolith raises
			// a notification storm on Reset, the decomposed side is silent).
			strategy.Reset();
		}

		return rec;
	}

	#endregion

	#region Stream comparison

	private static void CompareStreams(string family, List<string> monolith, List<string> decomposed, List<string> failures)
	{
		var min = monolith.Count.Min(decomposed.Count);

		for (var i = 0; i < min; i++)
		{
			if (monolith[i] == decomposed[i])
				continue;

			failures.Add(
				$"[{family}] diverged at index {i} of {monolith.Count}/{decomposed.Count}:{Environment.NewLine}" +
				$"  monolith:   {monolith[i]}{Environment.NewLine}" +
				$"  decomposed: {decomposed[i]}{Environment.NewLine}" +
				Neighborhood(family, monolith, decomposed, i));
			return;
		}

		if (monolith.Count != decomposed.Count)
		{
			var longer = monolith.Count > decomposed.Count ? "monolith" : "decomposed";
			var extra = monolith.Count > decomposed.Count ? monolith[min] : decomposed[min];

			failures.Add(
				$"[{family}] lengths differ: monolith={monolith.Count}, decomposed={decomposed.Count}; " +
				$"first extra record on {longer} side at index {min}:{Environment.NewLine}  {extra}");
		}
	}

	private static string Neighborhood(string family, List<string> monolith, List<string> decomposed, int index)
	{
		var from = (index - 2).Max(0);
		var to = (index + 2).Min(monolith.Count.Min(decomposed.Count) - 1);
		var sb = new StringBuilder();

		sb.AppendLine($"  context ({family}):");

		for (var i = from; i <= to; i++)
		{
			var marker = i == index ? ">>" : "  ";
			sb.AppendLine($"  {marker} [{i}] m: {monolith[i]}");
			sb.AppendLine($"  {marker} [{i}] d: {decomposed[i]}");
		}

		return sb.ToString();
	}

	private static int DistinctOrdinals(List<string> family)
		=> family.Select(p => p.Split(' ')[0]).Distinct().Count();

	private async Task RunFullEquivalence(EquivalenceScenario scenario)
	{
		if (Paths.HistoryDataPath == null)
		{
			// Not a silent pass: without market data a zero-vs-zero comparison would be
			// meaningless, so the test is reported as inconclusive instead of green.
			Inconclusive("HistoryDataPath is null (stocksharp.samples.historydata not installed) - equivalence cannot be verified");
		}

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(scenario.Days);

		var monolith = await RunSide(false, scenario, startTime, stopTime);
		var decomposed = await RunSide(true, scenario, startTime, stopTime);

		// --- Data-substance guards (reference side): the comparison below is meaningful
		// --- only when the scenario actually produced data. Zero-vs-zero cannot pass here.
		// --- Guard failures report per-family counts of both sides for diagnosability.
		void Guard(bool condition, string message)
		{
			if (!condition)
				Fail($"{message}{Environment.NewLine}monolith:   {monolith.FamilyCounts()}{Environment.NewLine}decomposed: {decomposed.FamilyCounts()}");
		}

		var refCandles = monolith.Family("Candle");
		var refOrders = monolith.Family("NewOrder");
		var refTrades = monolith.Family("Trade");
		var refStates = monolith.Family("State");

		Guard(refCandles.Count >= _minCandles, $"Reference run produced only {refCandles.Count} candles (need >= {_minCandles}) - data too thin for a meaningful comparison");
		Guard(refOrders.Count >= scenario.MinOrders, $"Reference run produced only {refOrders.Count} orders (need >= {scenario.MinOrders}) - scenario did not trade");
		Guard(refTrades.Count >= scenario.MinTrades, $"Reference run produced only {refTrades.Count} trades (need >= {scenario.MinTrades}) - orders never filled");
		Guard(refStates.Contains(ProcessStates.Started.ToString()), "Reference run never reached Started");
		Guard(refStates.Contains(ProcessStates.Stopped.ToString()), $"Reference run never reached Stopped (states: {refStates.JoinCommaSpace()})");

		if (scenario.UseProtection)
		{
			// Protection must actually fire in this scenario: protective orders are
			// registered on top of the crossover signals. Without this guard the
			// protective flow comparison would be a meaningless empty-vs-empty pass.
			var refSignals = monolith.Family("Signal");
			Guard(refOrders.Count > refSignals.Count,
				$"Reference run with protection produced no protective orders (orders={refOrders.Count}, signals={refSignals.Count}) - the scenario does not exercise protection");
		}

		if (scenario.ForceRegisterFail)
		{
			// The register-fail path must actually be driven on the reference side,
			// otherwise the fail families would compare empty-vs-empty.
			var refConnFails = monolith.Family("ConnOrderFail");
			Guard(refConnFails.Count >= 1,
				"Reference run produced no register fails - the off-step order was not rejected and the fail path is not exercised");
		}

		if (scenario.StopAfterCandles > 0)
		{
			// The mid-run stop must catch at least one resting active order, otherwise
			// the cancel-on-stop flow compares empty-vs-empty.
			var refCancels = monolith.Family("OrderCancel");
			Guard(refCancels.Count >= 1,
				$"Reference run canceled no orders at the mid-run stop (after {scenario.StopAfterCandles} candles) - the cancel flow is not exercised");
		}

		if (scenario.RegisterForeignOrder)
		{
			// The foreign order must be visible at the connector boundary on the
			// reference side without being attached by the strategy: the count of
			// distinct orders seen by the connector must exceed the strategy's own.
			var refConnOrderCount = DistinctOrdinals(monolith.Family("ConnOrder"));
			Guard(refConnOrderCount > refOrders.Count,
				$"Reference run does not see the foreign order at the connector boundary (connector orders={refConnOrderCount}, strategy orders={refOrders.Count})");
		}

		if (scenario.UseEditFlow)
		{
			// The edit/re-register flow must actually be driven: either a successful
			// edit or an edit failure must surface on the reference side, and the
			// cancel step must produce an order state change.
			var refEditActivity = monolith.Family("OrderEdit").Count + monolith.Family("OrderEditFail").Count + monolith.Family("ConnEditFail").Count;
			Guard(refEditActivity >= 1, "Reference run produced no edit activity - the scripted edit flow is not exercised");
			Guard(monolith.Family("OrderChg").Count >= 1, "Reference run produced no order state changes - the scripted cancel flow is not exercised");
		}

		if (scenario.LongOnlyWithComments)
		{
			// LongOnly must actually block some sell signals on the reference side,
			// otherwise the trading-mode gating compares unexercised defaults.
			var refSignals = monolith.Family("Signal");
			Guard(refOrders.Count < refSignals.Count,
				$"Reference run in LongOnly mode blocked no signals (orders={refOrders.Count}, signals={refSignals.Count}) - the gating is not exercised");
		}

		if (scenario.SubscribeRawData)
		{
			// The raw-data relays must actually be driven on the reference side
			// (depth is unguarded: the history package may not contain order books).
			Guard(monolith.Family("RelayTick").Count >= 1, "Reference run relayed no ticks - the raw tick subscription is not exercised");
			Guard(monolith.Family("RelayL1").Count >= 1, "Reference run relayed no level1 - the raw level1 subscription is not exercised");
		}

		// --- 1:1 stream comparison, all families, then the interleaved master stream.
		var failures = new List<string>();

		string[] families =
		[
			"Candle", "Sub", "SubOnline", "SubFail", "SubStop",
			"StratSubStarted", "StratSubOnline", "StratSubStopped", "StratSubFailed", "SubscriptionReceived",
			"State", "Online", "Signal",
			"RelayTick", "RelayL1", "RelayDepth", "RelayOrderLog", "RelaySecurity", "RelayBoard", "RelayNews", "RelayDataType",
			"ConnOrder", "ConnTrade", "ConnOrderFail", "ConnCancelFail", "ConnEditFail", "ConnPos", "Pf",
			"NewOrder", "OrderRegistering", "OrderReRegistering", "OrderReg", "OrderChg",
			"OrderFail", "OrderCancel", "OrderCancelFail", "OrderEdit", "OrderEditFail",
			"MassOrderCanceled", "MassOrderCanceled2", "MassOrderCancelFailed", "MassOrderCancelFailed2",
			"LookupPortfoliosResult", "LookupPortfoliosResult2",
			"Trade", "StratPos", "PosEvt", "AccPos",
			"PnL", "PnLReceived", "PnLReceived2", "Commission", "Slippage", "Latency", "Error",
			"ConnectorChanged", "ParametersChanged", "PropertyChanged", "Reseted", "CurrentTimeChanged",
			"Drawing", "DrawingOrder", "DrawingOrderFail",
			"Final",
		];

		foreach (var family in families)
			CompareStreams(family, monolith.Family(family), decomposed.Family(family), failures);

		CompareStreams("MASTER", monolith.FormatMaster(), decomposed.FormatMaster(), failures);

		if (failures.Count > 0)
		{
			Fail(
				$"Implementations are NOT equivalent: {failures.Count} diverging stream(s).{Environment.NewLine}{Environment.NewLine}" +
				failures.JoinNL());
		}
	}

	#endregion

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_Candles()
		=> RunFullEquivalence(new());

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_CandlesBuiltFromTicks()
		=> RunFullEquivalence(new() { BuildFrom = DataType.Ticks });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_CandlesBuiltFromLevel1()
		=> RunFullEquivalence(new() { BuildFrom = DataType.Level1, BuildField = Level1Fields.SpreadMiddle });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_CandlesWithProtection()
		=> RunFullEquivalence(new() { UseProtection = true });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_TicksWithProtection()
		=> RunFullEquivalence(new() { BuildFrom = DataType.Ticks, UseProtection = true });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_RegisterFailPath()
		=> RunFullEquivalence(new() { ForceRegisterFail = true });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_MidRunStopCancelsActiveOrders()
		=> RunFullEquivalence(new() { StopAfterCandles = 3000 });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_ForeignOrderNotAttached()
		=> RunFullEquivalence(new() { RegisterForeignOrder = true });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_EditReRegisterCancelFlow()
		=> RunFullEquivalence(new() { UseEditFlow = true, Days = 1, MinOrders = 2, MinTrades = 0 });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_LongOnlyTradingMode()
		=> RunFullEquivalence(new() { LongOnlyWithComments = true });

	[TestMethod]
	[Timeout(300_000, CooperativeCancellation = true)]
	public Task FullEquivalence_RawMarketDataRelays()
		=> RunFullEquivalence(new() { SubscribeRawData = true, Days = 2 });
}
