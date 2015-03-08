namespace StockSharp.Hydra.Sterling
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Sterling;

	[Category(TaskCategories.American)]
	[TaskDisplayName(_sourceName)]
	class SterlingTask : ConnectorHydraTask<SterlingTrader>
	{
		private const string _sourceName = "Sterling";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class SterlingSettings : ConnectorHydraTaskSettings
		{
			public SterlingSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}
		}

		public SterlingTask()
		{
		}

		public override Uri Icon
		{
			get { return "sterling_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		private SterlingSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<SterlingTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new SterlingSettings(settings);

			if (settings.IsDefault)
			{
			}

			return new MarketDataConnector<SterlingTrader>(EntityRegistry.Securities, this, () =>
			{
				var trader = new SterlingTrader();
				return trader;
			});
		}
	}
}