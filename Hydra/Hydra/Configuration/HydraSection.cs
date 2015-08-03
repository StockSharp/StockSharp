namespace StockSharp.Hydra.Configuration
{
	using System.Configuration;

	class HydraSection : ConfigurationSection
	{
		private const string _candlesKey = "candles";

		[ConfigurationProperty(_candlesKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(CandleElementCollection), AddItemName = "candle", ClearItemsName = "clear", RemoveItemName = "remove")]
		public CandleElementCollection Candles
		{
			get { return (CandleElementCollection)base[_candlesKey]; }
		}

		private const string _indicatorsKey = "indicators";

		[ConfigurationProperty(_indicatorsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(IndicatorElementCollection), AddItemName = "indicator", ClearItemsName = "clear", RemoveItemName = "remove")]
		public IndicatorElementCollection Indicators
		{
			get { return (IndicatorElementCollection)base[_indicatorsKey]; }
		}
	}
}