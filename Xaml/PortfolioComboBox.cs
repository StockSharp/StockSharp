#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: PortfolioComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System.Windows;
	using System.Windows.Controls;

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
			DisplayMemberPath = nameof(Portfolio.Name);
			//Portfolios = new ThreadSafeObservableCollection<Portfolio>(new ObservableCollectionEx<Portfolio>());
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Portfolios"/>.
		/// </summary>
		public static readonly DependencyProperty PortfoliosProperty = DependencyProperty.Register(nameof(Portfolios), typeof(ThreadSafeObservableCollection<Portfolio>), typeof(PortfolioComboBox), new PropertyMetadata(null, (o, args) =>
		{
			var cb = (PortfolioComboBox)o;
			cb.UpdatePortfolios((ThreadSafeObservableCollection<Portfolio>)args.NewValue);
		}));

		private void UpdatePortfolios(ThreadSafeObservableCollection<Portfolio> portfolios)
		{
			_portfolios = portfolios;
			ItemsSource = _portfolios?.Items;
		}

		private ThreadSafeObservableCollection<Portfolio> _portfolios;

		/// <summary>
		/// Available portfolios.
		/// </summary>
		public ThreadSafeObservableCollection<Portfolio> Portfolios
		{
			get { return _portfolios; }
			set { SetValue(PortfoliosProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="PortfolioComboBox.SelectedPortfolio"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedPortfolioProperty =
			 DependencyProperty.Register(nameof(SelectedPortfolio), typeof(Portfolio), typeof(PortfolioComboBox),
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

			if (portfolio != null && cb.Portfolios != null && !cb.Portfolios.Contains(portfolio))
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