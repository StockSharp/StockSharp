namespace SampleIQFeed
{
	using System;
	using System.Windows;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class HistoryCandlesWindow
	{
		private readonly Security _security;
		private readonly ChartCandleElement _candlesElem;

		public HistoryCandlesWindow(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			_security = security;

			InitializeComponent();
			Title = _security.Code + LocalizedStrings.Str3747;

			TimeFramePicker.ItemsSource = new[]
			{
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(5),
				TimeSpan.FromMinutes(15),
				TimeSpan.FromMinutes(60),
				TimeSpan.FromDays(1),
				TimeSpan.FromDays(7),
				TimeSpan.FromTicks(TimeHelper.TicksPerMonth)
			};
			TimeFramePicker.SelectedIndex = 1;

			DateFromPicker.Value = DateTime.Today.AddDays(-7);
			DateToPicker.Value = DateTime.Today;

			var area = new ChartArea();
			_candlesElem = new ChartCandleElement();
			area.Elements.Add(_candlesElem);

			Chart.Areas.Add(area);
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			if (DateFromPicker.Value == null || DateToPicker.Value == null)
			{
				MessageBox.Show(LocalizedStrings.Str3748, Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			bool isSuccess;
			var candles = MainWindow.Instance.Trader.GetHistoricalCandles(_security, typeof(TimeFrameCandle), (TimeSpan)TimeFramePicker.SelectedValue, (DateTime)DateFromPicker.Value, (DateTime)DateToPicker.Value, out isSuccess);

			Chart.Reset(new[] { _candlesElem });

			foreach (var candle in candles)
				Chart.Draw(_candlesElem, candle);
		}
	}
}
