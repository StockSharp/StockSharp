#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Oanda.OandaPublic
File: OandaTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Oanda
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Oanda;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/6a99aea5-1142-4b2e-b183-554fcc890fad.htm")]
	[TaskCategory(TaskCategories.Forex | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.History | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class OandaTask : ConnectorHydraTask<OandaMessageAdapter>
	{
		private const string _sourceName = "OANDA";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class OandaSettings : ConnectorHydraTaskSettings
		{
			public OandaSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3450Key)]
			[PropertyOrder(0)]
			public OandaServers Server
			{
				get { return ExtensionInfo[nameof(Server)].To<OandaServers>(); }
				set { ExtensionInfo[nameof(Server)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3451Key)]
			[DescriptionLoc(LocalizedStrings.Str3451Key, true)]
			[PropertyOrder(1)]
			public SecureString Token
			{
				get { return ExtensionInfo[nameof(Token)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Token)] = value; }
			}
		}

		public OandaTask()
		{
			SupportedDataTypes = OandaMessageAdapter
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(new[]
				{
					DataType.Create(typeof(Level1ChangeMessage), null),
					DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Transaction),
				})
				.ToArray();
		}

		private OandaSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new OandaSettings(settings);

			if (settings.IsDefault)
			{
				_settings.Server = OandaServers.Real;
				_settings.Token = new SecureString();
			}
		}

		protected override OandaMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new OandaMessageAdapter(generator)
			{
				Server = _settings.Server,
				Token = _settings.Token,
			};
		}
	}
}