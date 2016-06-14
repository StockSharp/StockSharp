#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleTwime.SampleTwimePublic
File: OrdersWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleTwime
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Twime;
	using StockSharp.Xaml;

	public partial class OrdersWindow
	{
		public OrdersWindow()
		{
			InitializeComponent();
		}

		private TwimeTrader Trader => MainWindow.Instance.Trader;

		private void OrderGrid_OnOrderCanceling(IEnumerable<Order> orders)
		{
			orders.ForEach(Trader.CancelOrder);
		}

		private void OrderGrid_OnOrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				SecurityProvider = Trader,
				MarketDataProvider = Trader,
				Portfolios = new PortfolioDataSource(Trader),
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};

			if (window.ShowModal(this))
			{
				Trader.ReRegisterOrder(order, window.Order);
			}
		}

		private void CancelAll_OnClick(object sender, RoutedEventArgs e)
		{
			Trader.CancelOrders(portfolio: Trader.Portfolios.FirstOrDefault());
		}
	}
}