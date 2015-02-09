namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Таблица, отображающая собственные сделки (<see cref="MyTrade"/>).
	/// </summary>
	public partial class MyTradeGrid
	{
		private readonly ThreadSafeObservableCollection<MyTrade> _trades;

		/// <summary>
		/// Создать <see cref="MyTradeGrid"/>.
		/// </summary>
		public MyTradeGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<MyTrade>();
			ItemsSource = itemsSource;

			_trades = new ThreadSafeObservableCollection<MyTrade>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// Максимальное число сделок для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _trades.MaxCount; }
			set { _trades.MaxCount = value; }
		}

		/// <summary>
		/// Список сделок, добавленных в таблицу.
		/// </summary>
		public IListEx<MyTrade> Trades
		{
			get { return _trades; }
		}

		/// <summary>
		/// Выбранная сделка.
		/// </summary>
		public MyTrade SelectedTrade
		{
			get { return SelectedTrades.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные сделки.
		/// </summary>
		public IEnumerable<MyTrade> SelectedTrades
		{
			get { return SelectedItems.Cast<MyTrade>(); }
		}
	}
}