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
	public partial class DdeColumnsPicker
	{
		/// <summary>
		/// </summary>
		public static RoutedCommand LeftDoubleClickCommand = new RoutedCommand();

		private readonly ObservableCollection<RefPair<string, string>> _columnsAll = new ObservableCollection<RefPair<string, string>>();
		private readonly ObservableCollection<RefPair<string, string>> _columnsSelected = new ObservableCollection<RefPair<string, string>>();
		private Type _ddeColumns;

		/// <summary>
		/// Событие изменения количества <see cref="SelectedColumns"/>.
		/// </summary>
		public event Action SelectedColumnsCountChange;

		/// <summary>
		/// DependencyProperty для <see cref="SelectedColumns"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedColumnsProperty =
			DependencyProperty.Register("SelectedColumns", typeof(List<string>), typeof(DdeColumnsPicker), new PropertyMetadata(new List<string>(), PropertyChangedCallback));

		/// <summary>
		/// Список выбранных столбцов.
		/// </summary>
		public List<string> SelectedColumns
		{
			get { return (List<string>)GetValue(SelectedColumnsProperty); }
			set { SetValue(SelectedColumnsProperty, value); }
		}

		/// <summary>
		/// Столбцы, исключенные из списка выбора.
		/// </summary>
		public SynchronizedList<string> ExcludeColumns { get; private set; }

		/// <summary>
		/// Тип таблицы для редактирования списка столбцов.
		/// </summary>
		public Type DdeColumns
		{
			get { return _ddeColumns; }
			protected set
			{
				_ddeColumns = value;

				var columns = _ddeColumns
					.GetProperties()
					.Where(prop => !ExcludeColumns.Contains(prop.Name))
					.Select(propertyInfo => new RefPair<string, string>
					{
						First = propertyInfo.Name,
						Second = propertyInfo.GetValue(_ddeColumns, null).ToString(),
					})
					.ToList();

				_columnsAll.AddRange(columns);
			}
		}

		/// <summary>
		/// Создать <see cref="DdeColumnsPicker"/>.
		/// </summary>
		public DdeColumnsPicker()
		{
			InitializeComponent();

			lsvColumnsAll.ItemsSource = _columnsAll;
			lsvColumnsSelected.ItemsSource = _columnsSelected;

			ExcludeColumns = new SynchronizedList<string>();
		}

		/// <summary>
		/// Получить список выбранных столбцов.
		/// </summary>
		/// <returns>Список столбцов.</returns>
		public IEnumerable<DdeTableColumn> GetSelectedColumns()
		{
			return DdeColumns.GetColumns(SelectedColumns);
		}

		private static void PropertyChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			var editor = obj as DdeColumnsPicker;

			if (editor == null)
				return;

			var list = (List<string>)args.NewValue;

			if (list.IsEmpty())
				return;

			//var s = editor._columnsAll.Where(c => list.Contains(c.First)).ToList();

			var selectedColumns = list.Select(column => editor._columnsAll.Single(t => t.First.Equals(column))).ToList();

			editor._columnsSelected.AddRange(selectedColumns);
			editor._columnsAll.RemoveRange(selectedColumns);

			editor.OnSelectedColumnsChange();
		}

		private void OnSelectedColumnsChange(bool invoke = true)
		{
			SelectedColumns.Clear();
			SelectedColumns.AddRange(_columnsSelected.Select(i => i.First));

			if (invoke)
				SelectedColumnsCountChange.SafeInvoke();
		}

		private void btnAdd_Click(object sender, RoutedEventArgs e)
		{
			var items = lsvColumnsAll.SelectedItems.Cast<RefPair<string, string>>().ToArray();

			_columnsSelected.AddRange(items);
			_columnsAll.RemoveRange(items);

			OnSelectedColumnsChange();
		}

		private void btnRemove_Click(object sender, RoutedEventArgs e)
		{
			var items = lsvColumnsSelected.SelectedItems.Cast<RefPair<string, string>>().ToArray();

			_columnsAll.AddRange(items);
			_columnsSelected.RemoveRange(items);

			OnSelectedColumnsChange();
		}

		private void EnableOrDisableUpDownButton()
		{
			var items = lsvColumnsSelected.SelectedItems;
			var ind = (from object item in items select lsvColumnsSelected.Items.IndexOf(item)).ToList();

			btnDown.IsEnabled = ind.Max() < lsvColumnsSelected.Items.Count - 1;
			btnUp.IsEnabled = ind.Min() > 0;
		}

		private void btnUp_Click(object sender, RoutedEventArgs e)
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

		private void btnDown_Click(object sender, RoutedEventArgs e)
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
				var item = (RefPair<string, string>)lsvColumnsAll.SelectedItem;
				
				_columnsAll.Remove(item);
				_columnsSelected.Add(item);

				OnSelectedColumnsChange();
			}
			else
			{
				var item = (RefPair<string, string>)lsvColumnsSelected.SelectedItem;
				
				_columnsAll.Add(item);
				_columnsSelected.Remove(item);

				OnSelectedColumnsChange();
			}
		}

		private void lsvSecurityChangesAll_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			btnAdd.IsEnabled = lsvColumnsAll.SelectedItems.Count > 0;
		}

		private void lsvSecurityChangesSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

	/// <summary>
	/// Визуальный компонент выбора набора столбцов таблицы Инструменты.
	/// </summary>
	public class DdeSecurityColumnsPicker : DdeColumnsPicker
	{
		/// <summary>
		/// Создать <see cref="DdeSecurityColumnsPicker"/>.
		/// </summary>
		public DdeSecurityColumnsPicker()
		{
			ExcludeColumns.AddRange(new[]
			{
				"Name",
				"Code",
				"Class",
				"Status",
				"VolumeStep",
				"PriceStep"
			});

			DdeColumns = typeof(DdeSecurityColumns);
		}
	}

	/// <summary>
	/// Визуальный компонент выбора набора столбцов таблицы Инструменты(изменения).
	/// </summary>
	public class DdeSecurityChangesColumnsPicker : DdeColumnsPicker
	{
		/// <summary>
		/// Создать <see cref="DdeSecurityChangesColumnsPicker"/>.
		/// </summary>
		public DdeSecurityChangesColumnsPicker()
		{
			ExcludeColumns.AddRange(new[]
			{
				"LastChangeTime", 
				"Code", 
				"Class"
			});

			DdeColumns = typeof(DdeSecurityColumns);
		}
	}
}
