namespace StockSharp.Quik.Xaml
{
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Data;
	
	using Ecng.Common;
	
	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
	using StockSharp.Localization;

	/// <summary>
	/// Визуальный редактор для выбора набора столбцов таблицы Инструменты.
	/// </summary>
	public partial class DdeSecurityColumnsEditor : ITypeEditor
	{
		/// <summary>
		/// DependencyProperty для <see cref="SelectedColumns"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedColumnsProperty =
			DependencyProperty.Register("SelectedColumns", typeof(List<string>), typeof(DdeSecurityColumnsEditor), new PropertyMetadata(new List<string>()));

		/// <summary>
		/// Список выбранных столбцов.
		/// </summary>
		public List<string> SelectedColumns
		{
			get { return (List<string>)GetValue(SelectedColumnsProperty); }
			set { SetValue(SelectedColumnsProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="DdeSecurityColumnsEditor"/>.
		/// </summary>
		public DdeSecurityColumnsEditor()
		{
			InitializeComponent();

			ColumnsPicker.SelectedColumnsCountChange += ChangeText;
		}

		private void ChangeText()
		{
			ColumnsCount.Text = LocalizedStrings.Str1852Params.Put(SelectedColumns.Count);
		}

		/// <summary>
		/// Получить список столбцов по их названиям.
		/// </summary>
		/// <param name="columns">Названия столбцов.</param>
		/// <returns>Список столбцов.</returns>
		public static IEnumerable<DdeTableColumn> GetColumns(IEnumerable<string> columns)
		{
			return typeof(DdeSecurityColumns).GetColumns(columns);
		}

		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			SetBinding(SelectedColumnsProperty, new Binding("Value") { Source = propertyItem, Mode = BindingMode.TwoWay });
			ColumnsPicker.SetBinding(DdeColumnsPicker.SelectedColumnsProperty, new Binding("SelectedColumns") { Source = this, Mode = BindingMode.TwoWay });

			return this;
		}
	}
}