namespace StockSharp.Hydra.BitStamp
{
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.BitStamp;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/7a11d9ff-17c9-406b-ab88-c4b9c080912d.htm")]
	[TaskCategory(TaskCategories.Crypto | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.Ticks | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Transactions)]
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

		private BitStampSettings _settings;

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