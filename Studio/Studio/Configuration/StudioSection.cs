namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	class StudioSection : ConfigurationSection
	{
		private const string _fixServerAddressKey = "fixServerAddress";

		[ConfigurationProperty(_fixServerAddressKey, DefaultValue = "stocksharp.com:5001")]
		public string FixServerAddress
		{
			get { return (string)base[_fixServerAddressKey]; }
		}

		private const string _connectionsKey = "connections";

		[ConfigurationProperty(_connectionsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(ConnectionElementCollection), AddItemName = "connection", ClearItemsName = "clear", RemoveItemName = "remove")]
		public ConnectionElementCollection Connections
		{
			get { return (ConnectionElementCollection)base[_connectionsKey]; }
		}

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

		private const string _toolControlsKey = "toolControls";

		[ConfigurationProperty(_toolControlsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(ControlElementCollection), AddItemName = "control", ClearItemsName = "clear", RemoveItemName = "remove")]
		public ControlElementCollection ToolControls
		{
			get { return (ControlElementCollection)base[_toolControlsKey]; }
		}

		private const string _strategyControlsKey = "strategyControls";

		[ConfigurationProperty(_strategyControlsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(ControlElementCollection), AddItemName = "control", ClearItemsName = "clear", RemoveItemName = "remove")]
		public ControlElementCollection StrategyControls
		{
			get { return (ControlElementCollection)base[_strategyControlsKey]; }
		}

		private const string _diagramElementsKey = "diagramElements";

		[ConfigurationProperty(_diagramElementsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(DiagramElementCollection), AddItemName = "element", ClearItemsName = "clear", RemoveItemName = "remove")]
		public DiagramElementCollection DiagramElements
		{
			get { return (DiagramElementCollection)base[_diagramElementsKey]; }
		}
	}
}