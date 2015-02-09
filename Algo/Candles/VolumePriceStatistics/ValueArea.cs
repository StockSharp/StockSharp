namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Область стоимости.
	/// </summary>
	public class ValueArea
	{
		private decimal _volumePercent = 70;

		/// <summary>
		/// Процент от общего объема (по умолчанию 70%).
		/// </summary>
		public decimal VolumePercent
		{
			get { return _volumePercent; }
			set
			{
				if (value < 0 || value > 100)
					_volumePercent = 70;

				_volumePercent = value;
			}
		}

		/// <summary>
		/// Ценовые уровни.
		/// </summary>
		public List<PriceLevel> PriceLevels { get; private set; }

		/// <summary>
		/// Верхний ценовой уровень.
		/// </summary>
		public PriceLevel VAH { get; private set; }

		/// <summary>
		/// Нижний ценовой уровень.
		/// </summary>
		public PriceLevel VAL { get; private set; }

		/// <summary>
		/// POC.
		/// </summary>
		public PriceLevel POC { get; private set; }

		/// <summary>
		/// Создать <see cref="ValueArea"/>
		/// </summary>
		/// <param name="priceLevels">Коллекция <see cref="PriceLevel"/>, для которых необходимо рассчитать область стоимости.</param>
		public ValueArea(IEnumerable<PriceLevel> priceLevels)
		{
			if (priceLevels == null)
				throw new ArgumentNullException("priceLevels");

			PriceLevels = priceLevels
				.GroupBy(p => p.Price)
				.Select(g => new PriceLevel(g.Key, g.SelectMany(p => p.BuyVolumes).ToList(), g.SelectMany(p => p.SellVolumes).ToList()))
				.ToList();
		}

		/// <summary>
		/// Рассчитать область стоимости.
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
			var currVolume = PriceLevels.Select(p => (p.BuyVolume + p.SellVolume)).Max();

			POC = PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == currVolume);

			var abovePoc = Combine(PriceLevels.Where(p => p.Price > POC.Price).OrderBy(p => p.Price));
			var belowePoc = Combine(PriceLevels.Where(p => p.Price < POC.Price).OrderByDescending(p => p.Price));

			if (abovePoc.Count == 0)
			{
				LinkedListNode<PriceLevel> node;

				for (node = belowePoc.First; node != null; node = node.Next)
				{
					var vol = node.Value.BuyVolume + node.Value.SellVolume;

					if (currVolume + vol > maxVolume)
					{
						VAH = POC;
						VAL = node.Value;
					}
					else
					{
						currVolume += vol;
					}
				}
			}
			else if (belowePoc.Count == 0)
			{
				LinkedListNode<PriceLevel> node;

				for (node = abovePoc.First; node != null; node = node.Next)
				{
					var vol = node.Value.BuyVolume + node.Value.SellVolume;

					if (currVolume + vol > maxVolume)
					{
						VAH = node.Value;
						VAL = POC;
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
							VAH = abovePocNode.Value;
							VAL = belowPocNode.Value;
							break;
						}

						currVolume += aboveVol;
						abovePocNode = abovePocNode.Next;
					}
					else
					{
						if (currVolume + belowVol > maxVolume)
						{
							VAH = abovePocNode.Value;
							VAL = belowPocNode.Value;
							break;
						}

						currVolume += belowVol;
						belowPocNode = belowPocNode.Next;
					}
				}
			}
		}

		private LinkedList<PriceLevel> Combine(IEnumerable<PriceLevel> prices)
		{
			var enumerator = prices.GetEnumerator();
			var list = new LinkedList<PriceLevel>();

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

				list.AddLast(new PriceLevel(enumerator.Current.Price,
					enumerator.Current.BuyVolumes.Concat(pl.BuyVolumes).ToList(),
					enumerator.Current.SellVolumes.Concat(pl.SellVolumes).ToList()));
			}

			return list;
		}
	}
}