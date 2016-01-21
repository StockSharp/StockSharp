#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.CorePublic
File: StudioStorageRegistry.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.History.Hydra;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public class StudioStorageRegistry : StorageRegistry
	{
		private sealed class StudioDrive : Disposable, IMarketDataDrive
		{
			private readonly LocalMarketDataDrive _localDrive;
			private readonly RemoteMarketDataDrive _remoteDrive;
			private readonly LocalMarketDataDrive _cacheDrive;

			public MarketDataSettings MarketDataSettings { get; set; }

			public StudioDrive()
			{
				_localDrive = new LocalMarketDataDrive();
				_remoteDrive = new RemoteMarketDataDrive();
				_cacheDrive = new LocalMarketDataDrive(Path.Combine(BaseApplication.AppDataPath, "Cache"));
			}

			string IMarketDataDrive.Path => GetDrive().Path;

			ISecurityMarketDataDrive IMarketDataDrive.GetSecurityDrive(Security security)
			{
				return GetDrive().GetSecurityDrive(security);
			}

			IMarketDataStorage<NewsMessage> IMarketDataDrive.GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer)
			{
				return GetDrive().GetNewsMessageStorage(serializer);
			}

			IEnumerable<SecurityId> IMarketDataDrive.AvailableSecurities => GetDrive().AvailableSecurities;

			IEnumerable<DataType> IMarketDataDrive.GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
			{
				return GetDrive().GetAvailableDataTypes(securityId, format);
			}

			IMarketDataStorageDrive IMarketDataDrive.GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
			{
				var drive = GetDrive();
				var storageDrive = drive.GetStorageDrive(securityId, dataType, arg, format);

				if (drive == _remoteDrive)
					storageDrive = new CacheableMarketDataDrive(storageDrive, _cacheDrive.GetStorageDrive(securityId, dataType, arg, format));

				return storageDrive;
			}

			private IMarketDataDrive GetDrive()
			{
				if (MarketDataSettings.UseLocal)
				{
					_localDrive.Path = MarketDataSettings.Path;

					return _localDrive;
				}
				else
				{
					if (_remoteDrive.Client.Address.OriginalString == MarketDataSettings.Path)
						return _remoteDrive;

					//var credentials = MarketDataSettings.IsStockSharpStorage
					//	? ConfigManager.GetService<IPersistableService>().GetCredentials()
					//	: MarketDataSettings.Credentials;

					var credentials = MarketDataSettings.Credentials;

					_remoteDrive.Client = new RemoteStorageClient(new Uri(MarketDataSettings.Path))
					{
						Credentials =
						{
							Login = credentials.Login, 
							Password = credentials.Password
						}
					};

					return _remoteDrive;
				}
			}

			void IPersistable.Load(SettingsStorage storage)
			{
				GetDrive().Load(storage);
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				GetDrive().Save(storage);
			}

			protected override void DisposeManaged()
			{
				_localDrive.Dispose();
				_remoteDrive.Dispose();

				base.DisposeManaged();
			}
		}

		private readonly StudioDrive _defaultDrive = new StudioDrive();

		public override IMarketDataDrive DefaultDrive
		{
			get { return _defaultDrive; }
		}

		public MarketDataSettings MarketDataSettings
		{
			get { return _defaultDrive.MarketDataSettings; }
			set { _defaultDrive.MarketDataSettings = value; }
		}
	}
}