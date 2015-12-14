#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: VolumeProfileHelper.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using System.Linq;

	using StockSharp.Messages;

	/// <summary>
	/// Extension class for <see cref="VolumeProfile"/>.
	/// </summary>
	public static class VolumeProfileHelper
	{
		/// <summary>
		/// The total volume of bids in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of bids.</returns>
		public static decimal TotalBuyVolume(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// The total volume of asks in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of asks.</returns>
		public static decimal TotalSellVolume(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// The total number of bids in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total number of bids.</returns>
		public static decimal TotalBuyCount(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.BuyCount).Sum();
		}

		/// <summary>
		/// The total number of asks in the <see cref="VolumeProfile"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total number of asks.</returns>
		public static decimal TotalSellCount(this VolumeProfile volumeProfile)
		{
			return volumeProfile.PriceLevels.Select(p => p.SellCount).Sum();
		}

		/// <summary>
		/// POC (Point Of Control) returns <see cref="CandlePriceLevel"/> which had the maximum volume.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The <see cref="CandlePriceLevel"/> which had the maximum volume.</returns>
		public static CandlePriceLevel PoC(this VolumeProfile volumeProfile)
		{
			var max = volumeProfile.PriceLevels.Select(p => (p.BuyVolume + p.SellVolume)).Max();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == max);
		}

		/// <summary>
		/// The total volume of bids which was above <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of bids.</returns>
		public static decimal BuyVolAbovePoC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.PoC();
			return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// The total volume of bids which was below <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of bids.</returns>
		public static decimal BuyVolBelowPoC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.PoC();
			return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.BuyVolume).Sum();
		}

		/// <summary>
		/// The total volume of asks which was above <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of asks.</returns>
		public static decimal SellVolAbovePoC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.PoC();
			return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// The total volume of asks which was below <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The total volume of asks.</returns>
		public static decimal SellVolBelowPoC(this VolumeProfile volumeProfile)
		{
			var poc = volumeProfile.PoC();
			return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.SellVolume).Sum();
		}

		/// <summary>
		/// The total volume which was above <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Total volume.</returns>
		public static decimal VolumeAbovePoC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolAbovePoC() + volumeProfile.SellVolAbovePoC();
		}

		/// <summary>
		/// The total volume which was below <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Total volume.</returns>
		public static decimal VolumeBelowPoC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolBelowPoC() + volumeProfile.SellVolBelowPoC();
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
		/// <returns><see cref="CandlePriceLevel"/>.</returns>
		public static CandlePriceLevel PriceLevelOfMaxDelta(this VolumeProfile volumeProfile)
		{
			var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Max();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
		}

		/// <summary>
		/// It returns the price level at which the minimum <see cref="Delta"/> is passed.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>The price level.</returns>
		public static CandlePriceLevel PriceLevelOfMinDelta(this VolumeProfile volumeProfile)
		{
			var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Min();
			return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
		}

		/// <summary>
		/// The total Delta which was above <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Delta.</returns>
		public static decimal DeltaAbovePoC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolAbovePoC() - volumeProfile.SellVolAbovePoC();
		}

		/// <summary>
		/// The total Delta which was below <see cref="PoC"/>.
		/// </summary>
		/// <param name="volumeProfile">Volume profile.</param>
		/// <returns>Delta.</returns>
		public static decimal DeltaBelowPoC(this VolumeProfile volumeProfile)
		{
			return volumeProfile.BuyVolBelowPoC() - volumeProfile.SellVolBelowPoC();
		}
	}
}
