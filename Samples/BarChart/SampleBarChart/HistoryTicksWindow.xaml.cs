#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleBarChart.SampleBarChartPublic
File: HistoryTicksWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(security));

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

			var ticks = MainWindow.Instance.Trader.GetHistoricalTicks(_security, (DateTime)DateFromPicker.Value, (DateTime)DateToPicker.Value, out bool isSuccess);

			Ticks.Messages.Clear();
			Ticks.Messages.AddRange(ticks);
		}
	}
}