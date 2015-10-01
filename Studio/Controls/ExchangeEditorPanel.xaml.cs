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