namespace SampleOptionQuoting
{
	using System.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Derivatives;
	using StockSharp.BusinessEntities;

	public partial class QuotesWindow
	{
		private MarketDepth _depth;

		public QuotesWindow()
		{
			InitializeComponent();
		}

		private static IConnector Connector
		{
			get { return MainWindow.Instance.Connector; }
		}

		public void Init(Security security)
		{
			_depth = Connector.GetMarketDepth(security);
			_depth.QuotesChanged += OnQuotesChanged;
			Connector.RegisterMarketDepth(_depth.Security);
		}

		private void OnQuotesChanged()
		{
			DepthCtrl.UpdateDepth(_depth.ImpliedVolatility(Connector, Connector, _depth.Security.ToExchangeTime(Connector.CurrentTime)));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_depth.QuotesChanged -= OnQuotesChanged;
			Connector.UnRegisterMarketDepth(_depth.Security);
			base.OnClosing(e);
		}
	}
}