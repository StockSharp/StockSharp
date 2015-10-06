namespace Terminal
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using ActiproSoftware.Windows;

	using Ecng.Collections;
	using Ecng.Configuration;

	using StockSharp.Algo;
    using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	public class Root
    {
		private class StorageEntityFactory : EntityFactory
		{
			private readonly ISecurityStorage _securityStorage;
			private readonly Dictionary<string, Security> _securities;

			public StorageEntityFactory(ISecurityStorage securityStorage)
			{
				if (securityStorage == null)
					throw new ArgumentNullException("securityStorage");

				_securityStorage = securityStorage;
				_securities = _securityStorage.LookupAll().ToDictionary(s => s.Id, s => s, StringComparer.InvariantCultureIgnoreCase);
			}

			public override Security CreateSecurity(string id)
			{
				return _securities.SafeAdd(id, key =>
				{
					var s = base.CreateSecurity(id);
					_securityStorage.Save(s);
					return s;
				});
			}
		}

        private static Root _root;

        private Root()
        {
			var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive("Data") };
			var securityStorage = storageRegistry.GetSecurityStorage();
			Connector = new Connector { EntityFactory = new StorageEntityFactory(securityStorage) };
			ConfigManager.RegisterService(new FilterableSecurityProvider(securityStorage));
        }

        public static Root GetInstance()
        {
            if (_root == null)
            {
                lock (typeof(Root))
                {
                    _root = new Root();
                }
            }
            return _root;
        }

        public Connector Connector { private set; get; }

        private DeferrableObservableCollection<ToolItemViewModel> _toolItems;
        public DeferrableObservableCollection<ToolItemViewModel> ToolItems
        {
            get
            {
                if (_toolItems == null) ToolItems = new DeferrableObservableCollection<ToolItemViewModel>();
                return _toolItems;
            }
            set { _toolItems = value; }
        }

    }
}
