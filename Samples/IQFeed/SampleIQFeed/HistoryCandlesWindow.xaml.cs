#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIQFeed.SampleIQFeedPublic
File: HistoryCandlesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIQFeed
{
	using System;
	using System.Windows;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.IQFeed;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class HistoryCandlesWindow
	{
		private readonly Security _security;
		private readonly ChartCandleElement _candlesElem;

		public HistoryCandlesWindow(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			_security = security;

			InitializeComponent();
			Title = _security.Code + LocalizedStrings.Str3747;

			TimeFramePicker.ItemsSource = IQFeedMarketDataMessageAdapter.AllTimeFrames;
			TimeFramePicker.SelectedIndex = 1;

			From.EditValue = DateTime.Today.AddDays(-7);
			To.EditValue = DateTime.Today;

			var area = new ChartArea();
			_candlesElem = new ChartCandleElement();
			area.Elements.Add(_candlesElem);

			Chart.Areas.Add(area);
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			if (From.EditValue == null || To.EditValue == null)
			{
				MessageBox.Show(LocalizedStrings.Str3748, Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var messages = MainWindow.Instance.Trader.GetHistoricalCandles(_security, typeof(TimeFrameCandle), (TimeSpan)TimeFramePicker.SelectedValue, (DateTime)From.EditValue, (DateTime)To.EditValue, out var _);

			Chart.Reset(new[] { _candlesElem });

			foreach (var message in messages)
				Chart.Draw(_candlesElem, message.ToCandle(_security));
		}
	}
}