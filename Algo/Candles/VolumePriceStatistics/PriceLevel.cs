namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Messages;

	/// <summary>
	/// Ценовой уровень.
	/// </summary>
	public class PriceLevel
	{
		/// <summary>
		/// Создать <see cref="PriceLevel"/>.
		/// </summary>
		/// <param name="price">Цена.</param>
		public PriceLevel(decimal price)
			: this(price, new List<decimal>(), new List<decimal>())
		{
		}

		/// <summary>
		/// Создать <see cref="PriceLevel"/>.
		/// </summary>
		/// <param name="price">Цена.</param>
		/// <param name="buyVolumes">Коллекция объемов на покупку.</param>
		/// <param name="sellVolumes">Коллекция объемов на продажу.</param>
		public PriceLevel(decimal price, List<decimal> buyVolumes, List<decimal> sellVolumes)
		{
			Price = price;

			BuyVolume = buyVolumes.Sum();
			SellVolume = sellVolumes.Sum();

			BuyCount = buyVolumes.Count;
			SellCount = sellVolumes.Count;

			BuyVolumes = buyVolumes;
			SellVolumes = sellVolumes;
		}

		/// <summary>
		/// Цена.
		/// </summary>
		public decimal Price { get; private set; }

		/// <summary>
		/// Объем покупок.
		/// </summary>
		public decimal BuyVolume { get; private set; }

		/// <summary>
		/// Объем продаж.
		/// </summary>
		public decimal SellVolume { get; private set; }

		/// <summary>
		/// Количество покупок.
		/// </summary>
		public decimal BuyCount { get; private set; }

		/// <summary>
		/// Количество продаж.
		/// </summary>
		public decimal SellCount { get; private set; }

		/// <summary>
		/// Коллекция объемов на покупку.
		/// </summary>
		public List<decimal> BuyVolumes { get; set; }

		/// <summary>
		/// Коллекция объемов на продажу.
		/// </summary>
		public List<decimal> SellVolumes { get; set; }

		/// <summary>
		/// Обновить ценовой уровень новым значением.
		/// </summary>
		/// <param name="value">Значение.</param>
		public void Update(ICandleBuilderSourceValue value)
		{
			var side = value.OrderDirection;

			if (side == null)
				throw new ArgumentOutOfRangeException();

			if (side == Sides.Buy)
			{
				BuyVolume += value.Volume;
				BuyCount++;
				BuyVolumes.Add(value.Volume);
			}
			else
			{
				SellVolume += value.Volume;
				SellCount++;
				SellVolumes.Add(value.Volume);
			}
		}
	}
}