namespace StockSharp.Hydra.SmartCom
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.SmartCom;
	using StockSharp.SmartCom.Native;
	using StockSharp.SmartCom.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/1cca5a33-e5ab-434e-bfed-287389fea2eb.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Candles | TaskCategories.Level1 | TaskCategories.MarketDepth |
		TaskCategories.Transactions | TaskCategories.Free | TaskCategories.Ticks)]
	class SmartComTask : ConnectorHydraTask<SmartTrader>
	{
		private const string _sourceName = "SmartCOM";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class SmartComSettings : ConnectorHydraTaskSettings
		{
			public SmartComSettings(HydraTaskSettings settings)
				: base(settings)
			{
				if (!ExtensionInfo.ContainsKey("IsVersion3"))
					ExtensionInfo.Add("IsVersion3", false);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[Editor(typeof(SmartComEndPointEditor), typeof(SmartComEndPointEditor))]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
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

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		private readonly IEnumerable<CandleSeries> _supportedCandleSeries;

		public override IEnumerable<CandleSeries> SupportedCandleSeries
		{
			get { return _supportedCandleSeries; }
		}

		protected override MarketDataConnector<SmartTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new SmartComSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Address = SmartComAddresses.Matrix;
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.IsVersion3 = true;
			}

			return new MarketDataConnector<SmartTrader>(EntityRegistry.Securities, this, () => new SmartTrader
			{
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				Address = _settings.Address,
				Version = _settings.IsVersion3 ? SmartComVersions.V3 : SmartComVersions.V2
			});
		}
	}
}