namespace SampleStrategies
{
	using System;

	using DevExpress.Xpf.Editors;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class QuotingWindow
	{
		public QuotingWindow()
		{
			InitializeComponent();

			SecurityCtrl.SecurityProvider = MainWindow.Instance.Connector;
			PortfolioCtrl.Portfolios = new PortfolioDataSource(MainWindow.Instance.Connector);
		}

		public Security Security
		{
			get { return SecurityCtrl.SelectedSecurity; }
			set { SecurityCtrl.SelectedSecurity = value; }
		}

		public Portfolio Portfolio
		{
			get { return PortfolioCtrl.SelectedPortfolio; }
			set { PortfolioCtrl.SelectedPortfolio = value; }
		}

		public decimal Volume
		{
			get { return (decimal?)AmountCtrl.EditValue ?? 0; }
			set { AmountCtrl.EditValue = value; }
		}

		public Sides Side
		{
			get { return IsBuyCtrl.IsChecked == true ? Sides.Buy : Sides.Sell; }
			set
			{
				switch (value)
				{
					case Sides.Buy:
						IsBuyCtrl.IsChecked = true;
						break;
					case Sides.Sell:
						IsSellCtrl.IsChecked = true;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(value), value, null);
				}
			}
		}

		private void SecurityCtrl_OnSecuritySelected(object sender, EditValueChangedEventArgs e)
		{
			TryEnableSend();
		}

		private void PortfolioCtrl_OnSelectionChanged(object sender, EditValueChangedEventArgs e)
		{
			TryEnableSend();
		}

		private void AmountCtrl_OnValueChanged(object sender, EditValueChangedEventArgs e)
		{
			TryEnableSend();
		}

		private void TryEnableSend()
		{
			Send.IsEnabled = Security != null && Portfolio != null && Volume != 0;
		}
	}
}