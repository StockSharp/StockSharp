namespace StockSharp.Algo.Derivatives;

/// <summary>
/// The Greeks values calculating model by the Black formula.
/// </summary>
public class Black : BlackScholes
{
	// http://riskencyclopedia.com/articles/black_1976/

	/// <summary>
	/// Initializes a new instance of the <see cref="Black"/>.
	/// </summary>
	/// <param name="option">Options contract.</param>
	/// <param name="underlyingAsset">Underlying asset.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, <see cref="Security.ExpiryDate"/> is used when available: with its time of day, or at the board's expiry time for a date alone.</param>
	public Black(Security option, Security underlyingAsset, IMarketDataProvider dataProvider, DateTime? expirationTime = null)
		: base(option, underlyingAsset, dataProvider, expirationTime)
	{
	}

	/// <inheritdoc />
	public override decimal Dividend
	{
		set
		{
			if (value != 0)
				throw new ArgumentOutOfRangeException(LocalizedStrings.DivsNotPaid.Put(UnderlyingAsset));

			base.Dividend = value;
		}
	}

	/// <inheritdoc />
	protected override decimal CalcPremium(decimal deviation, decimal assetPrice, double timeToExp)
	{
		// The option is written on a forward, so both legs of the payoff are discounted once and by the
		// same rate: C = e^(-rT) * (F * N(d1) - K * N(d2)). Pricing the legs at a zero rate and applying
		// e^(-rT) to their difference is that formula, and it leaves the strike discounted exactly once.
		var premium = DerivativesHelper.Premium(OptionType, GetStrike(), assetPrice, 0, 0, deviation, timeToExp, D1(deviation, assetPrice, timeToExp));

		return (decimal)DerivativesHelper.ExpRate(RiskFree, timeToExp) * premium;
	}

	/// <inheritdoc />
	protected override decimal CalcDelta(decimal deviation, decimal assetPrice, double timeToExp)
		=> (decimal)DerivativesHelper.ExpRate(RiskFree, timeToExp) * base.CalcDelta(deviation, assetPrice, timeToExp);

	/// <inheritdoc />
	protected override decimal CalcGamma(decimal deviation, decimal assetPrice, double timeToExp)
		=> (decimal)DerivativesHelper.ExpRate(RiskFree, timeToExp) * base.CalcGamma(deviation, assetPrice, timeToExp);

	/// <inheritdoc />
	protected override decimal CalcVega(decimal deviation, decimal assetPrice, double timeToExp)
		=> (decimal)DerivativesHelper.ExpRate(RiskFree, timeToExp) * base.CalcVega(deviation, assetPrice, timeToExp);

	/// <inheritdoc />
	protected override decimal CalcTheta(decimal deviation, decimal assetPrice, double timeToExp)
	{
		var expRate = (decimal)DerivativesHelper.ExpRate(RiskFree, timeToExp);
		var diffusion = expRate * DerivativesHelper.Theta(OptionType, GetStrike(), assetPrice, 0, deviation, timeToExp, D1(deviation, assetPrice, timeToExp));
		var carry = RiskFree * CalcPremium(deviation, assetPrice, timeToExp) / 365m;

		return diffusion + carry;
	}

	/// <inheritdoc />
	protected override decimal CalcRho(decimal deviation, decimal assetPrice, double timeToExp)
		=> -0.01m * (decimal)timeToExp * CalcPremium(deviation, assetPrice, timeToExp);

	/// <inheritdoc />
	protected override double D1(decimal deviation, decimal assetPrice, double timeToExp)
	{
		return DerivativesHelper.D1(assetPrice, GetStrike(), 0, 0, deviation, timeToExp);
	}
}
