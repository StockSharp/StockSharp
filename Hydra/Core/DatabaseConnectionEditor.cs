namespace StockSharp.Hydra.Core
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="DatabaseConnectionComboBox"/>.
	/// </summary>
	public class DatabaseConnectionEditor : TypeEditor<DatabaseConnectionComboBox>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="DatabaseConnectionComboBox.SelectedConnectionProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = DatabaseConnectionComboBox.SelectedConnectionProperty;
		}
	}
}