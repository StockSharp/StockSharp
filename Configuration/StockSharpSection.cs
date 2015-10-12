namespace StockSharp.Configuration
{
	using System.Configuration;

	/// <summary>
	/// Represents the root StockSharp section in a configuration file.
	/// </summary>
	public class StockSharpSection : ConfigurationSection
	{
		private const string _connectionsKey = "customConnections";

		/// <summary>
		/// Custom message adapters.
		/// </summary>
		[ConfigurationProperty(_connectionsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(ConnectionElementCollection), AddItemName = "connection", ClearItemsName = "clear", RemoveItemName = "remove")]
		public ConnectionElementCollection CustomConnections
		{
			get { return (ConnectionElementCollection)base[_connectionsKey]; }
		}

		private const string _candlesKey = "customCandles";

		/// <summary>
		/// Custom candles.
		/// </summary>
		[ConfigurationProperty(_candlesKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(CandleElementCollection), AddItemName = "candle", ClearItemsName = "clear", RemoveItemName = "remove")]
		public CandleElementCollection CustomCandles
		{
			get { return (CandleElementCollection)base[_candlesKey]; }
		}

		private const string _indicatorsKey = "customIndicators";

		/// <summary>
		/// Custom indicators.
		/// </summary>
		[ConfigurationProperty(_indicatorsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(IndicatorElementCollection), AddItemName = "indicator", ClearItemsName = "clear", RemoveItemName = "remove")]
		public IndicatorElementCollection CustomIndicators
		{
			get { return (IndicatorElementCollection)base[_indicatorsKey]; }
		}

		private const string _diagramElementsKey = "customDiagramElements";

		/// <summary>
		/// Custom diagram elements.
		/// </summary>
		[ConfigurationProperty(_diagramElementsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(DiagramElementCollection), AddItemName = "element", ClearItemsName = "clear", RemoveItemName = "remove")]
		public DiagramElementCollection CustomDiagramElements
		{
			get { return (DiagramElementCollection)base[_diagramElementsKey]; }
		}
	}
}