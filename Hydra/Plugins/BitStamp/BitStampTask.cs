#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.BitStamp.BitStampPublic
File: BitStampTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.BitStamp
{
	using System.Security;

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
	class BitStampTask : ConnectorHydraTask<BitStampMessageAdapter>
	{
		private const string _sourceName = "BitStamp";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BitStampSettings : ConnectorHydraTaskSettings
		{
			public BitStampSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3304Key)]
			[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
			[PropertyOrder(1)]
			public SecureString Key
			{
				get { return (SecureString)ExtensionInfo[nameof(Key)]; }
				set { ExtensionInfo[nameof(Key)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3306Key)]
			[DescriptionLoc(LocalizedStrings.Str3307Key)]
			[PropertyOrder(2)]
			public SecureString Secret
			{
				get { return (SecureString)ExtensionInfo[nameof(Secret)]; }
				set { ExtensionInfo[nameof(Secret)] = value; }
			}
		}

		private BitStampSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new BitStampSettings(settings);

			if (!_settings.IsDefault)
				return;

			_settings.Key = new SecureString();
			_settings.Secret = new SecureString();
		}

		protected override BitStampMessageAdapter GetAdapter(IdGenerator generator)
		{
			var adapter = new BitStampMessageAdapter(generator)
			{
				Key = _settings.Key,
				Secret = _settings.Secret,
			};

			if (adapter.Key.IsEmpty())
				adapter.RemoveTransactionalSupport();

			return adapter;
		}
	}
}