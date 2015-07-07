namespace SampleBarChart
{
	using System;
	using System.Windows;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public partial class HistoryTicksWindow
	{
		private readonly Security _security;

		public HistoryTicksWindow(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			_security = security;

			InitializeComponent();
			Title = _security.Code + " ticks history";

			DateFromPicker.Value = DateTime.Today.AddDays(-7);
			DateToPicker.Value = DateTime.Today;
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			if (DateFromPicker.Value == null || DateToPicker.Value == null)
			{
				MessageBox.Show(LocalizedStrings.Str3748, Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			bool isSuccess;
			var ticks = MainWindow.Instance.Trader.GetHistoricalTicks(_security, (DateTime)DateFromPicker.Value, (DateTime)DateToPicker.Value, out isSuccess);

			Ticks.Trades.Clear();
			Ticks.Trades.AddRange(ticks);
		}
	}
}
