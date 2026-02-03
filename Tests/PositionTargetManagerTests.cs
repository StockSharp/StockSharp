namespace StockSharp.Tests;

using Moq;

using StockSharp.Algo;
using StockSharp.Algo.PositionManagement;

[TestClass]
public class PositionTargetManagerTests : BaseTestClass
{
	private Mock<ISubscriptionProvider> _subProvider;
	private Mock<ITransactionProvider> _transProvider;
	private Mock<IMarketRuleContainer> _container;
	private Security _security;
	private Portfolio _portfolio;
	private decimal _currentPosition;
	private MarketRuleList _rulesList;

	[TestInitialize]
	public void Setup()
	{
		_subProvider = new Mock<ISubscriptionProvider>();
		_transProvider = new Mock<ITransactionProvider>();
		_container = new Mock<IMarketRuleContainer>();
		_rulesList = new MarketRuleList(_container.Object);
		_container.Setup(c => c.Rules).Returns(_rulesList);

		_security = new Security { Id = "SBER@TQBR" };
		_portfolio = new Portfolio { Name = "test" };
		_currentPosition = 0;
	}

	private PositionTargetManager CreateManager(
		Func<bool> canTrade = null,
		Func<Sides, decimal, IPositionModifyAlgo> algoFactory = null)
	{
		return new(
			_subProvider.Object,
			_transProvider.Object,
			_container.Object,
			getPosition: (s, p) => _currentPosition,
			orderFactory: () => new Order(),
			canTrade: canTrade ?? (() => true),
			algoFactory: algoFactory ?? ((side, vol) => new MarketOrderAlgo(side, vol))
		);
	}

	[TestMethod]
	public void SetTarget_RegistersOrder_WhenDeltaNonZero()
	{
		using var manager = CreateManager();

		Order registeredOrder = null;
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => registeredOrder = o);

		manager.SetTarget(_security, _portfolio, 100);

		IsNotNull(registeredOrder);
		registeredOrder.Side.AreEqual(Sides.Buy);
		registeredOrder.Volume.AreEqual(100m);
		registeredOrder.Security.AreEqual(_security);
		registeredOrder.Portfolio.AreEqual(_portfolio);
	}

	[TestMethod]
	public void SetTarget_SellOrder_WhenTargetNegative()
	{
		using var manager = CreateManager();

		Order registeredOrder = null;
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => registeredOrder = o);

		manager.SetTarget(_security, _portfolio, -50);

		IsNotNull(registeredOrder);
		registeredOrder.Side.AreEqual(Sides.Sell);
		registeredOrder.Volume.AreEqual(50m);
	}

	[TestMethod]
	public void SetTarget_NoOrder_WhenAlreadyAtTarget()
	{
		_currentPosition = 100;
		using var manager = CreateManager();

		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		_transProvider.Verify(t => t.RegisterOrder(It.IsAny<Order>()), Times.Never());
	}

	[TestMethod]
	public void SetTarget_NoOrder_WhenWithinTolerance()
	{
		_currentPosition = 99;
		using var manager = CreateManager();
		manager.PositionTolerance = 2;

		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		_transProvider.Verify(t => t.RegisterOrder(It.IsAny<Order>()), Times.Never());
	}

	[TestMethod]
	public void GetTarget_ReturnsNull_WhenNotSet()
	{
		using var manager = CreateManager();

		var target = manager.GetTarget(_security, _portfolio);

		IsNull(target);
	}

	[TestMethod]
	public void GetTarget_ReturnsValue_WhenSet()
	{
		using var manager = CreateManager();
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		manager.GetTarget(_security, _portfolio).AreEqual(100m);
	}

	[TestMethod]
	public void IsTargetReached_True_WhenAtTarget()
	{
		_currentPosition = 100;
		using var manager = CreateManager();
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		IsTrue(manager.IsTargetReached(_security, _portfolio));
	}

	[TestMethod]
	public void IsTargetReached_True_WhenWithinTolerance()
	{
		_currentPosition = 98;
		using var manager = CreateManager();
		manager.PositionTolerance = 3;
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		IsTrue(manager.IsTargetReached(_security, _portfolio));
	}

	[TestMethod]
	public void IsTargetReached_False_WhenNotSet()
	{
		using var manager = CreateManager();

		IsFalse(manager.IsTargetReached(_security, _portfolio));
	}

	[TestMethod]
	public void CancelTarget_RemovesTarget()
	{
		using var manager = CreateManager();
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);
		manager.CancelTarget(_security, _portfolio);

		IsNull(manager.GetTarget(_security, _portfolio));
	}

	[TestMethod]
	public void SetTarget_NoOrder_WhenCanTradeFalse()
	{
		using var manager = CreateManager(canTrade: () => false);

		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		_transProvider.Verify(t => t.RegisterOrder(It.IsAny<Order>()), Times.Never());
	}

	[TestMethod]
	public void TargetReached_EventFires_WhenAtTarget()
	{
		_currentPosition = 100;
		using var manager = CreateManager();

		var reached = false;
		manager.TargetReached += (s, p) => reached = true;

		manager.SetTarget(_security, _portfolio, 100);

		IsTrue(reached);
	}

	[TestMethod]
	public void OrderRegistered_EventFires()
	{
		using var manager = CreateManager();
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		Order eventOrder = null;
		manager.OrderRegistered += o => eventOrder = o;

		manager.SetTarget(_security, _portfolio, 100);

		IsNotNull(eventOrder);
	}

	[TestMethod]
	public void SetTarget_ChangeTarget_CancelsExistingOrder()
	{
		using var manager = CreateManager();

		Order firstOrder = null;
		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o =>
			{
				firstOrder ??= o;
				o.ApplyNewState(OrderStates.Active);
			});

		manager.SetTarget(_security, _portfolio, 100);

		IsNotNull(firstOrder);
		var orderToCancel = firstOrder;

		// Change target in opposite direction
		manager.SetTarget(_security, _portfolio, -50);

		_transProvider.Verify(t => t.CancelOrder(orderToCancel), Times.Once());
	}

	[TestMethod]
	public void SetTarget_NullSecurity_Throws()
	{
		using var manager = CreateManager();

		Throws<ArgumentNullException>(() => manager.SetTarget(null, _portfolio, 100));
	}

	[TestMethod]
	public void SetTarget_NullPortfolio_Throws()
	{
		using var manager = CreateManager();

		Throws<ArgumentNullException>(() => manager.SetTarget(_security, null, 100));
	}

	[TestMethod]
	public void OrderType_Default_IsMarket()
	{
		using var manager = CreateManager();

		manager.OrderType.AreEqual(OrderTypes.Market);
	}

	[TestMethod]
	public void AlgoFactory_Invoked_WithCorrectParameters()
	{
		Sides? passedSide = null;
		decimal? passedVolume = null;

		using var manager = CreateManager(algoFactory: (side, vol) =>
		{
			passedSide = side;
			passedVolume = vol;
			return new MarketOrderAlgo(side, vol);
		});

		_transProvider.Setup(t => t.RegisterOrder(It.IsAny<Order>()));

		manager.SetTarget(_security, _portfolio, 100);

		passedSide.AreEqual(Sides.Buy);
		passedVolume.AreEqual(100m);
	}

	[TestMethod]
	public void Constructor_NullSubProvider_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			null,
			_transProvider.Object,
			_container.Object,
			(s, p) => 0m,
			() => new Order(),
			() => true,
			(side, vol) => new MarketOrderAlgo(side, vol)));
	}

	[TestMethod]
	public void Constructor_NullTransProvider_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			_subProvider.Object,
			null,
			_container.Object,
			(s, p) => 0m,
			() => new Order(),
			() => true,
			(side, vol) => new MarketOrderAlgo(side, vol)));
	}

	[TestMethod]
	public void Constructor_NullContainer_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			_subProvider.Object,
			_transProvider.Object,
			null,
			(s, p) => 0m,
			() => new Order(),
			() => true,
			(side, vol) => new MarketOrderAlgo(side, vol)));
	}

	[TestMethod]
	public void Constructor_NullGetPosition_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			_subProvider.Object,
			_transProvider.Object,
			_container.Object,
			null,
			() => new Order(),
			() => true,
			(side, vol) => new MarketOrderAlgo(side, vol)));
	}

	[TestMethod]
	public void Constructor_NullOrderFactory_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			_subProvider.Object,
			_transProvider.Object,
			_container.Object,
			(s, p) => 0m,
			null,
			() => true,
			(side, vol) => new MarketOrderAlgo(side, vol)));
	}

	[TestMethod]
	public void Constructor_NullCanTrade_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			_subProvider.Object,
			_transProvider.Object,
			_container.Object,
			(s, p) => 0m,
			() => new Order(),
			null,
			(side, vol) => new MarketOrderAlgo(side, vol)));
	}

	[TestMethod]
	public void Constructor_NullAlgoFactory_Throws()
	{
		Throws<ArgumentNullException>(() => new PositionTargetManager(
			_subProvider.Object,
			_transProvider.Object,
			_container.Object,
			(s, p) => 0m,
			() => new Order(),
			() => true,
			null));
	}
}
