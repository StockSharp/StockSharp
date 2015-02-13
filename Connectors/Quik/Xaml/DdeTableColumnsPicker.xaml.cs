namespace StockSharp.Quik.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Визуальный компонент выбора набора столбцов DDE таблицы.
	/// </summary>
	public partial class DdeTableColumnsPicker
	{
		/// <summary>
		/// </summary>
		public static RoutedCommand LeftDoubleClickCommand = new RoutedCommand();

		/// <summary>
		/// </summary>
		public static RoutedCommand AddCommand = new RoutedCommand();

		/// <summary>
		/// </summary>
		public static RoutedCommand RemoveCommand = new RoutedCommand();

		private readonly ObservableCollection<DdeTableColumn> _columnsAll = new ObservableCollection<DdeTableColumn>();
		private readonly ObservableCollection<DdeTableColumn> _columnsSelected = new ObservableCollection<DdeTableColumn>();
		private Type _ddeColumns;

		/// <summary>
		/// Событие изменения количества <see cref="SelectedColumns"/>.
		/// </summary>
		public event Action SelectedColumnsCountChange;

		/// <summary>
		/// DependencyProperty для <see cref="SelectedColumns"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedColumnsProperty =
			DependencyProperty.Register("SelectedColumns", typeof(ICollection<DdeTableColumn>), typeof(DdeTableColumnsPicker), new PropertyMetadata(new List<DdeTableColumn>(), PropertyChangedCallback));

		/// <summary>
		/// Список выбранных столбцов.
		/// </summary>
		public ICollection<DdeTableColumn> SelectedColumns
		{
			get { return (ICollection<DdeTableColumn>)GetValue(SelectedColumnsProperty); }
			set { SetValue(SelectedColumnsProperty, value); }
		}

		/// <summary>
		/// Тип таблицы для редактирования списка столбцов.
		/// </summary>
		public Type DdeColumns
		{
			get { return _ddeColumns; }
			set
			{
				_ddeColumns = value;

				var columns = _ddeColumns
					.GetProperties()
					.Select(propertyInfo => (DdeTableColumn)propertyInfo.GetValue(_ddeColumns, null))
					.ToList();

				_columnsAll.AddRange(columns);
			}
		}

		/// <summary>
		/// Создать <see cref="DdeTableColumnsPicker"/>.
		/// </summary>
		public DdeTableColumnsPicker()
		{
			InitializeComponent();

			lsvColumnsAll.ItemsSource = _columnsAll;
			lsvColumnsSelected.ItemsSource = _columnsSelected;
		}

		private static void PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			var editor = obj as DdeTableColumnsPicker;

			if (editor == null)
				return;

			var list = (ICollection<DdeTableColumn>)args.NewValue;

			if (list.IsEmpty())
				return;

			editor._columnsSelected.AddRange(list);
			editor._columnsAll.RemoveRange(list);

			editor.OnSelectedColumnsChange();
		}

		private void OnSelectedColumnsChange(bool invoke = true)
		{
			SelectedColumns.RemoveWhere(c => !c.IsMandatory);
			SelectedColumns.AddRange(_columnsSelected.Where(c => !c.IsMandatory));

			if (invoke)
				SelectedColumnsCountChange.SafeInvoke();
		}

		private void Add_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var items = lsvColumnsAll.SelectedItems.Cast<DdeTableColumn>().ToArray();

			_columnsSelected.AddRange(items);
			_columnsAll.RemoveRange(items);

			OnSelectedColumnsChange();
		}

		private void Remove_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var items = lsvColumnsSelected.SelectedItems.Cast<DdeTableColumn>().ToArray();

			_columnsAll.AddRange(items);
			_columnsSelected.RemoveRange(items);

			OnSelectedColumnsChange();
		}

		private void Remove_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !lsvColumnsSelected.SelectedItems.Cast<DdeTableColumn>().Any(i => i.IsMandatory);
		}

		private void EnableOrDisableUpDownButton()
		{
			var items = lsvColumnsSelected.SelectedItems;
			var ind = items.Cast<object>().Select(item => lsvColumnsSelected.Items.IndexOf(item)).ToList();

			var min = _columnsSelected.Where(c => c.IsMandatory).Select(item => lsvColumnsSelected.Items.IndexOf(item)).Max();

			btnDown.IsEnabled = ind.Min() > min && ind.Max() < lsvColumnsSelected.Items.Count - 1;
			btnUp.IsEnabled = ind.Min() > min + 1;
		}

		private void BtnUp_Click(object sender, RoutedEventArgs e)
		{
			var items = lsvColumnsSelected.SelectedItems;
			var ind = (from object item in items select lsvColumnsSelected.Items.IndexOf(item)).OrderBy();

			foreach (var i in ind)
			{
				_columnsSelected.Move(i, i - 1);
			}

			EnableOrDisableUpDownButton();
			OnSelectedColumnsChange(false);
		}

		private void BtnDown_Click(object sender, RoutedEventArgs e)
		{
			var items = lsvColumnsSelected.SelectedItems;
			var ind = (from object item in items select lsvColumnsSelected.Items.IndexOf(item)).OrderByDescending();

			foreach (var i in ind)
			{
				_columnsSelected.Move(i, i + 1);
			}

			EnableOrDisableUpDownButton();
			OnSelectedColumnsChange(false);
		}

		private void LeftDoubleClickCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void LeftDoubleClickExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (Equals(sender, lsvColumnsAll))
			{
				var item = (DdeTableColumn)lsvColumnsAll.SelectedItem;
				
				_columnsAll.Remove(item);
				_columnsSelected.Add(item);

				OnSelectedColumnsChange();
			}
			else
			{
				var item = (DdeTableColumn)lsvColumnsSelected.SelectedItem;
				
				_columnsAll.Add(item);
				_columnsSelected.Remove(item);

				OnSelectedColumnsChange();
			}
		}

		private void LsvSecurityChangesAll_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			btnAdd.IsEnabled = lsvColumnsAll.SelectedItems.Count > 0;
		}

		private void LsvSecurityChangesSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var items = lsvColumnsSelected.SelectedItems;

			btnRemove.IsEnabled = btnUp.IsEnabled = btnDown.IsEnabled = items.Count > 0;

			if (items.Count == 0)
				return;

			EnableOrDisableUpDownButton();
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			((GridView)((ListView)sender).View).Columns.First().Width = ((ListView)sender).ActualWidth - 30 < 0 ? 0 : ((ListView)sender).ActualWidth - 30;
		}
	}
}
