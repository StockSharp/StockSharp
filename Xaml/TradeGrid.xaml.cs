namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The table showing tick trades (<see cref="Trade"/>).
	/// </summary>
	public partial class TradeGrid
	{
		private readonly ThreadSafeObservableCollection<Trade> _trades;

		/// <summary>
		/// Initializes a new instance of the <see cref="TradeGrid"/>.
		/// </summary>
		public TradeGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<Trade>();
			ItemsSource = itemsSource;

			_trades = new ThreadSafeObservableCollection<Trade>(itemsSource) { MaxCount = 1000000 };
		}

		/// <summary>
		/// The maximum number of trades to display. The -1 value means an unlimited amount. The default value is 1000000.
		/// </summary>
		public int MaxCount
		{
			get { return _trades.MaxCount; }
			set { _trades.MaxCount = value; }
		}

		/// <summary>
		/// List of trades added to the table.
		/// </summary>
		public IListEx<Trade> Trades
		{
			get { return _trades; }
		}

		/// <summary>
		/// The selected trade.
		/// </summary>
		public Trade SelectedTrade
		{
			get { return SelectedTrades.FirstOrDefault(); }
		}

		/// <summary>
		/// Selected trades.
		/// </summary>
		public IEnumerable<Trade> SelectedTrades
		{
			get { return SelectedItems.Cast<Trade>(); }
		}
	}
}