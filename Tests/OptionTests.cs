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
		const decimal deviation = 0m; // Zero deviation causes division by zero
		const double timeToExp = 0.5; // double, not decimal

		// Should return 0 instead of throwing DivideByZeroException
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
		const double timeToExp = 0; // Zero time causes Sqrt(0) = 0, division by zero

		// Should return 0 instead of throwing DivideByZeroException
		var d1 = DerivativesHelper.D1(assetPrice, strike, riskFree, dividend, deviation, timeToExp);
		d1.AssertEqual(0);
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
}