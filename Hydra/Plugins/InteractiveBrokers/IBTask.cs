#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.InteractiveBrokers.InteractiveBrokersPublic
File: IBTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.InteractiveBrokers
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.InteractiveBrokers;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/bcb8050d-d330-4071-affc-b6ed5639e1fb.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class IBTask : ConnectorHydraTask<InteractiveBrokersMessageAdapter>
	{
		private const string _sourceName = "Interactive Brokers";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class IBSettings : ConnectorHydraTaskSettings
		{
			private const string _category = _sourceName;

			public IBSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.Str2532Key)]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo[nameof(Address)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(Address)] = value.To<string>(); }
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str361Key)]
			[DescriptionLoc(LocalizedStrings.Str2518Key)]
			[PropertyOrder(1)]
			public int ClientId
			{
				get { return ExtensionInfo[nameof(ClientId)].To<int>(); }
				set { ExtensionInfo[nameof(ClientId)] = value.To<int>(); }
			}

			[Category(_category)]
			[DisplayNameLoc(LocalizedStrings.Str9Key)]
			[DescriptionLoc(LocalizedStrings.Str2521Key)]
			[PropertyOrder(2)]
			public ServerLogLevels ServerLogLevel
			{
				get { return ExtensionInfo[nameof(ServerLogLevel)].To<ServerLogLevels>(); }
				set { ExtensionInfo[nameof(ServerLogLevel)] = value.To<string>(); }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public IBTask()
		{
			SupportedDataTypes = IBTimeFrames
				.AllTimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(base.SupportedDataTypes)
				.ToArray();
		}

		private IBSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new IBSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Address = InteractiveBrokersMessageAdapter.DefaultAddress;
			_settings.IsDownloadNews = true;
			_settings.ClientId = 0;
			_settings.ServerLogLevel = ServerLogLevels.System;
			_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
		}

		protected override InteractiveBrokersMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new InteractiveBrokersMessageAdapter(generator)
			{
				Address = _settings.Address,
				ClientId = _settings.ClientId,
				ServerLogLevel = _settings.ServerLogLevel
			};
		}
	}
}