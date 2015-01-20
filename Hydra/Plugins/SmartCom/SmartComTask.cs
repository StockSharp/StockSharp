namespace StockSharp.Hydra.SmartCom
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.SmartCom;
	using StockSharp.SmartCom.Native;
	using StockSharp.SmartCom.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	[TargetPlatform(Languages.Russian)]
	class SmartComTask : ConnectorHydraTask<SmartTrader>
	{
		private const string _sourceName = "SmartCOM";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class SmartComSettings : ConnectorHydraTaskSettings
		{
			public SmartComSettings(HydraTaskSettings settings)
				: base(settings)
			{
				if (!ExtensionInfo.ContainsKey("IsVersion3"))
					ExtensionInfo.Add("IsVersion3", false);
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[Editor(typeof(SmartComEndPointEditor), typeof(SmartComEndPointEditor))]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayName("SmartCOM 3")]
			[DescriptionLoc(LocalizedStrings.Str2829Key)]
			[PropertyOrder(3)]
			public bool IsVersion3
			{
				get { return (bool)ExtensionInfo["IsVersion3"]; }
				set { ExtensionInfo["IsVersion3"] = value; }
			}
		}

		public SmartComTask()
		{
			_supportedCandleSeries = SmartComTimeFrames.AllTimeFrames.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private SmartComSettings _settings;

		public override Uri Icon
		{
			get { return "smart_logo.png".GetResourceUrl(GetType()); }
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

		protected override MarketDataConnector<SmartTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new SmartComSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Address = SmartComAddresses.Matrix;
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsVersion3 = true;
			}

			return new MarketDataConnector<SmartTrader>(EntityRegistry.Securities, this, () =>
			{
				var trader = new SmartTrader
				{
					Login = _settings.Login,
					Password = _settings.Password.To<string>(),
					Address = _settings.Address,
					Version = _settings.IsVersion3 ? SmartComVersions.V3 : SmartComVersions.V2
				};

				return trader;
			});
		}
	}
}