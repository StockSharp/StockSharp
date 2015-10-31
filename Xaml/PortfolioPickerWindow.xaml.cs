namespace StockSharp.Xaml
{
	using System;
	using System.Windows.Input;

	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The portfolio selection window.
	/// </summary>
	partial class PortfolioPickerWindow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioPickerWindow"/>.
		/// </summary>
		public PortfolioPickerWindow()
		{
			InitializeComponent();

			_portfolios = new ThreadSafeObservableCollection<Portfolio>(new ObservableCollectionEx<Portfolio>());
		}

		/// <summary>
		/// The selected portfolio.
		/// </summary>
		public Portfolio SelectedPortfolio
		{
			get { return (Portfolio)PortfoliosCtrl.SelectedItem; }
			set { PortfoliosCtrl.SelectedItem = value; }
		}

		private ThreadSafeObservableCollection<Portfolio> _portfolios;

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public ThreadSafeObservableCollection<Portfolio> Portfolios
		{
			get { return _portfolios; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_portfolios = value;
				PortfoliosCtrl.ItemsSource = value.Items;
			}
		}

		private void PortfoliosCtrl_OnSelectionChanged(object sender, EventArgs e)
		{
			OkBtn.IsEnabled = SelectedPortfolio != null;
		}

		private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
		{
			SelectedPortfolio = (Portfolio)PortfoliosCtrl.CurrentItem;
			DialogResult = true;
		}
	}
}