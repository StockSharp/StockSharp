#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ExecutionGrid.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Messages;

	/// <summary>
	/// Table showing (<see cref="ExecutionMessage"/>.
	/// </summary>
	public partial class ExecutionGrid
	{
		private readonly ThreadSafeObservableCollection<ExecutionMessage> _messages;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExecutionGrid"/>.
		/// </summary>
		public ExecutionGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<ExecutionMessage>();
			ItemsSource = itemsSource;

			_messages = new ThreadSafeObservableCollection<ExecutionMessage>(itemsSource) { MaxCount = 1000000 };
		}

		/// <summary>
		/// The maximum number of rows to display. The -1 value means an unlimited amount. The default is 1000000.
		/// </summary>
		public int MaxCount
		{
			get { return _messages.MaxCount; }
			set { _messages.MaxCount = value; }
		}

		/// <summary>
		/// The list of messages added to the table.
		/// </summary>
		public IListEx<ExecutionMessage> Messages => _messages;

		/// <summary>
		/// The selected message.
		/// </summary>
		public ExecutionMessage SelectedMessage => SelectedMessages.FirstOrDefault();

		/// <summary>
		/// Selected messages.
		/// </summary>
		public IEnumerable<ExecutionMessage> SelectedMessages => SelectedItems.Cast<ExecutionMessage>();

		/// <summary>
		/// To hide columns which do not show data for the passed type.
		/// </summary>
		/// <param name="type">Information type in <see cref="ExecutionMessage"/>.</param>
		public void HideColumns(ExecutionTypes type)
		{
			switch (type)
			{
				case ExecutionTypes.Tick:
				{
					OrderIdColumn.Visibility =
					OrderPriceColumn.Visibility =
					OrderTypeColumn.Visibility =
					OrderSideColumn.Visibility =
					OrderStateColumn.Visibility =
					OrderVolumeColumn.Visibility =
					TransactionColumn.Visibility =
					PortfolioColumn.Visibility =
					BalanceColumn.Visibility =
						Visibility.Collapsed;

					break;
				}
				//case ExecutionTypes.Order:
				//{
				//	TradeIdColumn.Visibility = TradePriceColumn.Visibility = Visibility.Collapsed;
				//	break;
				//}
				//case ExecutionTypes.Trade:
				//	break;
				case ExecutionTypes.OrderLog:
					TradeVolumeColumn.Visibility = Visibility.Collapsed;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type));
			}
		}
	}
}