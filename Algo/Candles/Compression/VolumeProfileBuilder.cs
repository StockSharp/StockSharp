namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Volume profile.
/// </summary>
public class VolumeProfileBuilder
{
	private readonly Dictionary<decimal, int> _volumeProfileInfo = [];

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

		var maxVolume = Math.Round(_levels.Sum(p => p.BuyVolume + p.SellVolume) * VolumePercent / 100, 0);
		var currVolume = _levels.Select(p => p.BuyVolume + p.SellVolume).Max();

		PoC = _levels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == currVolume);

		var abovePoc = Combine(_levels.Where(p => p.Price > PoC.Price).OrderBy(p => p.Price));
		var belowePoc = Combine(_levels.Where(p => p.Price < PoC.Price).OrderByDescending(p => p.Price));

		if (abovePoc.Count == 0)
		{
			LinkedListNode<CandlePriceLevel> node;

			for (node = belowePoc.First; node != null; node = node.Next)
			{
				var vol = node.Value.BuyVolume + node.Value.SellVolume;

				if (currVolume + vol > maxVolume)
				{
					High = PoC;
					Low = node.Value;
				}
				else
				{
					currVolume += vol;
				}
			}
		}
		else if (belowePoc.Count == 0)
		{
			LinkedListNode<CandlePriceLevel> node;

			for (node = abovePoc.First; node != null; node = node.Next)
			{
				var vol = node.Value.BuyVolume + node.Value.SellVolume;

				if (currVolume + vol > maxVolume)
				{
					High = node.Value;
					Low = PoC;
				}
				else
				{
					currVolume += vol;
				}
			}
		}
		else
		{
			var abovePocNode = abovePoc.First;
			var belowPocNode = belowePoc.First;

			while (abovePocNode != null && belowPocNode != null)
			{
				var aboveVol = abovePocNode.Value.BuyVolume + abovePocNode.Value.SellVolume;
				var belowVol = belowPocNode.Value.BuyVolume + belowPocNode.Value.SellVolume;

				if (aboveVol > belowVol)
				{
					if (currVolume + aboveVol > maxVolume)
					{
						High = abovePocNode.Value;
						Low = belowPocNode.Value;
						break;
					}

					currVolume += aboveVol;
					abovePocNode = abovePocNode.Next;
				}
				else
				{
					if (currVolume + belowVol > maxVolume)
					{
						High = abovePocNode.Value;
						Low = belowPocNode.Value;
						break;
					}

					currVolume += belowVol;
					belowPocNode = belowPocNode.Next;
				}
			}
		}
	}

	private static LinkedList<CandlePriceLevel> Combine(IEnumerable<CandlePriceLevel> prices)
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
				Price = curr.Price,
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