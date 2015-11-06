namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System.Collections.Generic;
	
	using Ecng.Collections;

	using StockSharp.Algo.Candles.Compression;

	/// <summary>
	/// Volume profile.
	/// </summary>
	public class VolumeProfile
	{
		private readonly CachedSynchronizedDictionary<decimal, PriceLevel> _volumeProfileInfo = new CachedSynchronizedDictionary<decimal, PriceLevel>();

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeProfile"/>.
		/// </summary>
		public VolumeProfile()
		{
		}

		/// <summary>
		/// Price levels.
		/// </summary>
		public IEnumerable<PriceLevel> PriceLevels => _volumeProfileInfo.CachedValues;

		/// <summary>
		/// To update the profile with new value.
		/// </summary>
		/// <param name="value">Value.</param>
		public void Update(ICandleBuilderSourceValue value)
		{
			if (value.OrderDirection == null)
				return;

			_volumeProfileInfo.SafeAdd(value.Price, key => new PriceLevel(key)).Update(value);
		}
	}
}
