namespace StockSharp.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> for <see cref="SecurityTypesComboBox"/>.
	/// </summary>
	public class SecurityTypesComboBoxEditor : TypeEditor<SecurityTypesComboBox>
	{
		/// <summary>
		/// To set <see cref="TypeEditor{T}.ValueProperty"/> with the <see cref="SecurityTypesComboBox.SelectedTypesProperty"/> value.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = SecurityTypesComboBox.SelectedTypesProperty;
		}
	}
}