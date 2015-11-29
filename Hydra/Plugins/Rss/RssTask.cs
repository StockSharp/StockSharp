namespace StockSharp.Hydra.Rss
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Rss;
	using StockSharp.Rss.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.RssSourceKey)]
	[Doc("http://stocksharp.com/doc/html/91454878-ec26-4872-9a85-1bfbc76dc77a.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.Russia | TaskCategories.News)]
	class RssTask : ConnectorHydraTask<RssMarketDataMessageAdapter>
	{
		private const string _sourceName = "RSS";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class RssSettings : ConnectorHydraTaskSettings
		{
			public RssSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str3505Key)]
			[CategoryLoc(_sourceName)]
			[Editor(typeof(RssAddressEditor), typeof(RssAddressEditor))]
			[PropertyOrder(0)]
			public Uri Address
			{
				get { return ExtensionInfo["Address"].To<Uri>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[DisplayNameLoc(LocalizedStrings.Str3506Key)]
			[DescriptionLoc(LocalizedStrings.Str3507Key)]
			[CategoryLoc(_sourceName)]
			[PropertyOrder(1)]
			public string CustomDateFormat
			{
				get { return (string)ExtensionInfo["CustomDateFormat"]; }
				set { ExtensionInfo["CustomDateFormat"] = value.To<string>(); }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}

			[Browsable(false)]
			public override IEnumerable<Level1Fields> SupportedLevel1Fields
			{
				get { return Enumerable.Empty<Level1Fields>(); }
				set { }
			}
		}

		private RssSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; } = new[]
		{
			DataType.Create(typeof(NewsMessage), null)
		};

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new RssSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Address = null;
			_settings.IsDownloadNews = true;
			_settings.CustomDateFormat = string.Empty;
		}

		protected override RssMarketDataMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new RssMarketDataMessageAdapter(generator)
			{
				Address = _settings.Address,
				CustomDateFormat = _settings.CustomDateFormat
			};
		}
	}
}