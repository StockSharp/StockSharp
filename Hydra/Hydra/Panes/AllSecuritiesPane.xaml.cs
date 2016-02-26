#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: AllSecuritiesPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Windows;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class AllSecuritiesPane : IPane
	{
		private bool _isDisposed;

		public AllSecuritiesPane()
		{
			InitializeComponent();

			Progress.Init(ExportBtn, MainGrid);

			SecurityPicker.SecurityProvider = ConfigManager.GetService<ISecurityProvider>();
			SecurityPicker.ExcludeAllSecurity();

			ExportBtn.EnableType(ExportTypes.Bin, false);

			MarketData.DataLoading += () => MarketDataBusyIndicator.IsBusy = true;
			MarketData.DataLoaded += () => MarketDataBusyIndicator.IsBusy = false;
		}

		private void DriveCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateMarketDataGrid();
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			UpdateMarketDataGrid();

			EditSecurities.IsEnabled = SecurityPicker.SelectedSecurities.Any();
		}

		private void DrivePanel_OnFormatChanged()
		{
			UpdateMarketDataGrid();
		}

		private void UpdateMarketDataGrid()
		{
			MarketData.BeginMakeEntries(ConfigManager.GetService<IStorageRegistry>(),
				SecurityPicker.SelectedSecurity, DrivePanel.StorageFormat, DrivePanel.SelectedDrive);
		}

		private void LookupPanel_OnLookup(Security filter)
		{
			//SecurityPicker.Securities.Clear();
			SecurityPicker.SecurityFilter = filter.Code;

			var downloaders = MainWindow.Instance.Tasks.Where(t => t.Settings.IsEnabled).OfType<ISecurityDownloader>().ToArray();

			if (downloaders.IsEmpty())
				return;

			BusyIndicator.BusyContent = LocalizedStrings.Str2834;
			BusyIndicator.IsBusy = true;

			ThreadingHelper
				.Thread(() =>
				{
					try
					{
						var securities = ConfigManager.TryGetService<IEntityRegistry>().Securities;

						SecurityPicker.Securities.AddRange(securities
							.Lookup(filter)
							.Where(s => !s.IsAllSecurity()));

						downloaders.ForEach(d =>
							d.Refresh(securities, filter, SecurityPicker.Securities.Add, () => _isDisposed));
					}
					catch (Exception ex)
					{
						ex.LogError();
					}

					try
					{
						this.GuiAsync(() => BusyIndicator.IsBusy = false);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				})
				.Launch();
		}

		private void CreateSecurity_OnClick(object sender, RoutedEventArgs e)
		{
			new SecurityEditWindow { Security = new Security() }.ShowModal(this);

			//var wnd = new SecurityEditWindow { Security = new Security() };

			//if (!wnd.ShowModal(this))
			//	return;

			//SecurityPicker.Securities.Add(wnd.Security);
		}

		private void SecurityPicker_OnSecurityDoubleClick(Security security)
		{
			new SecurityEditWindow { Security = security }.ShowModal(this);
		}

		private void EditSecurities_OnClick(object sender, RoutedEventArgs e)
		{
			var securities = SecurityPicker.SelectedSecurities.ToArray();

			if (securities.IsEmpty())
				return;

			new SecurityEditWindow { Securities = securities }.ShowModal(this);
		}

		string IPane.Title => LocalizedStrings.Str2835;

		Uri IPane.Icon => null;

		bool IPane.IsValid => true;

		void IPersistable.Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("Drive"))
				DrivePanel.SelectedDrive = DriveCache.Instance.GetDrive(storage.GetValue<string>("Drive"));

			DrivePanel.StorageFormat = storage.GetValue<StorageFormats>("StorageFormat");

			MarketData.Load(storage.GetValue<SettingsStorage>(nameof(MarketData)));
			SecurityPicker.Load(storage.GetValue<SettingsStorage>(nameof(SecurityPicker)));
			LookupPanel.Load(storage.GetValue<SettingsStorage>(nameof(LookupPanel)));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			if (DrivePanel.SelectedDrive != null)
				storage.SetValue("Drive", DrivePanel.SelectedDrive.Path);

			storage.SetValue("StorageFormat", DrivePanel.StorageFormat.To<string>());

			storage.SetValue(nameof(MarketData), MarketData.Save());
			storage.SetValue(nameof(SecurityPicker), SecurityPicker.Save());
			storage.SetValue(nameof(LookupPanel), LookupPanel.Save());
		}

		void IDisposable.Dispose()
		{
			Progress.Stop();
			_isDisposed = true;
			MarketData.CancelMakeEntires();
		}

		private void ExportBtn_OnExportStarted()
		{
			var securities = SecurityPicker.FilteredSecurities;

			if (securities.Count == 0)
			{
				Progress.DoesntExist();
				return;
			}

			var path = ExportBtn.GetPath(null, typeof(SecurityMessage), null, null, null, null);

			if (path == null)
				return;

			Progress.Start(null, typeof(SecurityMessage), null, securities.Select(s => s.ToMessage()).ToEx(securities.Count), path);
		}
	}
}