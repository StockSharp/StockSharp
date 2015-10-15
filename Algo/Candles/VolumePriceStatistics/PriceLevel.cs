namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Messages;

	/// <summary>
	/// The price level.
	/// </summary>
	public class PriceLevel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PriceLevel"/>.
		/// </summary>
		/// <param name="price">Price.</param>
		public PriceLevel(decimal price)
			: this(price, new List<decimal>(), new List<decimal>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PriceLevel"/>.
		/// </summary>
		/// <param name="price">Price.</param>
		/// <param name="buyVolumes">The volumes collection to buy.</param>
		/// <param name="sellVolumes">The volumes collection for sale.</param>
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
		/// Price.
		/// </summary>
		public decimal Price { get; private set; }

		/// <summary>
		/// The volume of purchases.
		/// </summary>
		public decimal BuyVolume { get; private set; }

		/// <summary>
		/// The volume of sales.
		/// </summary>
		public decimal SellVolume { get; private set; }

		/// <summary>
		/// The number of purchases.
		/// </summary>
		public decimal BuyCount { get; private set; }

		/// <summary>
		/// The number of sales.
		/// </summary>
		public decimal SellCount { get; private set; }

		/// <summary>
		/// The volumes collection to buy.
		/// </summary>
		public List<decimal> BuyVolumes { get; set; }

		/// <summary>
		/// The volumes collection for sale.
		/// </summary>
		public List<decimal> SellVolumes { get; set; }

		/// <summary>
		/// To update the price level with the new value.
		/// </summary>
		/// <param name="value">Value.</param>
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