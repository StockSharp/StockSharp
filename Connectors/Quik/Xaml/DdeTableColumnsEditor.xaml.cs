#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Xaml.QuikPublic
File: DdeTableColumnsEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;
	using Ecng.Xaml;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	using StockSharp.Localization;

	/// <summary>
	/// Визуальный редактор для выбора набора столбцов DDE таблицы.
	/// </summary>
	public partial class DdeTableColumnsEditor : ITypeEditor
	{
		/// <summary>
		/// DependencyProperty для <see cref="SelectedColumns"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedColumnsProperty =
			DependencyProperty.Register(nameof(SelectedColumns), typeof(ICollection<DdeTableColumn>), typeof(DdeTableColumnsEditor), new PropertyMetadata(new List<DdeTableColumn>()));

		/// <summary>
		/// Список выбранных столбцов.
		/// </summary>
		public ICollection<DdeTableColumn> SelectedColumns
		{
			get { return (ICollection<DdeTableColumn>)GetValue(SelectedColumnsProperty); }
			set { SetValue(SelectedColumnsProperty, value); }
		}

		/// <summary>
		/// Создать <see cref="DdeTableColumnsEditor"/>.
		/// </summary>
		public DdeTableColumnsEditor()
		{
			InitializeComponent();
			
			ColumnsPicker.SelectedColumnsCountChange += ChangeText;
		}

		private void ChangeText()
		{
			ColumnsCount.Text = LocalizedStrings.Str1852Params.Put(SelectedColumns.Count);
		}

		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			var type = ((DdeTable)propertyItem.Value).Type;

			switch (type)
			{
				case DdeTableTypes.Security:
					ColumnsPicker.DdeColumns = typeof(DdeSecurityColumns);
					break;
				case DdeTableTypes.Order:
					ColumnsPicker.DdeColumns = typeof(DdeOrderColumns);
					break;
				case DdeTableTypes.StopOrder:
					ColumnsPicker.DdeColumns = typeof(DdeStopOrderColumns);
					break;
				case DdeTableTypes.Trade:
					ColumnsPicker.DdeColumns = typeof(DdeTradeColumns);
					break;
				case DdeTableTypes.MyTrade:
					ColumnsPicker.DdeColumns = typeof(DdeMyTradeColumns);
					break;
				case DdeTableTypes.Quote:
					ColumnsPicker.DdeColumns = typeof(DdeQuoteColumns);
					break;
				case DdeTableTypes.EquityPosition:
					ColumnsPicker.DdeColumns = typeof(DdeEquityPositionColumns);
					break;
				case DdeTableTypes.DerivativePosition:
					ColumnsPicker.DdeColumns = typeof(DdeDerivativePositionColumns);
					break;
				case DdeTableTypes.EquityPortfolio:
					ColumnsPicker.DdeColumns = typeof(DdeEquityPortfolioColumns);
					break;
				case DdeTableTypes.DerivativePortfolio:
					ColumnsPicker.DdeColumns = typeof(DdeDerivativePortfolioColumns);
					break;
				case DdeTableTypes.CurrencyPortfolio:
					ColumnsPicker.DdeColumns = typeof(DdeCurrencyPortfolioColumns);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			this.SetBindings(SelectedColumnsProperty, propertyItem, "Value.Columns", BindingMode.OneWay);
			ColumnsPicker.SetBindings(DdeTableColumnsPicker.SelectedColumnsProperty, propertyItem, "Value.Columns", BindingMode.OneWay);

			return this;
		}
	}
}
