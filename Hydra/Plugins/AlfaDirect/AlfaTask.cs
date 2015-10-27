namespace StockSharp.Hydra.AlfaDirect
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.AlfaDirect;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/2a908a19-0272-48e1-b143-df7ff9e2607c.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.Transactions | TaskCategories.RealTime |
		TaskCategories.Candles | TaskCategories.Level1 | TaskCategories.MarketDepth |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks | TaskCategories.News)]
	class AlfaTask : ConnectorHydraTask<AlfaDirectMessageAdapter>
	{
		private const string _sourceName = "AlfaDirect";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class AlfaSettings : ConnectorHydraTaskSettings
		{
			public AlfaSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
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

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new AlfaSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsDownloadNews = true;
		}

		protected override AlfaDirectMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new AlfaDirectMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password
			};
		}
	}
}