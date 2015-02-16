namespace SampleIQFeed
{
	using System;
	using System.Windows;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public partial class HistoryLevel1Window
	{
		private readonly Security _security;

		public HistoryLevel1Window(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			_security = security;

			InitializeComponent();
			Title = _security.Code + LocalizedStrings.Str3749;

			DatePicker.Value = DateTime.Today.AddDays(-7);
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			if (DatePicker.Value == null)
			{
				MessageBox.Show(LocalizedStrings.Str3750, Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			L1Grid.Messages.Clear();

			var date = ((DateTime)DatePicker.Value).Date;
			bool isSuccess;
			L1Grid.Messages.AddRange(MainWindow.Instance.Trader.GetHistoricalLevel1(_security.ToSecurityId(), date, date.AddDays(1), out isSuccess));
		}
	}
}
