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

		// DefaultDeviation should return 0 when Option is null instead of throwing NullReferenceException
		var deviation = bs.DefaultDeviation;
		deviation.AssertEqual(0m);
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
	public void D1_ZeroDeviation_ReturnsZero()
	{
		const decimal assetPrice = 100m;
		const decimal strike = 100m;
		const decimal riskFree = 0.05m;
		const decimal dividend = 0m;
		const decimal deviation = 0m;
		const double timeToExp = 0.5;

		// Zero deviation makes the divisor zero, so D1 returns the degenerate default 0.
		var d1 = DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, deviation, timeToExp);
		d1.AssertEqual(0);
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
	public void Synthetic_BuyCall_IsLongUnderlyingPlusLongPut()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Put, 110m, expiration), CreateOption(OptionTypes.Put, 100m, _underlyingExpiry)]);

		// Put-call parity: a long call is a long underlying plus a long put of the same strike and expiry,
		// so the decoy strike and the decoy series must both be passed over.
		var legs = new Synthetic(call, provider).Buy();

		legs.Length.AssertEqual(2);
		SideOf(legs, underlying).AssertEqual(Sides.Buy);
		SideOf(legs, put).AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public void Synthetic_SellPut_IsLongUnderlyingPlusShortCall()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);
		var put = CreateOption(OptionTypes.Put, 100m, expiration);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Call, 110m, expiration), CreateOption(OptionTypes.Call, 100m, _underlyingExpiry)]);

		// A long put is a short underlying plus a long call, so selling one is a long underlying and a short call.
		var legs = new Synthetic(put, provider).Sell();

		legs.Length.AssertEqual(2);
		SideOf(legs, underlying).AssertEqual(Sides.Buy);
		SideOf(legs, call).AssertEqual(Sides.Sell);
	}

	[TestMethod]
	public void Synthetic_OppositeOptionMissing_Throws()
	{
		var expiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, expiration);

		// Only the wrong strike and the wrong series are listed, so the position cannot be built at all: the
		// caller must be told rather than handed a one-legged position that no longer replicates the call.
		var provider = new CollectionSecurityProvider([underlying, call,
			CreateOption(OptionTypes.Put, 110m, expiration), CreateOption(OptionTypes.Put, 100m, _underlyingExpiry)]);

		ThrowsExactly<ArgumentException>(() => new Synthetic(call, provider).Buy());
	}

	[TestMethod]
	public void Synthetic_Underlying_BuyAndSell_UseRequestedStrikeAndExpiry()
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
		var buy = synthetic.Buy(100m, expiration);

		buy.Length.AssertEqual(2);
		SideOf(buy, call).AssertEqual(Sides.Buy);
		SideOf(buy, put).AssertEqual(Sides.Sell);

		var sell = synthetic.Sell(100m, expiration);

		sell.Length.AssertEqual(2);
		SideOf(sell, call).AssertEqual(Sides.Sell);
		SideOf(sell, put).AssertEqual(Sides.Buy);
	}

	[TestMethod]
	public void Synthetic_Underlying_BuyWithoutExpiry_UsesInstrumentExpiry()
	{
		var otherExpiration = new DateTime(2024, 7, 1).UtcKind();
		var underlying = CreateUnderlying();
		var call = CreateOption(OptionTypes.Call, 100m, _underlyingExpiry);
		var put = CreateOption(OptionTypes.Put, 100m, _underlyingExpiry);

		var provider = new CollectionSecurityProvider([underlying, call, put,
			CreateOption(OptionTypes.Call, 100m, otherExpiration), CreateOption(OptionTypes.Put, 100m, otherExpiration)]);

		// Without a date the series comes from the instrument being replicated, so the legs expire with it
		// on 19 September and not in the earlier series that is also listed.
		var legs = new Synthetic(underlying, provider).Buy(100m);

		legs.Length.AssertEqual(2);
		SideOf(legs, call).AssertEqual(Sides.Buy);
		SideOf(legs, put).AssertEqual(Sides.Sell);
	}
}
