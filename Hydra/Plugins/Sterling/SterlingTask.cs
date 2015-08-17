namespace StockSharp.Hydra.Sterling
{
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Sterling;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/f43591c9-c215-4dee-b297-4b9ed34fd465.htm")]
	[TaskIcon("sterling_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class SterlingTask : ConnectorHydraTask<SterlingTrader>
	{
		private const string _sourceName = "Sterling";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
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

		private SterlingSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<SterlingTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new SterlingSettings(settings);

			if (settings.IsDefault)
			{
			}

			return new MarketDataConnector<SterlingTrader>(EntityRegistry.Securities, this, () => new SterlingTrader());
		}
	}
}