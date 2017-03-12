namespace SampleIB
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Xaml;

	using StockSharp.InteractiveBrokers;

	public partial class ScannerWindow
	{
		private readonly List<long> _scannerIds = new List<long>();
		private static InteractiveBrokersTrader Trader => MainWindow.Instance.Trader;
		private readonly ThreadSafeObservableCollection<ScannerResult> _results;

		public ScannerWindow()
		{
			InitializeComponent();

			Trader.NewScannerParameters += TraderOnNewScannerParameters;
			Trader.NewScannerResults += TraderOnNewScannerResults;

			var itemsSource = new ObservableCollectionEx<ScannerResult>();
			Results.ItemsSource = itemsSource;
			_results = new ThreadSafeObservableCollection<ScannerResult>(itemsSource);

			ScanCode.SetDataSource<ScannerFilterTypes>();
			ScanCode.SetSelectedValue<ScannerFilterTypes>(ScannerFilterTypes.TopPercGain);
		}

		private void TraderOnNewScannerParameters(string parameters)
		{
			this.GuiAsync(() =>
			{
				ScannerParameters.Text = parameters;
				Parameters.IsEnabled = true;
			});
		}

		private void Subscribe_OnClick(object sender, RoutedEventArgs e)
		{
			_scannerIds.Add(Trader.SubscribeScanner(new ScannerFilter
			{
				ScanCode = ScanCode.GetSelectedValue<ScannerFilterTypes>() ?? ScannerFilterTypes.TopPercGain,
				BoardCode = BoardCode.Text,
				SecurityType = SecurityType.Text,
				RowCount = 15,
			}));
		}

		private void Parameters_OnClick(object sender, RoutedEventArgs e)
		{
			Parameters.IsEnabled = false;
			Trader.RequestScannerParameters();
		}

		private void TraderOnNewScannerResults(ScannerFilter filter, IEnumerable<ScannerResult> results)
		{
			_results.AddRange(results);
		}

		protected override void OnClosed(EventArgs e)
		{
			Trader.NewScannerParameters -= TraderOnNewScannerParameters;
			Trader.NewScannerResults -= TraderOnNewScannerResults;

			_scannerIds.ForEach(Trader.UnSubscribeScanner);

			base.OnClosed(e);
		}
	}
}