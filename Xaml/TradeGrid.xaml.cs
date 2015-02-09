namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Таблица, отображающая тиковые сделки (<see cref="Trade"/>).
	/// </summary>
	public partial class TradeGrid
	{
		private readonly ThreadSafeObservableCollection<Trade> _trades;

		/// <summary>
		/// Создать <see cref="TradeGrid"/>.
		/// </summary>
		public TradeGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<Trade>();
			ItemsSource = itemsSource;

			_trades = new ThreadSafeObservableCollection<Trade>(itemsSource) { MaxCount = 1000000 };
		}

		/// <summary>
		/// Максимальное число сделок для показа. Значение -1 означает бесконечное количество.
		/// По-умолчанию равно 1000000.
		/// </summary>
		public int MaxCount
		{
			get { return _trades.MaxCount; }
			set { _trades.MaxCount = value; }
		}

		/// <summary>
		/// Список сделок, добавленных в таблицу.
		/// </summary>
		public IListEx<Trade> Trades
		{
			get { return _trades; }
		}

		/// <summary>
		/// Выбранная сделка.
		/// </summary>
		public Trade SelectedTrade
		{
			get { return SelectedTrades.FirstOrDefault(); }
		}

		/// <summary>
		/// Выбранные сделки.
		/// </summary>
		public IEnumerable<Trade> SelectedTrades
		{
			get { return SelectedItems.Cast<Trade>(); }
		}
	}
}