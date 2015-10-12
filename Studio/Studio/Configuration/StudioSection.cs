namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	using StockSharp.Configuration;

	class StudioSection : StockSharpSection
	{
		private const string _fixServerAddressKey = "fixServerAddress";

		[ConfigurationProperty(_fixServerAddressKey, DefaultValue = "stocksharp.com:5001")]
		public string FixServerAddress
		{
			get { return (string)base[_fixServerAddressKey]; }
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
	}
}