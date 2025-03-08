namespace StockSharp.Algo.Derivatives;

/// <summary>
/// The model for calculating Greeks values by the Black-Scholes formula.
/// </summary>
public class BlackScholes : IBlackScholes
{
	/// <summary>
	/// Initialize <see cref="BlackScholes"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	protected BlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlackScholes"/>.
	/// </summary>
	/// <param name="option">Options contract.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public BlackScholes(Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider)
		: this(securityProvider, dataProvider, exchangeInfoProvider)
	{
		Option = option ?? throw new ArgumentNullException(nameof(option));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlackScholes"/>.
	/// </summary>
	/// <param name="underlyingAsset">Underlying asset.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	protected BlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider)
	{
		_underlyingAsset = underlyingAsset ?? throw new ArgumentNullException(nameof(underlyingAsset));
		DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BlackScholes"/>.
	/// </summary>
	/// <param name="option">Options contract.</param>
	/// <param name="underlyingAsset">Underlying asset.</param>
	/// <param name="dataProvider">The market data provider.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public BlackScholes(Security option, Security underlyingAsset, IMarketDataProvider dataProvider, IExchangeInfoProvider exchangeInfoProvider)
		: this(underlyingAsset, dataProvider, exchangeInfoProvider)
	{
		Option = option ?? throw new ArgumentNullException(nameof(option));
	}

	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	public ISecurityProvider SecurityProvider { get; }

	/// <summary>
	/// The market data provider.
	/// </summary>
	public virtual IMarketDataProvider DataProvider { get; }

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

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
		get => _underlyingAsset ??= Option.GetUnderlyingAsset(SecurityProvider);
		set => _underlyingAsset = value;
	}

	/// <summary>
	/// The standard deviation by default.
	/// </summary>
	public decimal DefaultDeviation => ((decimal?)DataProvider.GetSecurityValue(Option, Level1Fields.ImpliedVolatility) ?? 0) / 100;

	/// <summary>
	/// The time before expiration calculation.
	/// </summary>
	/// <param name="currentTime">The current time.</param>
	/// <returns>The time remaining until expiration. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
	public virtual double? GetExpirationTimeLine(DateTimeOffset currentTime)
	{
		return DerivativesHelper.GetExpirationTimeLine(Option.GetExpirationTime(ExchangeInfoProvider), currentTime);
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
	{
		get
		{
			var type = Option.OptionType;

			if (type == null)
				throw new InvalidOperationException(LocalizedStrings.OrderTypeMissed.Put(Option));

			return type.Value;
		}
	}

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
	public virtual decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
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
	public virtual decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
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
	public virtual decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
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
	public virtual decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
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
	public virtual decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
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
	public virtual decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
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
	public virtual decimal? ImpliedVolatility(DateTimeOffset currentTime, decimal premium)
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
		return Option.Strike.Value;
	}
}