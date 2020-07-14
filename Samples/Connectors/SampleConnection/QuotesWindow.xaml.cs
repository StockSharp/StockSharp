namespace SampleConnection
{
	using System.Windows.Input;
	using System.Linq;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Xaml;

	public partial class QuotesWindow
	{
		public QuotesWindow()
		{
			InitializeComponent();
		}

		public Security Security { get; set; }

		private static Connector Connector => MainWindow.Instance.MainPanel.Connector;

		private void DepthCtrl_MovingOrder(Order order, decimal newPrice)
		{
			Connector.EditOrder(order, order.ReRegisterClone(newPrice));
		}

		private void DepthCtrl_CancelingOrder(Order order)
		{
			Connector.CancelOrder(order);
		}

		private void DepthCtrl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				var connector = Connector;

				var quote = DepthCtrl.SelectedQuote;

				var wnd = new OrderWindow
				{
					Order = new Order
					{
						Price = quote?.Price ?? 0,
						Security = Security,
						Portfolio = connector.Portfolios.FirstOrDefault(),
					},
				}.Init(connector);

				if (wnd.ShowModal(this))
					connector.RegisterOrder(wnd.Order);
			}
		}

		public void ProcessOrder(Order order)
		{
			DepthCtrl.ProcessOrder(order, order.Balance, order.State);
		}

		public void ProcessOrderRegisterFail(OrderFail fail)
		{
			DepthCtrl.ProcessOrderRegisterFail(fail);
		}

		public void ProcessOrderCancelFail(OrderFail fail)
		{
			DepthCtrl.ProcessOrderCancelFail(fail);
		}
	}
}