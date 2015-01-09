namespace StockSharp.Hydra.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.OpenECry;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.American)]
	[TaskDisplayName(_sourceName)]
	class OECTask : ConnectorHydraTask<OECTrader>
	{
		private const string _sourceName = "OpenECry";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class OECSettings : ConnectorHydraTaskSettings
		{
			public OECSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1439Key)]
			[DescriptionLoc(LocalizedStrings.Str1668Key)]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1445Key)]
			[DescriptionLoc(LocalizedStrings.Str1445Key, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1447Key)]
			[DescriptionLoc(LocalizedStrings.Str1448Key)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayName("UUID")]
			[DescriptionLoc(LocalizedStrings.Str2565Key)]
			[PropertyOrder(3)]
			public string Uuid
			{
				get { return (string)ExtensionInfo.TryGetValue("Uuid"); }
				set { ExtensionInfo["Uuid"] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public OECTask()
		{
			_supportedCandleSeries = OpenECrySessionHolder.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private OECSettings _settings;

		public override Uri Icon
		{
			get { return "oec_logo.png".GetResourceUrl(GetType()); }
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

		protected override MarketDataConnector<OECTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new OECSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Address = OpenECryAddresses.Api;
				_settings.Uuid = string.Empty;
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsDownloadNews = true;
				_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
			}

			return new MarketDataConnector<OECTrader>(EntityRegistry.Securities, this, () => new OECTrader
			{
				Uuid = _settings.Uuid,
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				Address = _settings.Address
			});
		}
	}
}