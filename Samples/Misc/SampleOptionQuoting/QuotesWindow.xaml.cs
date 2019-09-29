namespace SampleOptionQuoting
{
	using StockSharp.BusinessEntities;

	public partial class QuotesWindow
	{
		public QuotesWindow()
		{
			InitializeComponent();
		}

		public void Update(MarketDepth depth)
		{
			DepthCtrl.UpdateDepth(depth);
		}
	}
}