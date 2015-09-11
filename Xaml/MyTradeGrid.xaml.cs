namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// A table showing own trades (<see cref="MyTrade"/>).
	/// </summary>
	public partial class MyTradeGrid
	{
		private readonly ThreadSafeObservableCollection<MyTrade> _trades;

		/// <summary>
		/// Initializes a new instance of the <see cref="MyTradeGrid"/>.
		/// </summary>
		public MyTradeGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<MyTrade>();
			ItemsSource = itemsSource;

			_trades = new ThreadSafeObservableCollection<MyTrade>(itemsSource) { MaxCount = 10000 };
		}

		/// <summary>
		/// The maximum number of trades to display. The -1 value means an unlimited amount. The default value is 10000.
		/// </summary>
		public int MaxCount
		{
			get { return _trades.MaxCount; }
			set { _trades.MaxCount = value; }
		}

		/// <summary>
		/// List of trades added to the table.
		/// </summary>
		public IListEx<MyTrade> Trades
		{
			get { return _trades; }
		}

		/// <summary>
		/// The selected trade.
		/// </summary>
		public MyTrade SelectedTrade
		{
			get { return SelectedTrades.FirstOrDefault(); }
		}

		/// <summary>
		/// Selected trades.
		/// </summary>
		public IEnumerable<MyTrade> SelectedTrades
		{
			get { return SelectedItems.Cast<MyTrade>(); }
		}
	}
}