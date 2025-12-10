namespace StockSharp.Tests;

[TestClass]
public class MarketRuleTests
{
	private class MarketRuleContainer : BaseLogReceiver, IMarketRuleContainer
	{
		private readonly Lock _rulesSuspendLock = new();
		private int _rulesSuspendCount;

		public MarketRuleContainer()
		{
			_rules = new MarketRuleList(this);
		}

		ProcessStates IMarketRuleContainer.ProcessState => ProcessStates.Started;

		void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
		{
			if (IsRulesSuspended)
				return;

			this.ActiveRule(rule, process);
		}

		public bool IsRulesSuspended => _rulesSuspendCount > 0;

		void IMarketRuleContainer.SuspendRules()
		{
			using (_rulesSuspendLock.EnterScope())
				_rulesSuspendCount++;
		}

		void IMarketRuleContainer.ResumeRules()
		{
			using (_rulesSuspendLock.EnterScope())
			{
				if (_rulesSuspendCount > 0)
					_rulesSuspendCount--;
			}
		}

		private readonly MarketRuleList _rules;

		IMarketRuleList IMarketRuleContainer.Rules => _rules;
	}

	private sealed class TestRule : MarketRule<object, object>
	{
		public TestRule() : base(new object())
		{
			Name = "TestRule";
		}

		public void Trigger(object arg = null)
		{
			Activate(arg);
		}
	}

	private static IMarketRuleContainer CreateContainer()
		=> new MarketRuleContainer();

	[TestMethod]
	public void ApplyAndBasics()
	{
		var container = CreateContainer();
		var rule = new TestRule()
			.UpdateName("X")
			.UpdateLogLevel(LogLevels.Debug)
			.Suspend(false)
			.Apply(container);

		rule.Name.AssertEqual("X");
		rule.LogLevel.AssertEqual(LogLevels.Debug);
		rule.IsSuspended.AssertFalse();
		rule.IsReady.AssertTrue();

		// Once should finish immediately on activation
		bool fired = false;
		var once = new TestRule().Once().Apply(container).Do(_ => fired = true);
		container.Rules.Contains(once).AssertTrue();
		((TestRule)once).Trigger();
		fired.AssertTrue();
		container.Rules.Contains(once).AssertFalse();
	}

	[TestMethod]
	public void ExclusiveAndRemove()
	{
		var container = CreateContainer();
		var r1 = new TestRule().Apply(container);
		var r2 = new TestRule().Apply(container);
		r1.Exclusive(r2);

		bool f1 = false;
		bool f2 = false;
		r1.Do(_ => f1 = true);
		r2.Do(_ => f2 = true);

		((TestRule)r1).Trigger();

		f1.AssertTrue();

		// r2 must be removed due to exclusivity after r1 fired
		container.Rules.Contains(r2).AssertFalse();

		// TryRemove API
		container.TryRemoveRule(r1, false).AssertTrue();
		container.Rules.Contains(r1).AssertFalse();
	}

	[TestMethod]
	public void RemoveWithExclusive()
	{
		var container = CreateContainer();
		var r1 = new TestRule().Apply(container);
		var r2 = new TestRule().Apply(container);
		r1.Exclusive(r2);

		// Remove r1 and its exclusive r2
		container.TryRemoveWithExclusive(r1).AssertTrue();
		container.Rules.Contains(r1).AssertFalse();
		container.Rules.Contains(r2).AssertFalse();
	}

	[TestMethod]
	public void OrAndAnd()
	{
		var container = CreateContainer();
		var a = new TestRule();
		var b = new TestRule();

		bool orFired = false;
		a.Or(b).Apply(container).Do(_ => orFired = true);
		b.Trigger();
		orFired.AssertTrue();

		a = new TestRule();
		b = new TestRule();
		bool andFired = false;
		a.And(b).Apply(container).Do(_ => andFired = true);
		andFired.AssertFalse();
		a.Trigger();
		andFired.AssertFalse();
		b.Trigger();
		andFired.AssertTrue();
	}

	[TestMethod]
	public void ConnectorRules()
	{
		var container = CreateContainer();
		var mock = new Mock<IConnector>(MockBehavior.Loose);
		var adapter = new Mock<IMessageAdapter>().Object;

		bool connected = false;
		bool disconnected = false;
		(IMessageAdapter adapter, Exception error)? lost = null;

		mock.Object.WhenConnected().Apply(container).Do(a => connected = a == adapter);
		mock.Object.WhenDisconnected().Apply(container).Do(a => disconnected = a == adapter);
		mock.Object.WhenConnectionLost().Apply(container).Do(t => lost = t);

		mock.Raise(m => m.ConnectedEx += null, adapter);
		connected.AssertTrue();

		mock.Raise(m => m.DisconnectedEx += null, adapter);
		disconnected.AssertTrue();

		var ex = new Exception("x");
		mock.Raise(m => m.ConnectionErrorEx += null, adapter, ex);
		(lost?.adapter == adapter).AssertTrue();
		(lost?.error == ex).AssertTrue();
	}

	[TestMethod]
	public void OrAndMultipleAndOnce()
	{
		var container = CreateContainer();
		// Or with 3 rules, fire the middle one
		var r1 = new TestRule();
		var r2 = new TestRule();
		var r3 = new TestRule();
		int orCount = 0;
		r1.Or(r2, r3).Apply(container).Do(_ => orCount++);
		r2.Trigger();
		orCount.AssertEqual(1);

		// And with 3 rules, ensure activates once after the last trigger
		r1 = new TestRule();
		r2 = new TestRule();
		r3 = new TestRule();
		int andCount = 0;
		r1.And(r2, r3).Apply(container).Do(_ => andCount++);
		r1.Trigger();
		andCount.AssertEqual(0);
		r3.Trigger();
		andCount.AssertEqual(0);
		r2.Trigger();
		andCount.AssertEqual(1);

		// Once – the second activation does not re-run the handler
		int onceCount = 0;
		var once = new TestRule().Once().Apply(container).Do(_ => onceCount++);
		((TestRule)once).Trigger();
		Assert.ThrowsExactly<ObjectDisposedException>(() => ((TestRule)once).Trigger());
		onceCount.AssertEqual(1);

		// TryRemoveRule(checkCanFinish=false) removes even an endless rule
		var inf = new TestRule().Apply(container);
		inf.Until(() => false);
		container.TryRemoveRule(inf, false).AssertTrue();
		container.Rules.Contains(inf).AssertFalse();
	}

	[TestMethod]
	public void TimeRules()
	{
		var container = CreateContainer();
		var start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var mock = new Mock<ITimeProvider>(MockBehavior.Loose);
		mock.SetupGet(p => p.CurrentTimeUtc).Returns(start);

		var intervalFired = 0;
		mock.Object.WhenIntervalElapsed(TimeSpan.FromSeconds(5)).Apply(container).Do(_ => intervalFired++);

		// Advance 5 seconds
		mock.Raise(m => m.CurrentTimeChanged += null, TimeSpan.FromSeconds(5));
		intervalFired.AssertEqual(1);

		// WhenTimeCome – two activations
		var times = new[] { start.AddSeconds(3), start.AddSeconds(6) };
		var firedAt = new List<DateTime>();
		mock.Object.WhenTimeCome(times).Apply(container).Do(firedAt.Add);

		// Move 3 seconds to the first time
		mock.Raise(m => m.CurrentTimeChanged += null, TimeSpan.FromSeconds(3));
		firedAt.Count.AssertEqual(1);
		firedAt[0].AssertEqual(times[0]);

		// Then 3 more seconds to the second time
		mock.Raise(m => m.CurrentTimeChanged += null, TimeSpan.FromSeconds(3));
		firedAt.Count.AssertEqual(2);
		firedAt[1].AssertEqual(times[1]);
	}

	[TestMethod]
	public void OrderRules()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var order = new Order { Volume = 10m, Balance = 10m };
		var sub = new Subscription(DataType.Ticks, Helper.CreateSecurity());

		Order regRes = null;
		order.WhenRegistered(provider.Object).Apply(container).Do(o => regRes = o);
		order.State = OrderStates.Active;
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(regRes == order).AssertTrue();

		// Partial match
		Order partialRes = null;
		order.Balance = 10m; // initial
		order.WhenPartiallyMatched(provider.Object).Apply(container).Do(o => partialRes = o);
		provider.Raise(p => p.OrderReceived += null, sub, order); // baseline
		order.Balance = 6m; // changed
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(partialRes == order).AssertTrue();

		// Register failed
		OrderFail regFail = null;
		order.WhenRegisterFailed(provider.Object).Apply(container).Do(f => regFail = f);
		var of1 = new OrderFail { Order = order };
		provider.Raise(p => p.OrderRegisterFailReceived += null, sub, of1);
		(regFail == of1).AssertTrue();

		// Cancel failed
		OrderFail cancelFail = null;
		order.WhenCancelFailed(provider.Object).Apply(container).Do(f => cancelFail = f);
		var of2 = new OrderFail { Order = order };
		provider.Raise(p => p.OrderCancelFailReceived += null, sub, of2);
		(cancelFail == of2).AssertTrue();

		// Canceled (simulate by Done + CancelledTime)
		Order canceledRes = null;
		order.WhenCanceled(provider.Object).Apply(container).Do(o => canceledRes = o);
		order.State = OrderStates.Done;
		order.CancelledTime = DateTime.UtcNow;
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(canceledRes == order).AssertTrue();

		// Matched
		Order matchedRes = null;
		order.WhenMatched(provider.Object).Apply(container).Do(o => matchedRes = o);
		order.State = OrderStates.Done;
		order.Balance = 0m;
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(matchedRes == order).AssertTrue();

		// Changed
		Order changedRes = null;
		order.WhenChanged(provider.Object).Apply(container).Do(o => changedRes = o);
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(changedRes == order).AssertTrue();

		// Edit failed
		OrderFail editFail = null;
		order.WhenEditFailed(provider.Object).Apply(container).Do(f => editFail = f);
		var of3 = new OrderFail { Order = order };
		provider.Raise(p => p.OrderEditFailReceived += null, sub, of3);
		(editFail == of3).AssertTrue();

#pragma warning disable CS0618
		// Edited (obsolete API path)
		var tx = new Mock<ITransactionProvider>(MockBehavior.Loose);
		Order editedRes = null;
		order.WhenEdited(tx.Object).Apply(container).Do(o => editedRes = o);
		tx.Raise(t => t.OrderEdited += null, 1L, order);
		(editedRes == order).AssertTrue();
#pragma warning restore CS0618

		// New trade
		MyTrade tradeRes = null;
		order.WhenNewTrade(provider.Object).Apply(container).Do(t => tradeRes = t);
		var mt = new MyTrade { Order = order, Trade = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 1m, TradeVolume = 3m } };
		provider.Raise(p => p.OwnTradeReceived += null, sub, mt);
		(tradeRes == mt).AssertTrue();

		// All trades
		IEnumerable<MyTrade> allRes = null;
		order.WhenAllTrades(provider.Object).Apply(container).Do(ts => allRes = ts);
		provider.Raise(p => p.OwnTradeReceived += null, sub, new MyTrade { Order = order, Trade = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 1m, TradeVolume = 7m } });
		provider.Raise(p => p.OrderReceived += null, sub, order);
		order.State = OrderStates.Done;
		provider.Raise(p => p.OwnTradeReceived += null, sub, new MyTrade { Order = order, Trade = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 1m, TradeVolume = 3m } });
		(allRes?.Sum(t => t.Trade.Volume)).AssertEqual(10m);

		// WhenRegistered – fires only once (rule .Once())
		int regCount = 0;
		var o2 = new Order();
		o2.WhenRegistered(provider.Object).Apply(container).Do(_ => regCount++);
		o2.State = OrderStates.Active;
		provider.Raise(p => p.OrderReceived += null, sub, o2);
		o2.State = OrderStates.Done;
		provider.Raise(p => p.OrderReceived += null, sub, o2);
		regCount.AssertEqual(1);
	}

	[TestMethod]
	public void PortfolioAndPositionRules()
	{
		var container = CreateContainer();
		var pfProvider = new Mock<IPortfolioProvider>(MockBehavior.Loose);
		var posProvider = new Mock<IPositionProvider>(MockBehavior.Loose);

		var pf = new Portfolio { Name = "P", CurrentValue = 100m };
		Portfolio pfChanged = null;
		pf.WhenChanged(pfProvider.Object).Apply(container).Do(p => pfChanged = p);
		pfProvider.Raise(p => p.PortfolioChanged += null, pf);
		(pfChanged == pf).AssertTrue();

		Portfolio pfLess = null;
		pf.WhenMoneyLess(pfProvider.Object, 90m).Apply(container).Do(p => pfLess = p);
		pf.CurrentValue = 80m;
		pfProvider.Raise(p => p.PortfolioChanged += null, pf);
		(pfLess == pf).AssertTrue();

		Portfolio pfMore = null;
		pf.CurrentValue = 100m;
		pf.WhenMoneyMore(pfProvider.Object, 110m).Apply(container).Do(p => pfMore = p);
		pf.CurrentValue = 120m;
		pfProvider.Raise(p => p.PortfolioChanged += null, pf);
		(pfMore == pf).AssertTrue();

		var pos = new Position
		{
			Security = Helper.CreateSecurity(),
			Portfolio = pf,
			CurrentValue = 10m
		};

		Position posLess = null;
		pos.WhenLess(posProvider.Object, 9m).Apply(container).Do(p => posLess = p);
		pos.CurrentValue = 8m;
		posProvider.Raise(p => p.PositionChanged += null, pos);
		(posLess == pos).AssertTrue();

		Position posMore = null;
		pos.WhenMore(posProvider.Object, 7m).Apply(container).Do(p => posMore = p);
		pos.CurrentValue = 12m;
		posProvider.Raise(p => p.PositionChanged += null, pos);
		(posMore == pos).AssertTrue();

		Position posChanged = null;
		pos.Changed(posProvider.Object).Apply(container).Do(p => posChanged = p);
		posProvider.Raise(p => p.PositionChanged += null, pos);
		(posChanged == pos).AssertTrue();
	}

	[TestMethod]
	public void SubscriptionRules()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.Ticks, sec) { TransactionId = 123 };

		Subscription started = null;
		sub.WhenSubscriptionStarted(provider.Object).Apply(container).Do(s => started = s);
		provider.Raise(p => p.SubscriptionStarted += null, sub);
		(started == sub).AssertTrue();

		Subscription online = null;
		sub.WhenSubscriptionOnline(provider.Object).Apply(container).Do(s => online = s);
		provider.Raise(p => p.SubscriptionOnline += null, sub);
		(online == sub).AssertTrue();

		(Subscription sub, Exception error)? stopped = null;
		sub.WhenSubscriptionStopped(provider.Object).Apply(container).Do(t => stopped = t);
		var ex = new Exception("stop");
		provider.Raise(p => p.SubscriptionStopped += null, sub, ex);
		(stopped?.sub == sub).AssertTrue();
		(stopped?.error == ex).AssertTrue();

		(Subscription sub, Exception error, bool isSubscribe)? failed = null;
		sub.WhenSubscriptionFailed(provider.Object).Apply(container).Do(t => failed = t);
		provider.Raise(p => p.SubscriptionFailed += null, sub, ex, true);
		(failed?.sub == sub).AssertTrue();
		(failed?.error == ex).AssertTrue();
		failed.Value.isSubscribe.AssertTrue();

		var l1 = new Level1ChangeMessage { SecurityId = sec.ToSecurityId() };
		Level1ChangeMessage l1Res = null;
		sub.WhenLevel1Received(provider.Object).Apply(container).Do(m => l1Res = m);
		provider.Raise(p => p.Level1Received += null, sub, l1);
		(l1Res == l1).AssertTrue();

		var ob = new QuoteChangeMessage
		{
			SecurityId = sec.ToSecurityId(),
			Bids = [new QuoteChange(101m, 1m, 1)],
			Asks = [new QuoteChange(102m, 1m, 1)]
		};

		IOrderBookMessage obRes = null;
		sub.WhenOrderBookReceived(provider.Object).Apply(container).Do(m => obRes = m);
		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		(obRes == ob).AssertTrue();

		IOrderBookMessage bestBidMore = null;
		sub.WhenBestBidPriceMore(provider.Object, 100m).Apply(container).Do(m => bestBidMore = m);
		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		(bestBidMore == ob).AssertTrue();

		IOrderBookMessage bestAskLess = null;
		sub.WhenBestAskPriceLess(provider.Object, 103m).Apply(container).Do(m => bestAskLess = m);
		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		(bestAskLess == ob).AssertTrue();

		var tick = new ExecutionMessage { SecurityId = sec.ToSecurityId(), TradePrice = 50m, TradeVolume = 1m, DataTypeEx = DataType.Ticks };
		ITickTradeMessage lastMore = null;
		sub.WhenLastTradePriceMore(provider.Object, 40m).Apply(container).Do(t => lastMore = t);
		provider.Raise(p => p.TickTradeReceived += null, sub, tick);
		(lastMore == tick).AssertTrue();

		var news = new News();
		News newsRes = null;
		sub.WhenNewsReceived(provider.Object).Apply(container).Do(n => newsRes = n);
		provider.Raise(p => p.NewsReceived += null, sub, news);
		(newsRes == news).AssertTrue();

		var order = new Order();
		Order orderRes = null;
		sub.WhenOrderReceived(provider.Object).Apply(container).Do(o => orderRes = o);
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(orderRes == order).AssertTrue();

		Order orderRegisteredRes = null;
		sub.WhenOrderRegistered(provider.Object).Apply(container).Do(o => orderRegisteredRes = o);
		provider.Raise(p => p.OrderReceived += null, sub, order);
		(orderRegisteredRes == order).AssertTrue();

		OrderFail orderFailReg = null;
		sub.WhenOrderFailReceived(provider.Object, true).Apply(container).Do(f => orderFailReg = f);
		var of1 = new OrderFail { Order = order };
		provider.Raise(p => p.OrderRegisterFailReceived += null, sub, of1);
		(orderFailReg == of1).AssertTrue();

		OrderFail orderEditFail = null;
		sub.WhenOrderEditFailReceived(provider.Object).Apply(container).Do(f => orderEditFail = f);
		var of2 = new OrderFail { Order = order };
		provider.Raise(p => p.OrderEditFailReceived += null, sub, of2);
		(orderEditFail == of2).AssertTrue();

		var pos = new Position { Security = sec, Portfolio = new Portfolio() };
		Position posRes = null;
		sub.WhenPositionReceived(provider.Object).Apply(container).Do(p => posRes = p);
		provider.Raise(p => p.PositionReceived += null, sub, pos);
		(posRes == pos).AssertTrue();

		var pf = new Portfolio();
		Portfolio pfRes = null;
		sub.WhenPortfolioReceived(provider.Object).Apply(container).Do(p => pfRes = p);
		provider.Raise(p => p.PortfolioReceived += null, sub, pf);
		(pfRes == pf).AssertTrue();

		// OrderLogReceived
		var ol = new ExecutionMessage { SecurityId = sec.ToSecurityId(), DataTypeEx = DataType.OrderLog };
		IOrderLogMessage olRes = null;
		sub.WhenOrderLogReceived(provider.Object).Apply(container).Do(m => olRes = m);
		provider.Raise(p => p.OrderLogReceived += null, sub, ol);
		(olRes == ol).AssertTrue();

		// TickTradeReceived
		ITickTradeMessage tRes = null;
		sub.WhenTickTradeReceived(provider.Object).Apply(container).Do(t => tRes = t);
		var tick2 = new ExecutionMessage { SecurityId = sec.ToSecurityId(), TradePrice = 10m, TradeVolume = 1m, DataTypeEx = DataType.Ticks };
		provider.Raise(p => p.TickTradeReceived += null, sub, tick2);
		(tRes == tick2).AssertTrue();

		// CandleReceived (non-generic and generic)
		ICandleMessage candAny = null;
		sub.WhenCandleReceived(provider.Object).Apply(container).Do(c => candAny = c);
		var anyCandle = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active };
		provider.Raise(p => p.CandleReceived += null, sub, anyCandle);
		(candAny == anyCandle).AssertTrue();

		TimeFrameCandleMessage candTyped = null;
		sub.WhenCandleReceived<TimeFrameCandleMessage>(provider.Object).Apply(container).Do(c => candTyped = c);
		var tfc = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active };
		provider.Raise(p => p.CandleReceived += null, sub, tfc);
		(candTyped == tfc).AssertTrue();

		// Provider overloads using default lookup subscriptions
		provider.SetupGet(p => p.OrderLookup).Returns(new Subscription(DataType.Transactions, sec));
		provider.SetupGet(p => p.PortfolioLookup).Returns(new Subscription(DataType.PositionChanges, sec));

		MyTrade ownTradeRes = null;
		provider.Object.WhenOwnTradeReceived().Apply(container).Do(t => ownTradeRes = t);
		var mt2 = new MyTrade { Order = order, Trade = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 1m, TradeVolume = 1m } };
		provider.Raise(p => p.OwnTradeReceived += null, provider.Object.OrderLookup, mt2);
		(ownTradeRes == mt2).AssertTrue();

		Order provOrderRes = null;
		provider.Object.WhenOrderReceived().Apply(container).Do(o => provOrderRes = o);
		provider.Raise(p => p.OrderReceived += null, provider.Object.OrderLookup, order);
		(provOrderRes == order).AssertTrue();

		Order provOrderRegRes = null;
		provider.Object.WhenOrderRegistered().Apply(container).Do(o => provOrderRegRes = o);
		provider.Raise(p => p.OrderReceived += null, provider.Object.OrderLookup, order);
		(provOrderRegRes == order).AssertTrue();

		Position provPosRes = null;
		provider.Object.WhenPositionReceived().Apply(container).Do(p => provPosRes = p);
		provider.Raise(p => p.PositionReceived += null, provider.Object.PortfolioLookup, pos);
		(provPosRes == pos).AssertTrue();

		Portfolio provPfRes = null;
		provider.Object.WhenPortfolioReceived().Apply(container).Do(p => provPfRes = p);
		provider.Raise(p => p.PortfolioReceived += null, provider.Object.PortfolioLookup, pf);
		(provPfRes == pf).AssertTrue();

		// Opposite price conditions to cover both branches
		IOrderBookMessage bestBidLess = null;
		sub.WhenBestBidPriceLess(provider.Object, 200m).Apply(container).Do(m => bestBidLess = m);
		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		(bestBidLess == ob).AssertTrue();

		IOrderBookMessage bestAskMore = null;
		sub.WhenBestAskPriceMore(provider.Object, 50m).Apply(container).Do(m => bestAskMore = m);
		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		(bestAskMore == ob).AssertTrue();

		ITickTradeMessage lastLess = null;
		sub.WhenLastTradePriceLess(provider.Object, 60m).Apply(container).Do(t => lastLess = t);
		provider.Raise(p => p.TickTradeReceived += null, sub, tick);
		(lastLess == tick).AssertTrue();
	}

	[TestMethod]
	public void CandleRules()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec) { TransactionId = 321 };

		// Series: started/changed/finished/all
		ICandleMessage started = null;
		provider.Object.WhenCandlesStarted<ICandleMessage>(sub).Apply(container).Do(c => started = c);
		var c1 = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active };
		provider.Raise(p => p.CandleReceived += null, sub, c1);
		(started == c1).AssertTrue();

		ICandleMessage changed = null;
		provider.Object.WhenCandlesChanged<ICandleMessage>(sub).Apply(container).Do(c => changed = c);
		provider.Raise(p => p.CandleReceived += null, sub, new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active });
		(changed is not null).AssertTrue();

		ICandleMessage finished = null;
		provider.Object.WhenCandlesFinished<ICandleMessage>(sub).Apply(container).Do(c => finished = c);
		provider.Raise(p => p.CandleReceived += null, sub, new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Finished });
		(finished is not null).AssertTrue();

		ICandleMessage anyCandle = null;
		provider.Object.WhenCandles<ICandleMessage>(sub).Apply(container).Do(c => anyCandle = c);
		provider.Raise(p => p.CandleReceived += null, sub, new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active });
		(anyCandle is not null).AssertTrue();

		// Single candle rules
		var sc = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 105m };

		TimeFrameCandleMessage chCandle = null;
		provider.Object.WhenChanged(sc).Apply(container).Do(c => chCandle = c);
		provider.Raise(p => p.CandleReceived += null, sub, sc);
		(chCandle == sc).AssertTrue();

		TimeFrameCandleMessage finCandle = null;
		provider.Object.WhenFinished(sc).Apply(container).Do(c => finCandle = c);
		var finishedMsg = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Finished };
		provider.Object.WhenFinished(finishedMsg).Apply(container).Do(c => finCandle = c);
		provider.Raise(p => p.CandleReceived += null, sub, finishedMsg);
		(finCandle is not null).AssertTrue();

		// Price-based rules
		TimeFrameCandleMessage more = null;
		provider.Object.WhenClosePriceMore(sc, 100m).Apply(container).Do(c => more = c);
		provider.Raise(p => p.CandleReceived += null, sub, sc);
		(more == sc).AssertTrue();

		TimeFrameCandleMessage less = null;
		sc.ClosePrice = 95m;
		provider.Object.WhenClosePriceLess(sc, 100m).Apply(container).Do(c => less = c);
		provider.Raise(p => p.CandleReceived += null, sub, sc);
		(less == sc).AssertTrue();

		// Partial finished (series and single). For TimeFrame candle, method allows Finished case.
		ICandleMessage partSeries = null;
		provider.Object.WhenPartiallyFinishedCandles<ICandleMessage>(sub, 50m).Apply(container).Do(c => partSeries = c);
		provider.Raise(p => p.CandleReceived += null, sub, new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Finished, TotalVolume = 60m });
		(partSeries is not null).AssertTrue();

		TimeFrameCandleMessage partSingle = null;
		var sc2 = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Finished, TotalVolume = 60m };
		provider.Object.WhenPartiallyFinished(sc2, 50m).Apply(container).Do(c => partSingle = c);
		provider.Raise(p => p.CandleReceived += null, sub, sc2);
		(partSingle is not null).AssertTrue();

		// Total volume more
		var sc3 = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, TotalVolume = 10m };
		TimeFrameCandleMessage volMore = null;
		provider.Object.WhenTotalVolumeMore(sc3, 5m).Apply(container).Do(c => volMore = c);
		// simulate update with higher total volume
		sc3.TotalVolume = 20m;
		provider.Raise(p => p.CandleReceived += null, sub, sc3);
		(volMore == sc3).AssertTrue();

		// Price boundary: equal to threshold does not activate
		TimeFrameCandleMessage eq = null;
		var sc4 = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 100m };
		provider.Object.WhenClosePriceMore(sc4, 100m).Apply(container).Do(c => eq = c);
		provider.Raise(p => p.CandleReceived += null, sub, sc4);
		(eq is null).AssertTrue();

		// Relative price: +5 and -5 from current
		var sc5 = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 200m };
		TimeFrameCandleMessage relMore = null;
		provider.Object.WhenClosePriceMore(sc5, 205).Apply(container).Do(c => relMore = c);
		sc5.ClosePrice = 210m;
		provider.Raise(p => p.CandleReceived += null, sub, sc5);
		(relMore == sc5).AssertTrue();

		TimeFrameCandleMessage relLess = null;
		var sc6 = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 200m };
		provider.Object.WhenClosePriceLess(sc6, 205).Apply(container).Do(c => relLess = c);
		sc6.ClosePrice = 190m;
		provider.Raise(p => p.CandleReceived += null, sub, sc6);
		(relLess == sc6).AssertTrue();
	}

	[TestMethod]
	public void PriceEdgesAndNoDuplicates()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.MarketDepth, sec);

		// BestBid/Less – equal to threshold does not activate
		var obEq = new QuoteChangeMessage
		{
			SecurityId = sec.ToSecurityId(),
			Bids = [new QuoteChange(100m, 1m, 1)],
			Asks = [new QuoteChange(101m, 1m, 1)]
		};
		IOrderBookMessage res = null;
		sub.WhenBestBidPriceMore(provider.Object, 100m).Apply(container).Do(m => res = m);
		provider.Raise(p => p.OrderBookReceived += null, sub, obEq);
		(res is null).AssertTrue();

		// LastTrade – equal to threshold does not activate
		var tick = new ExecutionMessage { SecurityId = sec.ToSecurityId(), TradePrice = 50m, TradeVolume = 1m, DataTypeEx = DataType.Ticks };
		ITickTradeMessage ltRes = null;
		sub.WhenLastTradePriceMore(provider.Object, 50m).Apply(container).Do(t => ltRes = t);
		provider.Raise(p => p.TickTradeReceived += null, sub, tick);
		(ltRes is null).AssertTrue();

		// CandlesStarted – repeating the same message does not trigger again
		int startedCount = 0;
		provider.Object.WhenCandlesStarted<ICandleMessage>(sub).Apply(container).Do(_ => startedCount++);
		var candle = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active };
		provider.Raise(p => p.CandleReceived += null, sub, candle);
		provider.Raise(p => p.CandleReceived += null, sub, candle); // same message
		startedCount.AssertEqual(1);
	}

	[TestMethod]
	public void SuspendResumeRules()
	{
		var container = CreateContainer();
		var r = new TestRule();
		r.Apply(container);
		bool fired = false;
		r.Do(_ => fired = true);
		container.SuspendRules();
		container.IsRulesSuspended.AssertTrue();
		r.Trigger(); // should not activate
		fired.AssertFalse();
		container.ResumeRules();
		container.IsRulesSuspended.AssertFalse();
		r.Trigger(); // now activates
		fired.AssertTrue();
		container.TryRemoveRule(r, false).AssertTrue();
	}

	[TestMethod]
	public void ExclusiveThreeRules()
	{
		var container = CreateContainer();
		var r1 = new TestRule().Apply(container);
		var r2 = new TestRule().Apply(container);
		var r3 = new TestRule().Apply(container);
		r1.Exclusive(r2);
		r1.Exclusive(r3);
		bool fired = false;
		r1.Do(_ => fired = true);
		((TestRule)r1).Trigger();
		fired.AssertTrue();
		container.Rules.Contains(r2).AssertFalse();
		container.Rules.Contains(r3).AssertFalse();
	}

	[TestMethod]
	public void TryRemoveInfiniteRuleWithCheck()
	{
		var container = CreateContainer();
		var r = new TestRule().Apply(container);
		r.Until(() => false); // infinite
		container.TryRemoveRule(r, true).AssertFalse();
		container.Rules.Contains(r).AssertTrue();
		// cleanup
		container.TryRemoveRule(r, false).AssertTrue();
	}

	[TestMethod]
	public void SubscriptionOrderFailCancelBranch()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.Ticks, sec);
		var order = new Order();
		OrderFail fail = null;
		sub.WhenOrderFailReceived(provider.Object, false).Apply(container).Do(f => fail = f);
		var of = new OrderFail { Order = order };
		provider.Raise(p => p.OrderCancelFailReceived += null, sub, of);
		(fail == of).AssertTrue();
	}

	[TestMethod]
	public void DoActivatedOverloads()
	{
		var container = CreateContainer();
		var r1 = new TestRule().Once().Apply(container);
		int captured = 0;
		r1.Do(() => 7).Activated<int>(v => captured = v);
		((TestRule)r1).Trigger();
		captured.AssertEqual(7);

		var r2 = new TestRule().Once().Apply(container);
		bool activated = false;
		r2.Do(() => { }).Activated(() => activated = true);
		((TestRule)r2).Trigger();
		activated.AssertTrue();
	}

	[TestMethod]
	public void CandleRulesPercentUnits()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);

		var scUp = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 100m };
		TimeFrameCandleMessage up = null;
		provider.Object.WhenClosePriceMore(scUp, new Unit(5m, UnitTypes.Percent)).Apply(container).Do(c => up = c);
		scUp.ClosePrice = 106m; // +6%
		provider.Raise(p => p.CandleReceived += null, sub, scUp);
		(up == scUp).AssertTrue();

		var scDown = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 100m };
		TimeFrameCandleMessage down = null;
		provider.Object.WhenClosePriceLess(scDown, new Unit(5m, UnitTypes.Percent)).Apply(container).Do(c => down = c);
		scDown.ClosePrice = 94m; // -6%
		provider.Raise(p => p.CandleReceived += null, sub, scDown);
		(down == scDown).AssertTrue();
	}

	[TestMethod]
	public void RuleLifecycleBasics()
	{
		var container = CreateContainer();
		var r = new TestRule();
		r.IsReady.AssertFalse();
		r.IsActive.AssertFalse();

		r = (TestRule)r.Apply(container);
		r.IsReady.AssertTrue();
		r.IsActive.AssertFalse();

		int cnt = 0;
		r.Do(_ => cnt++);
		r.Trigger();
		cnt.AssertEqual(1);

		// remove and ensure no further activations
		container.TryRemoveRule(r, false).AssertTrue();
		container.Rules.Contains(r).AssertFalse();
		Assert.ThrowsExactly<ObjectDisposedException>(() => r.Trigger());
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void PeriodicUntil_FinishFirst()
	{
		var container = CreateContainer();
		var r = new TestRule().Until(() => true).Apply(container);
		int cnt = 0;
		r.Do(_ => cnt++);
		((TestRule)r).Trigger();
		cnt.AssertEqual(1);
		// rule is finished after the first activation
		container.Rules.Contains(r).AssertFalse();
		Assert.ThrowsExactly<ObjectDisposedException>(() => ((TestRule)r).Trigger());
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void PeriodicUntil_DynamicFinishSecond()
	{
		var container = CreateContainer();
		var finish = false;
		var r = new TestRule().Until(() => finish).Apply(container);
		int cnt = 0;
		r.Do(_ => cnt++);

		((TestRule)r).Trigger(); // first, not finished
		cnt.AssertEqual(1);
		container.Rules.Contains(r).AssertTrue();

		finish = true;
		((TestRule)r).Trigger(); // second, finishes
		cnt.AssertEqual(2);
		container.Rules.Contains(r).AssertFalse();
	}

	[TestMethod]
	public void SelfRemovalInsideHandler()
	{
		var container = CreateContainer();
		var r = new TestRule().Apply(container).Once();
		int cnt = 0;
		r.Do(_ =>
		{
			cnt++;

			// can't remove active rule
			container.TryRemoveRule(r).AssertFalse();
			container.TryRemoveRule(r, false).AssertFalse();
		});
		((TestRule)r).Trigger();
		cnt.AssertEqual(1);
		container.Rules.Contains(r).AssertFalse();

		// already removed
		container.TryRemoveRule(r).AssertFalse();
		container.TryRemoveRule(r, false).AssertFalse();

		Assert.ThrowsExactly<ObjectDisposedException>(() => ((TestRule)r).Trigger());
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void DisposeStopsFurtherActivations()
	{
		var container = CreateContainer();
		var r = new TestRule().Apply(container);
		int cnt = 0;
		r.Do(_ => cnt++);
		((TestRule)r).Trigger();
		cnt.AssertEqual(1);
		r.Dispose();
		Assert.ThrowsExactly<ObjectDisposedException>(() => ((TestRule)r).Trigger());
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void SuspendBetweenAndParts()
	{
		var container = CreateContainer();
		var a = new TestRule();
		var b = new TestRule();
		int cnt = 0;
		a.And(b).Apply(container).Do(_ => cnt++);

		a.Trigger();
		cnt.AssertEqual(0);

		container.SuspendRules();
		b.Trigger(); // ignored
		cnt.AssertEqual(0);

		container.ResumeRules();
		b.Trigger(); // will work now
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void SuspendSpecificRule()
	{
		var container = CreateContainer();
		var r = new TestRule();
		r.Apply(container);
		int cnt = 0;
		r.Do(_ => cnt++);
		r.Suspend(true);
		r.Trigger();
		cnt.AssertEqual(0);
		r.Suspend(false);
		r.Trigger();
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void OrFive_NoDuplicateFire()
	{
		var container = CreateContainer();
		var r1 = new TestRule();
		var r2 = new TestRule();
		var r3 = new TestRule();
		var r4 = new TestRule();
		var r5 = new TestRule();

		int cnt = 0;
		var orRule = r1.Or(r2, r3, r4, r5).Once().Apply(container).Do(_ => cnt++);
		r3.Trigger();
		cnt.AssertEqual(1);
		container.Rules.Contains(orRule).AssertFalse();
		Assert.ThrowsExactly<ObjectDisposedException>(() => r4.Trigger());
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void AndFive_FiresOnce()
	{
		var container = CreateContainer();
		var r1 = new TestRule();
		var r2 = new TestRule();
		var r3 = new TestRule();
		var r4 = new TestRule();
		var r5 = new TestRule();

		int cnt = 0;
		r1.And(r2, r3, r4, r5).Apply(container).Do(_ => cnt++);
		r1.Trigger();
		r2.Trigger();
		r3.Trigger();
		cnt.AssertEqual(0);
		r4.Trigger();
		cnt.AssertEqual(0);
		r5.Trigger();
		cnt.AssertEqual(1);
		r1.Trigger();
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void NestedOrWithAnd()
	{
		var container = CreateContainer();
		var a = new TestRule();
		var b = new TestRule();
		var c = new TestRule();

		int cnt = 0;
		var andBC = b.And(c);
		// Ensure the outer OR rule is removed after first activation.
		var orRule = a.Or(andBC).Once().Apply(container).Do(_ => cnt++);

		b.Trigger();
		cnt.AssertEqual(0);
		c.Trigger(); // and(b,c) completes -> or fires
		cnt.AssertEqual(1);
		container.Rules.Contains(orRule).AssertFalse();
		Assert.ThrowsExactly<ObjectDisposedException>(() => a.Trigger());
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void AndSimultaneousActivation()
	{
		var container = CreateContainer();
		var a = new TestRule();
		var b = new TestRule();
		int cnt = 0;
		a.And(b).Apply(container).Do(_ => cnt++);
		// Fast sequence treated as "simultaneous"
		b.Trigger();
		a.Trigger();
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void DoWithRuleArgument()
	{
		var container = CreateContainer();
		var r = new TestRule().Apply(container);
		int cnt = 0;
		r.Do((rule, arg) =>
		{
			(rule == r).AssertTrue();
			(arg as string).AssertEqual("abc");
			cnt++;
		});
		((TestRule)r).Trigger("abc");
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void DoWithRuleArgumentAndResult()
	{
		var container = CreateContainer();
		var r = new TestRule().Once().Apply(container);
		int captured = 0;
		r.Do((rule, arg) =>
		{
			(rule == r).AssertTrue();
			return 42;
		}).Activated<int>(v => captured = v);
		((TestRule)r).Trigger();
		captured.AssertEqual(42);
	}

	[TestMethod]
	public void PercentEqualThresholdDoesNotActivate()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);

		var candle = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 100m };
		TimeFrameCandleMessage res = null;
		provider.Object.WhenClosePriceMore(candle, new Unit(5m, UnitTypes.Percent)).Apply(container).Do(c => res = c);
		candle.ClosePrice = 105m; // exactly +5%
		provider.Raise(p => p.CandleReceived += null, sub, candle);
		(res is null).AssertTrue();
	}

	[TestMethod]
	public void ExclusiveNullThrows()
	{
		var container = CreateContainer();
		var r = new TestRule().Apply(container);
		bool thrown = false;
		try
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			r.Exclusive(null);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	[TestMethod]
	public void TryRemoveNullRuleReturnsFalse()
	{
		var container = CreateContainer();
		Assert.ThrowsExactly<ArgumentNullException>(() => container.TryRemoveRule(null));
	}

	[TestMethod]
	public void OrderMatchedOnlyOnce()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sub = new Subscription(DataType.Ticks, Helper.CreateSecurity());
		var order = new Order { Volume = 10m, Balance = 10m };

		int cnt = 0;
		order.WhenMatched(provider.Object).Apply(container).Do(_ => cnt++);

		order.State = OrderStates.Done;
		order.Balance = 0m;
		provider.Raise(p => p.OrderReceived += null, sub, order);
		cnt.AssertEqual(1);

		// repeated events should not increase the counter
		provider.Raise(p => p.OrderReceived += null, sub, order);
		cnt.AssertEqual(1);

		order.Balance = 5m; // change after done should not affect
		provider.Raise(p => p.OrderReceived += null, sub, order);
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void CancelFailedThenCanceledFlow()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sub = new Subscription(DataType.Ticks, Helper.CreateSecurity());
		var order = new Order { Volume = 10m, Balance = 10m };

		int failCnt = 0;
		order.WhenCancelFailed(provider.Object).Apply(container).Do(_ => failCnt++);

		int canceledCnt = 0;
		order.WhenCanceled(provider.Object).Apply(container).Do(_ => canceledCnt++);

		var of = new OrderFail { Order = order };
		provider.Raise(p => p.OrderCancelFailReceived += null, sub, of);
		failCnt.AssertEqual(1);

		order.State = OrderStates.Done;
		order.CancelledTime = DateTime.UtcNow;
		provider.Raise(p => p.OrderReceived += null, sub, order);
		canceledCnt.AssertEqual(1);

		// repeat does not increase counters
		provider.Raise(p => p.OrderCancelFailReceived += null, sub, of);
		failCnt.AssertEqual(1);
		provider.Raise(p => p.OrderReceived += null, sub, order);
		canceledCnt.AssertEqual(1);
	}

	[TestMethod]
	public void SubscriptionStartedNotDuplicated()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.MarketDepth, sec);

		int cnt = 0;
		sub.WhenSubscriptionStarted(provider.Object).Once().Apply(container).Do(_ => cnt++);
		provider.Raise(p => p.SubscriptionStarted += null, sub);
		provider.Raise(p => p.SubscriptionStarted += null, sub);
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void SubscriptionStoppedWithoutError()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.Ticks, sec);

		(Subscription sub, Exception error)? stopped = null;
		sub.WhenSubscriptionStopped(provider.Object).Apply(container).Do(t => stopped = t);
		provider.Raise(p => p.SubscriptionStopped += null, sub, (Exception)null);
		(stopped?.sub == sub).AssertTrue();
		(stopped?.error == null).AssertTrue();
	}

	[TestMethod]
	public void SubscriptionFailedUnsubscribeBranch()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.Ticks, sec);

		(Subscription sub, Exception error, bool isSubscribe)? failed = null;
		sub.WhenSubscriptionFailed(provider.Object).Apply(container).Do(t => failed = t);
		var ex = new Exception("stop");
		provider.Raise(p => p.SubscriptionFailed += null, sub, ex, false);
		(failed?.sub == sub).AssertTrue();
		(failed?.error == ex).AssertTrue();
		failed.Value.isSubscribe.AssertFalse();
	}

	[TestMethod]
	public void VolumeCandlePartiallyFinishedSeries()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);

		VolumeCandleMessage fired = null;
		provider.Object.WhenPartiallyFinishedCandles<VolumeCandleMessage>(sub, 50m).Apply(container).Do(c => fired = c);
		provider.Raise(p => p.CandleReceived += null, sub, new VolumeCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Finished, TotalVolume = 60m });
		(fired is not null).AssertTrue();
	}

	[TestMethod]
	public void VolumeCandlePartiallyFinishedSingle()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var msg = new VolumeCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Finished, TotalVolume = 60m };
		VolumeCandleMessage fired = null;
		provider.Object.WhenPartiallyFinished(msg, 50m).Apply(container).Do(c => fired = c);
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);
		provider.Raise(p => p.CandleReceived += null, sub, msg);
		(fired == msg).AssertTrue();
	}

	[TestMethod]
	public void TotalVolumeMoreDoesNotReFireOnDecrease()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);
		var candle = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, TotalVolume = 12m };
		int cnt = 0;
		provider.Object.WhenTotalVolumeMore(candle, 10m).Apply(container).Do(_ => cnt++);
		provider.Raise(p => p.CandleReceived += null, sub, candle); // first fire
		cnt.AssertEqual(1);
		candle.TotalVolume = 9m; // decrease
		provider.Raise(p => p.CandleReceived += null, sub, candle);
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void OrderBook_BothConditionsSameUpdate()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.MarketDepth, sec);

		int bidCnt = 0, askCnt = 0;
		sub.WhenBestBidPriceMore(provider.Object, 100m).Apply(container).Do(_ => bidCnt++);
		sub.WhenBestAskPriceLess(provider.Object, 200m).Apply(container).Do(_ => askCnt++);

		var ob = new QuoteChangeMessage
		{
			SecurityId = sec.ToSecurityId(),
			Bids = [new QuoteChange(150m, 1m, 1)],
			Asks = [new QuoteChange(160m, 1m, 1)]
		};

		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		bidCnt.AssertEqual(1);
		askCnt.AssertEqual(1);
	}

	[TestMethod]
	public void OrderBook_EmptyDoesNotActivate()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.MarketDepth, sec);

		int cnt = 0;
		sub.WhenBestBidPriceMore(provider.Object, 1m).Apply(container).Do(_ => cnt++);
		sub.WhenBestAskPriceMore(provider.Object, 1m).Apply(container).Do(_ => cnt++);

		var ob = new QuoteChangeMessage
		{
			SecurityId = sec.ToSecurityId(),
			Bids = [],
			Asks = [],
		};

		provider.Raise(p => p.OrderBookReceived += null, sub, ob);
		cnt.AssertEqual(0);
	}

	[TestMethod]
	public void RuleStateTransitions()
	{
		var container = CreateContainer();
		var r = new TestRule();
		r.IsReady.AssertFalse();
		r.IsActive.AssertFalse();
		r = (TestRule)r.Apply(container);
		r.IsReady.AssertTrue();
		r.IsActive.AssertFalse();
		bool fired = false;
		r.Do(_ => fired = true);
		r.Trigger();
		fired.AssertTrue();
		r.IsActive.AssertFalse(); // not active after activation completes
	}

	[TestMethod]
	public void DoubleApplyIgnored()
	{
		var container = CreateContainer();
		var r = new TestRule();
		var first = r.Apply(container);
		var second = first.Apply(container); // repeat Apply
		(first == second).AssertTrue();
		container.Rules.Contains(first).AssertTrue();
	}

	[TestMethod]
	public void DisposeBeforeActivation()
	{
		var container = CreateContainer();
		var r = new TestRule().Apply(container);
		bool fired = false;
		r.Do(_ => fired = true);
		r.Dispose();
		Assert.ThrowsExactly<ObjectDisposedException>(() => ((TestRule)r).Trigger());
		fired.AssertFalse();
	}

	[TestMethod]
	public void PeriodicUntilMultiActivations()
	{
		var container = CreateContainer();
		int cnt = 0;
		bool finish = false;
		var r = new TestRule().Until(() => finish).Apply(container).Do(_ => cnt++);
		((TestRule)r).Trigger();
		((TestRule)r).Trigger();
		cnt.AssertEqual(2);
		finish = true;
		((TestRule)r).Trigger(); // final activation
		cnt.AssertEqual(3);
		container.Rules.Contains(r).AssertFalse();
	}

	[TestMethod]
	public void ExclusiveChainAllRemovedAfterFirstFire()
	{
		var container = CreateContainer();
		var a = new TestRule().Apply(container);
		var b = new TestRule().Apply(container);
		var c = new TestRule().Apply(container);
		var d = new TestRule().Apply(container);
		// chain: a is exclusive to b, c, d
		a.Exclusive(b);
		a.Exclusive(c);
		a.Exclusive(d);
		bool fired = false;
		a.Do(_ => fired = true);
		((TestRule)a).Trigger();
		fired.AssertTrue();
		container.Rules.Contains(b).AssertFalse();
		container.Rules.Contains(c).AssertFalse();
		container.Rules.Contains(d).AssertFalse();
	}

	[TestMethod]
	public void ExclusiveCrossRemoval()
	{
		var container = CreateContainer();
		var a = new TestRule().Apply(container);
		var b = new TestRule().Apply(container);
		var c = new TestRule().Apply(container);
		// bidirectional links
		a.Exclusive(b);
		b.Exclusive(c);
		bool af = false, bf = false, cf = false;
		a.Do(_ => af = true);
		b.Do(_ => bf = true);
		c.Do(_ => cf = true);
		// activate the middle
		((TestRule)b).Trigger();
		bf.AssertTrue();
		container.Rules.Contains(a).AssertFalse();
		container.Rules.Contains(c).AssertFalse();
	}

	[TestMethod]
	public void SuspendPreventsCombinedOrAndActivation()
	{
		var container = CreateContainer();
		var a = new TestRule();
		var b = new TestRule();
		var c = new TestRule();
		int cnt = 0;
		var combo = a.Or(b.And(c)).Once().Apply(container).Do(_ => cnt++);
		container.SuspendRules();
		b.Trigger();
		c.Trigger();
		a.Trigger();
		cnt.AssertEqual(0);
		container.ResumeRules();
		// complete and(b,c)
		b.Trigger();
		c.Trigger(); // and is ready -> or activates
		cnt.AssertEqual(1);
		container.Rules.Contains(combo).AssertFalse();
	}

	[TestMethod]
	public void SuspendBetweenMultiStepAnd()
	{
		var container = CreateContainer();
		var a = new TestRule();
		var b = new TestRule();
		var c = new TestRule();
		int cnt = 0;
		a.And(b, c).Apply(container).Do(_ => cnt++);
		a.Trigger();
		b.Trigger();
		cnt.AssertEqual(0);
		container.SuspendRules();
		c.Trigger(); // ignored
		cnt.AssertEqual(0);
		container.ResumeRules();
		c.Trigger();
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void LastTradePriceOnceRule()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(DataType.Ticks, sec);
		int cnt = 0;
		sub.WhenLastTradePriceMore(provider.Object, 10m).Once().Apply(container).Do(_ => cnt++);
		var tick = new ExecutionMessage { SecurityId = sec.ToSecurityId(), TradePrice = 15m, TradeVolume = 1m, DataTypeEx = DataType.Ticks };
		provider.Raise(p => p.TickTradeReceived += null, sub, tick);
		provider.Raise(p => p.TickTradeReceived += null, sub, tick);
		cnt.AssertEqual(1);
	}

	[TestMethod]
	public void ClosePriceLessEqualBoundaryNoFire()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);
		var candle = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 100m };
		TimeFrameCandleMessage res = null;
		provider.Object.WhenClosePriceLess(candle, 100m).Apply(container).Do(c => res = c);
		provider.Raise(p => p.CandleReceived += null, sub, candle);
		(res is null).AssertTrue();
	}

	[TestMethod]
	public void RelativePricePercentExactBoundaryNoFire()
	{
		var container = CreateContainer();
		var provider = new Mock<ISubscriptionProvider>(MockBehavior.Loose);
		var sec = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), sec);
		var candle = new TimeFrameCandleMessage { SecurityId = sec.ToSecurityId(), State = CandleStates.Active, ClosePrice = 200m };
		TimeFrameCandleMessage more = null, less = null;
		provider.Object.WhenClosePriceMore(candle, new Unit(5m, UnitTypes.Percent)).Apply(container).Do(c => more = c);
		provider.Object.WhenClosePriceLess(candle, new Unit(5m, UnitTypes.Percent)).Apply(container).Do(c => less = c);
		// exactly +5% and -5% should not activate
		candle.ClosePrice = 210m; // +5%
		provider.Raise(p => p.CandleReceived += null, sub, candle);
		(more is null).AssertTrue();
		candle.ClosePrice = 190m; // -5%
		provider.Raise(p => p.CandleReceived += null, sub, candle);
		(less is null).AssertTrue();
	}

	[TestMethod]
	public void NoContainer()
	{
		var r = new TestRule();
		Assert.ThrowsExactly<InvalidOperationException>(() => r.Trigger());
	}

	[TestMethod]
	public void Disposed()
	{
		var r = new TestRule();
		r.Dispose();
		r.IsReady.AssertFalse();
		Assert.ThrowsExactly<ObjectDisposedException>(() => r.Trigger());
	}
}