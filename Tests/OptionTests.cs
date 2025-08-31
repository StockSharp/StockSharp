namespace StockSharp.Tests;

using StockSharp.Algo.Derivatives;

[TestClass]
public class OptionTests
{
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