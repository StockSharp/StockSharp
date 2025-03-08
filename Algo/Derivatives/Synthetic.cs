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
	/// <returns>The synthetic position.</returns>
	public (Security security, Sides side)[] Buy()
	{
		return Position(Sides.Buy);
	}

	/// <summary>
	/// To get the synthetic position to sale the option.
	/// </summary>
	/// <returns>The synthetic position.</returns>
	public (Security security, Sides side)[] Sell()
	{
		return Position(Sides.Sell);
	}

	/// <summary>
	/// To get the synthetic position for the option.
	/// </summary>
	/// <param name="side">The main position direction.</param>
	/// <returns>The synthetic position.</returns>
	public (Security security, Sides side)[] Position(Sides side)
	{
		var asset = Option.GetUnderlyingAsset(_provider);

		return
		[
			new(asset, Option.OptionType == OptionTypes.Call ? side : side.Invert()),
			new(Option.GetOppositeOption(_provider), side)
		];
	}

	/// <summary>
	/// To get the option position for the underlying asset synthetic buy.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <returns>The option position.</returns>
	public (Security security, Sides side)[] Buy(decimal strike)
	{
		return Buy(strike, GetExpiryDate());
	}

	/// <summary>
	/// To get the option position for the underlying asset synthetic buy.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="expiryDate">The date of the option expiration.</param>
	/// <returns>The option position.</returns>
	public (Security security, Sides side)[] Buy(decimal strike, DateTimeOffset expiryDate)
	{
		return Position(strike, expiryDate, Sides.Buy);
	}

	/// <summary>
	/// To get the option position for synthetic sale of the base asset.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <returns>The option position.</returns>
	public (Security security, Sides side)[] Sell(decimal strike)
	{
		return Sell(strike, GetExpiryDate());
	}

	/// <summary>
	/// To get the option position for synthetic sale of the base asset.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="expiryDate">The date of the option expiration.</param>
	/// <returns>The option position.</returns>
	public (Security security, Sides side)[] Sell(decimal strike, DateTimeOffset expiryDate)
	{
		return Position(strike, expiryDate, Sides.Sell);
	}

	/// <summary>
	/// To get the option position for the synthetic base asset.
	/// </summary>
	/// <param name="strike">Strike.</param>
	/// <param name="expiryDate">The date of the option expiration.</param>
	/// <param name="side">The main position direction.</param>
	/// <returns>The option position.</returns>
	public (Security security, Sides side)[] Position(decimal strike, DateTimeOffset expiryDate, Sides side)
	{
		var call = _security.GetCall(_provider, strike, expiryDate);
		var put = _security.GetPut(_provider, strike, expiryDate);

		return
		[
			new (call, side),
			new (put, side.Invert())
		];
	}

	private DateTimeOffset GetExpiryDate()
	{
		if (_security.ExpiryDate == null)
			throw new InvalidOperationException(LocalizedStrings.NoExpirationDate.Put(_security.Id));

		return _security.ExpiryDate.Value;
	}
}