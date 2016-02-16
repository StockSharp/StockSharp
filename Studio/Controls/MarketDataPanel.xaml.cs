#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: MarketDataPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;
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

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
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
		public static RoutedCommand EditCommand = new RoutedCommand();
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand DeleteCommand = new RoutedCommand();

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

		public override string Key { get; set; }

		public MarketDataPanel()
		{
			DataContext = this;
			InitializeComponent();

			Key = GetType().GUID.To<string>();

			SelectedSettings = ConfigManager.GetService<MarketDataSettingsCache>().Settings.FirstOrDefault();
			SecurityPicker.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();

			MarketDataGrid.PropertyChanged += (s, e) => RaiseChangedCommand();
			MarketDataGrid.DataLoading += () => BusyIndicator1.IsBusy = true;
			MarketDataGrid.DataLoaded += () => BusyIndicator1.IsBusy = false;
		}

		private void SettingsChanged(MarketDataSettings settings)
		{
			if (settings != null)
			{
				MarketDataGrid.IsEnabled = true;
				SecurityPicker.SelectedSecurity = null;

				_storageRegistry = new StudioStorageRegistry
				{
					MarketDataSettings = settings
				};
				RefreshGrid();
			}
			else
				MarketDataGrid.IsEnabled = false;
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
			if (SelectedSettings == null)
				return;

			MarketDataGrid.BeginMakeEntries(_storageRegistry, SecurityPicker.SelectedSecurity, FormatCtrl.SelectedFormat, null);
		}

		private void RaiseChangedCommand()
		{
			new ControlChangedCommand(this).Process(this);
		}

		private void EditCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var settings = SelectedSettings;

			var settingsWnd = new StorageSettingsWindow
			{
				Settings = settings
			};

			if (!settingsWnd.ShowModal(this))
				return;

			if (!settings.UseLocal)
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
					_isCancelled = false;
					BusyIndicator.IsBusy = true;

					var progress = BusyIndicator.FindVisualChild<ProgressBar>();
					var cancel = (Button)BusyIndicator.FindVisualChild<CancelButton>();

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

		private void EditCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedSettings != null;
		}

		private void AddCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var settings = new MarketDataSettings
			{
				UseLocal = true,
				//Path = Environment.CurrentDirectory
			};

			var settingsWnd = new StorageSettingsWindow
			{
				Settings = settings
			};

			if (!settingsWnd.ShowModal(this))
				return;

			var cache = ConfigManager.GetService<MarketDataSettingsCache>();

			cache.Settings.Add(settings);
			SelectedSettings = settings;
		}

		private void AddCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void DeleteCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var selectedSettings = SelectedSettings;
			var cache = ConfigManager.GetService<MarketDataSettingsCache>();

			cache.Settings.Remove(selectedSettings);
			SelectedSettings = null;
		}

		private void DeleteCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedSettings != null;
		}

		public override void Load(SettingsStorage storage)
		{
			_isLoading = true;

			try
			{
				((IPersistable)MarketDataGrid).Load(storage.GetValue<SettingsStorage>("MarketDataGrid") ?? storage.GetValue<SettingsStorage>("Grid"));

				var selectedSettings = storage.GetValue("SelectedSettings", Guid.Empty);
				var settings = ConfigManager.GetService<MarketDataSettingsCache>().Settings;

				if (selectedSettings != Guid.Empty)
					SelectedSettings = settings.FirstOrDefault(s => s.Id == selectedSettings)
						?? settings.FirstOrDefault();

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
			storage.SetValue("MarketDataGrid", MarketDataGrid.Save());

			if (SelectedSettings != null)
				storage.SetValue("SelectedSettings", SelectedSettings.Id);

			if (SecurityPicker.SelectedSecurity != null)
				storage.SetValue("Security", SecurityPicker.SelectedSecurity.Id);
			
			storage.SetValue("StorageFormat", FormatCtrl.SelectedFormat.To<string>());
		}

		public override void Dispose()
		{
			_isCancelled = true;
			MarketDataGrid.CancelMakeEntires();
		}
	}

	public class CancelButton : Button
	{
	}
}