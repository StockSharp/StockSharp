namespace StockSharp.Studio.Controls
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Runtime.InteropServices;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Community;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str1405Key)]
	[DescriptionLoc(LocalizedStrings.StorageSettingsKey)]
	[Icon("Images/storage_32x32.png")]
	[Guid("8B702CF0-2D2E-4A6E-8C2E-63CEF1B75D84")]
	public partial class MarketDataPanel
	{
		public static RoutedCommand ApplyCommand = new RoutedCommand();

		public static readonly DependencyProperty SelectedSettingsProperty = DependencyProperty.Register("SelectedSettings", typeof(MarketDataSettings), typeof(MarketDataPanel),
			new PropertyMetadata(null, SelectedSettingsPropertyChangedCallback));

		private static void SelectedSettingsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (MarketDataPanel)sender;
			var settings = (MarketDataSettings)args.NewValue;

			ctrl.SettingsChanged(settings);
		}

		public MarketDataSettings SelectedSettings
		{
			get { return (MarketDataSettings)GetValue(SelectedSettingsProperty); }
			set { SetValue(SelectedSettingsProperty, value); }
		}

		private bool _isCancelled;
		private bool _isLoading;
		private StudioStorageRegistry _storageRegistry;

		public MarketDataPanel()
		{
			DataContext = this;
			InitializeComponent();

			SelectedSettings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.FirstOrDefault(s => s.Id != Guid.Empty);
			SecurityPicker.SecurityProvider = ConfigManager.GetService<FilterableSecurityProvider>();
			
			Grid.PropertyChanged += (s, e) => RaiseChangedCommand();
			Grid.DataLoading += () => BusyIndicator1.IsBusy = true;
			Grid.DataLoaded += () => BusyIndicator1.IsBusy = false;
		}

		private void SettingsChanged(MarketDataSettings settings)
		{
			if (settings == null)
			{
				SettingsPanel.IsEnabled = false;
				Grid.IsEnabled = false;
				return;
			}

			SettingsPanel.IsEnabled = true;
			Grid.IsEnabled = true;

			SettingsPanel.IsLocal = settings.UseLocal;
			//SettingsPanel.IsAlphabetic = settings.IsAlphabetic;

			if (settings.UseLocal)
				SettingsPanel.Path = settings.Path;
			else
				SettingsPanel.Address = settings.Path;

			SetCredentials(settings.IsStockSharpStorage, settings.Credentials);

			_storageRegistry = new StudioStorageRegistry { MarketDataSettings = settings };
			RefreshGrid();
		}

		private void SetCredentials(bool isStockSharpStorage, ServerCredentials credentials = null)
		{
			SettingsPanel.IsCredentialsEnabled = !isStockSharpStorage;

			var serverCredentials = !isStockSharpStorage
				? (credentials ?? new ServerCredentials())
				: ConfigManager
					.GetService<IPersistableService>()
					.GetCredentials();

			SettingsPanel.Login = serverCredentials.Login;
			SettingsPanel.Password = serverCredentials.Password;
		}

		private void SettingsPanel_OnRemotePathChanged()
		{
			var isStockSharpStorage = !SettingsPanel.IsLocal && SettingsPanel.Address.ContainsIgnoreCase("stocksharp.com");

			if (SettingsPanel.IsCredentialsEnabled == !isStockSharpStorage)
				return;

			SetCredentials(isStockSharpStorage);
		}

		private void SecurityPicker_OnSecuritySelected(Security obj)
		{
			if (_isLoading)
				return;

			RaiseChangedCommand();
			RefreshGrid();
		}

		private void FormatCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isLoading)
				return;

			RaiseChangedCommand();
			RefreshGrid();
		}

		private void RefreshGrid()
		{
			Grid.BeginMakeEntries(_storageRegistry, SecurityPicker.SelectedSecurity, FormatCtrl.SelectedFormat, null);
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void ApplyCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var settings = SelectedSettings;

			if (SettingsPanel.IsLocal == settings.UseLocal &&
				//SettingsPanel.IsAlphabetic == settings.IsAlphabetic &&
				(SettingsPanel.IsLocal ? SettingsPanel.Path : SettingsPanel.Address) == settings.Path &&
			    SettingsPanel.Login == settings.Credentials.Login &&
			    SettingsPanel.Password == settings.Credentials.Password)
			{
				return;
			}

			settings.UseLocal = SettingsPanel.IsLocal;
			//settings.IsAlphabetic = SettingsPanel.IsAlphabetic;
			settings.Path = SettingsPanel.IsLocal ? SettingsPanel.Path : SettingsPanel.Address;
			settings.Credentials.Login = SettingsPanel.Login;
			settings.Credentials.Password = SettingsPanel.Password;

			if (SettingsPanel.IsLocal)
			{
				if (!Directory.Exists(settings.Path))
				{
					var res = new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3263)
						.Warning()
						.YesNo()
						.Show();

					if (res != MessageBoxResult.Yes)
						return;
				}
			}
			else
			{
				var wnd = new MarketDataConfirmWindow
				{
					SecurityTypes = new[]
					{
						SecurityTypes.Currency,
						SecurityTypes.Index,
						SecurityTypes.Stock,
						SecurityTypes.Future
					}
				};

				if (wnd.ShowModal(this))
				{
					BusyIndicator.IsBusy = true;

					var progress = BusyIndicator.FindVisualChild<ProgressBar>();
					var cancel = (Button)BusyIndicator.FindVisualChild<StackPanel>().Children[2];

					var secTypes = wnd.SecurityTypes.ToArray();

					progress.Maximum = secTypes.Length;
					cancel.Click += (s1, e1) =>
					{
						_isCancelled = true;
						BusyIndicator.IsBusy = false;
					};

					new RefreshSecurities(settings, secTypes,
						() => _isCancelled,
						count => this.GuiAsync(() => progress.Value = count),
						count => this.GuiAsync(() =>
						{
							BusyIndicator.IsBusy = false;

							new MessageBoxBuilder()
								.Owner(this)
								.Text(LocalizedStrings.Str3264Params.Put(count))
								.Show();

							RefreshGrid();
						})).Process(this);
				}
			}

			ConfigManager.GetService<MarketDataSettingsCache>().Save();
		}

		private void ApplyCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !(SettingsPanel.IsLocal ? SettingsPanel.Path : SettingsPanel.Address).IsEmpty();
		}

		public override void Load(SettingsStorage storage)
		{
			_isLoading = true;

			try
			{
				((IPersistable)Grid).Load(storage.GetValue<SettingsStorage>("Grid"));

				if (storage.ContainsKey("Security"))
					SecurityPicker.SelectedSecurity = ConfigManager.GetService<IEntityRegistry>().Securities.ReadById(storage.GetValue<string>("Security"));

				FormatCtrl.SelectedFormat = storage.GetValue<StorageFormats>("SelectedFormat");
			}
			finally
			{
				_isLoading = false;
			}

			RefreshGrid();
		}

		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("Grid", Grid.Save());

			if (SecurityPicker.SelectedSecurity != null)
				storage.SetValue("Security", SecurityPicker.SelectedSecurity.Id);

			storage.SetValue("StorageFormat", FormatCtrl.SelectedFormat.To<string>());
		}

		public override void Dispose()
		{
			_isCancelled = true;
			Grid.CancelMakeEntires();
		}
	}
}