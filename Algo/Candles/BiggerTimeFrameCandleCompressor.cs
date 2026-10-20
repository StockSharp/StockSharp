namespace StockSharp.Algo.Candles;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Compressor of candles from smaller time-frames to bigger.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BiggerTimeFrameCandleCompressor"/>.
/// </remarks>
/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
/// <param name="builder">The builder of candles of <see cref="TimeFrameCandleMessage"/> type.</param>
/// <param name="buildFrom">Which market-data type is used as a source value.</param>
public class BiggerTimeFrameCandleCompressor(MarketDataMessage message, ICandleBuilder builder, DataType buildFrom) : ICandleBuilderSubscription
{
	private class PartCandleBuilderValueTransform(DataType dt) : BaseCandleBuilderValueTransform(dt)
	{
		public Level1Fields Part { get; set; }

		public override bool Process(Message message)
		{
			if (message is not CandleMessage candle)
				return base.Process(message);

			decimal price;
			decimal? volume = null;
			decimal? oi = null;
			IEnumerable<CandlePriceLevel> priceLevels = null;

			switch (Part)
			{
				case Level1Fields.OpenPrice:
					price = candle.OpenPrice;
					volume = candle.TotalVolume;
					oi = candle.OpenInterest;
					priceLevels = candle.PriceLevels;
					break;

				case Level1Fields.HighPrice:
					price = candle.HighPrice;
					break;

				case Level1Fields.LowPrice:
					price = candle.LowPrice;
					break;

				case Level1Fields.ClosePrice:
					price = candle.ClosePrice;
					break;

				default:
					throw new InvalidOperationException(Part.To<string>());
			}

			Update(candle.OpenTime, price, volume, null, oi, priceLevels);

			return true;
		}
	}

	private readonly PartCandleBuilderValueTransform _transform = new(buildFrom);
	private readonly ICandleBuilder _builder = builder ?? throw new ArgumentNullException(nameof(builder));

	// A source sends a forming candle again with every change, carrying its totals so far, so a big candle takes the latest
	// state of each source candle: a repeat is applied to the big candle as it stood before that source candle came.
	private DateTime? _sourceOpenTime;
	private CandleMessage _beforeSource;
	private VolumeProfileBuilder _profileBeforeSource;

	// The source candles of the big candle in progress, each in its latest state: an earlier one sent again after a later
	// one has the big candle built anew from them.
	private readonly SortedList<DateTime, CandleMessage> _sources = [];

	// What the source candles before the latest one add up to of what the parts do not carry: trades and the volume of
	// each side.
	private int? _settledTicks;
	private decimal? _settledBuyVolume;
	private decimal? _settledSellVolume;

	/// <inheritdoc />
	public MarketDataMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

	/// <inheritdoc />
	public VolumeProfileBuilder VolumeProfile { get; set; }

	/// <inheritdoc />
	public CandleMessage CurrentCandle { get; set; }

	/// <summary>
	/// Reset state.
	/// </summary>
	public void Reset()
	{
		CurrentCandle = null;
		ForgetSources();
	}

	private void ForgetSources()
	{
		_sourceOpenTime = null;
		_beforeSource = null;
		_profileBeforeSource = null;
		_sources.Clear();
		_settledTicks = null;
		_settledBuyVolume = null;
		_settledSellVolume = null;
	}

	private static readonly Level1Fields[] _processParts =
	[
		Level1Fields.OpenPrice,
		Level1Fields.HighPrice,
		Level1Fields.LowPrice,
		Level1Fields.ClosePrice,
	];

	/// <summary>
	/// To process the new data.
	/// </summary>
	/// <param name="message">The message contains information about the time-frame candle.</param>
	/// <returns>A new candles changes.</returns>
	public IEnumerable<CandleMessage> Process(CandleMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		// a source candle of a big candle that is finished already has nothing left to change
		if (CurrentCandle is not null && message.OpenTime < CurrentCandle.OpenTime)
			return [];

		if (message.OpenTime < _sourceOpenTime)
		{
			_sources[message.OpenTime] = message;
			return Rebuild();
		}

		return Apply(message);
	}

	private List<CandleMessage> Apply(CandleMessage message)
	{
		var isRepeat = message.OpenTime == _sourceOpenTime;
		var latest = _sources.Count > 0 ? _sources.Values[^1] : null;

		if (isRepeat)
		{
			CurrentCandle = _beforeSource?.TypedClone();
			VolumeProfile = _profileBeforeSource?.TypedClone();

			// The candle's levels are the profile's own list, which the builder goes on filling.
			if (CurrentCandle is not null && VolumeProfile is not null)
				CurrentCandle.PriceLevels = VolumeProfile.PriceLevels;
		}
		else
		{
			_sourceOpenTime = message.OpenTime;
			_beforeSource = CurrentCandle?.TypedClone();
			_profileBeforeSource = VolumeProfile?.TypedClone();
		}

		var openTime = CurrentCandle?.OpenTime;
		var candles = _processParts.SelectMany(p => ProcessCandlePart(p, message)).ToArray();

		if (CurrentCandle?.OpenTime != openTime)
		{
			// The source candle opened a big candle of its own, so nothing of that big candle stood before it.
			_beforeSource = null;
			_profileBeforeSource = null;
			_sources.Clear();
			_settledTicks = null;
			_settledBuyVolume = null;
			_settledSellVolume = null;
		}
		else if (!isRepeat && latest is not null)
		{
			_settledTicks = Add(_settledTicks, latest.TotalTicks);
			_settledBuyVolume = Add(_settledBuyVolume, latest.BuyVolume);
			_settledSellVolume = Add(_settledSellVolume, latest.SellVolume);
		}

		if (CurrentCandle is { } big && big.OpenTime <= message.OpenTime)
		{
			_sources[message.OpenTime] = message;

			big.TotalTicks = Add(_settledTicks, message.TotalTicks);
			big.BuyVolume = Add(_settledBuyVolume, message.BuyVolume);
			big.SellVolume = Add(_settledSellVolume, message.SellVolume);
			big.OpenInterest = message.OpenInterest;
		}

		var changes = new List<CandleMessage>();

		foreach (var candle in candles)
		{
			if (changes.Count == 0 || changes[^1] != candle)
				changes.Add(candle);
		}

		return changes;
	}

	// The big candle in progress built anew from its source candles in their latest states.
	private List<CandleMessage> Rebuild()
	{
		var sources = _sources.Values.ToArray();

		CurrentCandle = null;
		VolumeProfile = null;
		ForgetSources();

		List<CandleMessage> changes = [];

		foreach (var source in sources)
			changes = Apply(source);

		return changes.Count == 0 ? [] : [changes[^1]];
	}

	private static int? Add(int? total, int? value)
		=> total is null && value is null ? null : (total ?? 0) + (value ?? 0);

	private static decimal? Add(decimal? total, decimal? value)
		=> total is null && value is null ? null : (total ?? 0) + (value ?? 0);

	private IEnumerable<CandleMessage> ProcessCandlePart(Level1Fields part, CandleMessage message)
	{
		_transform.Part = part;
		_transform.Process(message);

		return _builder.Process(this, _transform);
	}
}