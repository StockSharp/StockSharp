namespace SampleIB
{
	public partial class OptionWindow
	{
		private class OptionParams
		{
			public decimal AssetPrice { get; set; }
			public decimal Volatility { get; set; }
			public decimal OptionPrice { get; set; }
		}

		private readonly OptionParams _params = new OptionParams();

		public OptionWindow()
		{
			InitializeComponent();

			PropGrid.SelectedObject = _params;
		}

		public decimal AssetPrice => _params.AssetPrice;
		public decimal Volatility => _params.Volatility;
		public decimal OptionPrice => _params.OptionPrice;
	}
}