#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Rithmic.RithmicPublic
File: RithmicTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Rithmic
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Rithmic;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/26ff0aad-623b-47e2-a8f8-a337506cd2ff.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Free | TaskCategories.Ticks | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class RithmicTask : ConnectorHydraTask<RithmicMessageAdapter>
	{
		private const string _sourceName = "Rithmic";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class RithmicSettings : ConnectorHydraTaskSettings
		{
			public RithmicSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(0)]
			public string UserName
			{
				get { return (string)ExtensionInfo[nameof(UserName)]; }
				set { ExtensionInfo[nameof(UserName)] = value; }
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
			[DisplayNameLoc(LocalizedStrings.Str3465Key)]
			[DescriptionLoc(LocalizedStrings.Str3466Key)]
			[PropertyOrder(2)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string CertFile
			{
				get { return (string)ExtensionInfo[nameof(CertFile)]; }
				set { ExtensionInfo[nameof(CertFile)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3416Key)]
			[DescriptionLoc(LocalizedStrings.Str3474Key)]
			[PropertyOrder(3)]
			public RithmicServers Server
			{
				get { return ExtensionInfo[nameof(Server)].To<RithmicServers>(); }
				set { ExtensionInfo[nameof(Server)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3471Key)]
			[DescriptionLoc(LocalizedStrings.Str3472Key)]
			[PropertyOrder(4)]
			[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
			public string LogFileName
			{
				get { return (string)ExtensionInfo[nameof(LogFileName)]; }
				set { ExtensionInfo[nameof(LogFileName)] = value; }
			}
		}

		public RithmicTask()
		{
			SupportedDataTypes = RithmicMessageAdapter
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(base.SupportedDataTypes)
				.ToArray();
		}

		private RithmicSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new RithmicSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.UserName = string.Empty;
			_settings.Password = new SecureString();
			_settings.Server = RithmicServers.Paper;
			_settings.LogFileName = string.Empty;
			_settings.CertFile = string.Empty;
			_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
		}

		protected override RithmicMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new RithmicMessageAdapter(generator)
			{
				UserName = _settings.UserName,
				Password = _settings.Password,
				CertFile = _settings.CertFile,
				Server = _settings.Server,
				LogFileName = _settings.LogFileName
			};
		}
	}
}