namespace StockSharp.Hydra.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.InteractiveBrokers;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.American)]
	[TaskDisplayName(_sourceName)]
	class IBTask : ConnectorHydraTask<IBTrader>
	{
		private const string _sourceName = "Interactive Brokers";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class IBSettings : ConnectorHydraTaskSettings
		{
			private const string _category = _sourceName;

			public IBSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str2532Key)]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str361Key)]
			[DescriptionLoc(LocalizedStrings.Str2518Key)]
			[PropertyOrder(1)]
			public int ClientId
			{
				get { return ExtensionInfo["ClientId"].To<int>(); }
				set { ExtensionInfo["ClientId"] = value.To<int>(); }
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str9Key)]
			[DescriptionLoc(LocalizedStrings.Str2521Key)]
			[PropertyOrder(2)]
			public ServerLogLevels ServerLogLevel
			{
				get { return ExtensionInfo["ServerLogLevel"].To<ServerLogLevels>(); }
				set { ExtensionInfo["ServerLogLevel"] = value.To<string>(); }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public IBTask()
		{
			_supportedCandleSeries = IBTimeFrames.AllTimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private IBSettings _settings;

		public override Uri Icon
		{
			get { return "ib_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<IBTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new IBSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Address = InteractiveBrokersSessionHolder.DefaultAddress;
				_settings.IsDownloadNews = true;
				_settings.ClientId = 0;
				_settings.ServerLogLevel = ServerLogLevels.System;
				_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
			}

			return new MarketDataConnector<IBTrader>(EntityRegistry.Securities, this, () => new IBTrader
			{
				Address = _settings.Address,
				ClientId = _settings.ClientId,
				ServerLogLevel = _settings.ServerLogLevel
			});
		}
	}
}