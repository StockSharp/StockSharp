#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: ExchangeEditorPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3234Key)]
	[DescriptionLoc(LocalizedStrings.Str3235Key)]
	[Icon("images/exchange_32x32.png")]
	public partial class ExchangeEditorPanel
	{
		public ExchangeEditorPanel()
		{
			InitializeComponent();
		}

		public override void Save(SettingsStorage storage)
		{
			Editor.Save(storage);
		}

		public override void Load(SettingsStorage storage)
		{
			Editor.Load(storage);
		}
	}
}