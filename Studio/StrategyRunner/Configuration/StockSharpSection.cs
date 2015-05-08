namespace StockSharp.Studio.StrategyRunner.Configuration
{
	using System.Configuration;

	using StockSharp.Studio.Configuration;

	class StockSharpSection : ConfigurationSection
	{
		private const string _connectionsKey = "connections";

		[ConfigurationProperty(_connectionsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(ConnectionElementCollection), AddItemName = "connection", ClearItemsName = "clear", RemoveItemName = "remove")]
		public ConnectionElementCollection Connections
		{
			get { return (ConnectionElementCollection)base[_connectionsKey]; }
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
