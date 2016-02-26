#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Server.HydraPublic
File: HydraServer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Server
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.ServiceModel;

	using StockSharp.Algo.Storages;
	using StockSharp.Hydra.Core;
	using StockSharp.Algo.History.Hydra;

	[ServiceBehavior(
		InstanceContextMode = InstanceContextMode.Single,
		ConcurrencyMode = ConcurrencyMode.Multiple,
		IncludeExceptionDetailInFaults = true,
		UseSynchronizationContext = false)]
	class HydraServer : RemoteStorage
	{
		private readonly IEnumerable<IHydraTask> _tasks;

		public HydraServer(IStorageRegistry storageRegistry, IEntityRegistry entityRegistry, IEnumerable<IHydraTask> tasks)
			: base(storageRegistry, entityRegistry)
		{
			if (tasks == null)
				throw new ArgumentNullException(nameof(tasks));

			_tasks = tasks;
		}

		protected override IEnumerable<IMarketDataDrive> GetDrives()
		{
			return DriveCache.Instance.Drives.OfType<LocalMarketDataDrive>();
		}
	}
}