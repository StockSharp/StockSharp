namespace StockSharp.Hydra.AlfaDirect
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.AlfaDirect;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	class AlfaTask : ConnectorHydraTask<AlfaTrader>
	{
		private const string _sourceName = "AlfaDirect";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class AlfaSettings : ConnectorHydraTaskSettings
		{
			public AlfaSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1445Key)]
			[DescriptionLoc(LocalizedStrings.Str1445Key, true)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str1447Key)]
			[DescriptionLoc(LocalizedStrings.Str1448Key)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public AlfaTask()
		{
			_supportedCandleSeries = AlfaTimeFrames.AllTimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private AlfaSettings _settings;

		public override Uri Icon
		{
			get { return "alfa_logo.png".GetResourceUrl(GetType()); }
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

		protected override MarketDataConnector<AlfaTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new AlfaSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsDownloadNews = true;
			}

			return new MarketDataConnector<AlfaTrader>(EntityRegistry.Securities, this, () => new AlfaTrader
			{
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
			});
		}
	}
}
