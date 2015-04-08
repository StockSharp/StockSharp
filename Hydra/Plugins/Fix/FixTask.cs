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

	[TaskDisplayName(_sourceName)]
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

			private FixSession _marketDataSession;

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.SessionKey)]
			[DescriptionLoc(LocalizedStrings.Str3746Key)]
			[PropertyOrder(0)]
			public FixSession MarketDataSession
			{
				get
				{
					if (_marketDataSession == null)
						_marketDataSession = new FixSession();

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

			/// <summary>
			/// Применить изменения, сделанные в копии настроек.
			/// </summary>
			/// <param name="settingsCopy">Копия.</param>
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

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<FixTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new FixSettings(settings);

			if (settings.IsDefault)
				_settings.MarketDataSession = new FixSession();

			return new MarketDataConnector<FixTrader>(EntityRegistry.Securities, this, () =>
			{
				var trader = new FixTrader();
				trader.MarketDataAdapter.Load(_settings.MarketDataSession.Save());
				return trader;
			});
		}
	}
}