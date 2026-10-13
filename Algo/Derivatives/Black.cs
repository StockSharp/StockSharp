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
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, midnight of <see cref="Security.ExpiryDate"/> is used when available.</param>
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

	private decimal? GetExpRate(DateTime currentTime)
	{
		var timeLine = GetExpirationTimeLine(currentTime);

		if (timeLine == null)
			return null;

		return (decimal)DerivativesHelper.ExpRate(RiskFree, timeLine.Value);
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
	public override decimal? Delta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return GetExpRate(currentTime) * base.Delta(currentTime, deviation, assetPrice);
	}

	/// <inheritdoc />
	public override decimal? Gamma(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return GetExpRate(currentTime) * base.Gamma(currentTime, deviation, assetPrice);
	}

	/// <inheritdoc />
	public override decimal? Vega(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return GetExpRate(currentTime) * base.Vega(currentTime, deviation, assetPrice);
	}

	/// <inheritdoc />
	public override decimal? Theta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		deviation ??= DefaultDeviation;
		assetPrice = GetAssetPrice(assetPrice);
		var timeToExp = GetExpirationTimeLine(currentTime);

		if (assetPrice is null || timeToExp is null)
			return null;

		var expRate = (decimal)DerivativesHelper.ExpRate(RiskFree, timeToExp.Value);
		var diffusion = expRate * DerivativesHelper.Theta(OptionType, GetStrike(), assetPrice.Value, 0, deviation.Value,
			timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value));
		var carry = RiskFree * CalcPremium(deviation.Value, assetPrice.Value, timeToExp.Value) / 365m;

		return TryRound(diffusion + carry);
	}

	/// <inheritdoc />
	public override decimal? Rho(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		deviation ??= DefaultDeviation;
		assetPrice = GetAssetPrice(assetPrice);
		var timeToExp = GetExpirationTimeLine(currentTime);

		if (assetPrice is null || timeToExp is null)
			return null;

		return TryRound(-0.01m * (decimal)timeToExp.Value * CalcPremium(deviation.Value, assetPrice.Value, timeToExp.Value));
	}

	/// <inheritdoc />
	protected override double D1(decimal deviation, decimal assetPrice, double timeToExp)
	{
		return DerivativesHelper.D1(assetPrice, GetStrike(), 0, 0, deviation, timeToExp);
	}
}
