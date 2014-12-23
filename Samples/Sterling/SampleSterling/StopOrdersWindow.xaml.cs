namespace SampleSterling
{
	using System.Collections.Generic;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	public partial class StopOrdersWindow
	{
		public StopOrdersWindow()
		{
			InitializeComponent();
		}

		private void OrderGrid_OnOrderCanceling(IEnumerable<Order> orders)
		{
			orders.ForEach(MainWindow.Instance.Trader.CancelOrder);
		}
	}
}
