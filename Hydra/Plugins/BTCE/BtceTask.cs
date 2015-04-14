namespace StockSharp.Hydra.Btce
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Hydra.Core;
	using StockSharp.Btce;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[TaskDisplayName(_sourceName)]
	[Category(TaskCategories.Crypto)]
	class BtceTask : ConnectorHydraTask<BtceTrader>
	{
		private const string _sourceName = "BTCE";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BtceSettings : ConnectorHydraTaskSettings
		{
			public BtceSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}
		}

		private BtceSettings _settings;

		public override Uri Icon
		{
			get { return "btce_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<BtceTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new BtceSettings(settings);

			return new MarketDataConnector<BtceTrader>(EntityRegistry.Securities, this, CreateConnector);
		}

		private static BtceTrader CreateConnector()
		{
			var trader = new BtceTrader();
			trader.Adapter.InnerAdapters.First().IsTransactionEnabled = false;
			trader.Adapter.InnerAdapters.Add(new PassThroughMessageAdapter(new IncrementalIdGenerator()) { IsMarketDataEnabled = false });
			return trader;
		}
	}
}