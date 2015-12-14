#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: MyTradeGrid.xaml.cs
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
		public IListEx<MyTrade> Trades => _trades;

		/// <summary>
		/// The selected trade.
		/// </summary>
		public MyTrade SelectedTrade => SelectedTrades.FirstOrDefault();

		/// <summary>
		/// Selected trades.
		/// </summary>
		public IEnumerable<MyTrade> SelectedTrades => SelectedItems.Cast<MyTrade>();
	}
}