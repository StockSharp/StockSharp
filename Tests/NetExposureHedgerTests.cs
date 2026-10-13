namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

/// <summary>
/// The position that has to be covered at the exchange against the one actually held on the
/// hedging account. Both numbers meet in one place, so that is where the difference between them
/// is measured against the caps and the order that closes it is decided.
/// </summary>
[TestClass]
public class NetExposureHedgerTests : BaseTestClass
{
	private const string _hedgePortfolio = "HEDGE";

	private static readonly SecurityId _aapl = new() { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
	private static readonly SecurityId _msft = new() { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

	private static NetExposureHedger Hedger(decimal maxVolume = 5m, decimal? maxNotional = null)
		=> new(new Dictionary<SecurityId, NetExposureLimit>
		{
			[_aapl] = new() { MaxVolume = maxVolume, MaxNotional = maxNotional },
		}, _hedgePortfolio, new IncrementalIdGenerator());

	private static NetExposureHedger BothInstruments(decimal maxVolume)
		=> new(new Dictionary<SecurityId, NetExposureLimit>
		{
			[_aapl] = new() { MaxVolume = maxVolume },
			[_msft] = new() { MaxVolume = maxVolume },
		}, _hedgePortfolio, new IncrementalIdGenerator());

	[TestMethod]
	public void NullLimitsAreRefused()
		=> Throws<ArgumentNullException>(() => new NetExposureHedger(null, _hedgePortfolio, new IncrementalIdGenerator()));

	[TestMethod]
	public void AnEmptyPortfolioIsRefused()
		=> Throws<ArgumentNullException>(() => new NetExposureHedger(new Dictionary<SecurityId, NetExposureLimit>(), string.Empty, new IncrementalIdGenerator()));

	[TestMethod]
	public void ANullIdGeneratorIsRefused()
		=> Throws<ArgumentNullException>(() => new NetExposureHedger(new Dictionary<SecurityId, NetExposureLimit>(), _hedgePortfolio, null));

	[TestMethod]
	public void NamingNoLimitAtAllHedgesNothing()
	{
		var hedger = new NetExposureHedger(new Dictionary<SecurityId, NetExposureLimit>(), _hedgePortfolio, new IncrementalIdGenerator());

		hedger.IsHedging.AssertFalse("a table that caps no instrument never decides anything");
		hedger.PortfolioName.AssertEqual(_hedgePortfolio);
	}

	[TestMethod]
	public void ANamedLimitTurnsHedgingOn()
		=> Hedger().IsHedging.AssertTrue();

	[TestMethod]
	public void AnInstrumentWithNoLimitIsCarriedWhateverItComesTo()
	{
		var hedger = Hedger();

		hedger.SetExposure(_msft, 1_000m, 100m)
			.AssertNull("saying nothing about an instrument is how the table says it hedges none of it");
	}

	[TestMethod]
	public void AnInstrumentTheTableNeverNamesIsNotRemembered()
	{
		var hedger = Hedger();

		hedger.SetExposure(_msft, 1_000m, 100m).AssertNull();
		hedger.SetHedgePosition(_msft, 500m, 100m).AssertNull();

		hedger.Count.AssertEqual(0, "nothing ever decides on it, so there is nothing about it worth keeping");
	}

	[TestMethod]
	public void AnInstrumentNamedLaterIsHedgedFromTheNextRowOn()
	{
		var limits = new Dictionary<SecurityId, NetExposureLimit>
		{
			[_aapl] = new() { MaxVolume = 5m },
		};

		var hedger = new NetExposureHedger(limits, _hedgePortfolio, new IncrementalIdGenerator());

		hedger.SetExposure(_msft, 20m, 0m).AssertNull("the table does not name it yet");

		limits[_msft] = new() { MaxVolume = 5m };

		hedger.SetExposure(_msft, 20m, 0m)
			.AssertNotNull("the table names it now, and the row that states the exposure again is where it starts");
	}

	[TestMethod]
	public void NothingIsHedgedBeforeTheExposureIsStated()
	{
		// Started against an account that already holds something, the hedger knows only that side.
		// Read as a gap, a position that is exactly what is wanted would be sold off wholesale on
		// the strength of never having been told about it.
		var hedger = Hedger();

		hedger.SetHedgePosition(_aapl, 500m, 0m).AssertNull("half the subtraction is not the answer to it");

		hedger.SetExposure(_aapl, 500m, 0m)
			.AssertNull("and once it is stated, what is held turns out to be exactly what is wanted");
	}

	[TestMethod]
	public void AGapInsideTheVolumeCapIsCarried()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 5m, 0m).AssertNull("five is what the cap allows to be carried");
	}

	[TestMethod]
	public void AGapPastTheVolumeCapIsClosedInFull()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, 12m, 0m);

		order.AssertNotNull();
		order.Volume.AssertEqual(12m, "the whole gap goes, not the part of it past the cap");
	}

	[TestMethod]
	public void TheOrderIsMarketOnTheHedgePortfolio()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, 12m, 0m);

		order.AssertNotNull();
		order.SecurityId.AssertEqual(_aapl);
		order.PortfolioName.AssertEqual(_hedgePortfolio, "a hedge goes on the account that covers the exposure");
		order.OrderType.AssertEqual(OrderTypes.Market, "the gap is closed at whatever the exchange asks, not left resting");
		order.Side.AssertEqual(Sides.Buy, "twelve are wanted there and none are held, so twelve are bought");
		(order.TransactionId > 0).AssertTrue("the order carries the transaction it was reserved under");
	}

	[TestMethod]
	public void WhatIsAlreadyHeldIsNotBoughtAgain()
	{
		var hedger = Hedger();

		hedger.SetHedgePosition(_aapl, 10m, 0m)
			.AssertNull("nothing has stated the exposure yet, so what is held answers to nothing");

		hedger.SetExposure(_aapl, 12m, 0m).AssertNull("two short of twelve is inside the cap");

		var order = hedger.SetExposure(_aapl, 20m, 0m);

		order.AssertNotNull();
		order.Volume.AssertEqual(10m, "ten are already there, so only the other ten are bought");
		order.Side.AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public void HoldingMoreThanIsWantedSellsTheDifference()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 3m, 0m);

		var order = hedger.SetHedgePosition(_aapl, 20m, 0m);

		order.AssertNotNull();
		order.Side.AssertEqual(Sides.Sell, "seventeen more are held there than the exposure needs covered");
		order.Volume.AssertEqual(17m);
	}

	[TestMethod]
	public void AShortExposureIsCoveredBySelling()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, -20m, 0m);

		order.AssertNotNull();
		order.Side.AssertEqual(Sides.Sell, "the side follows the sign of the gap, not of the exposure alone");
		order.Volume.AssertEqual(20m);
	}

	[TestMethod]
	public void AGapInsideBothCapsIsCarried()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetExposure(_aapl, 2m, 200m)
			.AssertNull("two is well inside the volume cap and four hundred is inside the money one");
	}

	[TestMethod]
	public void AGapPastTheNotionalCapIsClosedThoughItsVolumeIsAllowed()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		var order = hedger.SetExposure(_aapl, 4m, 200m);

		order.AssertNotNull("eight hundred is past the money cap however small the volume");
		order.Volume.AssertEqual(4m, "crossing the money cap closes the whole gap too");
		order.Side.AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public void ANotionalCapWithNoPriceToMeasureAgainstIsNotGuessedAt()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 1m);

		hedger.SetExposure(_aapl, 4m, 0m)
			.AssertNull("nothing states what it is worth, so only the volume cap answers");
	}

	[TestMethod]
	public void ANotionalExactlyAtTheCapIsCarried()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetExposure(_aapl, 2.5m, 200m).AssertNull("the cap is what may be carried, not what may not");
	}

	[TestMethod]
	public void TheNotionalCapIsMeasuredOnTheGapAndNotOnTheWholePosition()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetHedgePosition(_aapl, 8m, 200m);

		hedger.SetExposure(_aapl, 10m, 200m)
			.AssertNull("two thousand is held and wanted alike; only the four hundred still uncovered is measured");
	}

	[TestMethod]
	public void ThePriceTheHedgeSideStatesDoesNotPriceTheExposuresGap()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetExposure(_aapl, 0m, 0m).AssertNull("nothing is wanted and nothing is held");

		hedger.SetHedgePosition(_aapl, -4m, 200m)
			.AssertNull("what the hedge account paid prices what it holds; nothing says what the exposure is worth, so only the volume cap answers");
	}

	[TestMethod]
	public void TheGapIsPricedByTheExposureAndNotByWhatTheHedgeAccountPaid()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetExposure(_aapl, 2m, 200m).AssertNull("four hundred is inside the money cap");

		var order = hedger.SetHedgePosition(_aapl, -1m, 10m);

		order.AssertNotNull("three are uncovered at the two hundred the exposure was taken on, whatever the hedge account paid for what it holds");
		order.Volume.AssertEqual(3m);
		order.Side.AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public void APriceStatedAsZeroSaysNothingIsKnownAnyMore()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetExposure(_aapl, 2m, 200m).AssertNull("four hundred is inside the money cap");

		hedger.SetExposure(_aapl, 4m, 0m)
			.AssertNull("the row states no price, so the money cap has nothing to measure against and does not answer on one that stopped being true");
	}

	[TestMethod]
	public void AZeroFromOneSideLeavesTheOtherSidesPriceStanding()
	{
		var hedger = Hedger(maxVolume: 100m, maxNotional: 500m);

		hedger.SetExposure(_aapl, 2m, 200m).AssertNull("four hundred is inside the money cap");

		hedger.SetHedgePosition(_aapl, -1m, 0m)
			.AssertNotNull("the hedge side says nothing about what the exposure is worth, and three at two hundred is past the cap");
	}

	[TestMethod]
	public void TheGapIsNotSentTwiceWhileTheHedgeIsOnItsWay()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 20m, 0m).AssertNotNull();

		hedger.SetExposure(_aapl, 20m, 0m)
			.AssertNull("the same twenty are already on their way, and sending again would double them");
	}

	[TestMethod]
	public void AGapGrowingPastTheReservationIsHedgedForTheGrowthAlone()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 20m, 0m).AssertNotNull();

		var order = hedger.SetExposure(_aapl, 30m, 0m);

		order.AssertNotNull();
		order.Volume.AssertEqual(10m, "twenty are on their way already, so only the ten they do not cover go");
		order.Side.AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public void AFinishedHedgeGivesItsReservationBack()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, 20m, 0m);

		hedger.HedgeFinished(order.TransactionId).AssertTrue();

		hedger.SetExposure(_aapl, 20m, 0m)
			.AssertNotNull("nothing is on its way any more, so the gap is open again");
	}

	[TestMethod]
	public void AHedgeThatArrivedInThePositionNeedsNoReportToGiveItsReservationBack()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 20m, 0m).AssertNotNull("twenty are wanted and none are held");

		hedger.SetHedgePosition(_aapl, 20m, 0m)
			.AssertNull("the twenty that were on their way are held now, and nothing says so twice");

		hedger.SetExposure(_aapl, 40m, 0m)
			.AssertNotNull("what arrived stands for itself, so the twenty newly wanted are the whole gap");
	}

	[TestMethod]
	public void AHedgeThatArrivedInPartKeepsWhatIsStillOnItsWay()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 20m, 0m).AssertNotNull();

		hedger.SetHedgePosition(_aapl, 12m, 0m)
			.AssertNull("twelve of the twenty are held now and the other eight are still on their way");

		hedger.SetExposure(_aapl, 20m, 0m)
			.AssertNull("nothing more is wanted, and the eight still cover what is left");
	}

	[TestMethod]
	public void AReportOnAHedgeThatArrivedInPartGivesBackOnlyWhatIsLeftOfIt()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, 20m, 0m);

		hedger.SetHedgePosition(_aapl, 12m, 0m).AssertNull();

		hedger.HedgeFinished(order.TransactionId).AssertTrue("eight of it were still on their way");

		hedger.SetExposure(_aapl, 20m, 0m)
			.AssertNotNull("the eight never arrived and are not reserved any more, so they go again");
	}

	[TestMethod]
	public void APositionMovingAgainstTheHedgeLeavesItsReservationAlone()
	{
		var hedger = Hedger();

		hedger.SetExposure(_aapl, 20m, 0m).AssertNotNull("twenty are bought");

		hedger.SetHedgePosition(_aapl, -3m, 0m)
			.AssertNull("the account moved the other way, which is not the buy arriving; three short of twenty on its way is inside the cap");

		hedger.SetExposure(_aapl, 20m, 0m)
			.AssertNull("the twenty are still on their way and sending again would double them");
	}

	[TestMethod]
	public void AHedgeThePositionAlreadyAnsweredForIsNoLongerOnItsWay()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, 20m, 0m);

		hedger.SetHedgePosition(_aapl, 20m, 0m).AssertNull();

		hedger.HedgeFinished(order.TransactionId)
			.AssertFalse("the position it produced already gave the reservation back, and handing the same twenty back twice would open a gap that is closed");
	}

	[TestMethod]
	public void AHedgeNobodySentIsNotOurs()
	{
		var hedger = Hedger();

		hedger.HedgeFinished(4242).AssertFalse();
	}

	[TestMethod]
	public void AHedgeIsFinishedOnlyOnce()
	{
		var hedger = Hedger();

		var order = hedger.SetExposure(_aapl, 20m, 0m);

		hedger.HedgeFinished(order.TransactionId).AssertTrue();

		hedger.HedgeFinished(order.TransactionId)
			.AssertFalse("a hedge reported over twice must not hand its reservation back twice");
	}

	[TestMethod]
	public void AFinishedHedgeReleasesOnlyItsOwnInstrument()
	{
		var hedger = BothInstruments(maxVolume: 5m);

		var apple = hedger.SetExposure(_aapl, 20m, 0m);
		hedger.SetExposure(_msft, 20m, 0m).AssertNotNull();

		hedger.HedgeFinished(apple.TransactionId).AssertTrue();

		hedger.SetExposure(_msft, 20m, 0m)
			.AssertNull("the other instrument's hedge is still on its way and keeps its own reservation");
	}

	[TestMethod]
	public void EveryHedgeTakesATransactionOfItsOwn()
	{
		var hedger = BothInstruments(maxVolume: 5m);

		var apple = hedger.SetExposure(_aapl, 20m, 0m);
		var microsoft = hedger.SetExposure(_msft, 20m, 0m);

		apple.AssertNotNull();
		microsoft.AssertNotNull();
		(apple.TransactionId != microsoft.TransactionId).AssertTrue("two orders answering under one transaction are one order");
	}

	[TestMethod]
	public void TheTwoSidesAreKeptPerInstrument()
	{
		var hedger = BothInstruments(maxVolume: 5m);

		hedger.SetExposure(_aapl, 0m, 0m);
		hedger.SetHedgePosition(_aapl, 50m, 0m).AssertNotNull("what is held on one instrument is a gap on that one");

		hedger.SetExposure(_msft, 3m, 0m)
			.AssertNull("and says nothing about another, whose own gap is inside its cap");

		hedger.Count.AssertEqual(2);
	}
}
