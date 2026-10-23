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
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, <see cref="Security.ExpiryDate"/> is used when available: with its time of day, or at the board's expiry time for a date alone.</param>
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
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, <see cref="Security.ExpiryDate"/> is used when available: with its time of day, or at the board's expiry time for a date alone.</param>
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
	/// <param name="expirationTime">Explicit option expiration moment. If <c>null</c>, <see cref="Security.ExpiryDate"/> is used when available: with its time of day, or at the board's expiry time for a date alone.</param>
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
	/// Explicit expiration moment. If <c>null</c>, <see cref="Security.ExpiryDate"/> is used when available: with its time of day, or at the board's expiry time for a date alone.
	/// </summary>
	public virtual DateTime? ExpirationTime { get; set; }

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
	/// The standard deviation the option is quoted at: its implied volatility as a fraction. <see langword="null"/> while no
	/// volatility is quoted, and then the model computes nothing for which the caller gives no deviation.
	/// </summary>
	public decimal? DefaultDeviation => Option is null ? null : (decimal?)DataProvider.GetSecurityValue(Option, Level1Fields.ImpliedVolatility) / 100;

	/// <summary>
	/// The time before expiration calculation.
	/// </summary>
	/// <param name="currentTime">The current time.</param>
	/// <returns>The time remaining until expiration. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
	public virtual double? GetExpirationTimeLine(DateTime currentTime)
	{
		var expTime = GetExpirationMoment();

		if (expTime == null)
			return null;

		return DerivativesHelper.GetExpirationTimeLine(expTime.Value, currentTime);
	}

	// The moment the model takes the option to expire at: the one it was given, otherwise the option's own expiry - with the
	// time of day the venue sent, or the board's expiry time for a date alone.
	private DateTime? GetExpirationMoment()
	{
		if (ExpirationTime is { } given)
			return given;

		return Option?.ExpiryDate is { } expiry
			? DerivativesHelper.GetExpirationMoment(expiry, Option.Board)
			: null;
	}

	/// <summary>
	/// Whether the option has expired by the given moment, as the model takes its expiry.
	/// </summary>
	/// <param name="currentTime">The current time.</param>
	/// <returns><see langword="true"/> when the expiry is known and is not later than <paramref name="currentTime"/>.</returns>
	internal bool IsExpired(DateTime currentTime)
		=> GetExpirationMoment() is { } expiry && expiry <= currentTime;

	/// <summary>
	/// What the option pays at its expiry, before rounding: its intrinsic value at the price of the underlying asset.
	/// </summary>
	/// <param name="assetPrice">The price of the underlying asset if it is specified.</param>
	/// <returns>The payoff. If the value is equal to <see langword="null" />, then the price of the underlying asset is unknown.</returns>
	internal decimal? Payoff(decimal? assetPrice)
	{
		assetPrice = GetAssetPrice(assetPrice);

		if (assetPrice == null)
			return null;

		// With no time left the premium is the intrinsic value, whatever the volatility.
		return CalcPremium(0, assetPrice.Value, 0);
	}

	/// <summary>
	/// The slope of what the option pays at its expiry: the asset it delivers, one for one while it is in the money.
	/// </summary>
	/// <param name="assetPrice">The price of the underlying asset if it is specified.</param>
	/// <returns>The delta of the payoff. If the value is equal to <see langword="null" />, then the price of the underlying asset is unknown.</returns>
	internal decimal? PayoffDelta(decimal? assetPrice)
	{
		if (GetAssetPrice(assetPrice) is not decimal price)
			return null;

		var strike = GetStrike();

		return OptionType == OptionTypes.Call
			? price > strike ? 1 : 0
			: price < strike ? -1 : 0;
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
			value = value.Value.Round(RoundDecimals);

		return value;
	}

	/// <summary>
	/// What every value of the model is computed from: the deviation the caller gives or the one quoted, the asset price
	/// the caller gives or the last trade, and the time left to expiry.
	/// </summary>
	/// <param name="currentTime">The current time.</param>
	/// <param name="deviation">The standard deviation, or <see langword="null"/> for the quoted one.</param>
	/// <param name="assetPrice">The price of the underlying asset, or <see langword="null"/> for its last trade.</param>
	/// <returns>The inputs, or <see langword="null"/> when one of them is not known.</returns>
	protected (decimal Deviation, decimal AssetPrice, double TimeToExp)? GetInputs(DateTime currentTime, decimal? deviation, decimal? assetPrice)
	{
		if ((deviation ?? DefaultDeviation) is not decimal known)
			return null;

		if (GetAssetPrice(assetPrice) is not decimal price)
			return null;

		if (GetExpirationTimeLine(currentTime) is not double timeToExp)
			return null;

		return (known, price, timeToExp);
	}

	/// <inheritdoc />
	public virtual decimal? Premium(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> TryRound(PremiumCore(currentTime, deviation, assetPrice));

	// The premium before rounding: what the search for the implied volatility inverts.
	internal virtual decimal? PremiumCore(DateTime currentTime, decimal? deviation, decimal? assetPrice)
		=> GetInputs(currentTime, deviation, assetPrice) is { } inputs
			? CalcPremium(inputs.Deviation, inputs.AssetPrice, inputs.TimeToExp)
			: null;

	/// <summary>
	/// To calculate the option premium from the model inputs already resolved by <see cref="Premium"/>.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>The option premium.</returns>
	protected virtual decimal CalcPremium(decimal deviation, decimal assetPrice, double timeToExp)
		=> DerivativesHelper.Premium(OptionType, GetStrike(), assetPrice, RiskFree, Dividend, deviation, timeToExp, D1(deviation, assetPrice, timeToExp));

	/// <inheritdoc />
	public virtual decimal? Delta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> TryRound(GetInputs(currentTime, deviation, assetPrice) is { } inputs ? CalcDelta(inputs.Deviation, inputs.AssetPrice, inputs.TimeToExp) : null);

	/// <summary>
	/// To calculate the option delta from resolved inputs, before rounding.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>Option delta.</returns>
	protected virtual decimal CalcDelta(decimal deviation, decimal assetPrice, double timeToExp)
	{
		// Holding the asset until expiry forgoes what it pays out, which the premium discounts by
		// e^(-qT) - so its slope carries the same factor.
		var expDiv = (decimal)DerivativesHelper.ExpRate(Dividend, timeToExp);

		return expDiv * DerivativesHelper.Delta(OptionType, assetPrice, D1(deviation, assetPrice, timeToExp));
	}

	/// <inheritdoc />
	public virtual decimal? Gamma(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> TryRound(GetInputs(currentTime, deviation, assetPrice) is { } inputs ? CalcGamma(inputs.Deviation, inputs.AssetPrice, inputs.TimeToExp) : null);

	/// <summary>
	/// To calculate the option gamma from resolved inputs, before rounding.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>Option gamma.</returns>
	protected virtual decimal CalcGamma(decimal deviation, decimal assetPrice, double timeToExp)
	{
		var expDiv = (decimal)DerivativesHelper.ExpRate(Dividend, timeToExp);

		return expDiv * DerivativesHelper.Gamma(assetPrice, deviation, timeToExp, D1(deviation, assetPrice, timeToExp));
	}

	/// <inheritdoc />
	public virtual decimal? Vega(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> TryRound(GetInputs(currentTime, deviation, assetPrice) is { } inputs ? CalcVega(inputs.Deviation, inputs.AssetPrice, inputs.TimeToExp) : null);

	/// <summary>
	/// To calculate the option vega from resolved inputs, before rounding.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>Option vega.</returns>
	protected virtual decimal CalcVega(decimal deviation, decimal assetPrice, double timeToExp)
	{
		// The premium the vega measures is discounted by the payout, so one point of volatility is
		// worth e^(-qT) of what it adds to an asset that pays nothing out.
		var expDiv = (decimal)DerivativesHelper.ExpRate(Dividend, timeToExp);

		return expDiv * DerivativesHelper.Vega(assetPrice, timeToExp, D1(deviation, assetPrice, timeToExp));
	}

	/// <inheritdoc />
	public virtual decimal? Theta(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> TryRound(GetInputs(currentTime, deviation, assetPrice) is { } inputs ? CalcTheta(inputs.Deviation, inputs.AssetPrice, inputs.TimeToExp) : null);

	/// <summary>
	/// To calculate the option theta from resolved inputs, before rounding.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>Option theta.</returns>
	protected virtual decimal CalcTheta(decimal deviation, decimal assetPrice, double timeToExp)
		=> DerivativesHelper.Theta(OptionType, GetStrike(), assetPrice, RiskFree, Dividend, deviation, timeToExp, D1(deviation, assetPrice, timeToExp));

	/// <inheritdoc />
	public virtual decimal? Rho(DateTime currentTime, decimal? deviation = null, decimal? assetPrice = null)
		=> TryRound(GetInputs(currentTime, deviation, assetPrice) is { } inputs ? CalcRho(inputs.Deviation, inputs.AssetPrice, inputs.TimeToExp) : null);

	/// <summary>
	/// To calculate the option rho from resolved inputs, before rounding.
	/// </summary>
	/// <param name="deviation">Standard deviation.</param>
	/// <param name="assetPrice">Underlying asset price.</param>
	/// <param name="timeToExp">The option period before the expiration.</param>
	/// <returns>Option rho.</returns>
	protected virtual decimal CalcRho(decimal deviation, decimal assetPrice, double timeToExp)
		=> DerivativesHelper.Rho(OptionType, GetStrike(), assetPrice, RiskFree, deviation, timeToExp, D1(deviation, assetPrice, timeToExp));

	/// <inheritdoc />
	/// <remarks>The search inverts the premium before rounding; <see cref="RoundDecimals"/> rounds the answer.</remarks>
	public virtual decimal? ImpliedVolatility(DateTime currentTime, decimal premium, decimal? assetPrice = null)
		=> TryRound(DerivativesHelper.ImpliedVolatility(premium, deviation => PremiumCore(currentTime, deviation, assetPrice)));

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
