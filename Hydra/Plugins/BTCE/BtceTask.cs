namespace StockSharp.Hydra.Btce
{
	using System;
	using System.ComponentModel;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Hydra.Core;
	using StockSharp.Btce;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
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
				ExtensionInfo.TryAdd("Key", new SecureString());
				ExtensionInfo.TryAdd("Secret", new SecureString());
			}

			/// <summary>
			/// Ключ.
			/// </summary>
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3304Key)]
			[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
			[PropertyOrder(1)]
			public SecureString Key
			{
				get { return (SecureString)ExtensionInfo["Key"]; }
				set { ExtensionInfo["Key"] = value; }
			}

			/// <summary>
			/// Секрет.
			/// </summary>
			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3306Key)]
			[DescriptionLoc(LocalizedStrings.Str3307Key)]
			[PropertyOrder(2)]
			public SecureString Secret
			{
				get { return (SecureString)ExtensionInfo["Secret"]; }
				set { ExtensionInfo["Secret"] = value; }
			}
		}

		private BtceSettings _settings;

		public override Uri Icon
		{
			get { return "btce_logo.png".GetResourceUrl(GetType()); }
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

			return new MarketDataConnector<BtceTrader>(EntityRegistry.Securities, this, () =>
			{
				var trader = new BtceTrader
				{
					Key = _settings.Key.To<string>(),
					Secret = _settings.Secret.To<string>(),
				};

				if (trader.Key.IsEmpty())
					trader.TransactionAdapter.RemoveTransactionalSupport();

				return trader;
			});
		}
	}
}