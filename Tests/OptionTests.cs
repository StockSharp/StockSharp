namespace StockSharp.Tests;

using StockSharp.Algo.Derivatives;

[TestClass]
public class OptionTests : BaseTestClass
{
	// Test subclass to access protected constructor with null Option
	private class TestBlackScholes : BlackScholes
	{
		public TestBlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider)
			: base(underlyingAsset, dataProvider)
		{
		}
	}

	[TestMethod]
	public void BlackScholes_NullOption_DefaultDeviation()
	{
		var underlying = new Security { Id = "UNDER@TEST" };
		var dataProvider = Mock.Of<IMarketDataProvider>();

		// Use protected constructor that doesn't set Option
		var bs = new TestBlackScholes(underlying, dataProvider);

		// No option, so no volatility is quoted: unknown rather than a NullReferenceException.
		IsNull(bs.DefaultDeviation);
	}

	// No volatility quoted and none given: the model does not know the option's value or greeks, and says so rather than
	// pricing it as if the asset could not move - a delta of 0.5 on every strike.
	[TestMethod]
	public void BlackScholes_NoVolatilityQuoted_KnowsNothing()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var model = CreateBlackScholes(OptionTypes.Call, 100m, expiration, 0m, 0m);

		IsNull(model.Premium(now, assetPrice: 150m), "premium");
		IsNull(model.Delta(now, assetPrice: 150m), "delta");
		IsNull(model.Vega(now, assetPrice: 150m), "vega");
	}

	[TestMethod]
	public void BlackScholes_NullStrike_ThrowsMeaningfulException()
	{
		var underlying = new Security { Id = "UNDER@TEST" };
		var option = new Security
		{
			Id = "OPT@TEST",
			OptionType = OptionTypes.Call,
			Strike = null, // Bug: accessing .Value would throw NullReferenceException
		};
		var dataProvider = Mock.Of<IMarketDataProvider>();

		var now = new DateTime(2010, 1, 1).UtcKind();

		var bs = new BlackScholes(option, underlying, dataProvider, now.AddDays(90));

		// Should throw InvalidOperationException with meaningful message, not NullReferenceException
		ThrowsExactly<InvalidOperationException>(() =>
		{
			// Call Delta with a fixed time to trigger GetStrike() internally
			bs.Delta(now, 0.2m, 100m);
		});
	}

	[TestMethod]
	public void D1_ZeroDeviation_IsInfiniteOnTheSideOfTheForward()
	{
		const decimal assetPrice = 100m;
		const decimal strike = 100m;
		const decimal riskFree = 0.05m;
		const decimal dividend = 0m;
		const decimal deviation = 0m;
		const double timeToExp = 0.5;

		// Without volatility the forward 100 * e^(0.05 * 0.5) lies above the strike for certain: d1 is infinite.
		var d1 = DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, deviation, timeToExp);
		IsTrue(double.IsPositiveInfinity(d1), $"d1 is {d1}");

		// And below it when the asset pays out more than the rate earns.
		IsTrue(double.IsNegativeInfinity(DerivativesHelper.D1(assetPrice, strike, riskFree, 0.1m, deviation, timeToExp)));
	}

	[TestMethod]
	public void D1_ZeroTimeToExpiration_ReturnsZero()
	{
		const decimal assetPrice = 100m;
		const decimal strike = 100m;
		const decimal riskFree = 0.05m;
		const decimal dividend = 0m;
		const decimal deviation = 0.2m;
		const double timeToExp = 0;

		// Zero time to expiration makes the divisor zero, so D1 returns the degenerate default 0.
		var d1 = DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, deviation, timeToExp);
		d1.AssertEqual(0);
	}

	[TestMethod]
	public void D1_NegativeDeviation_Throws()
	{
		const decimal assetPrice = 100m;
		const decimal strike = 100m;
		const decimal riskFree = 0.05m;
		const decimal dividend = 0m;
		const decimal deviation = -0.2m;
		const double timeToExp = 0.5;

		// Negative deviation is an invalid input and must be rejected, not silently defaulted.
		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, deviation, timeToExp));
	}

	[TestMethod]
	public void Greeks()
	{
		var riskFree = 0.0m;

		const decimal volatility = 20.37m / 100;
		const decimal assetPrice = 196955m;
		const decimal dividend = 0m;
		const decimal strike = 195000;

		var timeToExp = DerivativesHelper.GetExpirationTimeLine(new DateTime(2011, 07, 15, 18, 45, 0), new DateTime(2011, 07, 08, 13, 0, 0))
			?? throw new InvalidOperationException();

		var d1 = DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, volatility, timeToExp);

		DerivativesHelper.Delta(OptionTypes.Call, assetPrice, d1).Round(2).AssertEqual(0.64m);
		DerivativesHelper.Gamma(assetPrice, volatility, timeToExp, d1).Round(5).AssertEqual(0.00007m);
		DerivativesHelper.Theta(OptionTypes.Call, strike, assetPrice, riskFree, volatility, timeToExp, d1).Round(2).AssertEqual(-145.80m);
		DerivativesHelper.Vega(assetPrice, timeToExp, d1).Round(2).AssertEqual(103.64m);
		DerivativesHelper.Rho(OptionTypes.Call, strike, assetPrice, riskFree, volatility, timeToExp, d1).Round(2).AssertEqual(24.39m);

		riskFree = 0.1m;
		d1 = DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, volatility, timeToExp);

		DerivativesHelper.Delta(OptionTypes.Call, assetPrice, d1).Round(2).AssertEqual(0.67m);
		DerivativesHelper.Gamma(assetPrice, volatility, timeToExp, d1).Round(5).AssertEqual(0.00006m);
		DerivativesHelper.Theta(OptionTypes.Call, strike, assetPrice, riskFree, volatility, timeToExp, d1).Round(2).AssertEqual(-176.86m);
		DerivativesHelper.Vega(assetPrice, timeToExp, d1).Round(2).AssertEqual(100.83m);
		DerivativesHelper.Rho(OptionTypes.Call, strike, assetPrice, riskFree, volatility, timeToExp, d1).Round(2).AssertEqual(25.34m);
	}

	// Prices an option the way BlackScholes.Premium does: d1 from the same inputs, then the premium formula.
	private static decimal Premium(OptionTypes optionType, decimal assetPrice, decimal strike, decimal riskFree, decimal dividend, decimal deviation, double timeToExp)
		=> DerivativesHelper.Premium(optionType, strike, assetPrice, riskFree, dividend, deviation, timeToExp,
			DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, deviation, timeToExp));

	[TestMethod]
	public void Premium_ZeroVolatility_IsIntrinsicValue()
	{
		const decimal assetPrice = 110m;
		const decimal strike = 100m;
		const decimal riskFree = 0m;
		const decimal dividend = 0m;
		const decimal deviation = 0m;
		const double timeToExp = 0.5;

		// A motionless asset ends at 110, so the payoff is certain: the call pays S - K = 10,
		// the put pays max(K - S, 0) = 0, and with a zero rate there is nothing to discount.
		Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(10m);
		Premium(OptionTypes.Put, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(0m);
	}

	[TestMethod]
	public void Premium_AtExpiry_IsIntrinsicValue()
	{
		const decimal assetPrice = 110m;
		const decimal strike = 100m;
		const decimal riskFree = 0.05m;
		const decimal dividend = 0m;
		const decimal deviation = 0.2m;
		const double timeToExp = 0;

		// At expiry no time value is left and the discount factor e^(-r*0) is 1, so the premium is
		// the payoff itself: max(S - K, 0) = 10 for the call and max(K - S, 0) = 0 for the put.
		Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(10m);
		Premium(OptionTypes.Put, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(0m);
	}

	[TestMethod]
	public void Premium_DeepInTheMoney_IsDiscountedIntrinsicValue()
	{
		const decimal assetPrice = 1000m;
		const decimal strike = 100m;
		const decimal riskFree = 0m;
		const decimal dividend = 0m;
		const decimal deviation = 0.2m;
		const double timeToExp = 0.5;

		// Spot ten times the strike puts d1 and d2 far above zero, so N(d1) = N(d2) = 1: the call is
		// worth S*e^(-qT) - K*e^(-rT) = 1000 - 100 = 900 at zero rates, and the put is never exercised.
		Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(900m);
		Premium(OptionTypes.Put, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(0m);
	}

	[TestMethod]
	public void Premium_DeepOutOfTheMoney_IsAlmostWorthless()
	{
		const decimal assetPrice = 10m;
		const decimal strike = 1000m;
		const decimal riskFree = 0m;
		const decimal dividend = 0m;
		const decimal deviation = 0.2m;
		const double timeToExp = 0.5;

		// A strike a hundred times spot puts d1 and d2 far below zero, so N(d1) = N(d2) = 0: the call
		// expires worthless, while the put is certain to be exercised for K - S = 1000 - 10 = 990.
		Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(0m);
		Premium(OptionTypes.Put, assetPrice, strike, riskFree, dividend, deviation, timeToExp).Round(2).AssertEqual(990m);
	}

	[TestMethod]
	public void Premium_PutCallParity_Holds()
	{
		const decimal assetPrice = 100m;
		const decimal strike = 95m;
		const decimal riskFree = 0.05m;
		const decimal dividend = 0.02m;
		const decimal deviation = 0.25m;
		const double timeToExp = 0.5;

		// Parity is model-free: long call plus short put replicates the forward, so
		// C - P = S*e^(-qT) - K*e^(-rT) = 100*e^(-0.01) - 95*e^(-0.025) = 6.350542.
		var forward = assetPrice * (decimal)Math.Exp(-(double)dividend * timeToExp)
			- strike * (decimal)Math.Exp(-(double)riskFree * timeToExp);

		var call = Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp);
		var put = Premium(OptionTypes.Put, assetPrice, strike, riskFree, dividend, deviation, timeToExp);

		forward.Round(6).AssertEqual(6.350542m);
		(call - put).Round(6).AssertEqual(forward.Round(6));
	}

	[TestMethod]
	public void ImpliedVolatility_RoundTripsPremium()
	{
		const decimal assetPrice = 110m;
		const decimal strike = 100m;
		const decimal riskFree = 0m;
		const decimal dividend = 0m;
		const decimal deviation = 0.25m;
		const double timeToExp = 0.5;

		var premium = Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp);

		// Inverting the model must return the volatility that priced it: a premium computed at 0.25
		// comes back as 25, since implied volatility is reported in percent (as Level1 ImpliedVolatility is).
		var implied = DerivativesHelper.ImpliedVolatility(premium,
			dev => Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, dev, timeToExp));

		implied.AssertNotNull();
		implied.Value.Round(2).AssertEqual(25m);
	}

	/// <summary>
	/// Implied volatility is a number a user acts on: it is compared across strikes and traded as a spread.
	/// The answer must be the volatility that actually explains the premium, however high that is. A premium
	/// no volatility below 200 per cent can reach must not come back as 200, because that value is the search
	/// stopping at its own ceiling and is indistinguishable from a genuine 200 per cent quote.
	/// </summary>
	[TestMethod]
	public void ImpliedVolatility_AboveTheSearchCeiling_StillReturnsTheVolatilityThatPricedThePremium()
	{
		const decimal assetPrice = 110m;
		const decimal strike = 100m;
		const decimal riskFree = 0m;
		const decimal dividend = 0m;
		const decimal deviation = 3m;
		const double timeToExp = 0.5;

		var premium = Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, deviation, timeToExp);

		var implied = DerivativesHelper.ImpliedVolatility(premium,
			dev => Premium(OptionTypes.Call, assetPrice, strike, riskFree, dividend, dev, timeToExp));

		// The premium was priced at a deviation of 3, so inverting it must report 300 per cent.
		implied.AssertNotNull();
		implied.Value.Round(1).AssertEqual(300m);
	}

	private const string _underlyingId = "SI-9.24@FORTS";

	private static readonly DateTime _underlyingExpiry = new DateTime(2024, 9, 19).UtcKind();

	private static Security CreateUnderlying()
		=> new()
		{
			Id = _underlyingId,
			Type = SecurityTypes.Future,
			ExpiryDate = _underlyingExpiry,
		};

	private static Security CreateOption(OptionTypes type, decimal strike, DateTime expiry)
		=> new()
		{
			Id = $"SI-{type}-{strike}-{expiry:yyyyMMdd}@FORTS",
			Type = SecurityTypes.Option,
			OptionType = type,
			Strike = strike,
			ExpiryDate = expiry,
			UnderlyingSecurityId = _underlyingId,
		};

	private static Black CreateBlack(OptionTypes type, decimal strike, DateTime expiration, decimal riskFree)
	{
		var underlying = CreateUnderlying();
		var option = CreateOption(type, strike, expiration);

		return new Black(option, underlying, Mock.Of<IMarketDataProvider>(), expiration) { RiskFree = riskFree };
	}

	[TestMethod]
	public void Black_Premium_NonZeroRate_DiscountsThePayoffOnce()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		// The model measures time against a 365-day line, so half of it is a time to expiry of exactly 0.5.
		var expiration = now.AddDays(182.5);

		var call = CreateBlack(OptionTypes.Call, 95m, expiration, 0.1m).Premium(now, 0.2m, 100m);
		var put = CreateBlack(OptionTypes.Put, 95m, expiration, 0.1m).Premium(now, 0.2m, 100m);

		// Black (1976) prices an option on a forward, so the payoff is discounted once and only once:
		// C = e^(-rT)*(F*N(d1) - K*N(d2)), P = e^(-rT)*(K*N(-d2) - F*N(-d1)), d1 = (ln(F/K) + s^2*T/2)/(s*sqrt(T)).
		// F=100, K=95, s=0.2, r=0.1, T=0.5 => d1 = (0.0512933 + 0.01)/0.1414214 = 0.4334090, d2 = 0.2919877,
		// N(d1) = 0.6676412, N(d2) = 0.6148520, e^(-0.05) = 0.9512294 => C = 7.94579, P = 3.18964.
		call.AssertNotNull();
		put.AssertNotNull();
		call.Value.Round(4).AssertEqual(7.9458m);
		put.Value.Round(4).AssertEqual(3.1896m);

		// Parity is model-free and holds whatever N() is worth: C - P = e^(-rT)*(F - K) = 0.9512294*5 = 4.756147.
		(call.Value - put.Value).Round(4).AssertEqual(4.7561m);
	}

	[TestMethod]
	public void Black_Delta_NonZeroRate_IsDiscountedExerciseProbability()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var call = CreateBlack(OptionTypes.Call, 95m, expiration, 0.1m).Delta(now, 0.2m, 100m);
		var put = CreateBlack(OptionTypes.Put, 95m, expiration, 0.1m).Delta(now, 0.2m, 100m);

		// Sensitivity to the forward is e^(-rT)*N(d1) for a call and e^(-rT)*(N(d1) - 1) for a put. With the
		// d1 above, 0.9512294*0.6676412 = 0.635080 and 0.9512294*(0.6676412 - 1) = -0.316149.
		call.AssertNotNull();
		put.AssertNotNull();
		call.Value.Round(4).AssertEqual(0.6351m);
		put.Value.Round(4).AssertEqual(-0.3161m);
	}

	[TestMethod]
	public void Black_GreeksMatchTheDiscountedPremiumDerivatives()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);
		const decimal deviation = 0.2m;
		const decimal assetPrice = 100m;
		const decimal priceStep = 0.001m;
		const decimal volatilityStep = 0.0001m;
		const decimal rateStep = 0.0001m;

		var model = CreateBlack(OptionTypes.Call, 95m, expiration, 0.1m);

		var deltaUp = model.Delta(now, deviation, assetPrice + priceStep).Value;
		var deltaDown = model.Delta(now, deviation, assetPrice - priceStep).Value;
		(model.Gamma(now, deviation, assetPrice).Value - (deltaUp - deltaDown) / (2 * priceStep))
			.Abs().AssertLess(0.000001m, "gamma");

		var premiumVolUp = model.Premium(now, deviation + volatilityStep, assetPrice).Value;
		var premiumVolDown = model.Premium(now, deviation - volatilityStep, assetPrice).Value;
		(model.Vega(now, deviation, assetPrice).Value - (premiumVolUp - premiumVolDown) / (2 * volatilityStep) * 0.01m)
			.Abs().AssertLess(0.000001m, "vega");

		var premiumTomorrow = model.Premium(now.AddHours(1), deviation, assetPrice).Value;
		var premiumYesterday = model.Premium(now.AddHours(-1), deviation, assetPrice).Value;
		(model.Theta(now, deviation, assetPrice).Value - (premiumTomorrow - premiumYesterday) * 12m)
			.Abs().AssertLess(0.000001m, "theta");

		var rate = model.RiskFree;
		model.RiskFree = rate + rateStep;
		var premiumRateUp = model.Premium(now, deviation, assetPrice).Value;
		model.RiskFree = rate - rateStep;
		var premiumRateDown = model.Premium(now, deviation, assetPrice).Value;
		model.RiskFree = rate;

		(model.Rho(now, deviation, assetPrice).Value - (premiumRateUp - premiumRateDown) / (2 * rateStep) * 0.01m)
			.Abs().AssertLess(0.000001m, "rho");
	}

	[TestMethod]
	public void Black_AtExpiry_ReportsNoValue()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var black = CreateBlack(OptionTypes.Call, 95m, expiration, 0.1m);

		// The model needs a positive time to expiry; at that moment and past it there is nothing left to price,
		// and the documented answer for "cannot be calculated" is null rather than an invented number.
		black.Premium(expiration, 0.2m, 100m).AssertNull();
		black.Delta(expiration, 0.2m, 100m).AssertNull();
		black.Premium(expiration.AddDays(1), 0.2m, 100m).AssertNull();
	}

	private static BlackScholes CreateBlackScholes(OptionTypes type, decimal strike, DateTime expiration, decimal riskFree, decimal dividend)
	{
		var underlying = CreateUnderlying();
		var option = CreateOption(type, strike, expiration);

		return new BlackScholes(option, underlying, Mock.Of<IMarketDataProvider>(), expiration)
		{
			RiskFree = riskFree,
			Dividend = dividend,
		};
	}

	/// <summary>
	/// Delta is the number a position is hedged on: it says what the option makes when the underlying moves
	/// by one. It must therefore be the slope of the premium the very same model prices. An asset that pays
	/// out is worth less to hold, which the premium discounts; a delta that does not discount it in step
	/// oversizes every hedge built from this model, by more the larger the yield and the longer the option.
	/// </summary>
	[TestMethod]
	public void Delta_IsTheSlopeOfThePremiumTheSameModelPrices()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		// The model measures time against a 365-day line, so half of it is a time to expiry of exactly 0.5.
		var expiration = now.AddDays(182.5);

		const decimal deviation = 0.2m;
		const decimal assetPrice = 100m;
		const decimal step = 0.01m;
		const decimal tolerance = 0.000001m;

		void check(decimal dividend)
		{
			var model = CreateBlackScholes(OptionTypes.Call, 100m, expiration, 0.05m, dividend);

			var delta = model.Delta(now, deviation, assetPrice);
			var up = model.Premium(now, deviation, assetPrice + step);
			var down = model.Premium(now, deviation, assetPrice - step);

			delta.AssertNotNull();
			up.AssertNotNull();
			down.AssertNotNull();

			var slope = (up.Value - down.Value) / (2 * step);

			(delta.Value - slope).Abs().AssertLess(tolerance, $"dividend {dividend}: delta {delta.Value}, premium slope {slope}");
		}

		// With nothing paid out the two agree, which is what makes the case below a statement about the
		// dividend and nothing else.
		check(0m);

		// A five per cent yield over half a year discounts the premium by e^(-0.025) = 0.975310, and the
		// slope of that premium with it: 0.975310 * N(d1) = 0.515148, not the undiscounted 0.528185.
		check(0.05m);
	}

	/// <summary>
	/// Vega is quoted per point of implied volatility, and a user reads it as what one point is worth. It has
	/// to be what one point actually adds to the premium the same model prices, the dividend included: a vega
	/// that ignores the payout overstates the money at stake in every volatility position.
	/// </summary>
	[TestMethod]
	public void Vega_IsWhatOnePointOfVolatilityAddsToThePremium()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		const decimal deviation = 0.2m;
		const decimal assetPrice = 100m;
		const decimal step = 0.0001m;
		const decimal tolerance = 0.000001m;

		void check(decimal dividend)
		{
			var model = CreateBlackScholes(OptionTypes.Call, 100m, expiration, 0.05m, dividend);

			var vega = model.Vega(now, deviation, assetPrice);
			var up = model.Premium(now, deviation + step, assetPrice);
			var down = model.Premium(now, deviation - step, assetPrice);

			vega.AssertNotNull();
			up.AssertNotNull();
			down.AssertNotNull();

			// One point is a hundredth of the deviation the model is given.
			var perPoint = (up.Value - down.Value) / (2 * step) / 100;

			(vega.Value - perPoint).Abs().AssertLess(tolerance, $"dividend {dividend}: vega {vega.Value}, premium per point {perPoint}");
		}

		check(0m);
		check(0.05m);
	}

	[TestMethod]
	public void BlackScholes_GammaThetaAndRhoMatchPremiumDerivatives()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);
		const decimal deviation = 0.2m;
		const decimal assetPrice = 100m;
		const decimal priceStep = 0.001m;
		const decimal rateStep = 0.0001m;

		foreach (var type in new[] { OptionTypes.Call, OptionTypes.Put })
		{
			var model = CreateBlackScholes(type, 100m, expiration, 0.05m, 0.04m);

			var gamma = model.Gamma(now, deviation, assetPrice).Value;
			var deltaUp = model.Delta(now, deviation, assetPrice + priceStep).Value;
			var deltaDown = model.Delta(now, deviation, assetPrice - priceStep).Value;
			(gamma - (deltaUp - deltaDown) / (2 * priceStep)).Abs().AssertLess(0.000001m, $"{type} gamma");

			var theta = model.Theta(now, deviation, assetPrice).Value;
			var premiumTomorrow = model.Premium(now.AddHours(1), deviation, assetPrice).Value;
			var premiumYesterday = model.Premium(now.AddHours(-1), deviation, assetPrice).Value;
			(theta - (premiumTomorrow - premiumYesterday) * 12m).Abs().AssertLess(0.000001m, $"{type} theta");

			var rate = model.RiskFree;
			model.RiskFree = rate + rateStep;
			var premiumRateUp = model.Premium(now, deviation, assetPrice).Value;
			model.RiskFree = rate - rateStep;
			var premiumRateDown = model.Premium(now, deviation, assetPrice).Value;
			model.RiskFree = rate;

			(model.Rho(now, deviation, assetPrice).Value - (premiumRateUp - premiumRateDown) / (2 * rateStep) * 0.01m)
				.Abs().AssertLess(0.000001m, $"{type} rho");
		}
	}

	// A basket of +10 calls and -5 puts, both struck at 100, held together with +3 of the underlying.
	private static (BasketBlackScholes basket, Security call, Security put) CreateBasket(Mock<IMarketDataProvider> dataProvider, DateTime expiration)
	{
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var positions = new[]
		{
			new Position { Security = underlying, CurrentValue = 3m },
			new Position { Security = call, CurrentValue = 10m },
			new Position { Security = put, CurrentValue = -5m },
		};

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(positions);

		var basket = new BasketBlackScholes(underlying, dataProvider.Object, positionProvider.Object, expiration);

		basket.InnerModels.Add(new BlackScholes(call, underlying, dataProvider.Object, expiration));
		basket.InnerModels.Add(new BlackScholes(put, underlying, dataProvider.Object, expiration));

		return (basket, call, put);
	}

	// The EXP line of an option position chart is the basket priced at the expiry moment: what each option pays there,
	// times the position held in it. A venue sends the expiry as a date alone or with its time of day.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void BasketBlackScholes_Premium_AtExpiry_IsPositionWeightedPayoff(bool withTimeOfDay)
	{
		var expiry = withTimeOfDay
			? new DateTime(2026, 12, 17, 15, 50, 0).UtcKind()
			: new DateTime(2026, 12, 17).UtcKind();

		var underlying = CreateUnderlying();
		var longCall = CreateOption(OptionTypes.Call, 110_000m, expiry);
		var shortCall = CreateOption(OptionTypes.Call, 115_000m, expiry);
		var put = CreateOption(OptionTypes.Put, 105_000m, expiry);

		var dataProvider = new Mock<IMarketDataProvider>();

		foreach (var option in new[] { longCall, shortCall, put })
			dataProvider.Setup(p => p.GetSecurityValue(option, Level1Fields.ImpliedVolatility)).Returns(20m);

		var positions = new[]
		{
			new Position { Security = longCall, CurrentValue = 10m },
			new Position { Security = shortCall, CurrentValue = -10m },
			new Position { Security = put, CurrentValue = 5m },
		};

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(positions);

		// Built as the option panels build it: no expiry moment of its own.
		var basket = new BasketBlackScholes(underlying, dataProvider.Object, positionProvider.Object);
		basket.InnerModels.Add(new BlackScholes(longCall, underlying, dataProvider.Object));
		basket.InnerModels.Add(new BlackScholes(shortCall, underlying, dataProvider.Object));
		basket.InnerModels.Add(new BlackScholes(put, underlying, dataProvider.Object));

		// At 100 000 only the puts pay, 5 * 5 000; at 112 000 the long calls, 10 * 2 000; at 120 000 the call spread is
		// capped, 10 * 10 000 - 10 * 5 000.
		void Check(decimal assetPrice, decimal expected)
		{
			var value = basket.Premium(expiry, assetPrice: assetPrice);
			value.AssertNotNull();
			value.Value.AssertEqual(expected, $"position value at expiry with the asset at {assetPrice}");
		}

		Check(100_000m, 25_000m);
		Check(112_000m, 20_000m);
		Check(120_000m, 50_000m);
	}

	[TestMethod]
	public void BasketBlackScholes_Delta_IsPositionWeightedSumPlusUnderlying()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var dataProvider = new Mock<IMarketDataProvider>();
		var (basket, call, put) = CreateBasket(dataProvider, expiration);

		dataProvider.Setup(p => p.GetSecurityValue(call, Level1Fields.ImpliedVolatility)).Returns(20m);
		dataProvider.Setup(p => p.GetSecurityValue(put, Level1Fields.ImpliedVolatility)).Returns(20m);

		// At S = K = 100 with a zero rate d1 = s*sqrt(T)/2; s = 0.2 (the quoted 20 per cent) and T = 0.5 give
		// d1 = 0.0707107, N(d1) = 0.5281860, so call delta = 0.5281860 and put delta = N(d1) - 1 = -0.4718140.
		// The basket is 10*0.5281860 - 5*(-0.4718140) + 3 (the underlying counts one for one) = 10.640930.
		var delta = basket.Delta(now, assetPrice: 100m);

		delta.AssertNotNull();
		delta.Value.Round(4).AssertEqual(10.6409m);
	}

	[TestMethod]
	public void BasketBlackScholes_Delta_ExplicitDeviation_NeedsNoQuotedVolatility()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		// Nothing is quoted for either option: the caller supplies the deviation and the asset price instead.
		var dataProvider = new Mock<IMarketDataProvider>();
		var (basket, _, _) = CreateBasket(dataProvider, expiration);

		// Every input the model needs was passed in, so both legs are computable and must be counted:
		// the answer is the same 10*0.5281860 - 5*(-0.4718140) + 3 = 10.640930 as when 20 is quoted.
		var delta = basket.Delta(now, 0.2m, 100m);

		delta.AssertNotNull();
		delta.Value.Round(4).AssertEqual(10.6409m);
	}

	[TestMethod]
	public void BasketBlackScholes_Delta_ExplicitDeviation_OverridesQuotedVolatility()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var dataProvider = new Mock<IMarketDataProvider>();
		var (basket, call, put) = CreateBasket(dataProvider, expiration);

		dataProvider.Setup(p => p.GetSecurityValue(call, Level1Fields.ImpliedVolatility)).Returns(20m);
		dataProvider.Setup(p => p.GetSecurityValue(put, Level1Fields.ImpliedVolatility)).Returns(20m);

		// The deviation the caller passes must price the basket, not the 20 the provider quotes: s = 0.3 gives
		// d1 = 0.3*sqrt(0.5)/2 = 0.1060660 and N(d1) = 0.5422350, so 10*0.5422350 - 5*(-0.4577650) + 3 = 10.711175.
		var delta = basket.Delta(now, 0.3m, 100m);

		delta.AssertNotNull();
		delta.Value.Round(4).AssertEqual(10.7112m);
	}

	private static Sides SideOf((Security security, Sides side)[] legs, Security security)
	{
		var found = legs.Where(l => l.security == security).ToArray();
		found.Length.AssertEqual(1, $"legs for {security.Id}");
		return found[0].side;
	}

	[TestMethod]
	public async Task Synthetic_BuyCall_IsLongUnderlyingPlusLongPut()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Put, 110m, expiration), CreateOption(OptionTypes.Put, 100m, _underlyingExpiry)]);

		// Put-call parity: a long call is a long underlying plus a long put of the same strike and expiry,
		// so the decoy strike and the decoy series must both be passed over.
		var legs = await new Synthetic(call, provider).BuyAsync(CancellationToken);

		legs.Length.AssertEqual(2);
		SideOf(legs, underlying).AssertEqual(Sides.Buy);
		SideOf(legs, put).AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public async Task Synthetic_SellPut_IsLongUnderlyingPlusShortCall()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Call, 110m, expiration), CreateOption(OptionTypes.Call, 100m, _underlyingExpiry)]);

		// A long put is a short underlying plus a long call, so selling one is a long underlying and a short call.
		var legs = await new Synthetic(put, provider).SellAsync(CancellationToken);

		legs.Length.AssertEqual(2);
		SideOf(legs, underlying).AssertEqual(Sides.Buy);
		SideOf(legs, call).AssertEqual(Sides.Sell);
	}

	[TestMethod]
	public async Task Synthetic_OppositeOptionMissing_Throws()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);

		// Only the wrong strike and the wrong series are listed, so the position cannot be built at all: the
		// caller must be told rather than handed a one-legged position that no longer replicates the call.
		var provider = new CollectionSecurityProvider([underlying, call,
			CreateOption(OptionTypes.Put, 110m, expiration), CreateOption(OptionTypes.Put, 100m, _underlyingExpiry)]);

		await ThrowsExactlyAsync<ArgumentException>(() => new Synthetic(call, provider).BuyAsync(CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Synthetic_Underlying_BuyAndSell_UseRequestedStrikeAndExpiry()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Call, 110m, expiration), CreateOption(OptionTypes.Put, 110m, expiration),
			CreateOption(OptionTypes.Call, 100m, _underlyingExpiry), CreateOption(OptionTypes.Put, 100m, _underlyingExpiry)]);

		var synthetic = new Synthetic(underlying, provider);

		// A synthetic long forward is a long call and a short put at one strike and one expiry; selling it
		// flips both legs. The requested 100 / 1 July pair must be picked out of the other strike and series.
		var buy = await synthetic.BuyAsync(100m, expiration, CancellationToken);

		buy.Length.AssertEqual(2);
		SideOf(buy, call).AssertEqual(Sides.Buy);
		SideOf(buy, put).AssertEqual(Sides.Sell);

		var sell = await synthetic.SellAsync(100m, expiration, CancellationToken);

		sell.Length.AssertEqual(2);
		SideOf(sell, call).AssertEqual(Sides.Sell);
		SideOf(sell, put).AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public async Task Synthetic_Underlying_BuyWithoutExpiry_UsesInstrumentExpiry()
	{
		var otherExpiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, _underlyingExpiry);
		var put = CreateOption(OptionTypes.Put, 100m, _underlyingExpiry);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Call, 100m, otherExpiration), CreateOption(OptionTypes.Put, 100m, otherExpiration)]);

		// Without a date the series comes from the instrument being replicated, so the legs expire with it
		// on 19 September and not in the earlier series that is also listed.
		var legs = await new Synthetic(underlying, provider).BuyAsync(100m, CancellationToken);

		legs.Length.AssertEqual(2);
		SideOf(legs, call).AssertEqual(Sides.Buy);
		SideOf(legs, put).AssertEqual(Sides.Sell);
	}

	[TestMethod]
	public async Task OffsetBasketStrike_ChoosesStrikesFromLoadedDerivatives()
	{
		var underlying = new Security { Id = "BASKETU@TEST" };
		var expiry = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		Security option(OptionTypes type, decimal strike) => new()
		{
			Id = $"BASKETU{type}{strike}@TEST",
			Type = SecurityTypes.Option,
			OptionType = type,
			Strike = strike,
			UnderlyingSecurityId = underlying.Id,
			ExpiryDate = expiry,
		};

		var call100 = option(OptionTypes.Call, 100);
		var put100 = option(OptionTypes.Put, 100);

		var provider = new CollectionSecurityProvider(
		[
			underlying,
			option(OptionTypes.Call, 90), call100, option(OptionTypes.Call, 110),
			option(OptionTypes.Put, 90), put100, option(OptionTypes.Put, 110),
		]);

		var dataProvider = new Mock<IMarketDataProvider>();
		dataProvider.Setup(p => p.GetSecurityValue(underlying, Level1Fields.LastTradePrice)).Returns(100m);

		var basket = new OffsetBasketStrike(underlying, provider, dataProvider.Object, new(0, 0));

		Throws<InvalidOperationException>(() => basket.InnerSecurityIds.ToArray());

		await basket.LoadDerivativesAsync(CancellationToken);

		var ids = basket.InnerSecurityIds.ToHashSet();

		AreEqual(2, ids.Count);
		IsTrue(ids.Contains(call100.ToSecurityId()));
		IsTrue(ids.Contains(put100.ToSecurityId()));
	}

	// Without volatility the forward is known, so the option finishes in the money or it does not: its delta is one or
	// nought, not the one half of an option at the money.
	[TestMethod]
	public void Delta_WithoutVolatility_IsWhetherTheOptionFinishesInTheMoney()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var call = CreateBlackScholes(OptionTypes.Call, 100m, expiration, 0m, 0m);
		var put = CreateBlackScholes(OptionTypes.Put, 100m, expiration, 0m, 0m);

		AreEqual(1m, call.Delta(now, 0m, 150m), "a call far in the money");
		AreEqual(0m, call.Delta(now, 0m, 50m), "a call far out of the money");
		AreEqual(0m, put.Delta(now, 0m, 150m), "a put far out of the money");
		AreEqual(-1m, put.Delta(now, 0m, 50m), "a put far in the money");
	}

	// Nor does a point of volatility move the premium of an option that finishes in or out of the money for certain.
	[TestMethod]
	public void Vega_WithoutVolatility_IsNothingAwayFromTheStrike()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var call = CreateBlackScholes(OptionTypes.Call, 100m, expiration, 0m, 0m);

		AreEqual(0m, call.Vega(now, 0m, 150m), "a call far in the money");
		AreEqual(0m, call.Vega(now, 0m, 50m), "a call far out of the money");
	}

	// Without volatility the premium of a call that finishes in the money is the asset less the discounted strike: its
	// theta is the strike's discount wearing off day by day, its rho the strike's present value falling with the rate.
	[TestMethod]
	public void ThetaAndRho_WithoutVolatility_AreTheSlopesOfTheDiscountedIntrinsicValue()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var call = CreateBlackScholes(OptionTypes.Call, 100m, expiration, 0.05m, 0m);
		var discountedStrike = 100m * (decimal)Math.Exp(-0.05 * 0.5);

		AreEqual(Math.Round(-0.05m * discountedStrike / 365m, 6), Math.Round(call.Theta(now, 0m, 150m).Value, 6), "theta of a call far in the money");
		AreEqual(Math.Round(0.01m * 0.5m * discountedStrike, 6), Math.Round(call.Rho(now, 0m, 150m).Value, 6), "rho of a call far in the money");
		AreEqual(0m, call.Theta(now, 0m, 50m), "theta of a call far out of the money");
		AreEqual(0m, call.Rho(now, 0m, 50m), "rho of a call far out of the money");
	}

	// The offsets around the central strike count strikes: one to each side keeps both types listed at three strikes,
	// however many contracts each strike has.
	[TestMethod]
	public void FilterByStrikes_CountsStrikesNotContracts()
	{
		var expiry = new DateTime(2026, 12, 17).UtcKind();

		var options = new[] { 80m, 90m, 100m, 110m, 120m }
			.SelectMany(strike => new[] { CreateOption(OptionTypes.Call, strike, expiry), CreateOption(OptionTypes.Put, strike, expiry) })
			.ToArray();

		var kept = options.FilterByStrikes(100m, 1, 1).ToArray();

		AreEqual(6, kept.Length);
		IsTrue(kept.All(o => o.Strike is 90m or 100m or 110m), string.Join(", ", kept.Select(o => o.Id)));
	}

	// Two expiries list the same strikes: a strike out keeps it in both series, and an offset left out keeps every strike
	// on its side.
	[TestMethod]
	public void FilterByStrikes_CountsAStrikeOnceAcrossSeries()
	{
		var near = new DateTime(2026, 12, 17).UtcKind();
		var far = new DateTime(2027, 3, 18).UtcKind();

		var options = new[] { near, far }
			.SelectMany(expiry => new[] { 80m, 90m, 100m, 110m, 120m }.Select(strike => CreateOption(OptionTypes.Call, strike, expiry)))
			.ToArray();

		var kept = options.FilterByStrikes(100m, 1, null).ToArray();

		AreEqual(8, kept.Length);
		IsTrue(kept.All(o => o.Strike >= 90m), string.Join(", ", kept.Select(o => o.Id)));
	}

	// A venue sends the expiry with its time of day. Until that moment the option trades and has time left; the model is
	// built the way the option panels build it, with no expiry moment of its own.
	[TestMethod]
	public void Premium_OnTheExpiryDayBeforeTheExpiryTime_StillHasTimeValue()
	{
		var expiry = new DateTime(2026, 12, 17, 15, 50, 0).UtcKind();
		var morning = new DateTime(2026, 12, 17, 10, 0, 0).UtcKind();

		var model = new BlackScholes(CreateOption(OptionTypes.Call, 100m, expiry), CreateUnderlying(), Mock.Of<IMarketDataProvider>());

		var premium = model.Premium(morning, 0.2m, 100m);

		IsNotNull(premium, "the option still trades on the morning of its expiry day");
		IsTrue(premium > 0m, $"an option at the money with hours to go is worth something, not {premium}");
		IsNotNull(model.Delta(morning, 0.2m, 100m), "its delta on the morning of its expiry day");
	}

	// When no option of the basket can be priced - none has a quoted volatility and the caller gave none - its value and
	// greeks are unknown, not nought: a nought reads as a flat book.
	[TestMethod]
	public void BasketBlackScholes_NoOptionPriceable_IsUnknown()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var (basket, _, _) = CreateBasket(new Mock<IMarketDataProvider>(), expiration);

		IsNull(basket.Premium(now, assetPrice: 100m), "premium");
		IsNull(basket.Delta(now, assetPrice: 100m), "delta");
		IsNull(basket.Gamma(now, assetPrice: 100m), "gamma");
	}

	// One option of the basket without a quoted volatility makes the basket's figures unknown: the sum of the others is not
	// the basket's, and shown as its total it misstates the book.
	[TestMethod]
	public void BasketBlackScholes_AnOptionNotPriceable_LeavesTheBasketUnknown()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var dataProvider = new Mock<IMarketDataProvider>();
		var (basket, call, _) = CreateBasket(dataProvider, expiration);

		dataProvider.Setup(p => p.GetSecurityValue(call, Level1Fields.ImpliedVolatility)).Returns(20m);

		IsNull(basket.Premium(now, assetPrice: 100m), "premium");
		IsNull(basket.Delta(now, assetPrice: 100m), "delta");
	}

	// At expiry the basket's delta is the slope of what it pays there - the payoff its premium returns - as it is a
	// minute before: the options that finish in the money count one for one.
	[TestMethod]
	public void BasketBlackScholes_Delta_AtExpiry_IsTheSlopeOfThePayoff()
	{
		var expiry = new DateTime(2026, 12, 17).UtcKind();

		var underlying = CreateUnderlying();
		var longCall = CreateOption(OptionTypes.Call, 110_000m, expiry);
		var shortCall = CreateOption(OptionTypes.Call, 115_000m, expiry);
		var put = CreateOption(OptionTypes.Put, 105_000m, expiry);

		var dataProvider = new Mock<IMarketDataProvider>();

		foreach (var option in new[] { longCall, shortCall, put })
			dataProvider.Setup(p => p.GetSecurityValue(option, Level1Fields.ImpliedVolatility)).Returns(20m);

		var positions = new[]
		{
			new Position { Security = longCall, CurrentValue = 10m },
			new Position { Security = shortCall, CurrentValue = -10m },
			new Position { Security = put, CurrentValue = 5m },
		};

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(positions);

		var basket = new BasketBlackScholes(underlying, dataProvider.Object, positionProvider.Object);
		basket.InnerModels.Add(new BlackScholes(longCall, underlying, dataProvider.Object));
		basket.InnerModels.Add(new BlackScholes(shortCall, underlying, dataProvider.Object));
		basket.InnerModels.Add(new BlackScholes(put, underlying, dataProvider.Object));

		// At 100 000 only the five puts are in the money; at 112 000 only the ten long calls.
		AreEqual(-5m, basket.Delta(expiry, assetPrice: 100_000m), "at 100 000");
		AreEqual(10m, basket.Delta(expiry, assetPrice: 112_000m), "at 112 000");
	}

	// The volatility of a basket is the one at which its own premium is the premium given, as for a single option: a
	// straddle priced at 20 per cent comes back at 20 per cent.
	[TestMethod]
	public void BasketBlackScholes_ImpliedVolatility_InvertsTheBasketsOwnPremium()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var dataProvider = new Mock<IMarketDataProvider>();
		dataProvider.Setup(p => p.GetSecurityValue(underlying, Level1Fields.LastTradePrice)).Returns(100m);
		dataProvider.Setup(p => p.GetSecurityValue(call, Level1Fields.ImpliedVolatility)).Returns(20m);
		dataProvider.Setup(p => p.GetSecurityValue(put, Level1Fields.ImpliedVolatility)).Returns(20m);

		var positions = new[]
		{
			new Position { Security = call, CurrentValue = 1m },
			new Position { Security = put, CurrentValue = 1m },
		};

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(positions);

		var basket = new BasketBlackScholes(underlying, dataProvider.Object, positionProvider.Object, expiration);
		basket.InnerModels.Add(new BlackScholes(call, underlying, dataProvider.Object, expiration));
		basket.InnerModels.Add(new BlackScholes(put, underlying, dataProvider.Object, expiration));

		var premium = basket.Premium(now, 0.2m).Value;

		AreEqual(20m, basket.ImpliedVolatility(now, premium)?.Round(1));
	}

	// An option listed with its expiry date alone expires at its board's expiry time, which is the board's local time: on a
	// board three hours east of UTC that closes its options at 18:45, the option has a quarter of an hour left at 15:30 UTC
	// and none at 15:45.
	[TestMethod]
	public void BlackScholes_ExpiryDateAlone_ExpiresAtTheBoardsExpiryTimeInItsZone()
	{
		var day = new DateTime(2026, 3, 19).UtcKind();
		var option = CreateOption(OptionTypes.Call, 100m, day);

		option.Board = new ExchangeBoard
		{
			Code = "EAST",
			Exchange = new Exchange { Name = "EAST" },
			ExpiryTime = new TimeSpan(18, 45, 0),
			TimeZone = TimeZoneInfo.CreateCustomTimeZone("UTC+3", TimeSpan.FromHours(3), "UTC+3", "UTC+3"),
		};

		var model = new BlackScholes(option, CreateUnderlying(), Mock.Of<IMarketDataProvider>());

		AreEqual((double)TimeSpan.FromMinutes(15).Ticks / TimeSpan.FromDays(365).Ticks, model.GetExpirationTimeLine(day.AddHours(15).AddMinutes(30)));
		IsNull(model.GetExpirationTimeLine(day.AddHours(15).AddMinutes(45)), "the time left at 18:45 of the board");
	}

	// A straddle of the given positions in a call and a put struck at 100, on an asset the market prices at 100.
	private static BasketBlackScholes CreateStraddle(DateTime expiration, decimal position)
	{
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var dataProvider = new Mock<IMarketDataProvider>();
		dataProvider.Setup(p => p.GetSecurityValue(underlying, Level1Fields.LastTradePrice)).Returns(100m);

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(
		[
			new Position { Security = call, CurrentValue = position },
			new Position { Security = put, CurrentValue = position },
		]);

		var basket = new BasketBlackScholes(underlying, dataProvider.Object, positionProvider.Object, expiration);
		basket.InnerModels.Add(new BlackScholes(call, underlying, dataProvider.Object, expiration));
		basket.InnerModels.Add(new BlackScholes(put, underlying, dataProvider.Object, expiration));

		return basket;
	}

	// A basket rounds the volatility it reports, not the premiums of its options its search compares: a straddle priced at
	// 25 per cent comes back at 25.00.
	[TestMethod]
	public void BasketBlackScholes_ImpliedVolatility_RoundDecimals_RoundsTheAnswerNotTheSearch()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var basket = CreateStraddle(now.AddDays(36.5), 1m);

		var premium = basket.Premium(now, 0.25m).Value;

		basket.RoundDecimals = 2;

		AreEqual(25.00m, basket.ImpliedVolatility(now, premium));
	}

	// A book short of volatility - a sold straddle - is worth less the higher the volatility: its premium comes back at the
	// volatility that priced it all the same.
	[TestMethod]
	public void BasketBlackScholes_ImpliedVolatility_OfABookShortOfVolatility()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var basket = CreateStraddle(now.AddDays(182.5), -1m);

		var premium = basket.Premium(now, 0.2m).Value;

		IsTrue(premium < 0, $"A sold straddle is worth {premium}.");
		AreEqual(20m, basket.ImpliedVolatility(now, premium)?.Round(1));
	}

	// A basket that holds no option and was given no asset of its own - the basket element of a scheme whose list of options
	// came empty - is a book of nothing: it is worth nought and has no slope, rather than failing for want of an asset, and
	// no volatility explains a premium of it.
	[TestMethod]
	public void BasketBlackScholes_WithNothingInIt_IsWorthNothing()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(Array.Empty<Position>());

		var basket = new BasketBlackScholes(new Mock<IMarketDataProvider>().Object, positionProvider.Object);

		AreEqual(0m, basket.Premium(now, 0.2m, 100m), "premium");
		AreEqual(0m, basket.Delta(now, 0.2m, 100m), "delta");
		IsNull(basket.ImpliedVolatility(now, 1m), "volatility");
	}

	// The expiry moment a basket is given is the one its constructor promises to price every option to; the options here
	// are listed to an earlier date.
	[TestMethod]
	public void BasketBlackScholes_ExpirationOfItsOwn_PricesEveryOptionToIt()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var listed = new DateTime(2024, 4, 1).UtcKind();

		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, listed);
		var put = CreateOption(OptionTypes.Put, 100m, listed);

		var positions = new[]
		{
			new Position { Security = call, CurrentValue = 10m },
			new Position { Security = put, CurrentValue = -5m },
		};

		var positionProvider = new Mock<IPositionProvider>();
		positionProvider.Setup(p => p.Positions).Returns(positions);

		var dataProvider = Mock.Of<IMarketDataProvider>();

		var basket = new BasketBlackScholes(underlying, dataProvider, positionProvider.Object, now.AddDays(182.5));
		basket.InnerModels.Add(new BlackScholes(call, underlying, dataProvider));
		basket.InnerModels.Add(new BlackScholes(put, underlying, dataProvider));

		// Half a year at 20 per cent prices either option at 5.6372 with the asset on the strike: 10 * 5.6372 - 5 * 5.6372.
		AreEqual(28.1860m, basket.Premium(now, 0.2m, 100m)?.Round(4));
	}

	// A basket with no expiry moment of its own has no time line of its own either: it is not an option.
	[TestMethod]
	public void BasketBlackScholes_WithoutAnExpirationOfItsOwn_HasNoTimeLine()
	{
		var basket = new BasketBlackScholes(CreateUnderlying(), Mock.Of<IMarketDataProvider>(), Mock.Of<IPositionProvider>());

		IsNull(basket.GetExpirationTimeLine(new DateTime(2024, 1, 1).UtcKind()));
	}

	// The basket's delta is the slope of the basket's premium, as for a single option: what is held of the asset counts in
	// both, or in neither.
	[TestMethod]
	public void BasketBlackScholes_Delta_IsTheSlopeOfTheBasketsPremium()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var (basket, _, _) = CreateBasket(new Mock<IMarketDataProvider>(), now.AddDays(182.5));

		var delta = basket.Delta(now, 0.2m, 100m).Value;
		var slope = (basket.Premium(now, 0.2m, 100.01m).Value - basket.Premium(now, 0.2m, 99.99m).Value) / 0.02m;

		IsTrue((delta - slope).Abs() < 0.0001m, $"delta {delta}, the slope of the premium {slope}");
	}

	// A level of the book that no volatility explains - a bid under what the option is worth for certain, an ask above the
	// asset itself - is left out of the book of volatilities rather than shown at 0 per cent.
	[TestMethod]
	public void ImpliedVolatilityBook_ALevelNoVolatilityExplains_IsLeftOut()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(182.5);

		var underlying = CreateUnderlying();
		var option = CreateOption(OptionTypes.Call, 100m, expiration);

		var dataProvider = new Mock<IMarketDataProvider>();
		dataProvider.Setup(p => p.GetSecurityValue(underlying, Level1Fields.LastTradePrice)).Returns(150m);

		var model = new BlackScholes(option, underlying, dataProvider.Object, expiration);

		var book = new QuoteChangeMessage
		{
			SecurityId = option.ToSecurityId(),
			ServerTime = now,
			Bids = [new(52m, 1m), new(49m, 1m)],
			Asks = [new(53m, 1m), new(200m, 1m)],
		};

		var volatility = book.ImpliedVolatility(model, now);

		AreEqual(1, volatility.Bids.Length, "bids");
		AreEqual(1, volatility.Asks.Length, "asks");
	}

	// A FORTS option sent with its expiry date alone expires at 18:45 in Moscow, 15:45 by the clock UTC.
	[TestMethod]
	public void IsExpired_ExpiryDateAlone_TakesTheBoardsExpiryTimeInItsTimeZone()
	{
		var option = CreateOption(OptionTypes.Call, 100m, new DateTime(2024, 9, 19).UtcKind());
		var exchangeInfo = new InMemoryExchangeInfoProvider();

		IsFalse(option.IsExpired(exchangeInfo, new DateTime(2024, 9, 19, 15, 0, 0).UtcKind()), "at 18:00 in Moscow");
		IsTrue(option.IsExpired(exchangeInfo, new DateTime(2024, 9, 19, 16, 0, 0).UtcKind()), "at 19:00 in Moscow");
	}

	// Strikes widen away from the money on some venues: one step out of the money is the next strike listed, whatever the
	// distance between the two lowest strikes.
	[TestMethod]
	public async Task OffsetBasketStrike_OneStepOut_IsTheNextListedStrike()
	{
		var underlying = new Security { Id = "UNEVEN@TEST" };
		var expiry = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		Security option(OptionTypes type, decimal strike) => new()
		{
			Id = $"UNEVEN{type}{strike}@TEST",
			Type = SecurityTypes.Option,
			OptionType = type,
			Strike = strike,
			UnderlyingSecurityId = underlying.Id,
			ExpiryDate = expiry,
		};

		decimal[] strikes = [50, 75, 100, 105, 110, 115, 120];
		var calls = strikes.ToDictionary(s => s, s => option(OptionTypes.Call, s));
		var puts = strikes.ToDictionary(s => s, s => option(OptionTypes.Put, s));

		var provider = new CollectionSecurityProvider([underlying, .. calls.Values, .. puts.Values]);

		var dataProvider = new Mock<IMarketDataProvider>();
		dataProvider.Setup(p => p.GetSecurityValue(underlying, Level1Fields.LastTradePrice)).Returns(105m);

		var basket = new OffsetBasketStrike(underlying, provider, dataProvider.Object, new(0, 1));
		await basket.LoadDerivativesAsync(CancellationToken);

		var expected = new[] { calls[105], calls[110], puts[100], puts[105] }.Select(s => s.ToSecurityId()).ToHashSet();

		IsTrue(expected.SetEquals(basket.InnerSecurityIds), $"the basket holds {basket.InnerSecurityIds.Select(id => id.SecurityCode).JoinCommaSpace()}");
	}

	// An offset into the money is negative, and a basket saved with one comes back with it.
	[TestMethod]
	public void OffsetBasketStrike_InTheMoneyOffset_SurvivesItsExpression()
	{
		var underlying = new Security { Id = "BASKETU@TEST" };
		var provider = new CollectionSecurityProvider([underlying]);
		var dataProvider = Mock.Of<IMarketDataProvider>();

		var saved = new OffsetBasketStrike(underlying, provider, dataProvider, new(-2, 3));
		var loaded = new OffsetBasketStrike(underlying, provider, dataProvider, new(0, 0));

		loaded.BasketExpression = saved.BasketExpression;

		AreEqual(saved.BasketExpression, loaded.BasketExpression);
	}

	// A basket of strikes is a basket: storage and subscriptions ask for its code.
	[TestMethod]
	public void OffsetBasketStrike_IsABasket()
	{
		var underlying = new Security { Id = "BASKETU@TEST" };
		var basket = new OffsetBasketStrike(underlying, new CollectionSecurityProvider([underlying]), Mock.Of<IMarketDataProvider>(), new(0, 1));

		IsTrue(basket.IsBasket());
	}

	// Rounding is applied to the answer, not to the premiums the search compares: an option priced at 25 per cent comes
	// back at 25.00.
	[TestMethod]
	public void ImpliedVolatility_RoundDecimals_RoundsTheAnswerNotTheSearch()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var expiration = now.AddDays(36.5);

		var underlying = CreateUnderlying();

		var dataProvider = new Mock<IMarketDataProvider>();
		dataProvider.Setup(p => p.GetSecurityValue(underlying, Level1Fields.LastTradePrice)).Returns(100m);

		var model = new BlackScholes(CreateOption(OptionTypes.Call, 110m, expiration), underlying, dataProvider.Object, expiration);

		var premium = model.Premium(now, 0.25m).Value;

		model.RoundDecimals = 2;

		AreEqual(25.00m, model.ImpliedVolatility(now, premium));
	}

	// The Black model rounds what it reports, as every model does, and not a figure it then goes on to discount: a delta of
	// 0.635 reads 0.64.
	[TestMethod]
	public void Black_RoundDecimals_RoundsTheGreeksItReports()
	{
		var now = new DateTime(2024, 1, 1).UtcKind();
		var model = CreateBlack(OptionTypes.Call, 95m, now.AddDays(182.5), 0.1m);

		model.RoundDecimals = 2;

		AreEqual(0.64m, model.Delta(now, 0.2m, 100m), "delta");
		AreEqual(0.02m, model.Gamma(now, 0.2m, 100m), "gamma");
		AreEqual(0.24m, model.Vega(now, 0.2m, 100m), "vega");
	}
}
