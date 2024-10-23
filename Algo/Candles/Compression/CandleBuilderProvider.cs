namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Candle builders provider.
/// </summary>
public class CandleBuilderProvider
{
	private readonly SynchronizedDictionary<Type, ICandleBuilder> _builders = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleBuilderProvider"/>.
	/// </summary>
	/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
	public CandleBuilderProvider(IExchangeInfoProvider exchangeInfoProvider)
	{
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

		Register(new TimeFrameCandleBuilder(exchangeInfoProvider));
		Register(new TickCandleBuilder(exchangeInfoProvider));
		Register(new VolumeCandleBuilder(exchangeInfoProvider));
		Register(new RangeCandleBuilder(exchangeInfoProvider));
		Register(new RenkoCandleBuilder(exchangeInfoProvider));
		Register(new PnFCandleBuilder(exchangeInfoProvider));
		Register(new HeikinAshiCandleBuilder(exchangeInfoProvider));
	}

	/// <summary>
	/// The exchange boards provider.
	/// </summary>
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

	/// <summary>
	/// Whether the candle type registered.
	/// </summary>
	/// <param name="type">Market data type.</param>
	/// <returns><see langword="true" /> if the candle type registered, <see langword="false" /> otherwise.</returns>
	public bool IsRegistered(Type type) => _builders.ContainsKey(type);

	/// <summary>
	/// Get candles builder.
	/// </summary>
	/// <param name="type">Market data type.</param>
	/// <returns>Candles builder.</returns>
	public ICandleBuilder Get(Type type)
	{
		var builder = _builders.TryGetValue(type);

		if (builder == null)
			throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);

		return builder;
	}

	/// <summary>
	/// Register candles builder.
	/// </summary>
	/// <param name="builder">Candles builder.</param>
	public void Register(ICandleBuilder builder)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		_builders.Add(builder.CandleType, builder);
	}

	/// <summary>
	/// Unregister candles builder.
	/// </summary>
	/// <param name="builder">Candles builder.</param>
	public void UnRegister(ICandleBuilder builder)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		_builders.Remove(builder.CandleType);
	}
}