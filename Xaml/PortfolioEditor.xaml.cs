#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: PortfolioEditor.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The control activating <see cref="PortfolioPickerWindow"/>.
	/// </summary>
	public partial class PortfolioEditor
	{
		/// <summary>
		/// The command to delete the selected instrument.
		/// </summary>
		public static readonly RoutedCommand ClearCommand = new RoutedCommand();

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioEditor"/>.
		/// </summary>
		public PortfolioEditor()
		{
			InitializeComponent();

			//Portfolios = new ThreadSafeObservableCollection<Portfolio>(new ObservableCollectionEx<Portfolio>());
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Portfolios"/>.
		/// </summary>
		public static readonly DependencyProperty PortfoliosProperty = DependencyProperty.Register("Portfolios", typeof(ThreadSafeObservableCollection<Portfolio>), typeof(PortfolioEditor), new PropertyMetadata(null, (o, args) =>
		{
			var editor = (PortfolioEditor)o;
			editor.UpdatePortfolios((ThreadSafeObservableCollection<Portfolio>)args.NewValue);
		}));

		private void UpdatePortfolios(ThreadSafeObservableCollection<Portfolio> portfolios)
		{
			_portfolios = portfolios;
			PortfolioComboBox.ItemsSource = _portfolios?.Items;
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
		/// <see cref="DependencyProperty"/> for <see cref="PortfolioEditor.SelectedPortfolio"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedPortfolioProperty = DependencyProperty.Register("SelectedPortfolio", typeof(Portfolio), typeof(PortfolioEditor),
				new FrameworkPropertyMetadata(null, OnSelectedPortfolioPropertyChanged));

		/// <summary>
		/// The selected portfolio.
		/// </summary>
		public Portfolio SelectedPortfolio
		{
			get { return (Portfolio)GetValue(SelectedPortfolioProperty); }
			set { SetValue(SelectedPortfolioProperty, value); }
		}

		/// <summary>
		/// The selected portfolio change event.
		/// </summary>
		public event Action PortfolioSelected;

		private static void OnSelectedPortfolioPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			((PortfolioEditor)source).UpdatedControls();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var wnd = new PortfolioPickerWindow();

			if (Portfolios != null)
				wnd.Portfolios = Portfolios;

			if (wnd.ShowModal(this))
			{
				SelectedPortfolio = wnd.SelectedPortfolio;
			}
		}

		private void UpdatedControls()
		{
			PortfolioSelected.SafeInvoke();
		}

		private void ClearCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedPortfolio = null;
		}
	}
}
