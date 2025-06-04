namespace StockSharp.BusinessEntities;

using StockSharp.Algo.Candles;

/// <summary>
/// Subscription.
/// </summary>
public class Subscription : SubscriptionBase<Subscription>
{
	/// <summary>
	/// Candles series.
	/// </summary>
	[Obsolete("Use Subscription class.")]
	public CandleSeries CandleSeries { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	public Subscription(DataType dataType)
		: this(dataType, (SecurityMessage)null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="security">Security.</param>
	public Subscription(DataType dataType, SecurityMessage security)
		: this(dataType.ToSubscriptionMessage(), security)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="security">Security.</param>
	public Subscription(DataType dataType, Security security)
		: this(dataType.ToSubscriptionMessage(), security)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="candleSeries">Candles series.</param>
	[Obsolete("Use DataType overload.")]
	public Subscription(CandleSeries candleSeries)
		: this(candleSeries.ToMarketDataMessage(true), candleSeries.Security)
	{
		CandleSeries = candleSeries;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="subscriptionMessage">Subscription message.</param>
	/// <param name="security">Security.</param>
	public Subscription(ISubscriptionMessage subscriptionMessage, Security security)
		: this(subscriptionMessage, security?.ToMessage(copyExtendedId: true))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="subscriptionMessage">Subscription message.</param>
	/// <param name="security">Security.</param>
	public Subscription(ISubscriptionMessage subscriptionMessage, SecurityMessage security)
		: base(subscriptionMessage, security)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Subscription"/>.
	/// </summary>
	/// <param name="subscriptionMessage">Subscription message.</param>
	public Subscription(ISubscriptionMessage subscriptionMessage)
		: base(subscriptionMessage, null)
	{
	}

	/// <inheritdoc />
	public override Subscription Clone()
		=> new(SubscriptionMessage.TypedClone(), (SecurityMessage)null);
}