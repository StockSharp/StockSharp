#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Configuration.ConfigurationPublic
File: StockSharpSection.cs
Created: 2015, 12, 7, 5:06 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Configuration
{
	using System.Configuration;

	/// <summary>
	/// Represents the root StockSharp section in a configuration file.
	/// </summary>
	public class StockSharpSection : ConfigurationSection
	{
		private const string _candlesKey = "customCandles";

		/// <summary>
		/// Custom candles.
		/// </summary>
		[ConfigurationProperty(_candlesKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(CandleElementCollection), AddItemName = "candle", ClearItemsName = "clear", RemoveItemName = "remove")]
		public CandleElementCollection CustomCandles => (CandleElementCollection)base[_candlesKey];

		private const string _indicatorsKey = "customIndicators";

		/// <summary>
		/// Custom indicators.
		/// </summary>
		[ConfigurationProperty(_indicatorsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(IndicatorElementCollection), AddItemName = "indicator", ClearItemsName = "clear", RemoveItemName = "remove")]
		public IndicatorElementCollection CustomIndicators => (IndicatorElementCollection)base[_indicatorsKey];

		private const string _diagramElementsKey = "customDiagramElements";

		/// <summary>
		/// Custom diagram elements.
		/// </summary>
		[ConfigurationProperty(_diagramElementsKey, IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(DiagramElementCollection), AddItemName = "element", ClearItemsName = "clear", RemoveItemName = "remove")]
		public DiagramElementCollection CustomDiagramElements => (DiagramElementCollection)base[_diagramElementsKey];
	}
}