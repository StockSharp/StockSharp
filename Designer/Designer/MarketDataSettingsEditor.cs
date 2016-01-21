namespace StockSharp.Designer
{
	using StockSharp.Studio.Controls;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	class MarketDataSettingsEditor : TypeEditor<MarketDataSettingsComboBox>
	{
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = MarketDataSettingsComboBox.SelectedSettingsProperty;
		}
	}
}