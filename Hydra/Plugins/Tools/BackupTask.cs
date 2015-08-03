namespace StockSharp.Hydra.Tools
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security;

	using Amazon;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages.Backup;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(LocalizedStrings.BackupKey)]
	class BackupTask : BaseHydraTask
    {
		private enum BackupServices
		{
			AwsS3,
			AwsGlacier,
		}

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class BackupSettings : HydraTaskSettings
		{
			private const string _sourceName = LocalizedStrings.BackupKey;

			public BackupSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			public BackupServices Service
			{
				get { return ExtensionInfo["Service"].To<BackupServices>(); }
				set { ExtensionInfo["Service"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(1)]
			public string Address
			{
				get { return (string)ExtensionInfo["Address"]; }
				set { ExtensionInfo["Address"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3131Key)]
			[DescriptionLoc(LocalizedStrings.Str3131Key, true)]
			[PropertyOrder(2)]
			public string ServiceRepo
			{
				get { return (string)ExtensionInfo["ServiceRepo"]; }
				set { ExtensionInfo["ServiceRepo"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(3)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(4)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str3779Key)]
			[PropertyOrder(5)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
				set { ExtensionInfo["StartFrom"] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str3778Key)]
			[PropertyOrder(6)]
			public int Offset
			{
				get { return ExtensionInfo["Offset"].To<int>(); }
				set { ExtensionInfo["Offset"] = value; }
			}
		}

		public override TaskTypes Type
		{
			get { return TaskTypes.Tool; }
		}

		public override Uri Icon
		{
			get { return "backup_logo.png".GetResourceUrl(GetType()); }
		}

		private BackupSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new BackupSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Offset = 1;
				_settings.StartFrom = new DateTime(1900, 1, 1);
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.Service = BackupServices.AwsS3;
				_settings.Address = RegionEndpoint.USEast1.SystemName;
				_settings.ServiceRepo = "StockSharp";
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
			}
		}

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

			IEnumerable<Tuple<Type, object>> dataTypes = new[]
			{
				Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Tick),
				Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.OrderLog),
				Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Order),
				Tuple.Create(typeof(ExecutionMessage), (object)ExecutionTypes.Trade),
				Tuple.Create(typeof(QuoteChangeMessage), (object)null),
				Tuple.Create(typeof(Level1ChangeMessage), (object)null),
				Tuple.Create(typeof(NewsMessage), (object)null)
			};

			var workingSecurities = GetWorkingSecurities().ToArray();

			foreach (var date in allDates)
			{
				foreach (var security in workingSecurities)
				{
					hasSecurities = true;

					if (!CanProcess())
						break;

					var secEntry = new BackupEntry
					{
						Parent = new BackupEntry
						{
							Name = security.Security.Id.Substring(0, 1),
							Parent = pathEntry
						},
						Name = security.Security.Id,
					};

					var candleTypes = _settings.Drive.GetCandleTypes(security.Security.ToSecurityId(), _settings.StorageFormat);

					var secDataTypes = dataTypes.Concat(candleTypes.SelectMany(t => t.Item2.Select(a => Tuple.Create(t.Item1, a))));

					foreach (var tuple in secDataTypes)
					{
						var storage = StorageRegistry.GetStorage(security.Security, tuple.Item1, tuple.Item2, _settings.Drive, _settings.StorageFormat);

						var drive = storage.Drive;

						var stream = drive.LoadStream(date);

						if (stream == Stream.Null)
							continue;

						service.Upload(new BackupEntry
						{
							Name = _settings.StartFrom.ToString("yyyy_MM_dd"),
							Parent = secEntry
						}, stream, p => { });
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