namespace StockSharp.Hydra.BitStamp
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BitStamp;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	[TaskDisplayName(_sourceName)]
	[Category(TaskCategories.Crypto)]
	class BtceTask : ConnectorHydraTask<BitStampTrader>
	{
		private const string _sourceName = "BitStamp";

		[TaskSettingsDisplayName(_sourceName)]
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
			get { return "bitstamp_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<BitStampTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new BtceSettings(settings);

			return new MarketDataConnector<BitStampTrader>(EntityRegistry.Securities, this, () => new BitStampTrader
			{
				TransactionAdapter = new PassThroughMessageAdapter(new PassThroughSessionHolder(new IncrementalIdGenerator()))
			});
		}
	}
}