namespace StockSharp.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="SecurityTypesComboBox"/>.
	/// </summary>
	public class SecurityTypesComboBoxEditor : TypeEditor<SecurityTypesComboBox>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="SecurityTypesComboBox.SelectedTypesProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = SecurityTypesComboBox.SelectedTypesProperty;
		}
	}
}