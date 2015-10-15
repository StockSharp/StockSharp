namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System.Linq;

	/// <summary>
	/// Extension class for <see cref="VolumeProfile"/>.
	/// </summary>
	public static class VolumePriceStatisticHelper
	{
		/// <summary>
		/// The total volume of purchases in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of purchases.</returns>
		public static decimal TotalBuyVolume(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// The total volume of sales in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of sales.</returns>
		public static decimal TotalSellVolume(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// The total number of purchases in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total number of purchases.</returns>
		public static decimal TotalBuyCount(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.BuyCount).Sum();
		}

		/// <summary>
		/// The total number of sales in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total number of sales.</returns>
		public static decimal TotalSellCount(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.SellCount).Sum();
		}

		/// <summary>
		/// POC (Point Of Control) returns <see cref="PriceLevel"/> which had the maximum volume.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The <see cref="PriceLevel"/> which had the maximum volume.</returns>
		public static PriceLevel POC(this VolumeProfile volumeProfile)
		{
			var max = volumeProfile.PriceLevels.Select(p => (p.BuyVolume + p.SellVolume)).Max();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == max);
		}

		/// <summary>
		/// The total volume of purchases which was above <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of purchases.</returns>
		public static decimal BuyVolAbovePOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// The total volume of purchases which was below <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of purchases.</returns>
		public static decimal BuyVolBelowPOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// The total volume of sales which was above <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of sales.</returns>
		public static decimal SellVolAbovePOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// The total volume of sales which was below <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of sales.</returns>
		public static decimal SellVolBelowPOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// The total volume which was above <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Total volume.</returns>
		public static decimal VolumeAbovePOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolAbovePOC() + volumeProfile.SellVolAbovePOC();
		}

		/// <summary>
		/// The total volume which was below <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Total volume.</returns>
		public static decimal VolumeBelowPOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolBelowPOC() + volumeProfile.SellVolBelowPOC();
		}

		/// <summary>
		/// The difference between <see cref="TotalBuyVolume"/> and <see cref="TotalSellVolume"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Delta.</returns>
		public static decimal Delta(this VolumeProfile volumeProfile)
		{
			return volumeProfile.TotalBuyVolume() - volumeProfile.TotalSellVolume();
		}

		/// <summary>
		/// It returns the price level at which the maximum <see cref="Delta"/> is passed.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns><see cref="PriceLevel"/>.</returns>
		public static PriceLevel PriceLevelOfMaxDelta(this VolumeProfile volumeProfile)
		{
			var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Max();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
		}

		/// <summary>
		/// It returns the price level at which the minimum <see cref="Delta"/> is passed.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The price level.</returns>
		public static PriceLevel PriceLevelOfMinDelta(this VolumeProfile volumeProfile)
		{
			var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Min();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
		}

		/// <summary>
		/// The total Delta which was above <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Delta.</returns>
		public static decimal DeltaAbovePOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolAbovePOC() - volumeProfile.SellVolAbovePOC();
		}

		/// <summary>
		/// The total Delta which was below <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Delta.</returns>
		public static decimal DeltaBelowPOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolBelowPOC() - volumeProfile.SellVolBelowPOC();
		}
	}
}
