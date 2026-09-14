namespace StockSharp.Tests;

using StockSharp.Diagram;
using StockSharp.Diagram.Elements;

/// <summary>
/// A diagram strategy is two promises. The first is a hand-off: the composition field can take settings
/// before there is a composition to put them in, and once one has taken them the field holds nothing.
/// The second is that a wired-up composition actually runs - a value entering one element comes out of
/// the last one, an order element turns a trigger into a real order and reports back what became of it,
/// a sync element holds a value until its partners arrive, and a graph that was saved and read back runs
/// to the same answer as the one that was saved.
/// </summary>
[TestClass]
public class DiagramStrategyRoundTripTests : BaseTestClass
{
	private static readonly DateTime _time = new(2026, 9, 9, 10, 0, 0, DateTimeKind.Utc);

	private const string _inputName = "input";
	private const string _thresholdName = "threshold";

	private static string SocketId(StaticSocketIds id) => id.ToString();

	private static CompositionDiagramElement NewComposition(string name)
		=> new(new CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink>(new InMemoryCompositionModelBehavior()))
		{
			Name = name,
		};

	/// <summary>
	/// A root that emits exactly what a test hands it, so what is measured afterwards is the graph's
	/// own behaviour and not a source's.
	/// </summary>
	private class SourceDiagramElement : DiagramElement
	{
		private readonly DiagramSocket _output;

		public SourceDiagramElement()
			: this(DiagramSocketType.Any)
		{
		}

		public SourceDiagramElement(DiagramSocketType type)
		{
			_output = AddOutput(StaticSocketIds.Output, "Out", type);
		}

		public override Guid TypeId { get; } = "1F0A5D4C-3B77-4A21-9C8E-5D2B7A6E1042".To<Guid>();

		public override string IconName { get; } = "Pi";

		public void Emit(DateTime time, object value) => RaiseProcessOutput(_output, time, value);
	}

	/// <summary>
	/// A leaf that keeps what reached it, so a test reads an output the way the next element in the
	/// diagram would - a socket handler, one value at a time.
	/// </summary>
	private class RecordingDiagramElement : DiagramElement
	{
		public RecordingDiagramElement()
		{
			AddInput(StaticSocketIds.Input, "In", DiagramSocketType.Any, v =>
			{
				AssertCameFromASocketThatCarriesIt(v);
				Received.Add(v);
			});
		}

		/// <summary>
		/// A socket states what it carries, and everything wired behind it is entitled to that. An
		/// element that emits something else hands the next element a value it will cast - the cast
		/// throws at a point that names neither the element that emitted nor the socket it left by.
		/// Checked here so every test that records an output checks it, rather than each one
		/// remembering to.
		/// </summary>
		private static void AssertCameFromASocketThatCarriesIt(DiagramSocketValue value)
		{
			// The value arriving at this input was made when it crossed the link; the one it was made
			// from is the value as the upstream element emitted it, under the socket it emitted from.
			var emitted = value.Source;

			if (emitted?.Socket?.Type is not DiagramSocketType declared)
				return;

			// Any is the socket that promises nothing, and a socket carrying nothing breaks no promise.
			if (declared == DiagramSocketType.Any || emitted.Value is null)
				return;

			var emittedType = emitted.Value.GetType();

			// The Unit socket is the numeric one: GetSocketType puts every number on it, so a decimal
			// or an int leaving it is what it carries rather than a promise broken.
			if (declared == DiagramSocketType.Unit)
			{
				(emitted.Value is Unit || (emittedType.IsNumeric() && !emittedType.IsEnum())).AssertTrue(
					$"socket '{emitted.Socket}' carries numbers, and it emitted {emittedType.Name}");

				return;
			}

			declared.Type.IsInstanceOfType(emitted.Value).AssertTrue(
				$"socket '{emitted.Socket}' is declared as {declared.Type.Name}, so what leaves it must be one - it emitted {emittedType.Name}");
		}

		public override Guid TypeId { get; } = "6E3C9A18-24D5-4B0F-8E77-C1A93F5B2D66".To<Guid>();

		public override string IconName { get; } = "Pi";

		public List<DiagramSocketValue> Received { get; } = [];
	}

	/// <summary>
	/// A composition attached to a strategy, with the two calls a test needs to put elements and links
	/// into it.
	/// </summary>
	private sealed class Graph
	{
		public Graph()
		{
			Model = new(new InMemoryCompositionModelBehavior());
			Composition = new(Model);
			Strategy = new() { Composition = Composition };
		}

		public CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink> Model { get; }
		public CompositionDiagramElement Composition { get; }
		public DiagramStrategy Strategy { get; }

		public InMemoryCompositionModelNode Add(DiagramElement element)
		{
			var node = new InMemoryCompositionModelNode { Element = element };
			Model.AddNode(node);
			return node;
		}

		public void Link(InMemoryCompositionModelNode from, string fromSocket, InMemoryCompositionModelNode to, string toSocket)
			=> Model.AddLink(from, fromSocket, to, toSocket);

		public void Unlink(InMemoryCompositionModelNode from, string fromSocket, InMemoryCompositionModelNode to, string toSocket)
			=> Model.RemoveLink(from, fromSocket, to, toSocket);
	}

	/// <summary>
	/// A clone is loaded before its diagram is copied in, so the settings have nowhere to go when
	/// they arrive. They have to wait rather than be dropped, or the composition that turns up
	/// afterwards is empty.
	/// </summary>
	[TestMethod]
	public void SettingsThatArriveBeforeACompositionAreGivenToItWhenItComes()
	{
		var saved = new SettingsStorage();

		var source = new DiagramStrategy { Composition = NewComposition("the one that was saved") };
		source.Save(saved);

		var loaded = new DiagramStrategy();
		loaded.Load(saved);

		loaded.Composition = NewComposition("the one that turned up later");

		loaded.Composition.Name.AssertEqual("the one that was saved",
			"settings loaded before a composition existed have to reach the composition that arrives");
	}

	/// <summary>
	/// Saving is a read of the strategy. A copy of the composition left behind by a save outlives
	/// what it described, and the next composition to arrive would be loaded from it.
	/// </summary>
	[TestMethod]
	public void SavingLeavesNothingBehindForTheNextCompositionToSwallow()
	{
		var source = new DiagramStrategy { Composition = NewComposition("first") };
		source.Save(new SettingsStorage());

		source.Composition = NewComposition("second");

		source.Composition.Name.AssertEqual("second",
			"a save must not leave settings that the next composition is then loaded from");
	}

	#region A running graph

	// Two roots feed a comparison, the comparison feeds a negation, and the negation is the only
	// element whose socket the composition publishes - so whatever arrives at the composition's
	// output has passed through every element on the way.
	private static CompositionDiagramElement BuildThresholdComposition()
	{
		var graph = new Graph();

		var input = new SourceDiagramElement { Name = _inputName };
		var threshold = new SourceDiagramElement { Name = _thresholdName };
		var comparison = new ComparisonDiagramElement { Operator = ComparisonOperator.Greater };
		var negation = new LogicalConditionDiagramElement
		{
			Operator = LogicalConditionDiagramElement.Condition.Not,
			ShowSockets = true,
		};

		var inputNode = graph.Add(input);
		var thresholdNode = graph.Add(threshold);
		var comparisonNode = graph.Add(comparison);
		var negationNode = graph.Add(negation);

		graph.Link(inputNode, SocketId(StaticSocketIds.Output), comparisonNode, SocketId(StaticSocketIds.Input));
		graph.Link(thresholdNode, SocketId(StaticSocketIds.Output), comparisonNode, SocketId(StaticSocketIds.SecondInput));
		graph.Link(comparisonNode, SocketId(StaticSocketIds.Signal), negationNode, negation.InputSockets.First().Id);

		return graph.Composition;
	}

	private static SourceDiagramElement FindSource(CompositionDiagramElement composition, string name)
		=> composition.Elements.OfType<SourceDiagramElement>().First(e => e.Name == name);

	private static List<DiagramSocketValue> RunThreshold(CompositionDiagramElement composition, decimal input, decimal threshold)
	{
		var outputs = new List<DiagramSocketValue>();
		composition.ProcessOutput += outputs.Add;

		composition.Prepare();
		composition.Start(_time);

		FindSource(composition, _inputName).Emit(_time, new Unit(input));
		FindSource(composition, _thresholdName).Emit(_time, new Unit(threshold));

		return outputs;
	}

	/// <summary>
	/// The point of a diagram: a value put into one element comes out of the last one, changed by
	/// every element it passed. Two runs, because a graph that always answers the same thing has
	/// carried nothing.
	/// </summary>
	[TestMethod]
	public void A_graph_carries_a_value_through_every_element_to_its_output()
	{
		var above = RunThreshold(BuildThresholdComposition(), 10, 5);

		above.Count.AssertEqual(1, "one pass of the inputs must put exactly one value out");
		above[0].GetValue<bool>().AssertFalse("10 is above 5, and the negation of that is what the last element must emit");

		var below = RunThreshold(BuildThresholdComposition(), 3, 5);

		below.Count.AssertEqual(1, "one pass of the inputs must put exactly one value out");
		below[0].GetValue<bool>().AssertTrue("3 is not above 5, and the negation of that is what the last element must emit");
	}

	/// <summary>
	/// A graph is worth nothing if it only runs in the session that built it. What was saved must come
	/// back with every element and link it had, and answer the same thing when it is run again.
	/// </summary>
	[TestMethod]
	public void A_saved_graph_restores_and_runs_to_the_same_result()
	{
		var registry = new CompositionRegistry<InMemoryCompositionModelNode, InMemoryCompositionModelLink>(() => new InMemoryCompositionModelBehavior());

		registry.DiagramElements.Add(new SourceDiagramElement());
		registry.DiagramElements.Add(new ComparisonDiagramElement());
		registry.DiagramElements.Add(new LogicalConditionDiagramElement());

		var saved = registry.Serialize(BuildThresholdComposition()).SerializeInvariant();

		var restored = registry.Deserialize(saved.DeserializeInvariant(), ICompositionRegistryExtensions.NotSupported).element;

		restored.Elements.Count().AssertEqual(4, "a saved graph must come back with every element it had");

		// The restored composition needs a strategy of its own before it can run.
		var restoredStrategy = new DiagramStrategy { Composition = restored };
		restoredStrategy.Composition.AssertSame(restored, "the restored graph must be the one the strategy runs");

		var expected = RunThreshold(BuildThresholdComposition(), 10, 5);
		var actual = RunThreshold(restored, 10, 5);

		expected.Count.AssertEqual(1, "the graph that was saved puts out one value for one pass of its inputs");
		actual.Count.AssertEqual(expected.Count, "a restored graph must put out as many values as the graph that was saved");
		actual[0].GetValue<bool>().AssertEqual(expected[0].GetValue<bool>(),
			"a restored graph must answer what the graph that was saved answered");
	}

	#endregion

	#region An order element

	private sealed class OrderRun
	{
		public DiagramStrategy Strategy { get; set; }
		public CompositionDiagramElement Composition { get; set; }
		public OrderRegisterDiagramElement Register { get; set; }
		public SourceDiagramElement Trigger { get; set; }
		public SourceDiagramElement Volume { get; set; }
		public SourceDiagramElement Price { get; set; }
		public RecordingDiagramElement Registered { get; set; }
		public RecordingDiagramElement Failed { get; set; }
		public RecordingDiagramElement Traded { get; set; }
		public RecordingDiagramElement Matched { get; set; }
		public RecordingDiagramElement Finished { get; set; }
		public RecordingDiagramElement Cancelled { get; set; }
		public List<Order> Submitted { get; set; }
		public Subscription Orders { get; set; }
	}

	// Every output of the register element is wired to a leaf: the element only reports what someone
	// downstream is listening for, so an unwired socket would report nothing at all. A limit element is
	// wired to a price source as well - it refuses to start without one.
	private async Task<OrderRun> StartOrderGraphAsync(Sides direction, bool isMarket, Action<OrderRegisterDiagramElement> configure)
	{
		var graph = new Graph();

		var trigger = new SourceDiagramElement();
		var volume = new SourceDiagramElement();
		var price = new SourceDiagramElement();
		var register = new OrderRegisterDiagramElement
		{
			Direction = direction,
			IsMarket = isMarket,
			OnlineOnly = false,
		};

		configure(register);

		var registered = new RecordingDiagramElement();
		var failed = new RecordingDiagramElement();
		var traded = new RecordingDiagramElement();
		var matched = new RecordingDiagramElement();
		var finished = new RecordingDiagramElement();
		var cancelled = new RecordingDiagramElement();

		var triggerNode = graph.Add(trigger);
		var volumeNode = graph.Add(volume);
		var priceNode = graph.Add(price);
		var registerNode = graph.Add(register);
		var registeredNode = graph.Add(registered);
		var failedNode = graph.Add(failed);
		var tradedNode = graph.Add(traded);
		var matchedNode = graph.Add(matched);
		var finishedNode = graph.Add(finished);
		var cancelledNode = graph.Add(cancelled);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(triggerNode, outputId, registerNode, SocketId(StaticSocketIds.Trigger));
		graph.Link(volumeNode, outputId, registerNode, SocketId(StaticSocketIds.Volume));

		if (!isMarket)
			graph.Link(priceNode, outputId, registerNode, SocketId(StaticSocketIds.Price));

		graph.Link(registerNode, SocketId(StaticSocketIds.Order), registeredNode, inputId);
		graph.Link(registerNode, SocketId(StaticSocketIds.OrderFail), failedNode, inputId);
		graph.Link(registerNode, SocketId(StaticSocketIds.MyTrade), tradedNode, inputId);
		graph.Link(registerNode, "Matched", matchedNode, inputId);
		graph.Link(registerNode, "Finished", finishedNode, inputId);
		graph.Link(registerNode, "Cancelled", cancelledNode, inputId);

		var idGenerator = new IncrementalIdGenerator();
		var submitted = new List<Order>();

		var connector = new Mock<IConnector>();
		connector.Setup(c => c.TransactionIdGenerator).Returns(idGenerator);
		connector
			.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o =>
			{
				// The transaction id is stamped on the way out, and the strategy tracks the order by it.
				o.TransactionId = idGenerator.GetNextId();
				submitted.Add(o);
			});

		var strategy = graph.Strategy;

		strategy.Connector = connector.Object;
		strategy.Security = Helper.CreateSecurity();
		strategy.Portfolio = Helper.CreatePortfolio();

		await strategy.StartAsync(CancellationToken);
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		var orders = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(orders);

		return new OrderRun
		{
			Strategy = strategy,
			Composition = graph.Composition,
			Register = register,
			Trigger = trigger,
			Volume = volume,
			Price = price,
			Registered = registered,
			Failed = failed,
			Traded = traded,
			Matched = matched,
			Finished = finished,
			Cancelled = cancelled,
			Submitted = submitted,
			Orders = orders,
		};
	}

	// A limit element is told its price the way a diagram tells it: on the price socket, before the
	// trigger that turns the whole set into an order.
	private static void Emit(OrderRun run, decimal volume, decimal? price, DateTime time)
	{
		if (price is decimal p)
			run.Price.Emit(time, p);

		run.Volume.Emit(time, volume);
		run.Trigger.Emit(time, true);
	}

	private static Order Fire(OrderRun run, decimal volume, decimal? price)
	{
		Emit(run, volume, price, _time);

		run.Submitted.Count.AssertEqual(1, "a trigger with a volume must produce one order");

		var order = run.Submitted[0];
		order.ServerTime = _time;
		return order;
	}

	private static void Activate(OrderRun run, Order order)
	{
		order.State = OrderStates.Active;
		order.Balance = order.Volume;
		run.Strategy.OnConnectorOrderReceived(run.Orders, order);
	}

	/// <summary>
	/// What the element is configured with is what the broker must be asked for. Direction, volume and
	/// order type are the whole of the instruction, and none of them may be quietly replaced.
	/// </summary>
	[TestMethod]
	public async Task An_order_element_registers_the_order_the_graph_asked_for()
	{
		var run = await StartOrderGraphAsync(Sides.Sell, isMarket: true, _ => { });

		var order = Fire(run, 3m, price: null);

		order.Side.AssertEqual(Sides.Sell, "the order must take the direction the element was configured with");
		order.Volume.AssertEqual(3m, "the order must take the volume that arrived on the volume socket");
		order.Type.AssertEqual(OrderTypes.Market, "an element set to market must register a market order");
		order.Security.AssertSame(run.Strategy.Security, "with no security wired in, the strategy's own security is the one to use");
		order.Portfolio.AssertSame(run.Strategy.Portfolio, "with no portfolio wired in, the strategy's own portfolio is the one to use");
	}

	/// <summary>
	/// Registration and a fill are the two things a diagram downstream of an order waits for, and each
	/// has its own socket. Both must carry the thing the socket is named after.
	/// </summary>
	[TestMethod]
	public async Task A_registered_order_and_its_trade_reach_the_order_and_trade_outputs()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		var order = Fire(run, 3m, price: null);

		Activate(run, order);

		run.Registered.Received.Count.AssertEqual(1, "an order that reached the exchange must be reported once");
		run.Registered.Received[0].Value.AssertSame(order, "the order output must carry the order that was registered");

		var trade = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = order.Volume,
				SecurityId = order.Security.ToSecurityId(),
				ServerTime = _time,
			},
		};

		run.Strategy.OnTradeReceived(run.Orders, trade);

		run.Traded.Received.Count.AssertEqual(1, "a fill must be reported once on the trade output");
		run.Traded.Received[0].Value.AssertSame(trade, "the trade output must carry the trade that filled the order");
	}

	/// <summary>
	/// The matched socket is declared as an order socket, so a downstream element - the cancel element
	/// casts its order input straight to <see cref="Order"/> - is entitled to an order on it, not the
	/// trade that happened to complete the fill.
	/// </summary>
	[TestMethod]
	public async Task The_matched_output_carries_the_order_that_filled()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		var order = Fire(run, 3m, price: null);

		Activate(run, order);

		run.Strategy.OnTradeReceived(run.Orders, new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = order.Volume,
				SecurityId = order.Security.ToSecurityId(),
				ServerTime = _time,
			},
		});

		run.Matched.Received.Count.AssertEqual(1, "an order filled in full must be reported once as matched");
		run.Matched.Received[0].Value.AssertOfType<Order>(
			"the matched socket is typed as an order socket, so what leaves it must be an order");
		run.Matched.Received[0].Value.AssertSame(order, "the matched output must carry the order that filled");
	}

	/// <summary>
	/// Finishing happens once. A final order is delivered again whenever a lookup answers or a session
	/// reconnects, and a diagram that acts on the finished socket would act twice on the same order.
	/// </summary>
	[TestMethod]
	public async Task A_cancelled_order_finishes_once_however_often_it_is_reported()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		var order = Fire(run, 3m, price: null);

		Activate(run, order);

		// The broker reports the cancellation: done, with the volume still unfilled.
		order.State = OrderStates.Done;
		order.CancelledTime = _time.AddSeconds(1);
		run.Strategy.OnConnectorOrderReceived(run.Orders, order);

		// And reports the same final order once more.
		run.Strategy.OnConnectorOrderReceived(run.Orders, order);

		run.Cancelled.Received.Count.AssertEqual(1, "a cancellation must be reported once, however often the final order arrives");
		run.Cancelled.Received[0].Value.AssertSame(order, "the cancelled output must carry the order that was cancelled");

		run.Finished.Received.Count.AssertEqual(1, "an order finishes once, however often the final order arrives");
		run.Finished.Received[0].Value.AssertSame(order, "the finished output must carry the order that finished");
	}

	/// <summary>
	/// A limit order is a price and nothing else distinguishes it from a market one. The price the diagram
	/// put on the price socket is the price the broker must be asked for: a limit order registered at some
	/// other price, or as a market order, buys at whatever the book offers.
	/// </summary>
	[TestMethod]
	public async Task A_limit_order_is_registered_at_the_price_that_arrived_on_the_price_socket()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: false, _ => { });

		var order = Fire(run, 3m, price: 101m);

		order.Type.AssertEqual(OrderTypes.Limit, "an element that was not set to market must register a limit order");
		order.Price.AssertEqual(101m, "the limit price must be the one that arrived on the price socket");
		order.Volume.AssertEqual(3m, "the order must take the volume that arrived on the volume socket");
		order.Side.AssertEqual(Sides.Buy, "the order must take the direction the element was configured with");
	}

	/// <summary>
	/// An exchange refuses a price that is not a multiple of the instrument's step, so the element rounds
	/// one to the nearest step it will accept. Sent as it came, the order is rejected and the diagram's
	/// whole plan stops at a price nobody could have entered.
	/// </summary>
	[TestMethod]
	public async Task A_price_off_the_instruments_step_is_rounded_to_one_the_exchange_accepts()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: false, r => r.ShrinkPrice = true);

		// A step of 0.1 is a price quoted to one decimal; 100.17 is not a price this instrument has.
		run.Strategy.Security.Decimals = 1;

		var order = Fire(run, 1m, price: 100.17m);

		order.Price.AssertEqual(100.2m, "a price off the step must be rounded to the nearest price the instrument has");
	}

	/// <summary>
	/// Rounding is the element's offer, not its habit. Turned off, the price goes out exactly as the diagram
	/// computed it - an author who rounds for himself must not have his number quietly moved.
	/// </summary>
	[TestMethod]
	public async Task With_rounding_turned_off_the_price_is_registered_exactly_as_it_arrived()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: false, r => r.ShrinkPrice = false);

		run.Strategy.Security.Decimals = 1;

		var order = Fire(run, 1m, price: 100.17m);

		order.Price.AssertEqual(100.17m, "with rounding turned off the price must reach the broker untouched");
	}

	/// <summary>
	/// Zero is a legitimate limit price on some boards and a way of saying "at the market" on others, so the
	/// element asks which one is meant. Guessing turns a resting order into an immediate fill, or the other
	/// way round.
	/// </summary>
	[TestMethod]
	public async Task A_zero_price_is_a_market_order_only_where_the_element_says_zero_means_market()
	{
		var asMarket = await StartOrderGraphAsync(Sides.Buy, isMarket: false, r => r.ZeroAsMarket = true);

		Fire(asMarket, 1m, price: 0m).Type.AssertEqual(OrderTypes.Market,
			"told that a zero price means the market, the element must register a market order");

		var asLimit = await StartOrderGraphAsync(Sides.Buy, isMarket: false, r => r.ZeroAsMarket = false);

		var order = Fire(asLimit, 1m, price: 0m);

		order.Type.AssertEqual(OrderTypes.Limit, "without that, a zero price is a limit price and must stay a limit order");
		order.Price.AssertEqual(0m, "the limit price must be the zero the diagram sent");
	}

	/// <summary>
	/// The order type is decided when the order is placed, not when the diagram was drawn. An element
	/// switched to market keeps sending limit orders otherwise - at a price it is no longer being given.
	/// </summary>
	[TestMethod]
	public async Task The_order_type_follows_what_the_element_says_when_the_trigger_arrives()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: false, _ => { });

		run.Register.IsMarket = true;

		var order = Fire(run, 1m, price: null);

		order.Type.AssertEqual(OrderTypes.Market, "an element switched to market must register a market order");
		order.Volume.AssertEqual(1m, "the volume is unaffected by the switch");
	}

	/// <summary>
	/// The extra conditions are instructions to the broker: how long the order lives, how much slippage is
	/// allowed, whose account code it carries. Dropped on the way out, the order is a different order from
	/// the one the diagram describes, and nothing says so.
	/// </summary>
	[TestMethod]
	public async Task Every_extra_instruction_the_element_carries_travels_on_the_order()
	{
		var expiry = _time.AddDays(1);

		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, r =>
		{
			r.ExpiryDate = expiry;
			r.Slippage = 0.5m;
			r.TimeInForce = TimeInForce.MatchOrCancel;
			r.Comment = "from the diagram";
			r.ClientCode = "client";
			r.BrokerCode = "broker";
			r.IsManual = true;
			r.IsMarketMaker = true;
			r.MarginMode = MarginModes.Cross;
		});

		var order = Fire(run, 1m, price: null);

		order.ExpiryDate.AssertEqual(expiry, "the order must expire when the element says it expires");
		order.Slippage.AssertEqual(0.5m, "the allowed slippage must travel on the order");
		order.TimeInForce.AssertEqual(TimeInForce.MatchOrCancel, "the time in force must travel on the order");
		order.Comment.AssertEqual("from the diagram", "the comment must travel on the order");
		order.ClientCode.AssertEqual("client", "the client code must travel on the order");
		order.BrokerCode.AssertEqual("broker", "the broker code must travel on the order");
		order.IsManual.AssertEqual(true, "the manual flag must travel on the order");
		order.IsMarketMaker.AssertEqual(true, "the market maker flag must travel on the order");
		order.MarginMode.AssertEqual(MarginModes.Cross, "the margin mode must travel on the order");
	}

	/// <summary>
	/// A conditional order is the whole point of the condition settings: the broker is asked to hold the
	/// order until a level is reached. Registered as an ordinary order the condition is never read and the
	/// order goes to the exchange at once - the opposite of what was asked for.
	/// </summary>
	[TestMethod]
	[DoNotParallelize] // The condition is built from the process-wide adapter provider.
	public async Task An_element_told_to_place_a_conditional_order_registers_one_carrying_its_condition()
	{
		var adapter = new StopOrderAdapter(new IncrementalIdGenerator());

		var provider = new Mock<IMessageAdapterProvider>();
		provider.Setup(p => p.PossibleAdapters).Returns(new IMessageAdapter[] { adapter });

		var previous = ConfigManager.TryGetService<IMessageAdapterProvider>();

		ConfigManager.RegisterService(provider.Object);

		try
		{
			var settings = new OrderConditionSettings { AdapterType = typeof(StopOrderAdapter) };

			settings.Parameters[nameof(EmulationOrderCondition.StopPrice)] = 99m;

			var run = await StartOrderGraphAsync(Sides.Sell, isMarket: true, r => r.ConditionalSettings = settings);

			var order = Fire(run, 3m, price: null);

			order.Type.AssertEqual(OrderTypes.Conditional,
				"an order carrying a condition is a conditional order; typed otherwise the condition is never read");
			order.Condition.AssertOfType<EmulationOrderCondition>("the condition must be the one the chosen adapter speaks");
			((EmulationOrderCondition)order.Condition).StopPrice.AssertEqual(99m,
				"the condition must carry the parameters the element was given");
		}
		finally
		{
			if (previous is not null)
				ConfigManager.RegisterService(previous);
		}
	}

	/// <summary>
	/// An order that never reached the exchange is news a diagram has to act on - a strategy that waits for
	/// a fill that can never come is stuck. The failure socket is where that news arrives.
	/// </summary>
	[TestMethod]
	public async Task An_order_that_was_refused_is_reported_on_the_failure_output()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		run.Strategy.TradingMode = StrategyTradingModes.Disabled;

		Emit(run, 1m, price: null, _time);

		run.Submitted.Count.AssertEqual(0, "an order the strategy refused was never handed on to be registered");

		run.Failed.Received.Count.AssertEqual(1, "an order that failed to register must be reported once on the failure output");
		run.Failed.Received[0].Value.AssertOfType<OrderFail>("the failure output must carry the failure");
		run.Registered.Received.Count.AssertEqual(0, "an order that never reached the exchange must not be reported as registered");
	}

	/// <summary>
	/// Failing to register is one of the ways an order ends, so the finished socket must report it - and
	/// report an order, because the socket is declared as an order socket and the elements downstream of it
	/// (the cancel element casts its input straight to <see cref="Order"/>) are entitled to one.
	/// </summary>
	[TestMethod]
	public async Task An_order_that_was_refused_finishes_once_and_the_finished_output_carries_the_order()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		run.Strategy.TradingMode = StrategyTradingModes.Disabled;

		Emit(run, 1m, price: null, _time);

		run.Finished.Received.Count.AssertEqual(1, "an order that failed to register has ended, and an ended order finishes once");
		run.Finished.Received[0].Value.AssertOfType<Order>(
			"the finished socket is typed as an order socket, so what leaves it must be an order");
	}

	/// <summary>
	/// A filled order is finished as surely as a cancelled one, and the finished socket is where a diagram
	/// watches for either. It is an order socket, so an order is what has to leave it - the trade that
	/// completed the fill is not one, and the element behind it casts.
	/// </summary>
	[TestMethod]
	public async Task An_order_filled_in_full_finishes_once_and_the_finished_output_carries_the_order()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		var order = Fire(run, 3m, price: null);

		Activate(run, order);

		run.Strategy.OnTradeReceived(run.Orders, new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = order.Volume,
				SecurityId = order.Security.ToSecurityId(),
				ServerTime = _time,
			},
		});

		run.Finished.Received.Count.AssertEqual(1, "an order filled in full has ended, and an ended order finishes once");
		run.Finished.Received[0].Value.AssertOfType<Order>(
			"the finished socket is typed as an order socket, so what leaves it must be an order");
		run.Finished.Received[0].Value.AssertSame(order, "the finished output must carry the order that finished");
	}

	/// <summary>
	/// A reset is what a backtest does between runs, and every run has to trade like the first. An element
	/// that forgets which of its outputs anyone is listening to registers orders that nothing downstream
	/// ever hears about - the second run of the same diagram silently does nothing.
	/// </summary>
	[TestMethod]
	public async Task A_graph_started_again_after_a_reset_registers_and_reports_as_it_did_the_first_time()
	{
		var run = await StartOrderGraphAsync(Sides.Buy, isMarket: true, _ => { });

		var first = Fire(run, 1m, price: null);

		Activate(run, first);

		run.Registered.Received.Count.AssertEqual(1, "the first run must report the order it registered");

		run.Composition.Reset();
		run.Composition.Prepare();
		run.Composition.Start(_time);

		var later = _time.AddMinutes(1);

		Emit(run, 2m, price: null, later);

		run.Submitted.Count.AssertEqual(2, "a graph started again must register the order its trigger asks for");

		var second = run.Submitted[1];

		second.Volume.AssertEqual(2m, "the second order must take the volume of the second run");
		second.ServerTime = later;

		Activate(run, second);

		run.Registered.Received.Count.AssertEqual(2, "the run after the reset must report its order too");
		run.Registered.Received[1].Value.AssertSame(second, "the order output must carry the order the second run registered");
	}

	#endregion

	#region A sync element

	private sealed class SyncRun
	{
		public CompositionDiagramElement Composition { get; set; }
		public SourceDiagramElement First { get; set; }
		public SourceDiagramElement Second { get; set; }
		public RecordingDiagramElement FirstOut { get; set; }
		public RecordingDiagramElement SecondOut { get; set; }
	}

	// The element makes a new socket pair as soon as the last one is taken, so the pairs have to be
	// wired one at a time: link an input, link the output that goes with it, then move on.
	private static SyncRun StartSyncGraph(TimeSpan interval, bool clearSockets)
	{
		var graph = new Graph();

		var first = new SourceDiagramElement();
		var second = new SourceDiagramElement();
		var sync = new SyncDiagramElement { Interval = interval, ClearSockets = clearSockets };
		var firstOut = new RecordingDiagramElement();
		var secondOut = new RecordingDiagramElement();

		var firstNode = graph.Add(first);
		var secondNode = graph.Add(second);
		var syncNode = graph.Add(sync);
		var firstOutNode = graph.Add(firstOut);
		var secondOutNode = graph.Add(secondOut);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(firstNode, outputId, syncNode, "in 1");
		graph.Link(syncNode, "out 1", firstOutNode, inputId);
		graph.Link(secondNode, outputId, syncNode, "in 2");
		graph.Link(syncNode, "out 2", secondOutNode, inputId);

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new SyncRun
		{
			Composition = graph.Composition,
			First = first,
			Second = second,
			FirstOut = firstOut,
			SecondOut = secondOut,
		};
	}

	/// <summary>
	/// Half a set is not a set. Until every wired input has spoken for the interval the element must
	/// pass nothing on, and when the last one arrives all of them go out together, as one moment.
	/// </summary>
	[TestMethod]
	public void A_sync_element_emits_nothing_until_every_input_has_a_value_for_the_interval()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: true);

		run.First.Emit(_time.AddSeconds(30), "first");

		run.FirstOut.Received.Count.AssertEqual(0, "one input out of two is an incomplete set and must not be passed on");
		run.SecondOut.Received.Count.AssertEqual(0, "one input out of two is an incomplete set and must not be passed on");

		run.Second.Emit(_time.AddSeconds(45), "second");

		run.FirstOut.Received.Count.AssertEqual(1, "a completed set must be passed on once");
		run.FirstOut.Received[0].Value.AssertEqual("first", "each output must carry the value of the input it is paired with");

		run.SecondOut.Received.Count.AssertEqual(1, "a completed set must be passed on once");
		run.SecondOut.Received[0].Value.AssertEqual("second", "each output must carry the value of the input it is paired with");

		run.SecondOut.Received[0].Time.AssertEqual(run.FirstOut.Received[0].Time,
			"a synchronised set is one moment, so the values that make it up must go out stamped alike");
	}

	/// <summary>
	/// Values from different intervals are not a set, whatever order they turned up in. The element must
	/// keep them apart, and release an interval only when that interval's own missing value arrives.
	/// </summary>
	[TestMethod]
	public void A_sync_element_keeps_intervals_apart_and_releases_one_when_its_late_value_arrives()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: true);

		run.First.Emit(_time.AddSeconds(30), "first minute");
		run.Second.Emit(_time.AddSeconds(70), "second minute");

		run.FirstOut.Received.Count.AssertEqual(0, "values a minute apart do not make a set and must not be paired");
		run.SecondOut.Received.Count.AssertEqual(0, "values a minute apart do not make a set and must not be paired");

		// The partner for the first minute turns up after a value for the second minute already has.
		run.Second.Emit(_time.AddSeconds(50), "first minute too");

		run.FirstOut.Received.Count.AssertEqual(1, "the completed interval must be released");
		run.FirstOut.Received[0].Value.AssertEqual("first minute", "the released set is the one whose interval completed");

		run.SecondOut.Received.Count.AssertEqual(1, "the completed interval must be released, and only it");
		run.SecondOut.Received[0].Value.AssertEqual("first minute too",
			"the value belonging to the still incomplete interval must be held back, not passed on in its place");
	}

	/// <summary>
	/// Clearing on means a released set is spent. The element must not keep serving the same values to a
	/// strategy behind it every time one input speaks - it waits for a whole new set and releases that.
	/// </summary>
	[TestMethod]
	public void A_released_set_is_spent_when_the_element_clears_its_sockets()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: true);

		run.First.Emit(_time.AddSeconds(30), "first");
		run.Second.Emit(_time.AddSeconds(45), "second");

		run.FirstOut.Received.Count.AssertEqual(1, "a completed set must be released once");
		run.SecondOut.Received.Count.AssertEqual(1, "a completed set must be released once");

		run.Second.Emit(_time.AddSeconds(50), "second again");

		run.FirstOut.Received.Count.AssertEqual(1, "the released set is spent, so one input speaking alone is half a set again and must release nothing");
		run.SecondOut.Received.Count.AssertEqual(1, "the released set is spent, so one input speaking alone is half a set again and must release nothing");

		run.First.Emit(_time.AddSeconds(55), "first again");

		run.FirstOut.Received.Count.AssertEqual(2, "a whole new set has arrived and must be released");
		run.SecondOut.Received.Count.AssertEqual(2, "a whole new set has arrived and must be released");
		run.FirstOut.Received[1].Value.AssertEqual("first again", "the new set must carry the new values, not the ones already spent");
		run.SecondOut.Received[1].Value.AssertEqual("second again", "the new set must carry the new values, not the ones already spent");
	}

	/// <summary>
	/// Clearing off is the opposite promise: what an input last said stands until it says something else,
	/// so a new value on one input is paired with the partner's standing value and released.
	/// </summary>
	[TestMethod]
	public void Without_clearing_the_standing_value_of_an_input_is_paired_again()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: false);

		run.First.Emit(_time.AddSeconds(30), "first");
		run.Second.Emit(_time.AddSeconds(45), "second");

		run.FirstOut.Received.Count.AssertEqual(1, "a completed set must be released once");
		run.SecondOut.Received.Count.AssertEqual(1, "a completed set must be released once");

		run.Second.Emit(_time.AddSeconds(50), "second again");

		run.FirstOut.Received.Count.AssertEqual(2, "without clearing the first input's value still stands, so the set is complete again and must be released");
		run.SecondOut.Received.Count.AssertEqual(2, "without clearing the first input's value still stands, so the set is complete again and must be released");
		run.FirstOut.Received[1].Value.AssertEqual("first", "the standing value of the input that said nothing is what goes out with the new set");
		run.SecondOut.Received[1].Value.AssertEqual("second again", "the input that spoke goes out with what it just said");
	}

	/// <summary>
	/// Candles are told apart by more than their time: an update and a close of the same candle are two
	/// different sets. Each pair of updates goes out matched, the closed pair goes out as its own set,
	/// and the next candle starts over. A mismatched pair would hand a strategy two different moments.
	/// </summary>
	[TestMethod]
	public void Candle_updates_are_released_as_matching_pairs_and_the_closed_pair_is_its_own_set()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: true);

		var openTime = _time;
		var nextOpenTime = _time.AddMinutes(1);

		var firstActive = Candle(openTime, CandleStates.Active);
		var secondActive = Candle(openTime, CandleStates.Active);

		run.First.Emit(openTime, firstActive);

		run.FirstOut.Received.Count.AssertEqual(0, "one candle out of two is an incomplete set");

		run.Second.Emit(openTime, secondActive);

		run.FirstOut.Received.Count.AssertEqual(1, "a matched pair of updates must be released");
		run.SecondOut.Received.Count.AssertEqual(1, "a matched pair of updates must be released");

		var firstActiveAgain = Candle(openTime, CandleStates.Active);
		var secondActiveAgain = Candle(openTime, CandleStates.Active);

		run.First.Emit(openTime, firstActiveAgain);
		run.Second.Emit(openTime, secondActiveAgain);

		run.FirstOut.Received.Count.AssertEqual(2, "the next matched pair of updates must be released too");
		run.SecondOut.Received.Count.AssertEqual(2, "the next matched pair of updates must be released too");

		var firstFinished = Candle(openTime, CandleStates.Finished);
		var secondFinished = Candle(openTime, CandleStates.Finished);

		run.First.Emit(openTime, firstFinished);
		run.Second.Emit(openTime, secondFinished);

		run.FirstOut.Received.Count.AssertEqual(3, "the close of the candle is a set of its own and must be released");
		run.SecondOut.Received.Count.AssertEqual(3, "the close of the candle is a set of its own and must be released");

		var firstNext = Candle(nextOpenTime, CandleStates.Active);
		var secondNext = Candle(nextOpenTime, CandleStates.Active);

		run.First.Emit(nextOpenTime, firstNext);
		run.Second.Emit(nextOpenTime, secondNext);

		run.FirstOut.Received.Count.AssertEqual(4, "the next candle is a new interval and starts a new set");
		run.SecondOut.Received.Count.AssertEqual(4, "the next candle is a new interval and starts a new set");

		// Each release must pair the two inputs' own observations, in the order they were made.
		run.FirstOut.Received[0].Value.AssertSame(firstActive, "the first release must carry the first update of the first input");
		run.SecondOut.Received[0].Value.AssertSame(secondActive, "the first release must carry the first update of the second input");
		run.FirstOut.Received[1].Value.AssertSame(firstActiveAgain, "the second release must carry the second update, not a repeat of the first");
		run.SecondOut.Received[1].Value.AssertSame(secondActiveAgain, "the second release must carry the second update, not a repeat of the first");
		run.FirstOut.Received[2].Value.AssertSame(firstFinished, "the third release must carry the closed candle");
		run.SecondOut.Received[2].Value.AssertSame(secondFinished, "the third release must carry the closed candle");
		run.FirstOut.Received[3].Value.AssertSame(firstNext, "the fourth release must carry the next candle");
		run.SecondOut.Received[3].Value.AssertSame(secondNext, "the fourth release must carry the next candle");
	}

	/// <summary>
	/// Intervals leave in the order they happened. A complete later interval waits behind an earlier one
	/// that is still a value short, and when that value turns up both leave, earliest first - otherwise a
	/// strategy behind the element sees time run backwards.
	/// </summary>
	[TestMethod]
	public void Intervals_leave_in_order_and_a_late_value_releases_what_waited_behind_it()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: true);

		run.First.Emit(_time.AddSeconds(30), "minute one");
		run.First.Emit(_time.AddSeconds(70), "minute two");
		run.Second.Emit(_time.AddSeconds(80), "minute two too");

		run.FirstOut.Received.Count.AssertEqual(0, "the later interval is complete but the earlier one is not, and releasing it first would put the values out of order");
		run.SecondOut.Received.Count.AssertEqual(0, "the later interval is complete but the earlier one is not, and releasing it first would put the values out of order");

		run.Second.Emit(_time.AddSeconds(45), "minute one too");

		run.FirstOut.Received.Count.AssertEqual(2, "completing the earlier interval releases it and the interval that was waiting behind it");
		run.SecondOut.Received.Count.AssertEqual(2, "completing the earlier interval releases it and the interval that was waiting behind it");

		run.FirstOut.Received[0].Value.AssertEqual("minute one", "the earlier interval must leave first");
		run.SecondOut.Received[0].Value.AssertEqual("minute one too", "the earlier interval must leave first");
		run.FirstOut.Received[1].Value.AssertEqual("minute two", "the later interval must leave second");
		run.SecondOut.Received[1].Value.AssertEqual("minute two too", "the later interval must leave second");

		// A set is stamped with the earliest of the times that made it: 10:00:30 for the first minute,
		// 10:01:10 for the second.
		run.FirstOut.Received[0].Time.AssertEqual(_time.AddSeconds(30), "a set is stamped with the earliest time of the values in it");
		run.FirstOut.Received[1].Time.AssertEqual(_time.AddSeconds(70), "a set is stamped with the earliest time of the values in it");
	}

	/// <summary>
	/// A reset is a new run. A value that arrived before it must not be waiting to pair with one that
	/// arrives after it, or the first set of the new run is half made of the old one.
	/// </summary>
	[TestMethod]
	public void A_reset_forgets_a_half_set_so_it_cannot_be_completed_afterwards()
	{
		var run = StartSyncGraph(TimeSpan.FromMinutes(1), clearSockets: true);

		run.First.Emit(_time.AddSeconds(30), "before the reset");

		run.FirstOut.Received.Count.AssertEqual(0, "half a set is not released");

		run.Composition.Reset();
		run.Composition.Prepare();
		run.Composition.Start(_time);

		run.Second.Emit(_time.AddSeconds(45), "after the reset");

		run.FirstOut.Received.Count.AssertEqual(0, "the value from before the reset is gone, so this is half a set and must release nothing");
		run.SecondOut.Received.Count.AssertEqual(0, "the value from before the reset is gone, so this is half a set and must release nothing");

		run.First.Emit(_time.AddSeconds(50), "after the reset too");

		run.FirstOut.Received.Count.AssertEqual(1, "a set made entirely after the reset must be released");
		run.SecondOut.Received.Count.AssertEqual(1, "a set made entirely after the reset must be released");
		run.FirstOut.Received[0].Value.AssertEqual("after the reset too", "the released set must carry the value from after the reset, not the one the reset discarded");
		run.SecondOut.Received[0].Value.AssertEqual("after the reset", "the released set must carry the value from after the reset");
	}

	#endregion

	#region A position protect element

	private sealed class ProtectRun
	{
		public DiagramStrategy Strategy { get; set; }
		public PositionProtectDiagramElement Protect { get; set; }
		public SourceDiagramElement Trades { get; set; }
		public SourceDiagramElement Prices { get; set; }
		public SourceDiagramElement Depths { get; set; }
		public RecordingDiagramElement Take { get; set; }
		public RecordingDiagramElement Stop { get; set; }
		public Security Security { get; set; }
		public Portfolio Portfolio { get; set; }
		public Subscription Orders { get; set; }

		/// <summary>
		/// Every order the element handed to the strategy, in the order it handed them over.
		/// </summary>
		public List<Order> Submitted { get; } = [];
	}

	// The server-side protective path asks the adapter one thing - which order condition it speaks -
	// and builds the condition from that type, so that is all this adapter has to answer.
	private sealed class StopOrderAdapter(IdGenerator transactionIdGenerator) : MessageAdapter(transactionIdGenerator)
	{
		public override Type OrderConditionType { get; } = typeof(EmulationOrderCondition);
	}

	// A fill and a price (or a book) are the element's two inputs, and the take and stop sockets are
	// what a diagram downstream of it listens to, so all four are wired to leaves.
	private static ProtectRun BuildProtectGraph(Unit take, Unit stop, bool useMarketOrders, bool useServer)
	{
		var graph = new Graph();

		var trades = new SourceDiagramElement();
		var prices = new SourceDiagramElement();
		var depths = new SourceDiagramElement();

		var protect = new PositionProtectDiagramElement
		{
			TakeValue = take,
			StopValue = stop,
			UseMarketOrders = useMarketOrders,
			UseServer = useServer,
		};

		var takeOut = new RecordingDiagramElement();
		var stopOut = new RecordingDiagramElement();

		var tradesNode = graph.Add(trades);
		var pricesNode = graph.Add(prices);
		var depthsNode = graph.Add(depths);
		var protectNode = graph.Add(protect);
		var takeNode = graph.Add(takeOut);
		var stopNode = graph.Add(stopOut);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(tradesNode, outputId, protectNode, SocketId(StaticSocketIds.Trade));
		graph.Link(pricesNode, outputId, protectNode, SocketId(StaticSocketIds.Price));
		graph.Link(depthsNode, outputId, protectNode, SocketId(StaticSocketIds.MarketDepth));
		graph.Link(protectNode, "Take", takeNode, inputId);
		graph.Link(protectNode, "Stop", stopNode, inputId);

		var run = new ProtectRun
		{
			Strategy = graph.Strategy,
			Protect = protect,
			Trades = trades,
			Prices = prices,
			Depths = depths,
			Take = takeOut,
			Stop = stopOut,
			Security = Helper.CreateSecurity(),
			Portfolio = Helper.CreatePortfolio(),
		};

		// Recorded where the strategy takes the order in hand, which is before any connector sees it -
		// so an order the strategy refuses is recorded nowhere.
		graph.Strategy.OrderRegistering += run.Submitted.Add;

		return run;
	}

	private async Task StartProtectAsync(ProtectRun run)
	{
		run.Strategy.Security = run.Security;
		run.Strategy.Portfolio = run.Portfolio;

		await run.Strategy.StartAsync(CancellationToken);
		run.Strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		run.Orders = new Subscription(DataType.Transactions);
		run.Strategy.Subscriptions.Subscribe(run.Orders);
	}

	private async Task<ProtectRun> StartLocalProtectAsync(Unit take, Unit stop, bool useMarketOrders)
	{
		var run = BuildProtectGraph(take, stop, useMarketOrders, useServer: false);

		var idGenerator = new IncrementalIdGenerator();

		var connector = new Mock<IConnector>();
		connector.Setup(c => c.TransactionIdGenerator).Returns(idGenerator);
		connector
			.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			// The transaction id is stamped on the way out, and the strategy tracks the order by it.
			.Callback<Order>(o => o.TransactionId = idGenerator.GetNextId());

		run.Strategy.Connector = connector.Object;

		await StartProtectAsync(run);

		return run;
	}

	private static MyTrade Fill(Security security, Portfolio portfolio, Sides side, decimal price, decimal volume, long tradeId)
		=> new()
		{
			Order = new()
			{
				Security = security,
				Portfolio = portfolio,
				Side = side,
				Volume = volume,
				Price = price,
			},
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = tradeId,
				TradePrice = price,
				TradeVolume = volume,
				SecurityId = security.ToSecurityId(),
				ServerTime = _time,
			},
		};

	private static MyTrade Fill(ProtectRun run, Sides side, decimal price, decimal volume, long tradeId)
		=> Fill(run.Security, run.Portfolio, side, price, volume, tradeId);

	private static QuoteChangeMessage Book(Security security, decimal bid, decimal ask, DateTime time)
		=> new()
		{
			SecurityId = security.ToSecurityId(),
			Bids = [new QuoteChange(bid, 1m)],
			Asks = [new QuoteChange(ask, 1m)],
			ServerTime = time,
		};

	// The broker answers that the order reached the exchange, which is the event the take and stop
	// sockets report.
	private static void Accept(ProtectRun run, Order order, DateTime time)
	{
		order.ServerTime = time;
		order.State = OrderStates.Active;
		order.Balance = order.Volume;
		run.Strategy.OnConnectorOrderReceived(run.Orders, order);
	}

	/// <summary>
	/// A long bought at 100 with a take of 10 is closed at 110 and nowhere before it: one sell for the
	/// whole position, reported on the take socket, and one only - the position is protected once.
	/// </summary>
	[TestMethod]
	public async Task A_protected_long_closes_once_when_the_take_level_is_reached()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(5m), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 2m, 1));

		run.Submitted.Count.AssertEqual(0, "a fill on its own protects nothing - no level has been reached yet");

		// Halfway to the take (100 + 10) and still clear of the stop (100 - 5): neither side may fire.
		run.Prices.Emit(_time.AddSeconds(1), new Unit(105m));

		run.Submitted.Count.AssertEqual(0, "a price between the stop and the take must leave the position alone");

		run.Prices.Emit(_time.AddSeconds(2), new Unit(110m));

		run.Submitted.Count.AssertEqual(1, "reaching the take level must produce exactly one protective order");

		var order = run.Submitted[0];

		order.Side.AssertEqual(Sides.Sell, "a long is closed by selling");
		order.Volume.AssertEqual(2m, "the protective order must close the whole position and no more than it");
		order.Price.AssertEqual(110m, "the closing limit is the level that activated the protection");
		order.Type.AssertEqual(OrderTypes.Limit, "with market orders turned off the protection must go out as a limit order");
		order.Condition.AssertNull("a locally emulated protection is a plain order - the condition belongs to the server path");
		order.Security.AssertSame(run.Security, "the protective order must be for the instrument that was filled");
		order.Portfolio.AssertSame(run.Portfolio, "the protective order must be for the portfolio that was filled");

		Accept(run, order, _time.AddSeconds(2));

		run.Take.Received.Count.AssertEqual(1, "a take that reached the exchange must be reported once on the take socket");
		run.Take.Received[0].Value.AssertSame(order, "the take socket must carry the protective order");
		run.Stop.Received.Count.AssertEqual(0, "a take is not a stop, and the stop socket must stay silent");

		run.Prices.Emit(_time.AddSeconds(3), new Unit(120m));

		run.Submitted.Count.AssertEqual(1, "a position already protected must not be protected a second time");
	}

	/// <summary>
	/// The mirror of the long: 3 sold at 100 with a stop of 5 must be bought back at 105, on the stop
	/// socket, with the take of 10 (which sits at 90) untouched.
	/// </summary>
	[TestMethod]
	public async Task A_protected_short_buys_back_once_when_the_stop_level_is_reached()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(5m), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Sell, 100m, 3m, 1));

		// 100 sold: the take of a short sits below at 90, the stop above at 105. 102 is neither.
		run.Prices.Emit(_time.AddSeconds(1), new Unit(102m));

		run.Submitted.Count.AssertEqual(0, "a price inside the protective band must leave the position alone");

		run.Prices.Emit(_time.AddSeconds(2), new Unit(105m));

		run.Submitted.Count.AssertEqual(1, "reaching the stop level must produce exactly one protective order");

		var order = run.Submitted[0];

		order.Side.AssertEqual(Sides.Buy, "a short is closed by buying");
		order.Volume.AssertEqual(3m, "the protective order must close the whole short");
		order.Price.AssertEqual(105m, "the closing limit is the level that activated the protection");

		Accept(run, order, _time.AddSeconds(2));

		run.Stop.Received.Count.AssertEqual(1, "a stop that reached the exchange must be reported once on the stop socket");
		run.Stop.Received[0].Value.AssertSame(order, "the stop socket must carry the protective order");
		run.Take.Received.Count.AssertEqual(0, "a stop is not a take, and the take socket must stay silent");
	}

	/// <summary>
	/// Selling part of a long leaves the rest of it exposed. 5 bought at 100 less 2 sold leaves 3 held
	/// at 100, so the take still sits at 110 and covers 3 - not the 5 that were once held.
	/// </summary>
	[TestMethod]
	public async Task A_partly_closed_position_is_protected_for_what_is_left_of_it()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 5m, 1));
		run.Trades.Emit(_time.AddSeconds(1), Fill(run, Sides.Sell, 102m, 2m, 2));

		run.Prices.Emit(_time.AddSeconds(2), new Unit(110m));

		run.Submitted.Count.AssertEqual(1, "the position that is left must be protected once");

		var order = run.Submitted[0];

		order.Side.AssertEqual(Sides.Sell, "what is left is still long, and a long is closed by selling");
		order.Volume.AssertEqual(3m, "5 bought less 2 sold leaves 3 to protect");
		order.Price.AssertEqual(110m, "the 3 that are left were bought at 100, so a take of 10 sits at 110");
	}

	/// <summary>
	/// Selling more than is held is not a smaller long, it is a short - protected from the price and
	/// the side of the trade that turned it around. 2 long met by a sell of 5 leaves 3 short at 90,
	/// so the stop of 5 sits at 95 and buys 3 back.
	/// </summary>
	[TestMethod]
	public async Task A_reversed_position_is_protected_from_its_new_side_and_price()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(5m), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 2m, 1));
		run.Trades.Emit(_time.AddSeconds(1), Fill(run, Sides.Sell, 90m, 5m, 2));

		run.Prices.Emit(_time.AddSeconds(2), new Unit(95m));

		run.Submitted.Count.AssertEqual(1, "the reversed position must be protected once");

		var order = run.Submitted[0];

		order.Side.AssertEqual(Sides.Buy, "the position is short now, and a short is closed by buying");
		order.Volume.AssertEqual(3m, "a sell of 5 against 2 long leaves 3 short");
		order.Price.AssertEqual(95m, "3 sold at 90 with a stop of 5 puts the level at 95");

		Accept(run, order, _time.AddSeconds(2));

		run.Stop.Received.Count.AssertEqual(1, "the reversal is protected by its stop, reported once");
		run.Take.Received.Count.AssertEqual(0, "the take of the reversed position sits at 80 and was never reached");
	}

	/// <summary>
	/// A fill has an identity - its trade id and its order - and the same fill delivered twice is still
	/// one fill. Counting it twice protects a position that was never held: two bought, four sold.
	/// </summary>
	[TestMethod]
	public async Task The_same_fill_delivered_twice_protects_only_the_position_that_was_held()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(), useMarketOrders: false);

		var fill = Fill(run, Sides.Buy, 100m, 2m, 1);

		run.Trades.Emit(_time, fill);
		run.Trades.Emit(_time.AddSeconds(1), fill);

		run.Prices.Emit(_time.AddSeconds(2), new Unit(110m));

		run.Submitted.Count.AssertEqual(1, "one position, however often its fill was reported, is protected by one order");
		run.Submitted[0].Volume.AssertEqual(2m,
			"one fill of 2 was delivered twice; 2 is what is held, so 2 is what the protective order must close");
	}

	/// <summary>
	/// The first fill decides what is being protected. A fill for another instrument or another account
	/// is not part of that position, and taking it in would protect a position nobody holds.
	/// </summary>
	[TestMethod]
	public async Task A_fill_for_another_instrument_or_account_is_refused()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 2m, 1));

		var otherSecurity = Helper.CreateSecurity();
		var otherPortfolio = new Portfolio { Name = "another account" };

		Throws<InvalidOperationException>(() => run.Trades.Emit(_time.AddSeconds(1),
			Fill(otherSecurity, run.Portfolio, Sides.Buy, 100m, 7m, 2)));

		Throws<InvalidOperationException>(() => run.Trades.Emit(_time.AddSeconds(2),
			Fill(run.Security, otherPortfolio, Sides.Buy, 100m, 7m, 3)));

		run.Prices.Emit(_time.AddSeconds(3), new Unit(110m));

		run.Submitted.Count.AssertEqual(1, "the position that was accepted must still be protected once");
		run.Submitted[0].Volume.AssertEqual(2m, "only the accepted fill counts towards the protected position");
		run.Submitted[0].Security.AssertSame(run.Security, "the refused fills must not have moved the protection to another instrument");
	}

	/// <summary>
	/// A reset puts the element back to knowing nothing, so the next fill decides afresh what is being
	/// protected - including an instrument that would have been refused a moment earlier.
	/// </summary>
	[TestMethod]
	public async Task A_reset_lets_the_element_protect_a_different_instrument_from_scratch()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 2m, 1));
		run.Prices.Emit(_time.AddSeconds(1), new Unit(110m));

		run.Submitted.Count.AssertEqual(1, "the first position must be protected before the reset");

		run.Protect.Reset();

		var other = Helper.CreateSecurity();

		run.Trades.Emit(_time.AddSeconds(2), Fill(other, run.Portfolio, Sides.Buy, 200m, 1m, 2));
		run.Prices.Emit(_time.AddSeconds(3), new Unit(210m));

		run.Submitted.Count.AssertEqual(2, "after a reset the element must protect whatever position it is given next");

		var order = run.Submitted[1];

		order.Security.AssertSame(other, "a reset releases the instrument the element had bound itself to");
		order.Volume.AssertEqual(1m, "the position after the reset is the single fill that followed it");
		order.Price.AssertEqual(210m, "200 paid with a take of 10 puts the level at 210");
	}

	/// <summary>
	/// The take and stop sockets say an order reached the exchange. An order the strategy refused never
	/// did, so nothing may leave those sockets for it.
	/// </summary>
	[TestMethod]
	public async Task A_refused_protective_order_is_reported_on_neither_socket()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(), useMarketOrders: false);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 2m, 1));

		run.Strategy.TradingMode = StrategyTradingModes.Disabled;

		run.Prices.Emit(_time.AddSeconds(1), new Unit(110m));

		run.Submitted.Count.AssertEqual(0, "an order the strategy refused was never handed on to be registered");
		run.Take.Received.Count.AssertEqual(0, "the take socket reports orders that reached the exchange, not attempts");
		run.Stop.Received.Count.AssertEqual(0, "the stop socket reports orders that reached the exchange, not attempts");
	}

	/// <summary>
	/// A book is the other way the element learns the price. Before any fill there is nothing to protect,
	/// and after one a book that has cleared the take level protects the position exactly once.
	/// </summary>
	[TestMethod]
	public async Task A_book_triggers_the_protection_the_way_a_price_does()
	{
		var run = await StartLocalProtectAsync(new Unit(10m), new Unit(), useMarketOrders: false);

		run.Depths.Emit(_time, Book(run.Security, 99m, 99.5m, _time));

		run.Submitted.Count.AssertEqual(0, "a book that arrives before any fill has no position to protect");

		run.Trades.Emit(_time.AddSeconds(1), Fill(run, Sides.Buy, 100m, 2m, 1));

		// Both sides of the book are past the take level of 100 + 10, so the outcome does not depend on
		// which side of the spread the element reads.
		run.Depths.Emit(_time.AddSeconds(2), Book(run.Security, 110m, 110.5m, _time.AddSeconds(2)));

		run.Submitted.Count.AssertEqual(1, "a book that clears the take level must protect the position once");

		var order = run.Submitted[0];

		order.Side.AssertEqual(Sides.Sell, "a long is closed by selling");
		order.Volume.AssertEqual(2m, "the protective order must close the whole position");

		Accept(run, order, _time.AddSeconds(2));

		run.Take.Received.Count.AssertEqual(1, "a take reached through the book is still a take, reported once");
		run.Stop.Received.Count.AssertEqual(0, "the stop socket must stay silent");
	}

	/// <summary>
	/// Server-side protection is a stop order left with the broker: the level travels as an order
	/// condition, and an order that carries a condition is a conditional order. Typed as anything else
	/// it is routed, tracked and cancelled as a plain order and its condition is never read.
	/// </summary>
	[TestMethod]
	public async Task A_server_side_protective_order_is_conditional()
	{
		var run = BuildProtectGraph(new Unit(10m), new Unit(), useMarketOrders: false, useServer: true);

		using var connector = new Connector();

		var adapter = new StopOrderAdapter(connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.PortfolioAdapterProvider.SetAdapter(run.Portfolio.Name, adapter)
			.AssertTrue("the account must be routed to the adapter that speaks stop orders");

		run.Strategy.Connector = connector;

		await StartProtectAsync(run);

		run.Trades.Emit(_time, Fill(run, Sides.Buy, 100m, 2m, 1));

		run.Submitted.Count.AssertEqual(1, "a server-side protection is left with the broker on the fill, not waited for");

		var order = run.Submitted[0];

		order.Side.AssertEqual(Sides.Sell, "a long is closed by selling");
		order.Volume.AssertEqual(2m, "the protective order must close the whole position");
		order.Condition.AssertNotNull("a server-side protection travels as an order condition");

		// 100 paid with a take offset of 10 puts the activation at 110.
		((ITakeProfitOrderCondition)order.Condition).ActivationPrice.AssertEqual(110m,
			"the take activates at the entry price plus the take offset");

		order.Type.AssertEqual(OrderTypes.Conditional,
			"an order carrying a condition is a conditional order; typed otherwise the condition is never read");
	}

	#endregion

	#region A crossing element

	private sealed class CrossingRun
	{
		public SourceDiagramElement Up { get; set; }
		public SourceDiagramElement Down { get; set; }
		public RecordingDiagramElement Signals { get; set; }
	}

	private static CrossingRun StartCrossingGraph()
	{
		var graph = new Graph();

		var up = new SourceDiagramElement();
		var down = new SourceDiagramElement();
		var crossing = new CrossingDiagramElement();
		var signals = new RecordingDiagramElement();

		var upNode = graph.Add(up);
		var downNode = graph.Add(down);
		var crossingNode = graph.Add(crossing);
		var signalsNode = graph.Add(signals);

		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(upNode, outputId, crossingNode, "Input Up");
		graph.Link(downNode, outputId, crossingNode, "Input Down");
		graph.Link(crossingNode, outputId, signalsNode, SocketId(StaticSocketIds.Input));

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new CrossingRun { Up = up, Down = down, Signals = signals };
	}

	// One observation of both lines. The second value completes the pair, which is the moment a
	// crossing can be declared at all.
	private static void Observe(CrossingRun run, decimal up, decimal down, DateTime time)
	{
		run.Up.Emit(time, new Unit(up));
		run.Down.Emit(time, new Unit(down));
	}

	/// <summary>
	/// A crossing is a change of order between the two lines, declared at the observation that changed
	/// it and at no other. The first observation has nothing to change from, and an observation that
	/// repeats the previous order has changed nothing.
	/// </summary>
	[TestMethod]
	public void A_crossing_is_declared_only_where_the_two_inputs_change_order()
	{
		var run = StartCrossingGraph();

		Observe(run, 10m, 5m, _time.AddSeconds(1));

		run.Signals.Received.Count.AssertEqual(0, "the first observation has nothing to be compared with");

		Observe(run, 1m, 5m, _time.AddSeconds(2));
		Observe(run, 2m, 5m, _time.AddSeconds(3));
		Observe(run, 9m, 5m, _time.AddSeconds(4));

		// Above, below, below, above: the order changed twice, at the second and the fourth observation.
		run.Signals.Received.Count.AssertEqual(2, "four observations that change order twice must declare two crossings");

		run.Signals.Received[0].GetValue<bool>().AssertFalse("the first line fell below the second, and that is what the crossing must say");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(2), "a crossing is declared at the observation that completed it");

		run.Signals.Received[1].GetValue<bool>().AssertTrue("the first line rose back above the second");
		run.Signals.Received[1].Time.AssertEqual(_time.AddSeconds(4), "a crossing is declared at the observation that completed it");
	}

	/// <summary>
	/// A line that moves from one side of another to the far side has crossed once, whether or not it
	/// was seen exactly level on the way. Two signals for one move, or none, would both be wrong.
	/// </summary>
	[TestMethod]
	public void A_move_that_passes_through_equal_values_is_one_crossing()
	{
		var upwards = StartCrossingGraph();

		Observe(upwards, 1m, 5m, _time.AddSeconds(1));
		Observe(upwards, 5m, 5m, _time.AddSeconds(2));
		Observe(upwards, 9m, 5m, _time.AddSeconds(3));

		upwards.Signals.Received.Count.AssertEqual(1, "below, level, above is one move upwards and so one crossing");
		upwards.Signals.Received[0].GetValue<bool>().AssertTrue("the first line ended above the second, so the crossing is an upward one");

		var downwards = StartCrossingGraph();

		Observe(downwards, 9m, 5m, _time.AddSeconds(1));
		Observe(downwards, 5m, 5m, _time.AddSeconds(2));
		Observe(downwards, 1m, 5m, _time.AddSeconds(3));

		downwards.Signals.Received.Count.AssertEqual(1, "above, level, below is one move downwards and so one crossing");
		downwards.Signals.Received[0].GetValue<bool>().AssertFalse("the first line ended below the second, so the crossing is a downward one");
	}

	/// <summary>
	/// The two inputs are not queues. A value that is replaced before its partner ever arrives was never
	/// observed alongside anything, and pairing it with a later partner would announce a crossing that
	/// never happened.
	/// </summary>
	[TestMethod]
	public void A_value_replaced_before_its_partner_arrives_is_never_paired()
	{
		var run = StartCrossingGraph();

		Observe(run, 1m, 5m, _time.AddSeconds(1));

		// The first line is seen above, then below again, all before the second line speaks at all.
		run.Up.Emit(_time.AddSeconds(2), new Unit(9m));
		run.Up.Emit(_time.AddSeconds(3), new Unit(1m));
		run.Down.Emit(_time.AddSeconds(4), new Unit(5m));

		run.Signals.Received.Count.AssertEqual(0,
			"the two lines were never observed with the first above the second, so there was nothing to declare");

		Observe(run, 9m, 5m, _time.AddSeconds(5));

		run.Signals.Received.Count.AssertEqual(1, "the first observation that really changes the order declares one crossing");
		run.Signals.Received[0].GetValue<bool>().AssertTrue("the first line is above the second now");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(5), "a crossing is declared at the observation that completed it");
	}

	#endregion

	#region A delay element

	private sealed class DelayRun
	{
		public SourceDiagramElement Trigger { get; set; }
		public SourceDiagramElement Input { get; set; }
		public RecordingDiagramElement Signals { get; set; }
	}

	private static DelayRun StartDelayGraph(int n)
	{
		var graph = new Graph();

		var trigger = new SourceDiagramElement();
		var input = new SourceDiagramElement();
		var delay = new DelayDiagramElement { N = n };
		var signals = new RecordingDiagramElement();

		var triggerNode = graph.Add(trigger);
		var inputNode = graph.Add(input);
		var delayNode = graph.Add(delay);
		var signalsNode = graph.Add(signals);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(triggerNode, outputId, delayNode, SocketId(StaticSocketIds.Trigger));
		graph.Link(inputNode, outputId, delayNode, inputId);
		graph.Link(delayNode, outputId, signalsNode, inputId);

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new DelayRun { Trigger = trigger, Input = input, Signals = signals };
	}

	private static TimeFrameCandleMessage Candle(DateTime time, CandleStates state)
		=> new()
		{
			OpenTime = time,
			ClosePrice = 100m,
			State = state,
		};

	/// <summary>
	/// The shortest delay there is. One value asked for means the signal belongs to the very next value
	/// and to nothing after it - the delay is spent once it has fired.
	/// </summary>
	[TestMethod]
	public void A_delay_of_one_releases_the_signal_on_the_very_next_value()
	{
		var run = StartDelayGraph(1);

		run.Trigger.Emit(_time.AddSeconds(1), true);

		run.Signals.Received.Count.AssertEqual(0, "the trigger is not the signal - a value has still to pass");

		run.Input.Emit(_time.AddSeconds(2), 1m);

		run.Signals.Received.Count.AssertEqual(1, "one value asked for means the signal is released on the first value");
		run.Signals.Received[0].GetValue<bool>().AssertTrue("the delay releases a signal, and a signal is true");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(2), "the signal belongs to the value that completed the delay");

		run.Input.Emit(_time.AddSeconds(3), 2m);

		run.Signals.Received.Count.AssertEqual(1, "a spent delay must not keep signalling on every value that follows");
	}

	/// <summary>
	/// A delay of three counts three values and releases one signal on the third. Values before it are
	/// silent, values after it are silent, and the signal carries the moment of the third value.
	/// </summary>
	[TestMethod]
	public void A_delay_of_N_releases_one_signal_on_the_Nth_value()
	{
		var run = StartDelayGraph(3);

		run.Trigger.Emit(_time, true);

		run.Input.Emit(_time.AddSeconds(1), 1m);
		run.Input.Emit(_time.AddSeconds(2), 2m);

		run.Signals.Received.Count.AssertEqual(0, "two of the three values asked for is not yet the delay");

		run.Input.Emit(_time.AddSeconds(3), 3m);
		run.Input.Emit(_time.AddSeconds(4), 4m);
		run.Input.Emit(_time.AddSeconds(5), 5m);

		run.Signals.Received.Count.AssertEqual(1, "one trigger asks for one signal, however many values follow it");
		run.Signals.Received[0].GetValue<bool>().AssertTrue("the delay releases a signal, and a signal is true");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(3), "the third value is the one the signal belongs to");
	}

	/// <summary>
	/// A trigger the element is already counting for cannot duplicate the signal that was asked for,
	/// nor push it further away: the first trigger asked for three values and gets its signal on the
	/// third, whatever arrives on the trigger meanwhile.
	/// </summary>
	[TestMethod]
	public void A_trigger_that_arrives_while_the_delay_is_running_changes_nothing()
	{
		var run = StartDelayGraph(3);

		run.Trigger.Emit(_time, true);

		run.Input.Emit(_time.AddSeconds(1), 1m);

		run.Trigger.Emit(_time.AddSeconds(2), true);

		run.Input.Emit(_time.AddSeconds(3), 2m);
		run.Input.Emit(_time.AddSeconds(4), 3m);
		run.Input.Emit(_time.AddSeconds(5), 4m);
		run.Input.Emit(_time.AddSeconds(6), 5m);

		run.Signals.Received.Count.AssertEqual(1, "a second trigger while the first is being counted must not add a signal of its own");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(4),
			"the values counted are those at 1, 3 and 4 seconds - the third of them is where the first trigger's signal falls due");
	}

	/// <summary>
	/// Set to finished values only, the element counts values that are finished and skips values that
	/// say they are not. Two finished values are asked for, and the two unfinished ones between them
	/// must leave the count where it was.
	/// </summary>
	[TestMethod]
	public void Only_values_that_say_they_are_finished_count_down_the_delay()
	{
		var run = StartDelayGraph(2);

		run.Trigger.Emit(_time, true);

		run.Input.Emit(_time.AddSeconds(1), Candle(_time.AddSeconds(1), CandleStates.Active));

		run.Signals.Received.Count.AssertEqual(0, "an unfinished value must not count towards the delay");

		run.Input.Emit(_time.AddSeconds(2), Candle(_time.AddSeconds(2), CandleStates.Finished));
		run.Input.Emit(_time.AddSeconds(3), Candle(_time.AddSeconds(3), CandleStates.Active));

		run.Signals.Received.Count.AssertEqual(0, "one finished value of the two asked for is not yet the delay");

		run.Input.Emit(_time.AddSeconds(4), Candle(_time.AddSeconds(4), CandleStates.Finished));

		run.Signals.Received.Count.AssertEqual(1, "the second finished value completes the delay and releases one signal");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(4), "the signal belongs to the second finished value");

		run.Input.Emit(_time.AddSeconds(5), Candle(_time.AddSeconds(5), CandleStates.Finished));

		run.Signals.Received.Count.AssertEqual(1, "a spent delay must not signal again");
	}

	/// <summary>
	/// The trigger carries a signal, and a signal that says no is not a trigger. A delay started by a
	/// false would fire on values nobody asked about.
	/// </summary>
	[TestMethod]
	public void A_false_trigger_does_not_start_the_delay()
	{
		var run = StartDelayGraph(1);

		run.Trigger.Emit(_time.AddSeconds(1), false);
		run.Input.Emit(_time.AddSeconds(2), 1m);

		run.Signals.Received.Count.AssertEqual(0, "a trigger that says no must not start a delay");

		run.Trigger.Emit(_time.AddSeconds(3), true);
		run.Input.Emit(_time.AddSeconds(4), 2m);

		run.Signals.Received.Count.AssertEqual(1, "a trigger that says yes starts the delay, and one value completes it");
		run.Signals.Received[0].Time.AssertEqual(_time.AddSeconds(4), "the signal belongs to the value that followed the real trigger");
	}

	#endregion

	#region A converter element

	/// <summary>
	/// The properties a converter offers its author come from the socket type it is set to, so that is the
	/// type the value has to be read back under. A tick arrives as an <see cref="ExecutionMessage"/>, which
	/// implements <see cref="ITickTradeMessage.Price"/> explicitly and carries an order price of its own
	/// under the very same name: read against the message alone the property is either invisible or means
	/// two things, and the element emits nothing while the diagram still looks correctly wired.
	/// </summary>
	[TestMethod]
	public void ConverterElement_ReadsAPropertyUnderItsSocketType()
	{
		var graph = new Graph();

		var source = new SourceDiagramElement();
		// The type has to be set first: changing it clears the property that was chosen under the old one.
		var converter = new ConverterDiagramElement
		{
			Type = DiagramSocketType.Trade,
			Property = nameof(ITickTradeMessage.Price),
		};
		var recorded = new RecordingDiagramElement();

		var sourceNode = graph.Add(source);
		var converterNode = graph.Add(converter);
		var recordedNode = graph.Add(recorded);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(sourceNode, outputId, converterNode, inputId);
		graph.Link(converterNode, outputId, recordedNode, inputId);

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		// One message carrying both meanings of "Price": 100 as a trade, 7 as an order.
		source.Emit(_time, new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = "AAPL@NASDAQ".ToSecurityId(),
			TradeId = 1,
			TradePrice = 100m,
			TradeVolume = 2m,
			OrderPrice = 7m,
			ServerTime = _time,
		});

		recorded.Received.Count.AssertEqual(1, "a converter set to a property of its socket type must put that property out");
		recorded.Received[0].GetValue<decimal>().AssertEqual(100m,
			"the socket type is a tick, so its Price is the trade price - not the order price the same message happens to carry");
	}

	#endregion

	#region An indicator element

	private sealed class IndicatorRun
	{
		public SourceDiagramElement Input { get; set; }
		public RecordingDiagramElement Values { get; set; }
	}

	private static IndicatorRun StartIndicatorGraph(IIndicator indicator, bool finalOnly, bool formedOnly)
	{
		var graph = new Graph();

		var input = new SourceDiagramElement();
		var element = new IndicatorDiagramElement
		{
			Indicator = indicator,
			IsFinal = finalOnly,
			IsFormed = formedOnly,
		};
		var values = new RecordingDiagramElement();

		var inputNode = graph.Add(input);
		var elementNode = graph.Add(element);
		var valuesNode = graph.Add(values);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(inputNode, outputId, elementNode, inputId);
		graph.Link(elementNode, outputId, valuesNode, inputId);

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new IndicatorRun { Input = input, Values = values };
	}

	private static decimal Value(RecordingDiagramElement recorded, int index)
		=> ((IIndicatorValue)recorded.Received[index].Value).ToDecimal();

	/// <summary>
	/// The element is how a diagram gets at an indicator at all: every value it is given goes into the
	/// indicator and what the indicator made of it comes out. An element that passed the value through
	/// untouched, or answered only at the end, would leave the rest of the diagram working off prices.
	/// </summary>
	[TestMethod]
	public void An_indicator_element_puts_out_what_the_indicator_made_of_each_value()
	{
		var run = StartIndicatorGraph(new SimpleMovingAverage { Length = 3 }, finalOnly: false, formedOnly: false);

		run.Input.Emit(_time.AddSeconds(1), 1m);
		run.Input.Emit(_time.AddSeconds(2), 2m);
		run.Input.Emit(_time.AddSeconds(3), 3m);

		run.Values.Received.Count.AssertEqual(3, "an answer is due for every value the element was given");
		Value(run.Values, 2).AssertEqual(2m, "the average of 1, 2 and 3 is what the last answer must carry");
		run.Values.Received[2].Time.AssertEqual(_time.AddSeconds(3), "an answer belongs to the moment of the value that produced it");
	}

	/// <summary>
	/// An indicator that has not seen enough values yet answers with a number all the same, and that number
	/// is not the indicator - it is an average of two when three were asked for. Set to formed values only,
	/// the element must hold those back, or a strategy trades on a warm-up.
	/// </summary>
	[TestMethod]
	public void Set_to_formed_only_an_indicator_element_stays_silent_until_the_indicator_has_formed()
	{
		var run = StartIndicatorGraph(new SimpleMovingAverage { Length = 3 }, finalOnly: false, formedOnly: true);

		run.Input.Emit(_time.AddSeconds(1), 1m);
		run.Input.Emit(_time.AddSeconds(2), 2m);

		run.Values.Received.Count.AssertEqual(0, "two of the three values the indicator needs do not make it formed");

		run.Input.Emit(_time.AddSeconds(3), 3m);

		run.Values.Received.Count.AssertEqual(1, "the value that forms the indicator is the first one worth passing on");
		Value(run.Values, 0).AssertEqual(2m, "the answer must be the formed indicator's own value");
	}

	/// <summary>
	/// A candle that is still open changes with every tick, and so does the indicator value taken from it.
	/// Set to final values only, the element must answer for the closed candle and not for the running one,
	/// or a strategy acts on a level that the candle's next tick takes away again.
	/// </summary>
	[TestMethod]
	public void Set_to_final_only_an_indicator_element_ignores_a_candle_that_is_still_open()
	{
		var run = StartIndicatorGraph(new SimpleMovingAverage { Length = 2 }, finalOnly: true, formedOnly: false);

		run.Input.Emit(_time.AddSeconds(1), Candle(_time.AddSeconds(1), CandleStates.Active));

		run.Values.Received.Count.AssertEqual(0, "a candle that is still open is not a final value and must be passed over");

		run.Input.Emit(_time.AddSeconds(2), Candle(_time.AddSeconds(2), CandleStates.Finished));

		run.Values.Received.Count.AssertEqual(1, "the closed candle is a final value and must be answered for");
	}

	#endregion

	#region A formula element

	private sealed class FormulaRun
	{
		public SourceDiagramElement First { get; set; }
		public SourceDiagramElement Second { get; set; }
		public RecordingDiagramElement Results { get; set; }
	}

	// The element makes one input socket per variable the formula names, so the sockets to wire are
	// whatever the formula asked for - found by the variable's own name.
	private static FormulaRun StartFormulaGraph(string expression, string validation)
	{
		var graph = new Graph();

		var first = new SourceDiagramElement();
		var second = new SourceDiagramElement();
		var math = new MathDiagramElement
		{
			Expression = expression,
			Validation = validation,
		};
		var results = new RecordingDiagramElement();

		var firstNode = graph.Add(first);
		var secondNode = graph.Add(second);
		var mathNode = graph.Add(math);
		var resultsNode = graph.Add(results);

		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(firstNode, outputId, mathNode, math.InputSockets.First(s => s.Name == "a").Id);
		graph.Link(secondNode, outputId, mathNode, math.InputSockets.First(s => s.Name == "b").Id);
		graph.Link(mathNode, outputId, resultsNode, SocketId(StaticSocketIds.Input));

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new FormulaRun { First = first, Second = second, Results = results };
	}

	/// <summary>
	/// A formula element is a diagram's arithmetic: what a user typed is what must be computed, over the
	/// values that arrived on the sockets named after its variables. A result computed from the wrong
	/// variable is a plausible number that nothing in the diagram shows to be wrong.
	/// </summary>
	[TestMethod]
	public void A_formula_element_answers_its_formula_over_the_values_on_its_variable_sockets()
	{
		var run = StartFormulaGraph("a - b", null);

		run.First.Emit(_time.AddSeconds(1), 10m);

		run.Results.Received.Count.AssertEqual(0, "a formula of two variables cannot be computed from one of them");

		run.Second.Emit(_time.AddSeconds(1), 4m);

		run.Results.Received.Count.AssertEqual(1, "a complete set of variables must produce one result");
		run.Results.Received[0].GetValue<decimal>().AssertEqual(6m,
			"10 less 4 is 6 - and were the variables swapped it would read -6, which is why the sockets are named");
	}

	/// <summary>
	/// The validation is the author's guard against input his formula cannot take - a zero divisor, a
	/// negative under a square root. Computing anyway puts a nonsense number, or an exception, into a
	/// diagram that said in advance how to recognise input it did not want.
	/// </summary>
	[TestMethod]
	public void A_formula_guarded_by_a_validation_answers_nothing_for_input_the_validation_rejects()
	{
		var run = StartFormulaGraph("a / b", "b > 0");

		run.First.Emit(_time.AddSeconds(1), 6m);
		run.Second.Emit(_time.AddSeconds(1), 0m);

		run.Results.Received.Count.AssertEqual(0, "input the validation rejects must produce no result at all");

		run.First.Emit(_time.AddSeconds(2), 6m);
		run.Second.Emit(_time.AddSeconds(2), 2m);

		run.Results.Received.Count.AssertEqual(1, "input the validation accepts must be computed");
		run.Results.Received[0].GetValue<decimal>().AssertEqual(3m, "6 divided by 2 is 3");
	}

	#endregion

	#region A working time element

	/// <summary>
	/// The element is how a diagram stops trading out of hours, and out of hours is decided by the moment
	/// the value reached it. The edges belong to the session: an order at the opening bell is in time, and
	/// a window that excluded its own bounds would refuse the first and last moments of every day.
	/// </summary>
	[TestMethod]
	public void A_working_time_element_answers_yes_inside_its_window_and_no_outside_it()
	{
		var graph = new Graph();

		var source = new SourceDiagramElement();
		var check = new TimeCheckDiagramElement
		{
			TimeBegin = TimeSpan.FromHours(10),
			TimeEnd = TimeSpan.FromHours(18),
		};
		var answers = new RecordingDiagramElement();

		var sourceNode = graph.Add(source);
		var checkNode = graph.Add(check);
		var answersNode = graph.Add(answers);

		graph.Link(sourceNode, SocketId(StaticSocketIds.Output), checkNode, SocketId(StaticSocketIds.Time));
		graph.Link(checkNode, SocketId(StaticSocketIds.Output), answersNode, SocketId(StaticSocketIds.Input));

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		// The session runs 10:00 to 18:00, and _time is the opening.
		source.Emit(_time.AddMinutes(-1), "before the open");
		source.Emit(_time, "the opening moment");
		source.Emit(_time.AddHours(4), "the middle of the session");
		source.Emit(_time.AddHours(8), "the closing moment");
		source.Emit(_time.AddHours(8).AddSeconds(1), "after the close");

		answers.Received.Count.AssertEqual(5, "every value put in must be answered for");

		answers.Received[0].GetValue<bool>().AssertFalse("a minute before the open is outside the session");
		answers.Received[1].GetValue<bool>().AssertTrue("the opening moment belongs to the session");
		answers.Received[2].GetValue<bool>().AssertTrue("the middle of the session is inside it");
		answers.Received[3].GetValue<bool>().AssertTrue("the closing moment belongs to the session");
		answers.Received[4].GetValue<bool>().AssertFalse("a second after the close is outside the session");
	}

	#endregion

	#region A previous value element

	private sealed class PreviousRun
	{
		public SourceDiagramElement Input { get; set; }
		public RecordingDiagramElement Previous { get; set; }
	}

	private static PreviousRun StartPreviousGraph(int shift, bool finishedOnly)
	{
		var graph = new Graph();

		var input = new SourceDiagramElement();
		var previous = new PreviousValueDiagramElement
		{
			Shift = shift,
			IsFinishedOnly = finishedOnly,
		};
		var recorded = new RecordingDiagramElement();

		var inputNode = graph.Add(input);
		var previousNode = graph.Add(previous);
		var recordedNode = graph.Add(recorded);

		var inputId = SocketId(StaticSocketIds.Input);
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(inputNode, outputId, previousNode, inputId);
		graph.Link(previousNode, outputId, recordedNode, inputId);

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new PreviousRun { Input = input, Previous = recorded };
	}

	/// <summary>
	/// The element is how a diagram compares now with then: alongside the value of this moment it puts out
	/// the value of N moments ago. Until N values have gone by there is no such value, and answering with
	/// the newest one instead would compare a moment with itself.
	/// </summary>
	[TestMethod]
	public void A_previous_value_element_hands_on_the_value_from_N_places_back()
	{
		var run = StartPreviousGraph(shift: 2, finishedOnly: false);

		run.Input.Emit(_time.AddSeconds(1), 1m);
		run.Input.Emit(_time.AddSeconds(2), 2m);

		run.Previous.Received.Count.AssertEqual(0, "two values back does not exist until two values have gone by");

		run.Input.Emit(_time.AddSeconds(3), 3m);

		run.Previous.Received.Count.AssertEqual(1, "the third value has a value two places behind it");
		run.Previous.Received[0].Value.AssertEqual(1m, "two places behind the third value is the first");
		run.Previous.Received[0].Time.AssertEqual(_time.AddSeconds(3), "the answer belongs to the moment that asked for it");

		run.Input.Emit(_time.AddSeconds(4), 4m);

		run.Previous.Received.Count.AssertEqual(2, "the window moves on with every value");
		run.Previous.Received[1].Value.AssertEqual(2m, "two places behind the fourth value is the second");
	}

	/// <summary>
	/// A candle that is still open is the same candle as the one before it, not the next one. Set to
	/// finished values only, the element must not let a running candle move the window on - the value it
	/// then hands out is one place nearer than the diagram asked for.
	/// </summary>
	[TestMethod]
	public void A_value_that_is_still_forming_does_not_move_the_previous_value_window()
	{
		var run = StartPreviousGraph(shift: 1, finishedOnly: true);

		var first = Candle(_time.AddSeconds(1), CandleStates.Finished);

		run.Input.Emit(_time.AddSeconds(1), first);
		run.Input.Emit(_time.AddSeconds(2), Candle(_time.AddSeconds(2), CandleStates.Active));
		run.Input.Emit(_time.AddSeconds(3), Candle(_time.AddSeconds(3), CandleStates.Active));

		run.Previous.Received.Count.AssertEqual(0, "candles that are still open must not count as values gone by");

		var second = Candle(_time.AddSeconds(4), CandleStates.Finished);

		run.Input.Emit(_time.AddSeconds(4), second);

		run.Previous.Received.Count.AssertEqual(1, "the second closed candle has one closed candle behind it");
		run.Previous.Received[0].Value.AssertSame(first, "the value handed out must be the closed candle before it, not one of the open ones");
	}

	#endregion

	#region A chart panel

	/// <summary>
	/// A chart panel is drawn by wiring things into it, and it has to make room as it goes: what is wired in
	/// becomes an element of the matching kind on the panel, and a free input is left for the next one. With
	/// no free input the panel takes nothing more; with no element behind the input nothing is ever drawn.
	/// </summary>
	[TestMethod]
	public void A_chart_panel_turns_what_is_wired_into_it_into_an_element_and_keeps_an_input_free()
	{
		var graph = new Graph();

		var candles = new SourceDiagramElement(DiagramSocketType.Candle);
		var panel = new DummyChartDiagramElement();

		var candlesNode = graph.Add(candles);
		var panelNode = graph.Add(panel);

		panel.InputSockets.Count.AssertEqual(1, "a panel with nothing on it still offers one input to take the first element");
		panel.CandleElements.Count.AssertEqual(0, "a panel with nothing wired into it has nothing to draw");

		var taken = panel.InputSockets.First();
		var outputId = SocketId(StaticSocketIds.Output);

		graph.Link(candlesNode, outputId, panelNode, taken.Id);

		panel.CandleElements.Count.AssertEqual(1, "wiring candles into a panel must put a candle element on it");
		panel.InputSockets.Count.AssertEqual(2, "the panel must offer a free input again once one has been taken");
		panel.InputSockets.Count(s => s.IsConnected).AssertEqual(1, "exactly the socket that was wired is the taken one");

		graph.Unlink(candlesNode, outputId, panelNode, taken.Id);

		panel.CandleElements.Count.AssertEqual(0, "unwiring the candles must take the element off the panel");
		panel.InputSockets.Count.AssertEqual(1, "the socket that lost its element goes with it, leaving the free one");
	}

	#endregion

	#region The compositions the Designer ships

	// The diagrams a new user is offered, versioned next to the repository they belong to.
	private const string _templatesFolder = "../../../../Designer.Templates/Backtest/";

	private static readonly string[] _shippedCompositions = ["Sma.json", "IndexCandles.json", "SampleCandles.json"];

	/// <summary>
	/// A shipped composition is the first diagram most users ever open. It names its elements by type id and
	/// its links by socket id, and nothing checks either when it is read: an element or a socket renamed in
	/// the core leaves the template loading into a graph with holes in it - nodes with no element behind
	/// them, links landing on sockets that are gone - and the user is the one who finds out.
	/// </summary>
	[TestMethod]
	public void ShippedJsonCompositions_StillLoad()
	{
		// An indicator element resolves the indicator it was saved with through the provider its host
		// registers. Without one the element still loads, but with no indicator behind it - which is
		// the very thing this test is meant to notice, so the provider is supplied here.
		if (ConfigManager.TryGetService<IIndicatorProvider>() is null)
		{
			var indicators = new IndicatorProvider();
			indicators.Init();
			ConfigManager.RegisterService<IIndicatorProvider>(indicators);
		}

		foreach (var fileName in _shippedCompositions)
		{
			var registry = new CompositionRegistry<InMemoryCompositionModelNode, InMemoryCompositionModelLink>(() => new InMemoryCompositionModelBehavior());
			registry.FillDefault();

			// The chart element a Designer offers comes with its user interface; away from one the dummy
			// stands in for it under the same type id, which is what the templates name it by.
			registry.DiagramElements.Add(new DummyChartDiagramElement());

			// The Designer stamps a fresh identifier into the template before reading it.
			var text = File.ReadAllText(Path.Combine(_templatesFolder, fileName)).Replace("%NEW_ID%", Guid.NewGuid().ToString());
			var file = text.UTF8().DeserializeInvariant();

			file.AssertNotNull($"{fileName} must be readable");

			// A shipped strategy keeps its diagram under Content/Value, and that is what the registry reads.
			var scheme = file.GetValue<SettingsStorage>("Content").GetValue<SettingsStorage>("Value");

			scheme.AssertNotNull($"{fileName} must carry a diagram to load");

			var composition = registry.Deserialize(scheme, ICompositionRegistryExtensions.NotSupported).element;
			var model = (CompositionModel<InMemoryCompositionModelNode, InMemoryCompositionModelLink>)composition.Model;

			var nodes = model.Nodes.ToArray();

			nodes.Length.AssertGreater(0, $"{fileName} must come back with the elements it was drawn from");

			foreach (var node in nodes)
				node.Element.AssertNotNull($"{fileName}: nothing in the core answers to element type {node.TypeId} any more ({node.Text})");

			var byKey = nodes.ToDictionary(n => n.Key);

			var links = model.Links.ToArray();

			links.Length.AssertGreater(0, $"{fileName} must come back with the links it was drawn with");

			foreach (var link in links)
			{
				byKey.TryGetValue(link.From, out var fromNode).AssertTrue($"{fileName}: {link} starts at a node the diagram does not have");
				byKey.TryGetValue(link.To, out var toNode).AssertTrue($"{fileName}: {link} ends at a node the diagram does not have");

				fromNode.Element.OutputSockets.FindById(link.FromPort)
					.AssertNotNull($"{fileName}: {link} starts at an output socket the element no longer has");

				toNode.Element.InputSockets.FindById(link.ToPort)
					.AssertNotNull($"{fileName}: {link} ends at an input socket the element no longer has");
			}
		}
	}

	#endregion

	#region A composition's own sockets

	/// <summary>
	/// A composition publishes the sockets its elements have left free, so a socket an element gives up
	/// has to stop being published. A published socket with nothing behind it accepts whatever is fed to
	/// it and drops it.
	/// </summary>
	[TestMethod]
	public void A_composition_publishes_only_the_sockets_its_elements_still_have()
	{
		var graph = new Graph();

		// A limit order asks for a price; a market order has no price input at all.
		var order = new OrderRegisterDiagramElement { IsMarket = false, ShowSockets = true };
		var node = graph.Add(order);

		var composition = graph.Composition;
		var priceId = $"{node.Key}_{SocketId(StaticSocketIds.Price)}";

		composition.InputSockets.Count(s => s.Id == priceId)
			.AssertEqual(1, "a limit order element has a free price input, and free sockets are what a composition publishes");

		var others = composition.InputSockets.Select(s => s.Id).Where(id => id != priceId).ToArray();

		order.IsMarket = true;

		composition.InputSockets.Count(s => s.Id == priceId)
			.AssertEqual(0, "the element gave up its price input, so the composition must stop publishing one");
		composition.InputSockets.Count
			.AssertEqual(others.Length, "only the price socket may go");
		others.All(id => composition.InputSockets.Any(s => s.Id == id))
			.AssertTrue("the element's other inputs are untouched and must still be published");

		order.IsMarket = false;

		composition.InputSockets.Count(s => s.Id == priceId)
			.AssertEqual(1, "the price input is back on the element, so the composition must publish it again");
	}

	/// <summary>
	/// A link is an agreement between two sockets. When one of them is gone the agreement goes with it,
	/// and the socket at the other end is free again - it must not be left booked by a link to nowhere.
	/// </summary>
	[TestMethod]
	public void A_link_dies_with_the_socket_it_landed_on()
	{
		var graph = new Graph();

		var price = new SourceDiagramElement { Name = "price", ShowSockets = true };
		var order = new OrderRegisterDiagramElement { IsMarket = false, ShowSockets = true };

		var priceNode = graph.Add(price);
		var orderNode = graph.Add(order);

		graph.Link(priceNode, SocketId(StaticSocketIds.Output), orderNode, SocketId(StaticSocketIds.Price));

		var composition = graph.Composition;
		var sourceOutId = $"{priceNode.Key}_{SocketId(StaticSocketIds.Output)}";
		var priceId = $"{orderNode.Key}_{SocketId(StaticSocketIds.Price)}";

		composition.OutputSockets.Count(s => s.Id == sourceOutId)
			.AssertEqual(0, "a socket taken by a link inside the composition is not free, so it is not published");
		composition.InputSockets.Count(s => s.Id == priceId)
			.AssertEqual(0, "a socket taken by a link inside the composition is not free, so it is not published");

		order.IsMarket = true;

		order.InputSockets.Count(s => s.Id == SocketId(StaticSocketIds.Price))
			.AssertEqual(0, "a market order element has no price input");
		composition.OutputSockets.Count(s => s.Id == sourceOutId)
			.AssertEqual(1, "the link died with the socket it landed on, so the source's output is free again and must be published");
	}

	/// <summary>
	/// A clone is a second graph, not a second name for the first. Both must run, and editing one must
	/// leave the other answering exactly what it answered before.
	/// </summary>
	[TestMethod]
	public void A_cloned_composition_is_a_graph_of_its_own()
	{
		var original = BuildThresholdComposition();
		var clone = (CompositionDiagramElement)original.Clone();

		// A clone comes without a strategy; it needs one of its own before it can run.
		var cloneStrategy = new DiagramStrategy { Composition = clone };
		cloneStrategy.Composition.AssertSame(clone, "the clone must be the graph its strategy runs");

		clone.Elements.Count().AssertEqual(4, "a clone must carry every element the graph it was copied from had");
		clone.Elements.Any(e => original.Elements.Any(o => ReferenceEquals(e, o)))
			.AssertFalse("a clone must own its elements - a shared one would make editing one graph edit the other");

		// Turn the clone's comparison round. 10 is greater than 5 but not less than 5, so once the
		// negation has had them the two graphs must answer opposite things.
		clone.Elements.OfType<ComparisonDiagramElement>().First().Operator = ComparisonOperator.Less;

		var fromClone = RunThreshold(clone, 10, 5);

		fromClone.Count.AssertEqual(1, "one pass of the inputs must put exactly one value out");
		fromClone[0].GetValue<bool>().AssertTrue("10 is not less than 5, and the negation of that is true");

		var fromOriginal = RunThreshold(original, 10, 5);

		fromOriginal.Count.AssertEqual(1, "one pass of the inputs must put exactly one value out");
		fromOriginal[0].GetValue<bool>().AssertFalse("10 is greater than 5, and the negation of that is false - editing the clone must not reach back into the graph it came from");
	}

	#endregion

	#region A debugger

	private static readonly TimeSpan _debuggerWait = TimeSpan.FromSeconds(5);

	/// <summary>
	/// A leaf that refuses the value it is given, so a test can watch what the debugger does with an
	/// element that throws.
	/// </summary>
	private class FailingDiagramElement : DiagramElement
	{
		public const string ErrorMessage = "the element refused the value";

		public FailingDiagramElement()
		{
			AddInput(StaticSocketIds.Input, "In", DiagramSocketType.Any, _ => throw new InvalidOperationException(ErrorMessage));
		}

		public override Guid TypeId { get; } = "9B41E2D7-58C0-4E63-A1F5-7C0D93B48A21".To<Guid>();

		public override string IconName { get; } = "Pi";
	}

	private sealed class DebugRun
	{
		public SourceDiagramElement Source { get; set; }
		public RecordingDiagramElement Recorded { get; set; }
		public DiagramElement Failing { get; set; }
		public DiagramDebugger Debugger { get; set; }
		public DiagramSocket Stopped { get; set; }
	}

	// The debugger takes over the elements that are in the composition when it is built, so it is built
	// after the graph is wired and before the graph is started.
	private static DebugRun StartDebuggerGraph(bool fails)
	{
		var graph = new Graph();

		var source = new SourceDiagramElement();
		var recorded = fails ? null : new RecordingDiagramElement();
		var failing = fails ? new FailingDiagramElement() : null;
		var sink = (DiagramElement)recorded ?? failing;

		var sourceNode = graph.Add(source);
		var sinkNode = graph.Add(sink);

		graph.Link(sourceNode, SocketId(StaticSocketIds.Output), sinkNode, SocketId(StaticSocketIds.Input));

		var debugger = new DiagramDebugger(graph.Composition);
		var stopped = sink.InputSockets.First();

		if (!fails)
			debugger.AddBreak(stopped).AssertTrue("a breakpoint must be accepted on a socket that has none");

		graph.Composition.Prepare();
		graph.Composition.Start(_time);

		return new DebugRun
		{
			Source = source,
			Recorded = recorded,
			Failing = failing,
			Debugger = debugger,
			Stopped = stopped,
		};
	}

	/// <summary>
	/// A breakpoint that does not stop is not a breakpoint. The value must be held at the socket, not
	/// delivered behind it, and it must be delivered - once, unchanged - when the debugger is continued.
	/// </summary>
	[TestMethod]
	public async Task A_breakpoint_holds_the_value_until_the_debugger_is_continued()
	{
		var run = StartDebuggerGraph(fails: false);

		var reported = new TaskCompletionSource<DiagramSocket>(TaskCreationOptions.RunContinuationsAsynchronously);
		run.Debugger.Break += s => reported.TrySetResult(s);

		var worker = Task.Run(() => run.Source.Emit(_time, "value"), CancellationToken);

		SpinWait.SpinUntil(() => run.Debugger.IsWaitingOnInput, _debuggerWait)
			.AssertTrue("a breakpoint must stop the run at the socket it is set on");
		var reportedSocket = await reported.Task.WaitAsync(_debuggerWait, CancellationToken);

		run.Recorded.Received.Count.AssertEqual(0, "a run stopped at a socket must not have delivered the value behind it");
		reportedSocket.AssertSame(run.Stopped, "the debugger must report the socket it stopped on");

		run.Debugger.Continue();

		await worker.WaitAsync(_debuggerWait, CancellationToken);

		run.Recorded.Received.Count.AssertEqual(1, "the held value must be delivered once the debugger is continued, and only once");
		run.Recorded.Received[0].Value.AssertEqual("value", "the value delivered must be the one that was held");
		run.Debugger.IsWaitingOnInput.AssertFalse("nothing is waiting once the run has gone through");
		run.Stopped.IsBreakActive.AssertFalse("the socket must not be left marked as stopped after the run continued");
	}

	/// <summary>
	/// The break notification is the only place a caller learns that a stop happened, so continuing from
	/// inside it is the plain way to drive the debugger. That continue has to be honoured; dropped, the
	/// run waits for a second one nobody knows to give.
	/// </summary>
	[TestMethod]
	public async Task A_debugger_continued_from_its_own_break_lets_the_run_go_on()
	{
		var run = StartDebuggerGraph(fails: false);

		run.Debugger.Break += _ => run.Debugger.Continue();

		var worker = Task.Run(() => run.Source.Emit(_time, "value"), CancellationToken);

		var wentOn = true;

		try
		{
			await worker.WaitAsync(_debuggerWait, CancellationToken);
		}
		catch (TimeoutException)
		{
			wentOn = false;

			// Let the stuck run go from here so it does not outlive the test.
			SpinWait.SpinUntil(() => run.Debugger.IsWaitingOnInput, _debuggerWait);
			run.Debugger.Continue();
			await worker.WaitAsync(_debuggerWait, CancellationToken);
		}

		wentOn.AssertTrue("a debugger continued from its own break notification must let the run go on, not wait for a continue that was already given");
		run.Recorded.Received.Count.AssertEqual(1, "the value must reach the element behind the breakpoint exactly once");
	}

	/// <summary>
	/// An element that throws is a stop of its own: the debugger names the element rather than letting
	/// the failure go by unseen, and continuing hands the failure back to whoever started the run.
	/// </summary>
	[TestMethod]
	public async Task An_element_that_throws_stops_the_debugger_and_names_itself()
	{
		var run = StartDebuggerGraph(fails: true);

		var reported = new TaskCompletionSource<DiagramElement>(TaskCreationOptions.RunContinuationsAsynchronously);
		run.Debugger.Error += e => reported.TrySetResult(e);

		var worker = Task.Run(() => run.Source.Emit(_time, "value"), CancellationToken);

		SpinWait.SpinUntil(() => run.Debugger.IsWaitingOnInput, _debuggerWait)
			.AssertTrue("an element that threw must stop the debugger, not let the run carry on past the failure");
		var reportedElement = await reported.Task.WaitAsync(_debuggerWait, CancellationToken);

		run.Debugger.IsWaitingOnError.AssertTrue("the stop must be reported as a stop on an error, not as an ordinary breakpoint");
		reportedElement.AssertSame(run.Failing, "the debugger must name the element that threw");

		run.Debugger.Continue();

		Exception failure = null;

		try
		{
			await worker.WaitAsync(_debuggerWait, CancellationToken);
		}
		catch (Exception excp)
		{
			failure = excp;
		}

		failure.AssertNotNull("stopping on the error must not swallow it - it still belongs to whoever started the run");
		failure.Message.AssertEqual(FailingDiagramElement.ErrorMessage, "the failure handed back must be the one the element threw");
	}

	#endregion
}
