#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.HydraServer.HydraServerPublic
File: HydraServerTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.HydraServer
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.History;
	using StockSharp.Algo.History.Hydra;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/c84d96f5-d466-4dbd-b7d4-9f87cba8ea7f.htm")]
	[Icon("hydra_server_logo.png")]
	[TaskCategory(TaskCategories.History | TaskCategories.Ticks | TaskCategories.Stock |
		TaskCategories.Forex | TaskCategories.Free | TaskCategories.MarketDepth | TaskCategories.OrderLog |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class HydraServerTask : BaseHydraTask, ISecurityDownloader
	{
		private const string _sourceName = "S#.Data Server";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class HydraServerSettings : HydraTaskSettings
		{
			public HydraServerSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd(nameof(IgnoreWeekends), true);
				CollectionHelper.TryAdd(ExtensionInfo, nameof(DayOffset), 1);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			public Uri Address
			{
				get { return ExtensionInfo[nameof(Address)].To<Uri>(); }
				set { ExtensionInfo[nameof(Address)] = value.ToString(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.Str2302Key)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.Str2303Key)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2282Key)]
			[DescriptionLoc(LocalizedStrings.Str2304Key)]
			[PropertyOrder(3)]
			public DateTime StartFrom
			{
				get { return ExtensionInfo[nameof(StartFrom)].To<DateTime>(); }
				set { ExtensionInfo[nameof(StartFrom)] = value.Ticks; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2284Key)]
			[DescriptionLoc(LocalizedStrings.Str2285Key)]
			[PropertyOrder(4)]
			public int DayOffset
			{
				get { return ExtensionInfo[nameof(DayOffset)].To<int>(); }
				set { ExtensionInfo[nameof(DayOffset)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str2286Key)]
			[DescriptionLoc(LocalizedStrings.Str2287Key)]
			[PropertyOrder(5)]
			public bool IgnoreWeekends
			{
				get { return (bool)ExtensionInfo[nameof(IgnoreWeekends)]; }
				set { ExtensionInfo[nameof(IgnoreWeekends)] = value; }
			}

			[Browsable(false)]
			public override IEnumerable<Level1Fields> SupportedLevel1Fields
			{
				get { return base.SupportedLevel1Fields; }
				set { base.SupportedLevel1Fields = value; }
			}
		}

		private HydraServerSettings _settings;

		public HydraServerTask()
		{
			SupportedDataTypes = new[]
			{
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(5),
				TimeSpan.FromMinutes(15),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1)
			}
			.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
			.Concat(new[]
			{
				DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick),
				DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Transaction),
				DataType.Create(typeof(ExecutionMessage), ExecutionTypes.OrderLog),
				DataType.Create(typeof(QuoteChangeMessage), null),
				DataType.Create(typeof(Level1ChangeMessage), null),
				DataType.Create(typeof(NewsMessage), null),
			})
			.ToArray();
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new HydraServerSettings(settings);

			if (settings.IsDefault)
			{
				_settings.DayOffset = 1;
				_settings.StartFrom = new DateTime(2000,1,1);
				_settings.Address = "net.tcp://localhost:8000".To<Uri>();
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.Interval = TimeSpan.FromDays(1);
				_settings.IgnoreWeekends = true;
			}
		}

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override TimeSpan OnProcess()
		{
			using (var client = CreateClient())
			{
				var allSecurity = this.GetAllSecurity();

				if (allSecurity != null)
				{
					this.AddInfoLog(LocalizedStrings.Str2305);
					client.Refresh(EntityRegistry.Securities, new Security(), SaveSecurity, () => !CanProcess(false));
				}

				var supportedDataTypes = Enumerable.Empty<DataType>();

				if (allSecurity != null)
					supportedDataTypes = SupportedDataTypes.Intersect(allSecurity.DataTypes).ToArray();

				this.AddInfoLog(LocalizedStrings.Str2306Params.Put(_settings.StartFrom));

				var hasSecurities = false;

				foreach (var security in GetWorkingSecurities())
				{
					hasSecurities = true;

					if (!CanProcess())
						break;

					this.AddInfoLog(LocalizedStrings.Str2307Params.Put(security.Security.Id));

					//foreach (var dataType in security.GetSupportDataTypes(this))
					foreach (var pair in (allSecurity == null ? security.DataTypes : supportedDataTypes))
					{
						if (!CanProcess())
							break;

						if (!DownloadData(security, pair.MessageType, pair.Arg, client))
							break;
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
		}

		private bool DownloadData(HydraTaskSecurity security, Type dataType, object arg, RemoteStorageClient client)
		{
			var localStorage = StorageRegistry.GetStorage(security.Security, dataType, arg, _settings.Drive, _settings.StorageFormat);

			var remoteStorage = client.GetRemoteStorage(security.Security.ToSecurityId(), dataType, arg, _settings.StorageFormat);

			var endDate = DateTime.Today - TimeSpan.FromDays(_settings.DayOffset);
			var dates = remoteStorage.Dates.Where(date => date >= _settings.StartFrom && date <= endDate).Except(localStorage.Dates).ToArray();

			if (dates.IsEmpty())
			{
				if (!CanProcess())
					return false;
			}
			else
			{
				this.AddInfoLog(LocalizedStrings.Str2308Params.Put(dataType.Name));

				foreach (var date in dates)
				{
					if (!CanProcess())
						return false;

					if (_settings.IgnoreWeekends && !security.IsTradeDate(date))
					{
						this.AddDebugLog(LocalizedStrings.WeekEndDate, date);
						continue;
					}

					this.AddDebugLog(LocalizedStrings.StartDownloding, dataType, arg, date, security.Security.Id);

					using (var stream = remoteStorage.LoadStream(date))
					{
						if (stream == Stream.Null)
						{
							this.AddDebugLog(LocalizedStrings.NoData);
							continue;
						}

						this.AddInfoLog(LocalizedStrings.Str2309Params.Put(date));

						localStorage.Drive.SaveStream(date, stream);

						var info = localStorage.Serializer.CreateMetaInfo(date);

						stream.Position = 0;
						info.Read(stream);

						if (dataType == typeof(Trade))
						{
							dataType = typeof(ExecutionMessage);
							arg = ExecutionTypes.Tick;
						}
						else if (dataType == typeof(OrderLogItem))
						{
							dataType = typeof(ExecutionMessage);
							arg = ExecutionTypes.OrderLog;
						}
						else if (dataType.IsCandle())
						{
							dataType = dataType.ToCandleMessageType();
						}

						RaiseDataLoaded(security.Security, dataType, arg, date, info.Count);
					}
				}
			}

			return true;
		}

		void ISecurityDownloader.Refresh(ISecurityStorage storage, Security criteria, Action<Security> newSecurity, Func<bool> isCancelled)
		{
			using (var client = CreateClient())
				client.Refresh(storage, criteria, newSecurity, isCancelled);
		}

		private RemoteStorageClient CreateClient()
		{
			return new RemoteStorageClient(_settings.Address)
			{
				Credentials =
				{
					Login = _settings.Login,
					Password = _settings.Password
				}
			};
		}
	}
}