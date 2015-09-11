namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.Configuration;
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
		public readonly static RoutedCommand ClearCommand = new RoutedCommand();

		private readonly ThreadSafeObservableCollection<Portfolio> _portfolios;

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioEditor"/>.
		/// </summary>
		public PortfolioEditor()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<Portfolio>();
			PortfolioTextBox.ItemsSource = itemsSource;
			_portfolios = new ThreadSafeObservableCollection<Portfolio>(itemsSource);

			Connector = ConfigManager.TryGetService<IConnector>();

			if (Connector == null)
			{
				ConfigManager.ServiceRegistered += (t, s) =>
				{
					if (typeof(IConnector) != t)
						return;

					GuiDispatcher.GlobalDispatcher.AddAction(() => Connector = (IConnector)s);
				};
			}
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
					_portfolios.Clear();
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
			var wnd = new PortfolioPickerWindow
			{
				Connector = Connector,
			};

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

		private void OnNewPortfolios(IEnumerable<Portfolio> portfolios)
		{
			_portfolios.AddRange(portfolios);
		}
	}
}
