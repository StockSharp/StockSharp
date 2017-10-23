#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSterling.SampleSterlingPublic
File: StopOrdersWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSterling
{
	using StockSharp.BusinessEntities;

	public partial class StopOrdersWindow
	{
		public StopOrdersWindow()
		{
			InitializeComponent();
		}

		private static IConnector Connector => MainWindow.Instance.Trader;

		private void OrderGrid_OnOrderCanceling(Order order)
		{
			Connector.CancelOrder(order);
		}
	}
}
