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
				throw new ArgumentNullException("tasks");

			_tasks = tasks;
		}

		protected override IEnumerable<IMarketDataDrive> GetDrives()
		{
			return DriveCache.Instance.AllDrives.OfType<LocalMarketDataDrive>();
		}
	}
}