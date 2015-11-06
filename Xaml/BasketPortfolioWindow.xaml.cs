namespace StockSharp.Xaml
{
	using System.Collections.ObjectModel;
	using System;
	using System.Windows.Input;

	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The window for portfolios basket editing.
	/// </summary>
	public partial class BasketPortfolioWindow
	{
		/// <summary>
		/// The command for the portfolio basket saving.
		/// </summary>
		public readonly static RoutedCommand OkCommand = new RoutedCommand();

		/// <summary>
		/// The command for adding portfolio to a basket.
		/// </summary>
		public readonly static RoutedCommand AddCommand = new RoutedCommand();

		/// <summary>
		/// The command for removal of portfolio from the basket.
		/// </summary>
		public readonly static RoutedCommand RemoveCommand = new RoutedCommand();

		/// <summary>
		/// All available portfolios.
		/// </summary>
		public ObservableCollection<Portfolio> AllPortfolios { set; private get; }

		/// <summary>
		/// Portfolios included in the basket.
		/// </summary>
		public ObservableCollection<Portfolio> InnerPortfolios { set; private get; }

		private IConnector _connector;

		/// <summary>
		/// The interface to the trading system.
		/// </summary>
		public IConnector Connector
		{
			get
			{
				return _connector;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				_connector = value;

				AllPortfolios.Clear();
				AllPortfolios.AddRange(_connector.Portfolios);

				_portfolio = new WeightedPortfolio(_connector);
			}
		}

		private WeightedPortfolio _portfolio;

		/// <summary>
		/// Basket portfolio.
		/// </summary>
		public WeightedPortfolio Portfolio
		{
			get
			{
				return _portfolio;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				_portfolio = value;
				InnerPortfolios.AddRange(_portfolio.InnerPortfolios);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketPortfolioWindow"/>.
		/// </summary>
		public BasketPortfolioWindow()
		{
			AllPortfolios = new ObservableCollection<Portfolio>();
			InnerPortfolios = new ObservableCollection<Portfolio>();

			InitializeComponent();
		}

		private Portfolio SelectedAllPortfolio => ComboBoxAllPortfolios != null ? (Portfolio)ComboBoxAllPortfolios.SelectedItem : null;

		private Portfolio SelectedInnerPortfolio => ListBoxPortfolios != null ? (Portfolio)ListBoxPortfolios.SelectedItem : null;

		private void ExecutedOk(object sender, ExecutedRoutedEventArgs e)
		{
			//Portfolio.InnerPortfolios.Clear();
			//Portfolio.InnerPortfolios.AddRange(InnerPortfolios);

			DialogResult = true;
		}

		private void CanExecuteOk(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = InnerPortfolios.Count > 0;
		}

		private void ExecutedAdd(object sender, ExecutedRoutedEventArgs e)
		{
			InnerPortfolios.Add(SelectedAllPortfolio);
			_portfolio.Weights.Add(SelectedAllPortfolio, 1);
		}

		private void CanExecuteAdd(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedAllPortfolio != null && !InnerPortfolios.Contains(SelectedAllPortfolio);
		}

		private void ExecutedRemove(object sender, ExecutedRoutedEventArgs e)
		{
			InnerPortfolios.Remove(SelectedInnerPortfolio);
			_portfolio.Weights.Remove(SelectedInnerPortfolio);
		}

		private void CanExecuteRemove(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedInnerPortfolio != null;
		}
	}
}