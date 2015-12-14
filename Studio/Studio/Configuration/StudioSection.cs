#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Configuration.StudioPublic
File: StudioSection.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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