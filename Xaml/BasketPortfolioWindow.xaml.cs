namespace StockSharp.Xaml
{
	using System.Collections.ObjectModel;
	using System;
	using System.Windows.Input;

	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Окно редактирования корзины портфелей.
	/// </summary>
	public partial class BasketPortfolioWindow
	{
		/// <summary>
		/// Команда сохранения корзины портфелей.
		/// </summary>
		public readonly static RoutedCommand OkCommand = new RoutedCommand();

		/// <summary>
		/// Команда добавления портфеля в корзину.
		/// </summary>
		public readonly static RoutedCommand AddCommand = new RoutedCommand();

		/// <summary>
		/// Команда удаления портфеля из корзины.
		/// </summary>
		public readonly static RoutedCommand RemoveCommand = new RoutedCommand();

		/// <summary>
		/// Все доступные портфели.
		/// </summary>
		public ObservableCollection<Portfolio> AllPortfolios { set; private get; }

		/// <summary>
		/// Портфели, входящие в корзину.
		/// </summary>
		public ObservableCollection<Portfolio> InnerPortfolios { set; private get; }

		private IConnector _connector;

		/// <summary>
		/// Интерфейс к торговой системе.
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
			}
		}

		private WeightedPortfolio _portfolio = new WeightedPortfolio();

		/// <summary>
		/// Корзина портфелей.
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
		/// Создать <see cref="BasketPortfolioWindow"/>.
		/// </summary>
		public BasketPortfolioWindow()
		{
			AllPortfolios = new ObservableCollection<Portfolio>();
			InnerPortfolios = new ObservableCollection<Portfolio>();

			InitializeComponent();
		}

		private Portfolio SelectedAllPortfolio
		{
			get { return ComboBoxAllPortfolios != null ? (Portfolio)ComboBoxAllPortfolios.SelectedItem : null; }
		}

		private Portfolio SelectedInnerPortfolio
		{
			get { return ListBoxPortfolios != null ? (Portfolio)ListBoxPortfolios.SelectedItem : null; }
		}

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