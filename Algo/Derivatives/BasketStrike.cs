namespace StockSharp.Algo.Derivatives;

/// <summary>
/// The virtual strike created from a combination of other strikes.
/// </summary>
/// <remarks>
/// Initialize <see cref="BasketStrike"/>.
/// </remarks>
/// <param name="underlyingAsset">Underlying asset.</param>
/// <param name="securityProvider">The provider of information about instruments.</param>
/// <param name="dataProvider">The market data provider.</param>
public abstract class BasketStrike(Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider) : BasketSecurity
{
	/// <summary>
	/// The provider of information about instruments.
	/// </summary>
	public ISecurityProvider SecurityProvider { get; } = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));

	/// <summary>
	/// The market data provider.
	/// </summary>
	public virtual IMarketDataProvider DataProvider { get; } = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

	/// <summary>
	/// Underlying asset.
	/// </summary>
	public Security UnderlyingAsset { get; } = underlyingAsset ?? throw new ArgumentNullException(nameof(underlyingAsset));

	/// <inheritdoc />
	public override IEnumerable<SecurityId> InnerSecurityIds
	{
		get
		{
			var derivatives = UnderlyingAsset.GetDerivatives(SecurityProvider, ExpiryDate);

			var type = OptionType;

			if (type != null)
				derivatives = derivatives.Filter((OptionTypes)type);

			if (UnderlyingAsset.GetCurrentPrice(DataProvider) is not decimal assetPrice)
				return [];

			return FilterStrikes(derivatives, assetPrice).Select(s => s.ToSecurityId());
		}
	}

	/// <summary>
	/// To get filtered strikes.
	/// </summary>
	/// <param name="allStrikes">All strikes.</param>
	/// <param name="assetPrice">The asset price.</param>
	/// <returns>Filtered strikes.</returns>
	protected abstract IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes, decimal assetPrice);
}

/// <summary>
/// The virtual strike including strikes of the specified shift boundary.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OffsetBasketStrike"/>.
/// </remarks>
/// <param name="underlyingSecurity">Underlying asset.</param>
/// <param name="securityProvider">The provider of information about instruments.</param>
/// <param name="dataProvider">The market data provider.</param>
/// <param name="strikeOffset">Boundaries of shift from the main strike (a negative value specifies the shift to options in the money, a positive value - out of the money).</param>
public class OffsetBasketStrike(Security underlyingSecurity, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, Range<int> strikeOffset) : BasketStrike(underlyingSecurity, securityProvider, dataProvider)
{
	private Range<int> _strikeOffset = strikeOffset ?? throw new ArgumentNullException(nameof(strikeOffset));
	private decimal _strikeStep;

	/// <inheritdoc />
	protected override IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes, decimal assetPrice)
	{
		if (_strikeStep == 0)
			_strikeStep = UnderlyingAsset.GetStrikeStep(SecurityProvider, ExpiryDate);

		allStrikes = [.. allStrikes];

		var centralStrike = allStrikes.GetCentralStrike(assetPrice);

		if (centralStrike == null)
			return [];

		var callStrikeFrom = centralStrike.Strike + _strikeOffset.Min * _strikeStep;
		var callStrikeTo = centralStrike.Strike + _strikeOffset.Max * _strikeStep;

		var putStrikeFrom = centralStrike.Strike - _strikeOffset.Max * _strikeStep;
		var putStrikeTo = centralStrike.Strike - _strikeOffset.Min * _strikeStep;

		return allStrikes
			.Where(s =>
				(s.OptionType == OptionTypes.Call && s.Strike >= callStrikeFrom && s.Strike <= callStrikeTo)
				||
				(s.OptionType == OptionTypes.Put && s.Strike >= putStrikeFrom && s.Strike <= putStrikeTo)
			)
			.OrderBy(s => s.Strike);
	}

	/// <inheritdoc />
	protected override string ToSerializedString()
	{
		return _strikeOffset.ToString();
	}

	/// <inheritdoc />
	protected override void FromSerializedString(string text)
	{
		_strikeOffset = Range<int>.Parse(text);
	}
}

/// <summary>
/// The virtual strike including strikes of the specified volatility boundary.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VolatilityBasketStrike"/>.
/// </remarks>
/// <param name="underlyingAsset">Underlying asset.</param>
/// <param name="securityProvider">The provider of information about instruments.</param>
/// <param name="dataProvider">The market data provider.</param>
/// <param name="volatilityRange">Volatility range.</param>
public class VolatilityBasketStrike(Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, Range<decimal> volatilityRange) : BasketStrike(underlyingAsset, securityProvider, dataProvider)
{
	private Range<decimal> _volatilityRange = volatilityRange ?? throw new ArgumentNullException(nameof(volatilityRange));

	/// <inheritdoc />
	protected override IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes, decimal assetPrice)
	{
		return allStrikes.Where(s =>
		{
			var iv = (decimal?)DataProvider.GetSecurityValue(s, Level1Fields.ImpliedVolatility);
			return iv != null && _volatilityRange.Contains(iv.Value);
		});
	}

	/// <inheritdoc />
	protected override string ToSerializedString()
	{
		return _volatilityRange.ToString();
	}

	/// <inheritdoc />
	protected override void FromSerializedString(string text)
	{
		_volatilityRange = Range<decimal>.Parse(text);
	}
}