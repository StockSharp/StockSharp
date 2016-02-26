#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Tools.ToolsPublic
File: BackupTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Tools
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security;

	using Amazon;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Storages.Backup;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Logging;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(LocalizedStrings.BackupKey)]
	[DescriptionLoc(LocalizedStrings.BackupDescriptionKey)]
	[Doc("http://stocksharp.com/doc/html/5a056352-64c7-41ea-87a8-2e112935e3b9.htm")]
	[Icon("backup_logo.png")]
	[TaskCategory(TaskCategories.Tool)]
	class BackupTask : BaseHydraTask
    {
		private enum BackupServices
		{
			AwsS3,
			AwsGlacier,
		}

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrderLoc(_sourceName, 0)]
		[CategoryOrderLoc(LocalizedStrings.GeneralKey, 1)]
		private sealed class BackupSettings : HydraTaskSettings
		{
			private const string _sourceName = LocalizedStrings.BackupKey;

			public BackupSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3427Key)]
			[DescriptionLoc(LocalizedStrings.Str3427Key, true)]
			[PropertyOrder(0)]
			public BackupServices Service
			{
				get { return ExtensionInfo[nameof(Service)].To<BackupServices>(); }
				set { ExtensionInfo[nameof(Service)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(1)]
			public string Address
			{
				get { return (string)ExtensionInfo[nameof(Address)]; }
				set { ExtensionInfo[nameof(Address)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1405Key)]
			[DescriptionLoc(LocalizedStrings.Str1405Key, true)]
			[PropertyOrder(2)]
			public string ServiceRepo
			{
				get { return (string)ExtensionInfo[nameof(ServiceRepo)]; }
				set { ExtensionInfo[nameof(ServiceRepo)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(3)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(4)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str3779Key)]
			[PropertyOrder(5)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo[nameof(StartFrom)].To<DateTime>(); }
				set { ExtensionInfo[nameof(StartFrom)] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str3778Key)]
			[PropertyOrder(6)]
			public int Offset
			{
				get { return ExtensionInfo[nameof(Offset)].To<int>(); }
				set { ExtensionInfo[nameof(Offset)] = value; }
			}
		}

		private BackupSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new BackupSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Offset = 1;
			_settings.StartFrom = new DateTime(1900, 1, 1);
			_settings.Interval = TimeSpan.FromDays(1);
			_settings.Service = BackupServices.AwsS3;
			_settings.Address = RegionEndpoint.USEast1.SystemName;
			_settings.ServiceRepo = "stocksharp";
			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
		}

		public override IEnumerable<DataType> SupportedDataTypes => Enumerable.Empty<DataType>();

		protected override TimeSpan OnProcess()
		{
			IBackupService service;

			switch (_settings.Service)
			{
				case BackupServices.AwsS3:
					service = new AmazonS3Service(AmazonExtensions.GetEndpoint(_settings.Address), _settings.ServiceRepo, _settings.Login, _settings.Password.To<string>());
					break;
				case BackupServices.AwsGlacier:
					service = new AmazonGlacierService(AmazonExtensions.GetEndpoint(_settings.Address), _settings.ServiceRepo, _settings.Login, _settings.Password.To<string>());
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var hasSecurities = false;

			this.AddInfoLog(LocalizedStrings.Str2306Params.Put(_settings.StartFrom));

			var startDate = _settings.StartFrom;
			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.Offset);

			var allDates = startDate.Range(endDate, TimeSpan.FromDays(1)).ToArray();

			var pathEntry = ToEntry(new DirectoryInfo(_settings.Drive.Path));

			var workingSecurities = GetWorkingSecurities().ToArray();

			foreach (var date in allDates)
			{
				foreach (var security in workingSecurities)
				{
					hasSecurities = true;

					if (!CanProcess())
						break;

					var dateEntry = new BackupEntry
					{
						Name = date.ToString("yyyy_MM_dd"),
						Parent = new BackupEntry
						{
							Parent = new BackupEntry
							{
								Name = security.Security.Id.Substring(0, 1),
								Parent = pathEntry
							},
							Name = security.Security.Id,
						}
					};

					var dataTypes = _settings.Drive.GetAvailableDataTypes(security.Security.ToSecurityId(), _settings.StorageFormat);

					foreach (var dataType in dataTypes)
					{
						var storage = StorageRegistry.GetStorage(security.Security, dataType.MessageType, dataType.Arg, _settings.Drive, _settings.StorageFormat);

						var drive = storage.Drive;

						var stream = drive.LoadStream(date);

						if (stream == Stream.Null)
							continue;

						var entry = new BackupEntry
						{
							Name = LocalMarketDataDrive.GetFileName(dataType.MessageType, dataType.Arg) + LocalMarketDataDrive.GetExtension(StorageFormats.Binary),
							Parent = dateEntry,
						};

						service.Upload(entry, stream, p => { });

						this.AddInfoLog(LocalizedStrings.Str1580Params, GetPath(entry));
					}
				}

				if (CanProcess())
				{
					_settings.StartFrom += TimeSpan.FromDays(1);
					SaveSettings();
				}
			}

			if (!hasSecurities)
			{
				this.AddWarningLog(LocalizedStrings.Str2292);
				return TimeSpan.MaxValue;
			}

			if (CanProcess())
				this.AddInfoLog(LocalizedStrings.Str2300);

			return base.OnProcess();
		}

		private static string GetPath(BackupEntry entry)
		{
			if (entry == null)
				return null;

			return GetPath(entry.Parent) + "/" + entry.Name;
		}

		private static BackupEntry ToEntry(DirectoryInfo di)
		{
			// is a disk
			if (di.Parent == null)
				return null;

			return new BackupEntry
			{
				Name = di.Name,
				Parent = di.Parent != null ? ToEntry(di.Parent) : null
			};
		}
    }
}