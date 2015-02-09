namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System.Linq;

	/// <summary>
	/// Вспомогательный класс для работы с <see cref="VolumeProfile"/>.
	/// </summary>
	public static class VolumePriceStatisticHelper
	{
		/// <summary>
		/// Суммарный объем покупок в <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем покупок.</returns>
		public static decimal TotalBuyVolume(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// Суммарный объем продаж в <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем продаж.</returns>
		public static decimal TotalSellVolume(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// Суммарное количество покупок в <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарное количество покупок.</returns>
		public static decimal TotalBuyCount(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.BuyCount).Sum();
		}

		/// <summary>
		/// Суммарное количество продаж в <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарное количество продаж.</returns>
		public static decimal TotalSellCount(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.SellCount).Sum();
		}

		/// <summary>
		/// POC (Point Of Control) возвращает <see cref="PriceLevel"/>, по которому прошел максимальный объем.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns><see cref="PriceLevel"/>, по которому прошел максимальный объем.</returns>
		public static PriceLevel POC(this VolumeProfile volumeProfile)
		{
			var max = volumeProfile.PriceLevels.Select(p => (p.BuyVolume + p.SellVolume)).Max();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == max);
		}

		/// <summary>
		/// Суммарный объем покупок, который прошел выше <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем покупок.</returns>
		public static decimal BuyVolAbovePOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// Суммарный объем покупок, который прошел ниже <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем покупок.</returns>
		public static decimal BuyVolBelowPOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// Суммарный объем продаж, который прошел выше <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем продаж.</returns>
		public static decimal SellVolAbovePOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// Суммарный объем продаж, который прошел ниже <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем продаж.</returns>
		public static decimal SellVolBelowPOC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.POC();
			return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// Суммарный объем, который прошел выше <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем.</returns>
		public static decimal VolumeAbovePOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolAbovePOC() + volumeProfile.SellVolAbovePOC();
		}

		/// <summary>
		/// Суммарный объем, который прошел ниже <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Суммарный объем.</returns>
		public static decimal VolumeBelowPOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolBelowPOC() + volumeProfile.SellVolBelowPOC();
		}

		/// <summary>
		/// Разница между <see cref="TotalBuyVolume"/> и <see cref="TotalSellVolume"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Дельта.</returns>
		public static decimal Delta(this VolumeProfile volumeProfile)
		{
			return volumeProfile.TotalBuyVolume() - volumeProfile.TotalSellVolume();
		}

		/// <summary>
		/// Возвращает ценовой уровень, по которому прошла максимальная <see cref="Delta"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns><see cref="PriceLevel"/>.</returns>
		public static PriceLevel PriceLevelOfMaxDelta(this VolumeProfile volumeProfile)
		{
			var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Max();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
		}

		/// <summary>
		/// Возвращает ценовой уровень, по которому прошла минимальная <see cref="Delta"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема</param>
		/// <returns>Ценовой уровень.</returns>
		public static PriceLevel PriceLevelOfMinDelta(this VolumeProfile volumeProfile)
		{
			var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Min();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
		}

		/// <summary>
		/// Суммарная Дельта, которая прошла выше <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Дельта.</returns>
		public static decimal DeltaAbovePOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolAbovePOC() - volumeProfile.SellVolAbovePOC();
		}

		/// <summary>
		/// Суммарная Дельта, которая прошла ниже <see cref="POC"/>.
		/// </summary>
		/// <param name="volumeProfile">Профиль объема.</param>
		/// <returns>Дельта.</returns>
		public static decimal DeltaBelowPOC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolBelowPOC() - volumeProfile.SellVolBelowPOC();
		}
	}
}
