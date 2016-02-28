#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: DataPane.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Controls;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit;

	public abstract class DataPane : UserControl, IPane, INotifyPropertyChanged
	{
		public abstract Security SelectedSecurity { get; set; }

		protected abstract Type DataType { get; }

		protected virtual object Arg => null;

		protected IStorageRegistry StorageRegistry => ConfigManager.GetService<IStorageRegistry>();

		public abstract string Title { get; }

		protected void UpdateTitle()
		{
			_propertyChanged.SafeInvoke(this, nameof(Title));
		}

		Uri IPane.Icon => null;

		public virtual bool InProcess => Progress.IsStarted;

		public virtual bool IsValid => true;

		protected DateTime? From
		{
			get
			{
				var value = _from.Value;
				return value?.ChangeKind(DateTimeKind.Utc);
			}
			set { _from.Value = value; }
		}

		protected DateTime? To
		{
			get
			{
				var value = _to.Value;
				return value?.ChangeKind(DateTimeKind.Utc);
			}
			set { _to.Value = value; }
		}

		protected IMarketDataDrive Drive
		{
			get { return _drivePanel.SelectedDrive; }
			set { _drivePanel.SelectedDrive = value; }
		}

		protected StorageFormats StorageFormat
		{
			get { return _drivePanel.StorageFormat; }
			set { _drivePanel.StorageFormat = value; }
		}

		private ExportProgress Progress => ((dynamic)this).Progress;

		private ExportButton _exportBtn;
		private Func<IEnumerable> _getItems;
		private DateTimePicker _from;
		private DateTimePicker _to;
		private DrivePanel _drivePanel;

		protected void Init(ExportButton exportBtn, Grid mainGrid, Func<IEnumerable> getItems)
		{
			if (exportBtn == null)
				throw new ArgumentNullException(nameof(exportBtn));

			if (mainGrid == null)
				throw new ArgumentNullException(nameof(mainGrid));

			if (getItems == null)
				throw new ArgumentNullException(nameof(getItems));

			_exportBtn = exportBtn;
			_exportBtn.ExportStarted += ExportBtnOnExportStarted;

			dynamic ctrl = this;

			_from = ctrl.FromCtrl;
			_to = ctrl.ToCtrl;
			_drivePanel = ctrl.DrivePanel;

			Progress.Init(_exportBtn, mainGrid);

			From = DateTime.Today - TimeSpan.FromDays(7);
			To = DateTime.Today + TimeSpan.FromDays(1);

			_getItems = getItems;
		}

		protected virtual bool CheckSecurity()
		{
			if (SelectedSecurity != null)
				return true;

			new MessageBoxBuilder()
				.Text(LocalizedStrings.Str2875)
				.Info()
				.Owner(this)
				.Show();

			return false;
		}

		protected virtual bool CanDirectExport => true;

		protected virtual void ExportBtnOnExportStarted()
		{
			if (!CheckSecurity())
				return;

			if (!_getItems().Cast<object>().Any())
			{
				Progress.DoesntExist();
				return;
			}

			var path = _exportBtn.GetPath(SelectedSecurity, DataType, Arg, From, To, Drive);

			if (path == null)
				return;

			if	(	CanDirectExport &&
					(
						(_exportBtn.ExportType == ExportTypes.StockSharpBin && StorageFormat == StorageFormats.Binary) ||
						(_exportBtn.ExportType == ExportTypes.StockSharpCsv && StorageFormat == StorageFormats.Csv)
					) &&
					path is LocalMarketDataDrive && Drive is LocalMarketDataDrive
				)
			{
				var destDrive = (IMarketDataDrive)path;

				if (destDrive.Path.ComparePaths(Drive.Path))
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2876)
						.Error()
						.Owner(this)
							.Show();

					return;
				}

				var storage = ConfigManager.GetService<IStorageRegistry>().GetStorage(SelectedSecurity, DataType, Arg, Drive, StorageFormat);

				var dates = storage.Dates.ToArray();

				if (dates.IsEmpty())
					return;

				var allDates = (From ?? dates.First()).Range(To ?? dates.Last(), TimeSpan.FromDays(1));

				var datesToExport = storage.Dates
							.Intersect(allDates)
							.ToArray();

				var fileName = LocalMarketDataDrive.GetFileName(DataType, Arg, _exportBtn.ExportType == ExportTypes.StockSharpBin ? StorageFormats.Binary : StorageFormats.Csv);// + LocalMarketDataDrive.GetExtension(StorageFormats.Binary);

				Progress.Start(SelectedSecurity.ToSecurityId(), datesToExport, (LocalMarketDataDrive)Drive, (LocalMarketDataDrive)destDrive, fileName);
			}
			else
				Progress.Start(SelectedSecurity, DataType, Arg, _getItems(), int.MaxValue /* TODO */, path);
		}

		public virtual void Load(SettingsStorage storage)
		{
			if (storage.ContainsKey(nameof(SelectedSecurity)))
				SelectedSecurity = ConfigManager.GetService<IEntityRegistry>().Securities.ReadById(storage.GetValue<string>(nameof(SelectedSecurity)));

			From = storage.GetValue<DateTime?>(nameof(From));
			To = storage.GetValue<DateTime?>(nameof(To));

			if (storage.ContainsKey(nameof(Drive)))
				Drive = DriveCache.Instance.GetDrive(storage.GetValue<string>(nameof(Drive)));

			StorageFormat = storage.GetValue<StorageFormats>(nameof(StorageFormat));
		}

		public virtual void Save(SettingsStorage storage)
		{
			if (SelectedSecurity != null)
				storage.SetValue(nameof(SelectedSecurity), SelectedSecurity.Id);

			if (From != null)
				storage.SetValue(nameof(From), (DateTime)From);

			if (To != null)
				storage.SetValue(nameof(To), (DateTime)To);

			if (Drive != null)
				storage.SetValue(nameof(Drive), Drive.Path);

			storage.SetValue(nameof(StorageFormat), StorageFormat);
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		void IDisposable.Dispose()
		{
			Progress.Stop();
		}
	}
}