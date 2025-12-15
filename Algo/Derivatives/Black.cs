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
	public override decimal? Premium(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return GetExpRate(currentTime) * base.Premium(currentTime, deviation, assetPrice);
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
		return GetExpRate(currentTime) * base.Theta(currentTime, deviation, assetPrice);
	}

	/// <inheritdoc />
	public override decimal? Rho(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		return GetExpRate(currentTime) * base.Rho(currentTime, deviation, assetPrice);
	}

	/// <inheritdoc />
	protected override double D1(decimal deviation, decimal assetPrice, double timeToExp)
	{
		return DerivativesHelper.D1(assetPrice, GetStrike(), 0, 0, deviation, timeToExp);
	}
}
