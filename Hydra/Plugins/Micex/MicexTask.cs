namespace StockSharp.Hydra.Micex
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Xaml;

	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Micex;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Category(TaskCategories.Russian)]
	[TaskDisplayName(_sourceName)]
	[TargetPlatform(Languages.Russian)]
	class MicexTask : ConnectorHydraTask<MicexTrader>
	{
		private const string _sourceName = "Micex";

		[TaskSettingsDisplayName(_sourceName)]
		private sealed class MicexSettings : ConnectorHydraTaskSettings
		{
			public MicexSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
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
			[DisplayNameLoc(LocalizedStrings.Str3418Key)]
			[DescriptionLoc(LocalizedStrings.Str3419Key)]
			[PropertyOrder(3)]
			public string Interface
			{
				get { return (string)ExtensionInfo["Interface"]; }
				set { ExtensionInfo["Interface"] = value; }
			}

			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3692Key)]
			[PropertyOrder(4)]
			public string Server
			{
				get { return (string)ExtensionInfo["Server"]; }
				set { ExtensionInfo["Server"] = value; }
			}
		}

		private MicexSettings _settings;

		public override Uri Icon
		{
			get { return "micex_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<MicexTrader> CreateTrader(HydraTaskSettings settings)
		{
			_settings = new MicexSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Address = "127.0.0.1:8000".To<EndPoint>();
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.Interface = MicexInterfaces.Stock22;
				_settings.Server = string.Empty;
			}

			return new MarketDataConnector<MicexTrader>(EntityRegistry.Securities, this, () => new MicexTrader
			{
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				Interface = _settings.Interface,
				Server = _settings.Server,
				Addresses = new[] { _settings.Address },
			});
		}
	}
}