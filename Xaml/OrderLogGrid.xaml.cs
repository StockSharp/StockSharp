namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Таблица, отображающая лог заявок (<see cref="OrderLogItem"/>).
	/// </summary>
	public partial class OrderLogGrid
	{
		private readonly ThreadSafeObservableCollection<OrderLogItem> _items;

		/// <summary>
		/// Создать <see cref="OrderLogGrid"/>.
		/// </summary>
		public OrderLogGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<OrderLogItem>();
			ItemsSource = itemsSource;

			_items = new ThreadSafeObservableCollection<OrderLogItem>(itemsSource) { MaxCount = 100000 };
		}

		/// <summary>
		/// Максимальное число строчек для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 100000.
		/// </summary>
		public int MaxCount
		{
			get { return _items.MaxCount; }
			set { _items.MaxCount = value; }
		}

		/// <summary>
		/// Лог заявок.
		/// </summary>
		public IListEx<OrderLogItem> LogItems
		{
			get { return _items; }
		}

		/// <summary>
		/// Выбранная строчка.
		/// </summary>
		public OrderLogItem SelectedLogItem
		{
			get { return SelectedLogItems.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные строчки.
		/// </summary>
		public IEnumerable<OrderLogItem> SelectedLogItems
		{
			get { return SelectedItems.Cast<OrderLogItem>(); }
		}
	}
}