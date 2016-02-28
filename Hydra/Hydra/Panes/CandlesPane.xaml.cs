#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: CandlesPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class CandlesPane
	{
		public CandlesPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetCandles);
		}

		protected override Type DataType => CandleSettings.Settings.CandleType.ToCandleMessageType();

		protected override object Arg => CandleSettings.Settings.Arg;

		public override string Title => LocalizedStrings.Candles + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private CandleSeries CandleSeries => new CandleSeries(CandleSettings.Settings.CandleType, SelectedSecurity, Arg);

		private IEnumerable<CandleMessage> GetCandles()
		{
			var from = From.Value;
			var to = To.Value.EndOfDay();

			switch (BuildFrom.SelectedIndex)
			{
				case 0:
					return StorageRegistry
							.GetCandleMessageStorage(CandleSeries.CandleType.ToCandleMessageType(), CandleSeries.Security, CandleSeries.Arg, Drive, StorageFormat)
							.Load(from, to);
				case 1:
					return StorageRegistry
							.GetTickMessageStorage(SelectedSecurity, Drive, StorageFormat)
							.Load(from, to)
							.ToCandles(CandleSeries);
				case 2:
					return StorageRegistry
							.GetOrderLogMessageStorage(SelectedSecurity, Drive, StorageFormat)
							.Load(from, to)
							.ToTicks()
							.ToCandles(CandleSeries);
				case 3:
					return StorageRegistry
							.GetQuoteMessageStorage(SelectedSecurity, Drive, StorageFormat)
							.Load(from, to)
							.ToCandles(CandleSeries);
				case 4:
					return StorageRegistry
							.GetOrderLogMessageStorage(SelectedSecurity, Drive, StorageFormat)
							.Load(from, to)
							.ToMarketDepths(OrderLogBuilders.Plaza2.CreateBuilder(SelectedSecurity.ToSecurityId()))
							.ToCandles(CandleSeries);
				case 5:
					return StorageRegistry
							.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), SelectedSecurity, TimeSpan.FromMinutes(1), Drive, StorageFormat)
							.Load(from, to)
							.ToTrades(SelectedSecurity.VolumeStep ?? 1m)
							.ToCandles(CandleSeries);
				case 6:
					return StorageRegistry
							.GetLevel1MessageStorage(SelectedSecurity, Drive, StorageFormat)
							.Load(from, to)
							.ToTicks()
							.ToCandles(CandleSeries);
			
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2874Params.Put(BuildFrom.SelectedIndex));
			}
		}

		protected override bool CanDirectExport => BuildFrom.SelectedIndex == 0;

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (!CheckSecurity())
				return;

			//if (BuildFrom.SelectedIndex == 5 && DataType == typeof(TimeFrameCandle) && ((TimeSpan)Arg).Seconds != 0)
			//{
			//	new MessageBoxBuilder()
			//		.Caption(Title)
			//		.Text("Таймфрейм должен быть кратен 1 мин.")
			//		.Info()
			//		.Owner(this)
			//		.Show();

			//	return;
			//}

			var candles = new ObservableCollection<CandleMessage>();
			
			BuildedCandles.ItemsSource = candles;
			Progress.Load(GetCandles(), candles.AddRange, 100000);

			//ShowChart.IsEnabled = true;
		}

		private void ShowChartClick(object sender, RoutedEventArgs e)
		{
			var pane = new ChartPane();
			MainWindow.Instance.ShowPane(pane);
			pane.Draw(CandleSeries, GetCandles().ToCandles<Candle>(SelectedSecurity, CandleSettings.Settings.CandleType));
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		//protected override void OnClosed(EventArgs e)
		//{
		//    Progress.Stop();
		//    base.OnClosed(e);
		//}

		private void SelectSecurityBtn_SecuritySelected()
		{
			if (SelectedSecurity == null)
			{
				ExportBtn.IsEnabled = false;
			}
			else
			{
				ExportBtn.IsEnabled = true;
				UpdateTitle();
			}
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			BuildedCandles.Load(storage.GetValue<SettingsStorage>(nameof(BuildedCandles)));
			BuildFrom.SelectedIndex = storage.GetValue<int>(nameof(BuildFrom));

			var settings = new CandleSeries();
			settings.Load(storage.GetValue<SettingsStorage>(nameof(CandleSettings)));
			CandleSettings.Settings = settings;
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(BuildedCandles), BuildedCandles.Save());
			storage.SetValue(nameof(BuildFrom), BuildFrom.SelectedIndex);
			storage.SetValue(nameof(CandleSettings), CandleSettings.Settings.Save());
		}
	}
}