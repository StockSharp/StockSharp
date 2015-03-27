namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.ObjectModel;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class CandlesPane
	{
		public CandlesPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetCandles);
		}

		protected override Type DataType
		{
			get { return CandleSettings.Settings.CandleType.ToCandleMessageType(); }
		}

		protected override object Arg
		{
			get { return CandleSettings.Settings.Arg; }
		}

		public override string Title
		{
			get { return LocalizedStrings.Candles + " " + SelectedSecurity; }
		}

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set { SelectSecurityBtn.SelectedSecurity = value; }
		}

		private CandleSeries CandleSeries
		{
			get { return new CandleSeries(CandleSettings.Settings.CandleType, SelectedSecurity, Arg); }
		}

		private IEnumerableEx<CandleMessage> GetCandles()
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
							.GetExecutionStorage(SelectedSecurity, ExecutionTypes.Tick, Drive, StorageFormat)
							.Load(from, to)
							.ToCandles(CandleSeries);
				case 2:
					return StorageRegistry
							.GetExecutionStorage(SelectedSecurity, ExecutionTypes.OrderLog, Drive, StorageFormat)
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
							.GetExecutionStorage(SelectedSecurity, ExecutionTypes.OrderLog, Drive, StorageFormat)
							.Load(from, to)
							.ToMarketDepths()
							.ToCandles(CandleSeries);
				case 5:
					return StorageRegistry
							.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), SelectedSecurity, TimeSpan.FromMinutes(1), Drive, StorageFormat)
							.Load(from, to)
							.ToTrades(SelectedSecurity.VolumeStep ?? 1m)
							.ToCandles(CandleSeries);
			
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2874Params.Put(BuildFrom.SelectedIndex));
			}
		}

		protected override bool CanDirectBinExport
		{
			get { return base.CanDirectBinExport && BuildFrom.SelectedIndex == 0; }
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (SelectedSecurity == null)
			{
				new MessageBoxBuilder()
					.Caption(Title)
					.Text(LocalizedStrings.Str2875)
					.Info()
					.Owner(this)
					.Show();

				return;
			}

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

			BuildedCandles.Load(storage.GetValue<SettingsStorage>("BuildedCandles"));
			BuildFrom.SelectedIndex = storage.GetValue<int>("BuildFrom");

			var settings = new CandleSeries();
			settings.Load(storage.GetValue<SettingsStorage>("CandleSettings"));
			CandleSettings.Settings = settings;
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("BuildedCandles", BuildedCandles.Save());
			storage.SetValue("BuildFrom", BuildFrom.SelectedIndex);
			storage.SetValue("CandleSettings", CandleSettings.Settings.Save());
		}
	}
}