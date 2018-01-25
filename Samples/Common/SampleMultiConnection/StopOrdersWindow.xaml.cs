#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleMultiConnection.SampleMultiConnectionPublic
File: StopOrdersWindow.xaml.cs
Created: 2015, 12, 2, 8:18 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleMultiConnection
{
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class StopOrderWindow
	{
		public StopOrderWindow()
		{
			InitializeComponent();
		}

		private static IConnector Connector => MainWindow.Instance.Connector;

		private void OrderGrid_OnOrderCanceling(Order order)
		{
			Connector.CancelOrder(order);
		}

		private void OrderGrid_OnOrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				SecurityProvider = Connector,
				MarketDataProvider = Connector,
				Portfolios = new PortfolioDataSource(Connector),
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};

			if (window.ShowModal(this))
			{
				Connector.ReRegisterOrder(order, window.Order);
			}
		}
	}
}
