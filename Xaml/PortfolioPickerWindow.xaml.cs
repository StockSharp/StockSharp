#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: PortfolioPickerWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
					throw new ArgumentNullException(nameof(value));

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