namespace StockSharp.Hydra.BitStamp
{
	using System;
	using System.ComponentModel;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BitStamp;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[TaskDisplayName(_sourceName)]
	[Category(TaskCategories.Crypto)]
	class BitStampTask : ConnectorHydraTask<BitStampTrader>
	{
		private const string _sourceName = "BitStamp";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BitStampSettings : ConnectorHydraTaskSettings
		{
			public BitStampSettings(HydraTaskSettings settings)
				: base(settings)
			{
				ExtensionInfo.TryAdd("Key", new SecureString());
				ExtensionInfo.TryAdd("Secret", new SecureString());
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
				get { return (SecureString)ExtensionInfo["Key"]; }
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
				get { return (SecureString)ExtensionInfo["Secret"]; }
				set { ExtensionInfo["Secret"] = value; }
			}
		}

		private BitStampSettings _settings;

		public override Uri Icon
		{
			get { return "bitstamp_logo.png".GetResourceUrl(GetType()); }
		}

		public override string Description
		{
			get { return LocalizedStrings.Str2281Params.Put(_sourceName); }
		}

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<BitStampTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new BitStampSettings(settings);

			if (_settings.IsDefault)
			{
				_settings.Key = new SecureString();
				_settings.Secret = new SecureString();
			}

			return new MarketDataConnector<BitStampTrader>(EntityRegistry.Securities, this, () =>
			{
				var trader = new BitStampTrader
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