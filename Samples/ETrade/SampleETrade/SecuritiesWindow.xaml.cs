#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleETrade.SampleETradePublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleETrade
{
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			NewOrder.IsEnabled = security != null;
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
			}.Init(MainWindow.Instance.Trader);

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow
			{
				ShowAllOption = MainWindow.Instance.Trader.MarketDataAdapter.IsSupportSecuritiesLookupAll,
				Criteria = new Security { Code = "AAPL" }
			};

			if (!wnd.ShowModal(this))
				return;

			MainWindow.Instance.Trader.LookupSecurities(wnd.Criteria);
		}
	}
}