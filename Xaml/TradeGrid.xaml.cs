#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: TradeGrid.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
		public IListEx<Trade> Trades => _trades;

		/// <summary>
		/// The selected trade.
		/// </summary>
		public Trade SelectedTrade => SelectedTrades.FirstOrDefault();

		/// <summary>
		/// Selected trades.
		/// </summary>
		public IEnumerable<Trade> SelectedTrades => SelectedItems.Cast<Trade>();
	}
}