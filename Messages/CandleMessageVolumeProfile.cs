#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: VolumeProfile.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Volume profile.
	/// </summary>
	public class CandleMessageVolumeProfile
	{
		private readonly Dictionary<decimal, CandlePriceLevel> _volumeProfileInfo = new Dictionary<decimal, CandlePriceLevel>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleMessageVolumeProfile"/>.
		/// </summary>
		public CandleMessageVolumeProfile()
			: this(new List<CandlePriceLevel>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleMessageVolumeProfile"/>.
		/// </summary>
		/// <param name="priceLevels">Price levels.</param>
		public CandleMessageVolumeProfile(IList<CandlePriceLevel> priceLevels)
		{
			PriceLevels = priceLevels ?? throw new ArgumentNullException(nameof(priceLevels));
		}

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
				if (value < 0 || value > 100)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_volumePercent = value;
			}
		}

		/// <summary>
		/// Price levels.
		/// </summary>
		public IEnumerable<CandlePriceLevel> PriceLevels { get; }

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

			UpdatePriceLevel(GetPriceLevel(price), volume, side);
		}

		/// <summary>
		/// To update the profile with new value.
		/// </summary>
		/// <param name="priceLevel">Value.</param>
		public void Update(CandlePriceLevel priceLevel)
		{
			var level = GetPriceLevel(priceLevel.Price);

			level.BuyVolume += priceLevel.BuyVolume;
			level.BuyCount += priceLevel.BuyCount;
			level.SellVolume += priceLevel.SellVolume;
			level.SellCount += priceLevel.SellCount;

			level.TotalVolume += priceLevel.TotalVolume;

			if (priceLevel.BuyVolumes != null)
				((List<decimal>)level.BuyVolumes).AddRange(priceLevel.BuyVolumes);

			if (priceLevel.SellVolumes != null)
				((List<decimal>)level.SellVolumes).AddRange(priceLevel.SellVolumes);
		}

		private CandlePriceLevel GetPriceLevel(decimal price)
		{
			return _volumeProfileInfo.SafeAdd(price, key =>
			{
				var level = new CandlePriceLevel
				{
					Price = key,
					BuyVolumes = new List<decimal>(),
					SellVolumes = new List<decimal>()
				};

				((IList<CandlePriceLevel>)PriceLevels).Add(level);

				return level;
			});
		}

		private void UpdatePriceLevel(CandlePriceLevel level, decimal? volume, Sides? side)
		{
			if (level == null)
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

				((List<decimal>)level.BuyVolumes).Add(v);
			}
			else if (side == Sides.Sell)
			{
				level.SellVolume += v;
				level.SellCount++;

				((List<decimal>)level.SellVolumes).Add(v);
			}
		}

		/// <summary>
		/// To calculate the value area.
		/// </summary>
		public void Calculate()
		{
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

			var maxVolume = Math.Round(PriceLevels.Sum(p => p.BuyVolume + p.SellVolume) * VolumePercent / 100, 0);
			var currVolume = PriceLevels.Select(p => p.BuyVolume + p.SellVolume).Max();

			PoC = PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == currVolume);

			var abovePoc = Combine(PriceLevels.Where(p => p.Price > PoC.Price).OrderBy(p => p.Price));
			var belowePoc = Combine(PriceLevels.Where(p => p.Price < PoC.Price).OrderByDescending(p => p.Price));

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

				while (true)
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
			using (var enumerator = prices.GetEnumerator())
			{
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

					var buyVolumes = enumerator.Current.BuyVolumes.Concat(pl.BuyVolumes).ToArray();
					var sellVolumes = enumerator.Current.SellVolumes.Concat(pl.SellVolumes).ToArray();

					list.AddLast(new CandlePriceLevel
					{
						Price = enumerator.Current.Price,
						BuyCount = buyVolumes.Length,
						SellCount = sellVolumes.Length,
						BuyVolumes = buyVolumes,
						SellVolumes = sellVolumes,
						BuyVolume = buyVolumes.Sum(),
						SellVolume = sellVolumes.Sum()
					});
				}

				return list;
			}
		}
	}
}
