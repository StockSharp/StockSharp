namespace StockSharp.Hydra.Btce
{
	using System;
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Hydra.Core;
	using StockSharp.Btce;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[TaskDisplayName(_sourceName)]
	[Category(TaskCategories.Crypto)]
	class BtceTask : ConnectorHydraTask<BtceTrader>
	{
		private const string _sourceName = "BTCE";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BtceSettings : ConnectorHydraTaskSettings
		{
			public BtceSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			/// <summary>
			/// Ключ.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3304Key)]
			[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
			[PropertyOrder(1)]
			public SecureString Key
			{
				get { return ExtensionInfo["Key"].To<SecureString>(); }
				set { ExtensionInfo["Key"] = value; }
			}

			/// <summary>
			/// Секрет.
			/// </summary>
			[TaskCategory(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3306Key)]
			[DescriptionLoc(LocalizedStrings.Str3307Key)]
			[PropertyOrder(2)]
			public SecureString Secret
			{
				get { return ExtensionInfo["Secret"].To<SecureString>(); }
				set { ExtensionInfo["Secret"] = value; }
			}
		}

		private BtceSettings _settings;

		public override Uri Icon
		{
			get { return "btce_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<BtceTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new BtceSettings(settings);

			if (_settings.IsDefault)
			{
				_settings.Key = new SecureString();
				_settings.Secret = new SecureString();
			}

			return new MarketDataConnector<BtceTrader>(EntityRegistry.Securities, this, () => new BtceTrader
			{
				Key = _settings.Key.To<string>(),
				Secret = _settings.Secret.To<string>(),
			});
		}
	}
}