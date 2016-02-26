#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Blackwood.BlackwoodPublic
File: BlackwoodTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Blackwood
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Blackwood;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/89a8b34c-63cf-4623-bbb7-90251d53e8e6.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.Ticks |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class BlackwoodTask : ConnectorHydraTask<BlackwoodMessageAdapter>
	{
		private const string _sourceName = "Fusion/Blackwood";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class BlackwoodSettings : ConnectorHydraTaskSettings
		{
			public BlackwoodSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(1)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3694Key)]
			[DescriptionLoc(LocalizedStrings.Str3695Key)]
			[PropertyOrder(2)]
			public EndPoint HistoricalDataAddress
			{
				get { return ExtensionInfo[nameof(HistoricalDataAddress)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(HistoricalDataAddress)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3696Key)]
			[DescriptionLoc(LocalizedStrings.Str3697Key)]
			[PropertyOrder(3)]
			public EndPoint MarketDataAddress
			{
				get { return ExtensionInfo[nameof(MarketDataAddress)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(MarketDataAddress)] = value.To<string>(); }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public BlackwoodTask()
		{
			SupportedDataTypes = BlackwoodMessageAdapter
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(new[]
				{
					DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Tick),
					DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Transaction),
					DataType.Create(typeof(Level1ChangeMessage), null),
				})
				.ToArray();
		}

		private BlackwoodSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new BlackwoodSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsDownloadNews = true;
			_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();

			_settings.HistoricalDataAddress = new IPEndPoint(BlackwoodAddresses.WetBush, BlackwoodAddresses.HistoricalDataPort);
			_settings.MarketDataAddress = new IPEndPoint(BlackwoodAddresses.WetBush, BlackwoodAddresses.MarketDataPort);
		}

		protected override BlackwoodMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new BlackwoodMessageAdapter(generator)
			{
				HistoricalDataAddress = _settings.HistoricalDataAddress,
				MarketDataAddress = _settings.MarketDataAddress,
				Login = _settings.Login,
				Password = _settings.Password,
			};
		}
	}
}