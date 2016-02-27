#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Xaml.QuikPublic
File: DdeSecurityChangesColumnsEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Визуальный редактор для выбора набора столбцов таблицы Инструменты(изменения).
	/// </summary>
	public partial class DdeSecurityChangesColumnsEditor : ITypeEditor
	{
		/// <summary>
		/// DependencyProperty для <see cref="SelectedColumns"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedColumnsProperty =
			DependencyProperty.Register(nameof(SelectedColumns), typeof(List<string>), typeof(DdeSecurityChangesColumnsEditor), new PropertyMetadata(new List<string>()));

		/// <summary>
		/// Список выбранных столбцов.
		/// </summary>
		public List<string> SelectedColumns
		{
			get { return (List<string>)GetValue(SelectedColumnsProperty); }
			set { SetValue(SelectedColumnsProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="DdeSecurityChangesColumnsEditor"/>.
		/// </summary>
		public DdeSecurityChangesColumnsEditor()
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
			SetBinding(SelectedColumnsProperty, new Binding("Value")
			{
				Source = propertyItem,
				Mode = BindingMode.TwoWay
			});

			ColumnsPicker.SetBinding(DdeColumnsPicker.SelectedColumnsProperty, new Binding(nameof(SelectedColumns))
			{
				Source = this,
				Mode = BindingMode.TwoWay
			});

			return this;
		}
	}
}