namespace StockSharp.Algo.Derivatives;

/// <summary>
/// The model for calculating Greeks values by the Black-Scholes formula.
/// </summary>
public class BlackScholes : IBlackScholes
{
	/// <summary>
	/// Base constructor for inherited models.
	/// </summary>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, midnight of <see cref="Security.ExpiryDate"/> is used when available.</param>
	protected BlackScholes(IMarketDataProvider dataProvider, DateTime? expirationTime = null)
	{
		DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		ExpirationTime = expirationTime;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BlackScholes"/> for a specific option with its underlying asset.
	/// </summary>
	/// <param name="option">Options contract.</param>
	/// <param name="underlyingAsset">Underlying asset.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, midnight of <see cref="Security.ExpiryDate"/> is used when available.</param>
	public BlackScholes(Security option, Security underlyingAsset, IMarketDataProvider dataProvider, DateTime? expirationTime = null)
		: this(dataProvider, expirationTime)
	{
		Option = option ?? throw new ArgumentNullException(nameof(option));
		_underlyingAsset = underlyingAsset ?? throw new ArgumentNullException(nameof(underlyingAsset));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlackScholes"/> for an underlying asset only (non-option models).
	/// </summary>
	/// <param name="underlyingAsset">Underlying asset.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, midnight of <see cref="Security.ExpiryDate"/> is used when available.</param>
	protected BlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider, DateTime? expirationTime = null)
		: this(dataProvider, expirationTime)
	{
		_underlyingAsset = underlyingAsset ?? throw new ArgumentNullException(nameof(underlyingAsset));
	}

	/// <summary>
	/// The market data provider.
	/// </summary>
	public virtual IMarketDataProvider DataProvider { get; }

	/// <summary>
	/// Explicit expiration moment. If <c>null</c>, midnight of <see cref="Security.ExpiryDate"/> is used when available.
	/// </summary>
	public DateTime? ExpirationTime { get; set; }

	/// <inheritdoc />
	public virtual Security Option { get; }

	/// <inheritdoc />
	public decimal RiskFree { get; set; }

	/// <inheritdoc />
	public virtual decimal Dividend { get; set; }

	private int _roundDecimals = -1;

	/// <summary>
	/// The number of decimal places at calculated values. The default is -1, which means no values rounding.
	/// </summary>
	public virtual int RoundDecimals
	{
		get => _roundDecimals;
		set
		{
			if (value < -1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_roundDecimals = value;
		}
	}

	private Security _underlyingAsset;

	/// <summary>
	/// Underlying asset.
	/// </summary>
	public virtual Security UnderlyingAsset
	{
		get => _underlyingAsset;
		set => _underlyingAsset = value;
	}

	/// <summary>
	/// The standard deviation by default.
	/// </summary>
	public decimal DefaultDeviation => Option is null ? 0 : ((decimal?)DataProvider.GetSecurityValue(Option, Level1Fields.ImpliedVolatility) ?? 0) / 100;

	/// <summary>
	/// The time before expiration calculation.
	/// </summary>
	/// <param name="currentTime">The current time.</param>
	/// <returns>The time remaining until expiration. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
	public virtual double? GetExpirationTimeLine(DateTime currentTime)
	{
		var expTime = ExpirationTime;

		if (expTime == null && Option != null && Option.ExpiryDate != null)
			expTime = Option.ExpiryDate.Value.Date; // midnight by default

		if (expTime == null)
			return null;

		return DerivativesHelper.GetExpirationTimeLine(expTime.Value, currentTime);
	}

	/// <summary>
	/// To get the price of the underlying asset.
	/// </summary>
	/// <param name="assetPrice">The price of the underlying asset if it is specified.</param>
	/// <returns>The price of the underlying asset. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
	public decimal? GetAssetPrice(decimal? assetPrice = null)
	{
		if (assetPrice != null)
			return (decimal)assetPrice;

		return (decimal?)DataProvider.GetSecurityValue(UnderlyingAsset, Level1Fields.LastTradePrice);
	}

	/// <summary>
	/// Option type.
	/// </summary>
	protected OptionTypes OptionType
		=> Option is null
			? throw new InvalidOperationException(LocalizedStrings.NoData)
			: Option.OptionType ?? throw new InvalidOperationException(LocalizedStrings.OrderTypeMissed.Put(Option));

	/// <summary>
	/// To round to <see cref="RoundDecimals"/>.
	/// </summary>
	/// <param name="value">The initial value.</param>
	/// <returns>The rounded value.</returns>
	protected decimal? TryRound(decimal? value)
	{
		if (value != null && RoundDecimals >= 0)
			value = Math.Round(value.Value, RoundDecimals);

		return value;
	}

	/// <inheritdoc />
	public virtual decimal? Premium(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		deviation = deviation ?? DefaultDeviation;
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		var timeToExp = GetExpirationTimeLine(currentTime);

		if (timeToExp == null)
			return null;

		return TryRound(DerivativesHelper.Premium(OptionType, GetStrike(), assetPrice.Value, RiskFree, Dividend, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
	}

	/// <inheritdoc />
	public virtual decimal? Delta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		var timeToExp = GetExpirationTimeLine(currentTime);

		if (timeToExp == null)
			return null;

		return TryRound(DerivativesHelper.Delta(OptionType, assetPrice.Value, D1(deviation ?? DefaultDeviation, assetPrice.Value, timeToExp.Value)));
	}

	/// <inheritdoc />
	public virtual decimal? Gamma(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		deviation = deviation ?? DefaultDeviation;
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		var timeToExp = GetExpirationTimeLine(currentTime);

		if (timeToExp == null)
			return null;

		return TryRound(DerivativesHelper.Gamma(assetPrice.Value, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
	}

	/// <inheritdoc />
	public virtual decimal? Vega(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		var timeToExp = GetExpirationTimeLine(currentTime);

		if (timeToExp == null)
			return null;

		return TryRound(DerivativesHelper.Vega(assetPrice.Value, timeToExp.Value, D1(deviation ?? DefaultDeviation, assetPrice.Value, timeToExp.Value)));
	}

	/// <inheritdoc />
	public virtual decimal? Theta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		deviation = deviation ?? DefaultDeviation;
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		var timeToExp = GetExpirationTimeLine(currentTime);

		if (timeToExp == null)
			return null;

		return TryRound(DerivativesHelper.Theta(OptionType, GetStrike(), assetPrice.Value, RiskFree, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
	}

	/// <inheritdoc />
	public virtual decimal? Rho(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
	{
		deviation = deviation ?? DefaultDeviation;
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		var timeToExp = GetExpirationTimeLine(currentTime);

		if (timeToExp == null)
			return null;

		return TryRound(DerivativesHelper.Rho(OptionType, GetStrike(), assetPrice.Value, RiskFree, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
	}

	/// <inheritdoc />
	public virtual decimal? ImpliedVolatility(DateTime currentTime, decimal premium)
	{
		//var timeToExp = GetExpirationTimeLine();
		return TryRound(DerivativesHelper.ImpliedVolatility(premium, diviation => Premium(currentTime, diviation)));
	}

	/// <summary>
	/// To calculate the d1 parameter of the option fulfilment probability estimating.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>The d1 parameter.</returns>
	protected virtual double D1(decimal deviation, decimal assetPrice, double timeToExp)
	{
		return DerivativesHelper.D1(assetPrice, GetStrike(), RiskFree, Dividend, deviation, timeToExp);
	}

	internal decimal GetStrike()
	{
		if (Option is null)
			throw new InvalidOperationException(LocalizedStrings.NoData);

		return Option.Strike ?? throw new InvalidOperationException(LocalizedStrings.NoData);
	}
}
