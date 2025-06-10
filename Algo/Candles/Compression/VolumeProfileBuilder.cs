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
		//if (value.OrderDirection == null)
		//	return;

		var idx = GetPriceLevelIdx(price);
		var level = _levels[idx];
		UpdatePriceLevel(ref level, volume, side);
		_levels[idx] = level;
	}

	/// <summary>
	/// To update the profile with new value.
	/// </summary>
	/// <param name="priceLevel">Value.</param>
	public void Update(CandlePriceLevel priceLevel)
	{
		var idx = GetPriceLevelIdx(priceLevel.Price);

		var level = _levels[idx];

		level.BuyVolume += priceLevel.BuyVolume;
		level.BuyCount += priceLevel.BuyCount;
		level.SellVolume += priceLevel.SellVolume;
		level.SellCount += priceLevel.SellCount;

		level.TotalVolume += priceLevel.TotalVolume;

		if (priceLevel.BuyVolumes != null)
		{
			if (level.BuyVolumes is null)
				level.BuyVolumes = [.. priceLevel.BuyVolumes];
			else
				level.BuyVolumes = [.. level.BuyVolumes, .. priceLevel.BuyVolumes];
		}

		if (priceLevel.SellVolumes != null)
		{
			if (level.SellVolumes is null)
				level.SellVolumes = [.. priceLevel.SellVolumes];
			else
				level.SellVolumes = [.. level.SellVolumes, .. priceLevel.SellVolumes];
		}

		_levels[idx] = level;
	}

	private int GetPriceLevelIdx(decimal price)
	{
		if (price == 0)
			throw new ArgumentOutOfRangeException(nameof(price));

		if (!_volumeProfileInfo.TryGetValue(price, out var index))
		{
			index = _levels.Count;
			_volumeProfileInfo.Add(price, index);
			
			var level = new CandlePriceLevel
			{
				Price = price,
				//BuyVolumes = new List<decimal>(),
				//SellVolumes = new List<decimal>()
			};

			_levels.Add(level);
		}

		return index;
	}

	private static void UpdatePriceLevel(ref CandlePriceLevel level, decimal? volume, Sides? side)
	{
		if (level.Price == default)
			throw new ArgumentNullException(nameof(level));

		//var side = value.OrderDirection;

		//if (side == null)
		//	throw new ArgumentException(nameof(value));

		if (volume == null)
			return;

		var v = volume.Value;

		level.TotalVolume += v;

		if (side == Sides.Buy)
		{
			level.BuyVolume += v;
			level.BuyCount++;

			if (level.BuyVolumes is ICollection<decimal> vols)
				vols.Add(v);
		}
		else if (side == Sides.Sell)
		{
			level.SellVolume += v;
			level.SellCount++;

			if (level.SellVolumes is ICollection<decimal> vols)
				vols.Add(v);
		}
	}

	/// <summary>
	/// To calculate the value area.
	/// </summary>
	public void Calculate()
	{
		if (_levels.Count == 0)
			return;

		// Основная суть:
		// Есть POC Vol от него выше и ниже берется по два значения(объемы)
		// Суммируются и сравниваются, те что в сумме больше, складываются в общий объем, в котором изначально лежит POC Vol.
		// На следующей итерации берутся следующие два объема суммируются и сравниваются, и опять большая сумма ложится в общий объем
		// И так до тех пор пока общий объем не превысит порог, который устанавливается в процентном отношении к всему объему.
		// После превышения порога, самый верхний и самый нижний объем, из которых складывался общий объем будут VAH и VAL.
		// Возможные траблы:
		// Если POC Vol находится на границе ценового диапазона, то сверху/снизу брать нечего, то "набор" объемов только в одну сторону.
		// Если POC Vol находится на один шаг ниже/выше ценового диапазона, то сверху/снизу можно взять только одно значение для сравнения с двумя другими значениями.
		// Теоретически в ценовом диапазоне может быть несколько POC Vol, если будет несколько ценовых уровней с одинаковыми объемом,
		//   в таком случае должен браться POC Vol который ближе к центру. Теоретически они могут быть равно удалены от центра.)))
		// Если сумма сравниваемых объемов равна, х.з. какие брать.

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
			list.AddLast(new CandlePriceLevel
			{
				Price = curr.Price,
				BuyVolumes = combined.BuyVolumes?.ToArray(),
				SellVolumes = combined.SellVolumes?.ToArray(),
				BuyVolume = combined.BuyVolume,
				SellVolume = combined.SellVolume,
				TotalVolume = combined.TotalVolume,
				BuyCount = combined.BuyCount,
				SellCount = combined.SellCount
			});
		}

		return list;
	}
}