#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: BasketPortfolioWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		public static readonly RoutedCommand OkCommand = new RoutedCommand();

		/// <summary>
		/// The command for adding portfolio to a basket.
		/// </summary>
		public static readonly RoutedCommand AddCommand = new RoutedCommand();

		/// <summary>
		/// The command for removal of portfolio from the basket.
		/// </summary>
		public static readonly RoutedCommand RemoveCommand = new RoutedCommand();

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