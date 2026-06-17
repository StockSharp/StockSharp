namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Volume profile.
/// </summary>
public class VolumeProfileBuilder
{
	private readonly Dictionary<decimal, int> _volumeProfileInfo = [];

	// Optional price-step bucketing for the level price. When set (> 0), every level price is
	// snapped to the instrument price step before it keys a profile level. Without it a profile
	// built from a near-continuous source (e.g. Level1 SpreadMiddle on a wide-range instrument
	// such as a crypto/RUB pair) accumulates an unbounded number of distinct levels, bloating the
	// candle so much that it can no longer be stored/transmitted as deep history.
	private readonly decimal? _priceStep;

	/// <summary>
	/// The upper price level.
	/// </summary>
	public CandlePriceLevel High { get; private set; }

	/// <summary>
	/// The lower price level.
	/// </summary>
	public CandlePriceLevel Low { get; private set; }

	/// <summary>
	/// Point of control.
	/// </summary>
	public CandlePriceLevel PoC { get; private set; }

	private decimal _volumePercent = 70;

	/// <summary>
	/// The percentage of total volume (the default is 70%).
	/// </summary>
	public decimal VolumePercent
	{
		get => _volumePercent;
		set
		{
			if (value is < 0 or > 100)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_volumePercent = value;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeProfileBuilder"/>.
	/// </summary>
	public VolumeProfileBuilder()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeProfileBuilder"/> that snaps every level
	/// price to <paramref name="priceStep"/> so the number of distinct levels stays bounded.
	/// </summary>
	/// <param name="priceStep">Instrument price step used to bucket level prices; <see langword="null"/> or non-positive disables bucketing.</param>
	public VolumeProfileBuilder(decimal? priceStep)
	{
		_priceStep = priceStep;
	}

	private readonly List<CandlePriceLevel> _levels = [];

	/// <summary>
	/// Price levels.
	/// </summary>
	public IEnumerable<CandlePriceLevel> PriceLevels => _levels;

	/// <summary>
	/// To update the profile with new value.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">Volume.</param>
	/// <param name="side">Side.</param>
	public void Update(decimal price, decimal? volume, Sides? side)
	{
		var level = new CandlePriceLevel { Price = price };
		
		if (volume is decimal v)
		{
			level.TotalVolume = v;

			if (side == Sides.Buy)
			{
				level.BuyVolume = v;
				level.BuyCount = 1;
			}
			else if (side == Sides.Sell)
			{
				level.SellVolume = v;
				level.SellCount = 1;
			}
		}

		Update(level);
	}

	/// <summary>
	/// To update the profile with new value.
	/// </summary>
	/// <param name="level">Value.</param>
	public void Update(CandlePriceLevel level)
	{
		var price = level.Price;

		if (price == 0)
			throw new ArgumentOutOfRangeException(nameof(level));

		if (_priceStep is decimal step && step > 0m)
		{
			// Snap the level price to the instrument grid so a near-continuous source cannot
			// spawn a new level per tick. Volumes for prices that fall into the same bucket are
			// merged by the existing Join path below.
			price = price.ShrinkPrice(step, step.GetCachedDecimals());
			level.Price = price;
		}

		if (!_volumeProfileInfo.TryGetValue(price, out var idx))
		{
			idx = _levels.Count;
			_volumeProfileInfo.Add(price, idx);

			if (level.TotalVolume == 0)
				level.TotalVolume = level.BuyVolume + level.SellVolume;

			_levels.Add(level);
		}
		else
		{
			_levels[idx] = _levels[idx].Join(level);
		}
	}

	/// <summary>
	/// To calculate the value area.
	/// </summary>
	public void Calculate()
	{
		if (_levels.Count == 0)
			return;

		PoC = default;
		High = default;
		Low = default;

		static decimal GetVolume(CandlePriceLevel level)
			=> level.TotalVolume != 0 ? level.TotalVolume : level.BuyVolume + level.SellVolume;

		var maxVolume = (_levels.Sum(GetVolume) * VolumePercent / 100).Round(0);
		var currVolume = _levels.Select(GetVolume).Max();

		PoC = _levels.FirstOrDefault(p => GetVolume(p) == currVolume);

		if (PoC.Price == 0)
			return;

		High = PoC;
		Low = PoC;

		var abovePoc = Combine(_levels.Where(p => p.Price > PoC.Price).OrderBy(p => p.Price), true);
		var belowePoc = Combine(_levels.Where(p => p.Price < PoC.Price).OrderByDescending(p => p.Price), false);

		var abovePocNode = abovePoc.First;
		var belowPocNode = belowePoc.First;

		while (abovePocNode != null || belowPocNode != null)
		{
			var useAbove = belowPocNode == null;

			if (abovePocNode != null && belowPocNode != null)
			{
				var aboveVol = GetVolume(abovePocNode.Value);
				var belowVol = GetVolume(belowPocNode.Value);

				useAbove = aboveVol > belowVol;
			}

			var node = useAbove ? abovePocNode : belowPocNode;
			var vol = GetVolume(node.Value);

			if (useAbove)
				High = node.Value;
			else
				Low = node.Value;

			if (currVolume + vol > maxVolume)
				break;

			currVolume += vol;

			if (useAbove)
				abovePocNode = abovePocNode.Next;
			else
				belowPocNode = belowPocNode.Next;
		}
	}

	private static LinkedList<CandlePriceLevel> Combine(IEnumerable<CandlePriceLevel> prices, bool isAbovePoC)
	{
		ArgumentNullException.ThrowIfNull(prices);

		using var enumerator = prices.GetEnumerator();

		var list = new LinkedList<CandlePriceLevel>();

		while (true)
		{
			if (!enumerator.MoveNext())
				break;

			var pl = enumerator.Current;

			if (!enumerator.MoveNext())
			{
				list.AddLast(pl);
				break;
			}

			var curr = enumerator.Current;

			var combined = curr.Join(pl);

			var level = new CandlePriceLevel
			{
				Price = isAbovePoC
					? curr.Price.Max(pl.Price)
					: curr.Price.Min(pl.Price),
				BuyVolumes = combined.BuyVolumes?.ToArray(),
				SellVolumes = combined.SellVolumes?.ToArray(),
				BuyVolume = combined.BuyVolume,
				SellVolume = combined.SellVolume,
				TotalVolume = combined.TotalVolume,
				BuyCount = combined.BuyCount,
				SellCount = combined.SellCount
			};

			if (level.TotalVolume == 0)
				level.TotalVolume = level.BuyVolume + level.SellVolume;

			list.AddLast(level);
		}

		return list;
	}
}
