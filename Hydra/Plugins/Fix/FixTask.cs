namespace StockSharp.Hydra.Fix
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Serialization;

	using StockSharp.Fix;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TaskDoc("http://stocksharp.com/doc/html/e81b1b7f-5c96-488e-a90d-e60cb8675977.htm")]
	class FixTask : ConnectorHydraTask<FixTrader>
	{
		private const string _sourceName = "FIX";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class FixSettings : ConnectorHydraTaskSettings
		{
			public FixSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			private FixMessageAdapter _marketDataSession;

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.SessionKey)]
			[DescriptionLoc(LocalizedStrings.Str3746Key)]
			[PropertyOrder(0)]
			[ExpandableObject]
			public FixMessageAdapter MarketDataSession
			{
				get
				{
					if (_marketDataSession == null)
						_marketDataSession = new FixMessageAdapter(new IncrementalIdGenerator());

					_marketDataSession.Load((SettingsStorage)ExtensionInfo["MarketDataSession"]);

					return _marketDataSession;
				}
				set
				{
					if (value == null)
						throw new ArgumentNullException("value");

					ExtensionInfo["MarketDataSession"] = value.Save();
					_marketDataSession = value;
				}
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}

			public override void ApplyChanges(HydraTaskSettings settingsCopy)
			{
				((FixSettings)settingsCopy).MarketDataSession = ((FixSettings)settingsCopy)._marketDataSession;

				base.ApplyChanges(settingsCopy);
			}
		}

		private FixSettings _settings;

		public override Uri Icon
		{
			get { return "fix_logo.png".GetResourceUrl(GetType()); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<FixTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new FixSettings(settings);

			if (settings.IsDefault)
				_settings.MarketDataSession = new FixMessageAdapter(new IncrementalIdGenerator());

			return new MarketDataConnector<FixTrader>(EntityRegistry.Securities, this, () =>
			{
				var trader = new FixTrader();

				trader.MarketDataAdapter.Load(_settings.MarketDataSession.Save());

				if (!this.IsExecLogEnabled())
					trader.Adapter.InnerAdapters.Remove(trader.TransactionAdapter);

				return trader;
			});
		}
	}
}