namespace StockSharp.Rss.Xaml
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using Ecng.Xaml;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// The drop-down list to select the RSS feed address.
	/// </summary>
	public class RssAddressEditor : ITypeEditor
	{
		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			var comboBox = new RssAddressComboBox { IsEditable = true, Width = double.NaN };

			var binding = new Binding("Value")
			{
				Source = propertyItem,
				Converter = new UriValueConverter(comboBox, comboBox.SelectedAddress),
				Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
			};

			BindingOperations.SetBinding(comboBox, ComboBox.TextProperty, binding);
			return comboBox;
		}
	}
}