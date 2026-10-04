namespace StockSharp.Algo.Derivatives;

/// <summary>
/// The synthetic positions builder.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Synthetic"/>.
/// </remarks>
/// <param name="security">The instrument (the option or the underlying asset).</param>
/// <param name="provider">The provider of information about instruments.</param>
public class Synthetic(Security security, ISecurityProvider provider)
{
	private readonly Security _security = security ?? throw new ArgumentNullException(nameof(security));
	private readonly ISecurityProvider _provider = provider ?? throw new ArgumentNullException(nameof(provider));

	private Security Option
	{
		get
		{
			_security.CheckOption();
			return _security;
		}
	}

	/// <summary>
	/// To get the synthetic position to buy the option.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The synthetic position.</returns>
	public ValueTask<(Security security, Sides side)[]> BuyAsync(CancellationToken cancellationToken)
		=> PositionAsync(Sides.Buy, cancellationToken);

	/// <summary>
	/// To get the synthetic position to sale the option.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The synthetic position.</returns>
	public ValueTask<(Security security, Sides side)[]> SellAsync(CancellationToken cancellationToken)
		=> PositionAsync(Sides.Sell, cancellationToken);

	/// <summary>
	/// To get the synthetic position for the option.
	/// </summary>
	/// <param name="side">The main position direction.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The synthetic position.</returns>
	public async ValueTask<(Security security, Sides side)[]> PositionAsync(Sides side, CancellationToken cancellationToken)
	{
		var option = Option;

		var asset = await option.GetUnderlyingAssetAsync(_provider, cancellationToken);
		var opposite = await option.GetOppositeOptionAsync(_provider, cancellationToken);

		return
		[
			new(asset, option.OptionType == OptionTypes.Call ? side : side.Invert()),
			new(opposite, side)
		];
	}

	/// <summary>
	/// To get the option position for the underlying asset synthetic buy.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The option position.</returns>
	public ValueTask<(Security security, Sides side)[]> BuyAsync(decimal strike, CancellationToken cancellationToken)
		=> BuyAsync(strike, GetExpiryDate(), cancellationToken);

	/// <summary>
	/// To get the option position for the underlying asset synthetic buy.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="expiryDate">The date of the option expiration.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The option position.</returns>
	public ValueTask<(Security security, Sides side)[]> BuyAsync(decimal strike, DateTime expiryDate, CancellationToken cancellationToken)
		=> PositionAsync(strike, expiryDate, Sides.Buy, cancellationToken);

	/// <summary>
	/// To get the option position for synthetic sale of the base asset.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The option position.</returns>
	public ValueTask<(Security security, Sides side)[]> SellAsync(decimal strike, CancellationToken cancellationToken)
		=> SellAsync(strike, GetExpiryDate(), cancellationToken);

	/// <summary>
	/// To get the option position for synthetic sale of the base asset.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="expiryDate">The date of the option expiration.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The option position.</returns>
	public ValueTask<(Security security, Sides side)[]> SellAsync(decimal strike, DateTime expiryDate, CancellationToken cancellationToken)
		=> PositionAsync(strike, expiryDate, Sides.Sell, cancellationToken);

	/// <summary>
	/// To get the option position for the synthetic base asset.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="expiryDate">The date of the option expiration.</param>
	/// <param name="side">The main position direction.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The option position.</returns>
	public async ValueTask<(Security security, Sides side)[]> PositionAsync(decimal strike, DateTime expiryDate, Sides side, CancellationToken cancellationToken)
	{
		var call = await _security.GetCallAsync(_provider, strike, expiryDate, cancellationToken);
		var put = await _security.GetPutAsync(_provider, strike, expiryDate, cancellationToken);

		return
		[
			new (call, side),
			new (put, side.Invert())
		];
	}

	private DateTime GetExpiryDate()
	{
		if (_security.ExpiryDate == null)
			throw new InvalidOperationException(LocalizedStrings.NoExpirationDate.Put(_security.Id));

		return _security.ExpiryDate.Value;
	}
}
