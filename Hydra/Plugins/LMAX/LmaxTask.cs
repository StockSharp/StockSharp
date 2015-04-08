namespace StockSharp.Hydra.LMAX
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.LMAX;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Forex)]
	[TaskDisplayName(_sourceName)]
	class LmaxTask : ConnectorHydraTask<LmaxTrader>
	{
		private const string _sourceName = "LMAX";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class LmaxSettings : ConnectorHydraTaskSettings
		{
			public LmaxSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.DemoKey)]
			[DescriptionLoc(LocalizedStrings.Str3388Key)]
			[PropertyOrder(2)]
			public bool IsDemo
			{
				get { return ExtensionInfo["IsDemo"].To<bool>(); }
				set { ExtensionInfo["IsDemo"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3710Key)]
			[DescriptionLoc(LocalizedStrings.Str3711Key)]
			[PropertyOrder(3)]
			public bool IsDownloadSecurityFromSite
			{
				get { return ExtensionInfo["IsDownloadSecurityFromSite"].To<bool>(); }
				set { ExtensionInfo["IsDownloadSecurityFromSite"] = value; }
			}
		}

		public LmaxTask()
		{
			_supportedCandleSeries = LmaxMessageAdapter.TimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private LmaxSettings _settings;

		public override Uri Icon
		{
			get { return "lmax_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		public override IEnumerable<Type> SupportedMarketDataTypes
		{
			get { return new[] { typeof(Candle), typeof(MarketDepth), typeof(Level1ChangeMessage), typeof(ExecutionMessage) }; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<LmaxTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new LmaxSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsDemo = false;
				_settings.IsDownloadSecurityFromSite = false;
			}

			return new MarketDataConnector<LmaxTrader>(EntityRegistry.Securities, this, () => new LmaxTrader
			{
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				IsDemo = _settings.IsDemo,
				IsDownloadSecurityFromSite = _settings.IsDownloadSecurityFromSite
			});
		}
	}
}