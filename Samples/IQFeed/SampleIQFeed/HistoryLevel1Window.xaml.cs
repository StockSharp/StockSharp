#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIQFeed.SampleIQFeedPublic
File: HistoryLevel1Window.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIQFeed
{
	using System;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public partial class HistoryLevel1Window
	{
		private readonly Security _security;

		public HistoryLevel1Window(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			_security = security;

			InitializeComponent();
			Title = _security.Code + LocalizedStrings.Str3749;

			DatePicker.EditValue = DateTime.Today.AddDays(-7);
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			if (DatePicker.EditValue == null)
			{
				MessageBox.Show(LocalizedStrings.Str3750, Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			var date = ((DateTime)DatePicker.EditValue).Date;

			BusyIndicator.IsBusy = true;

			ThreadingHelper.Thread(() =>
			{
				try
				{
					var ticks = MainWindow.Instance.Trader.GetHistoricalLevel1(_security.ToSecurityId(), date, date.AddDays(1), out var _);

					this.GuiAsync(() =>
					{
						BusyIndicator.IsBusy = false;

						L1Grid.Messages.Clear();
						L1Grid.Messages.AddRange(ticks);
					});
				}
				catch (Exception ex)
				{
					this.GuiAsync(() =>
					{
						BusyIndicator.IsBusy = false;

						new MessageBoxBuilder().Text(ex.Message).Owner(this).Show();
					});
				}
			}).Launch();
		}
	}
}