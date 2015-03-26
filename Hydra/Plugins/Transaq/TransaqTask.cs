namespace StockSharp.Hydra.Transaq
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Localization;
	using Ecng.Xaml;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Transaq;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	[TargetPlatform(Languages.Russian)]
	class TransaqTask : ConnectorHydraTask<TransaqTrader>
	{
		private const string _sourceName = "Transaq";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class TransaqSettings : ConnectorHydraTaskSettings
		{
			public TransaqSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.Str3679Key)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.Str3680Key)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str3681Key)]
			[PropertyOrder(2)]
			public EndPoint Address
			{
				get { return ExtensionInfo["Address"].To<EndPoint>(); }
				set { ExtensionInfo["Address"] = value.To<string>(); }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.Str3682Key)]
			[DescriptionLoc(LocalizedStrings.Str3683Key)]
			[PropertyOrder(3)]
			public bool UseProxy
			{
				get { return (bool)ExtensionInfo["UseProxy"]; }
				set { ExtensionInfo["UseProxy"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.Str3684Key)]
			[DescriptionLoc(LocalizedStrings.Str3685Key)]
			[PropertyOrder(4)]
			[ItemsSource(typeof(ProxyItemsSource))]
			public string ProxyType
			{
				get { return (string)ExtensionInfo["ProxyType"]; }
				set { ExtensionInfo["ProxyType"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.Str3686Key)]
			[PropertyOrder(5)]
			public string ProxyLogin
			{
				get { return (string)ExtensionInfo["ProxyLogin"]; }
				set { ExtensionInfo["ProxyLogin"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.Str3687Key)]
			[PropertyOrder(6)]
			public SecureString ProxyPassword
			{
				get { return ExtensionInfo["ProxyPassword"].To<SecureString>(); }
				set { ExtensionInfo["ProxyPassword"] = value; }
			}

			[CategoryLoc(LocalizedStrings.Str3539Key)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str3688Key)]
			[PropertyOrder(7)]
			public EndPoint ProxyAddress
			{
				get { return ExtensionInfo["ProxyAddress"].To<EndPoint>(); }
				set { ExtensionInfo["ProxyAddress"] = value.To<string>(); }
			}

			[TaskCategory(_sourceName)]
			[DisplayName("HFT")]
			[DescriptionLoc(LocalizedStrings.Str3545Key)]
			[PropertyOrder(8)]
			public bool IsHFT
			{
				get { return (bool)ExtensionInfo["IsHFT"]; }
				set { ExtensionInfo["IsHFT"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str736Key)]
			[DescriptionLoc(LocalizedStrings.Str3547Key)]
			[PropertyOrder(9)]
			public TimeSpan? MarketDataInterval
			{
				get { return (TimeSpan?)ExtensionInfo["MarketDataInterval"]; }
				set { ExtensionInfo["MarketDataInterval"] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public TransaqTask()
		{
			_supportedCandleSeries = new[]
			{
				TimeSpan.FromMinutes(1),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1)
			}.Select(tf => new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = tf
			}).ToArray();
		}

		private TransaqSettings _settings;

		public override Uri Icon
		{
			get { return "transaq_logo.png".GetResourceUrl(GetType()); }
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

		protected override MarketDataConnector<TransaqTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new TransaqSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.Address = TransaqAddresses.FinamReal1;
				_settings.IsHFT = false;
				_settings.MarketDataInterval = null;
				_settings.IsDownloadNews = true;

				_settings.UseProxy = false;
				_settings.ProxyType = ProxyTypes.Http.To<string>();
				_settings.ProxyLogin = string.Empty;
				_settings.ProxyPassword = new SecureString();
				_settings.ProxyAddress = new IPEndPoint(IPAddress.Loopback, 8080);
			}

			return new MarketDataConnector<TransaqTrader>(EntityRegistry.Securities, this, () => 
			{
				var trader = new TransaqTrader
				{
					Login = _settings.Login, 
					Password = _settings.Password.To<string>(), 
					Address = _settings.Address,
					IsHFT = _settings.IsHFT,
					MarketDataInterval = _settings.MarketDataInterval
				};

				if (_settings.UseProxy)
				{
					trader.Proxy = new StockSharp.Transaq.Proxy
					{
						Login = _settings.ProxyLogin, 
						Password = _settings.ProxyPassword.To<string>(), 
						Address = _settings.ProxyAddress,
						Type = _settings.ProxyType.To<ProxyTypes>()
					};
				}

				return trader;
			});
		}
	}
}