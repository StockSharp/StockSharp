namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The drop-down list to select portfolio.
	/// </summary>
	public class PortfolioComboBox : ComboBox
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioComboBox"/>.
		/// </summary>
		public PortfolioComboBox()
		{
			DisplayMemberPath = "Name";

			var itemsSource = new ObservableCollectionEx<Portfolio>();
			ItemsSource = itemsSource;

			Portfolios = new ThreadSafeObservableCollection<Portfolio>(itemsSource);
			Connector = ConfigManager.TryGetService<IConnector>();
		}

		private IConnector _connector;

		/// <summary>
		/// Connection to the trading system.
		/// </summary>
		public IConnector Connector
		{
			get { return _connector; }
			set
			{
				if (_connector == value)
					return;

				if (_connector != null)
				{
					_connector.NewPortfolios -= OnNewPortfolios;
					Portfolios.Clear();
				}

				_connector = value;

				if (_connector != null)
				{
					OnNewPortfolios(_connector.Portfolios);
					_connector.NewPortfolios += OnNewPortfolios;
				}
			}
		}

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public IListEx<Portfolio> Portfolios { get; private set; }

		private void OnNewPortfolios(IEnumerable<Portfolio> portfolios)
		{
			Portfolios.AddRange(portfolios);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="PortfolioComboBox.SelectedPortfolio"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedPortfolioProperty =
			 DependencyProperty.Register("SelectedPortfolio", typeof(Portfolio), typeof(PortfolioComboBox),
				new FrameworkPropertyMetadata(null, OnSelectedPortfolioPropertyChanged));

		/// <summary>
		/// The selected portfolio.
		/// </summary>
		public Portfolio SelectedPortfolio
		{
			get { return (Portfolio)GetValue(SelectedPortfolioProperty); }
			set { SetValue(SelectedPortfolioProperty, value); }
		}

		private static void OnSelectedPortfolioPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var portfolio = (Portfolio)e.NewValue;
			var cb = (PortfolioComboBox)source;

			if (portfolio != null && !cb.Portfolios.Contains(portfolio))
				cb.Portfolios.Add(portfolio);

			cb.SelectedItem = portfolio;
		}

		/// <summary>
		/// The selected item change event handler.
		/// </summary>
		/// <param name="e">The event parameter.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			SelectedPortfolio = (Portfolio)SelectedItem;
			base.OnSelectionChanged(e);
		}
	}
}