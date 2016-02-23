#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Fix.FixPublic
File: FixTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Fix
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Fix;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/e81b1b7f-5c96-488e-a90d-e60cb8675977.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.Russia | TaskCategories.RealTime |
		TaskCategories.Stock | TaskCategories.Paid | TaskCategories.Ticks | TaskCategories.OrderLog |
		TaskCategories.Forex | TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class FixTask : ConnectorHydraTask<FixMessageAdapter>
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
						throw new ArgumentNullException(nameof(value));

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

		public override HydraTaskSettings Settings => _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new FixSettings(settings);

			if (settings.IsDefault)
				_settings.MarketDataSession = new FixMessageAdapter(new IncrementalIdGenerator());
		}

		protected override FixMessageAdapter GetAdapter(IdGenerator generator)
		{
			var adapter = new FixMessageAdapter(generator);

			adapter.Load(_settings.MarketDataSession.Save());

			return adapter;
		}
	}
}