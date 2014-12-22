namespace StockSharp.Hydra.Panes
{
	using System;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Controls;

	using Ecng.Serialization;
	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Hydra.Windows;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class GluingDataPane : IPane
	{
		private ContinuousSecurityWindow _continuousSecurityWindow;
		private ContinuousSecurity _continuousSecurity;
		private CancellationTokenSource _token;

		private readonly IEntityRegistry _entityRegistry;

		public GluingDataPane()
		{
			InitializeComponent();

			_entityRegistry = ConfigManager.GetService<IEntityRegistry>();

			SecurityPicker.SecurityProvider = ConfigManager.TryGetService<FilterableSecurityProvider>();

			MarketData.DataLoading += () => MarketDataBusyIndicator.IsBusy = true;
			MarketData.DataLoaded += () => MarketDataBusyIndicator.IsBusy = false;
		}

		private static IStorageRegistry StorageRegistry
		{
			get { return ConfigManager.GetService<IStorageRegistry>(); }
		}

		private void CreateContinuousSecurity_OnClick(object sender, RoutedEventArgs e)
		{
			ContinuousSecurity.Content = string.Empty;
			
			_continuousSecurityWindow = new ContinuousSecurityWindow
			{
				SecurityProvider = _entityRegistry.Securities
			};
			_continuousSecurity = new ContinuousSecurity { Board = ExchangeBoard.Associated };
			_continuousSecurityWindow.ContinuousSecurity = _continuousSecurity;

			if (_continuousSecurityWindow.ShowModal(this))
			{
				ContinuousSecurity.Content = _continuousSecurity.Id;

				var first = _continuousSecurity.InnerSecurities.First();

				var gluingSecurity = new Security
				{
					Id = _continuousSecurity.Id,
					Code = _continuousSecurity.Code,
					Board = ExchangeBoard.Associated,
					Type = _continuousSecurity.Type,
					VolumeStep = first.VolumeStep,
					PriceStep = first.PriceStep,
					ExtensionInfo = new Dictionary<object, object> { { "GluingSecurity", true } }
				};

				if (_entityRegistry.Securities.ReadById(gluingSecurity.Id) == null)
				{
					_entityRegistry.Securities.Save(gluingSecurity);
				}
			}
			else
			{
				ContinuousSecurity.Content = string.Empty;
			}
		}

		private void Gluing_OnClick(object sender, RoutedEventArgs e)
		{
			if (DataTypeComboBox.SelectedItem == null)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str2887)
					.Error()
					.Owner(this)
					.Show();
				
				return;
			}

			if (_continuousSecurity == null)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str2888)
					.Error()
					.Owner(this)
					.Show();

				return;
			}

			if (_token != null)
			{
				_token.Cancel();
				return;
			}

			var candleSettings = CandleSettings.Settings;

			Func<Security, IMarketDataDrive, StorageFormats, IMarketDataStorage> createStorage;

			switch (DataTypeComboBox.SelectedIndex)
			{
				case 0:
					createStorage = StorageRegistry.GetTradeStorage;
					break;

				case 1:
					createStorage = StorageRegistry.GetMarketDepthStorage;
					break;

				case 2:
					createStorage = (s, d, c) => StorageRegistry.GetCandleStorage(candleSettings.CandleType, s, candleSettings.Arg, d, c);
					break;

				case 3:
					createStorage = StorageRegistry.GetOrderLogStorage;
					break;

				case 4:
					createStorage = StorageRegistry.GetLevel1MessageStorage;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			var continuousStorage = createStorage(_continuousSecurity, DrivePanel.SelectedDrive, DrivePanel.StorageFormat);

			var dates = continuousStorage.Dates.ToArray();

			if (dates.IsEmpty())
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str2889)
					.Warning()
					.Owner(this)
					.Show();

				return;
			}

			EnableControls(false);

			ProgressBar.Maximum = dates.Length;
			ProgressBar.Visibility = Visibility.Visible;

			Gluing.Content = LocalizedStrings.Str2890;
			_token = new CancellationTokenSource();

			// так как ContinuousSecurity будет записывать данные в папки составных инструментов, то создаем клон ввиде обычного элемента
			var destinationStorage = createStorage(new Security { Id = _continuousSecurity.Id }, DrivePanel.SelectedDrive, DrivePanel.StorageFormat);

			Task.Factory
				.StartNew(() =>
				{
					foreach (var date in dates)
					{
						if (_token.IsCancellationRequested)
							break;

						using (var sourceStream = ((IContinuousSecurityMarketDataStorage)continuousStorage).GetStorage(date).Drive.LoadStream(date))
						{
							if (sourceStream != Stream.Null)
								destinationStorage.Drive.SaveStream(date, sourceStream);	
						}

						this.GuiAsync(() => ProgressBar.Value++);
					}
				})
				.ContinueWithExceptionHandling(this.GetWindow(), res =>
				{
					ProgressBar.Value = 0;
					//ProgressBar.Visibility = Visibility.Collapsed;

					Gluing.Content = LocalizedStrings.Str2891;
					_token = null;

					EnableControls(true);
				});
		}

		private void EnableControls(bool isEnabled)
		{
			CreateContinuousSecurity.IsEnabled = DataTypeComboBox.IsEnabled = CandleSettings.IsEnabled = SecurityPicker.IsEnabled = MarketData.IsEnabled = isEnabled;
		}

		private int GetDataType()
		{
			return DataTypeComboBox.SelectedIndex;
		}

		private void DataTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CandleSettings.Visibility = GetDataType() == 2 ? Visibility.Visible : Visibility.Collapsed;
		}

		private void DriveCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateMarketDataGrid();
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			UpdateMarketDataGrid();
		}

		private void UpdateMarketDataGrid()
		{
			MarketData.BeginMakeEntries(StorageRegistry, SecurityPicker.SelectedSecurity,
				DrivePanel.StorageFormat, DrivePanel.SelectedDrive);
		}

		private void SecurityPicker_OnSecurityDoubleClick(Security security)
		{
			new SecurityEditWindow { Security = security }.ShowModal(this);
		}

		string IPane.Title
		{
			get { return LocalizedStrings.Str2892; }
		}

		Uri IPane.Icon
		{
			get { return null; }
		}

		bool IPane.IsValid
		{
			get { return true; }
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("Drive"))
				DrivePanel.SelectedDrive = DriveCache.Instance.GetDrive(storage.GetValue<string>("Drive"));

			DrivePanel.StorageFormat = storage.GetValue<StorageFormats>("StorageFormat");

			MarketData.Load(storage.GetValue<SettingsStorage>("MarketData"));
			SecurityPicker.Load(storage.GetValue<SettingsStorage>("SecurityPicker"));
			DataTypeComboBox.SelectedIndex = storage.GetValue<int>("DataTypeComboBox");
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			if (DrivePanel.SelectedDrive != null)
				storage.SetValue("Drive", DrivePanel.SelectedDrive.Path);

			storage.SetValue("StorageFormat", DrivePanel.StorageFormat.To<string>());

			storage.SetValue("MarketData", MarketData.Save());
			storage.SetValue("SecurityPicker", SecurityPicker.Save());
			storage.SetValue("DataTypeComboBox", DataTypeComboBox.SelectedIndex);
		}

		void IDisposable.Dispose()
		{
			var t = _token;

			if (t != null)
				t.Cancel();

			MarketData.CancelMakeEntires();
		}
	}
}