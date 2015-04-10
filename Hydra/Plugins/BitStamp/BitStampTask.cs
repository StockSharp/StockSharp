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

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[TaskDisplayName(_sourceName)]
	[Category(TaskCategories.Crypto)]
	class BitStampTask : ConnectorHydraTask<BitStampTrader>
	{
		private const string _sourceName = "BitStamp";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BitStampSettings : ConnectorHydraTaskSettings
		{
			public BitStampSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}
		}

		private BitStampSettings _settings;

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
			_settings = new BitStampSettings(settings);

			return new MarketDataConnector<BitStampTrader>(EntityRegistry.Securities, this, () => new BitStampTrader
			{
				TransactionAdapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
			});
		}
	}
}