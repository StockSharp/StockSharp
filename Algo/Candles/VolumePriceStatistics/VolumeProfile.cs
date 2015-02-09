namespace StockSharp.Algo.Candles.VolumePriceStatistics
{
	using System.Collections.Generic;
	
	using Ecng.Collections;

	using StockSharp.Algo.Candles.Compression;

	/// <summary>
	/// Профиль объема.
	/// </summary>
	public class VolumeProfile
	{
		private readonly CachedSynchronizedDictionary<decimal, PriceLevel> _volumeProfileInfo = new CachedSynchronizedDictionary<decimal, PriceLevel>();

		/// <summary>
		/// Создать <see cref="VolumeProfile"/>.
		/// </summary>
		public VolumeProfile()
		{
		}

		/// <summary>
		/// Ценовые уровни.
		/// </summary>
		public IEnumerable<PriceLevel> PriceLevels 
		{
			get { return _volumeProfileInfo.CachedValues; }
		}

		/// <summary>
		/// Обновить профиль новым значением.
		/// </summary>
		/// <param name="value">Значение.</param>
		public void Update(ICandleBuilderSourceValue value)
		{
			if (value.OrderDirection == null)
				return;

			_volumeProfileInfo.SafeAdd(value.Price, key => new PriceLevel(key)).Update(value);
		}
	}
}
