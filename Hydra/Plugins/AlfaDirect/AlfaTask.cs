#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.AlfaDirect.AlfaDirectPublic
File: AlfaTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.AlfaDirect
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.ComponentModel;

	using StockSharp.Hydra.Core;
	using StockSharp.AlfaDirect;
	using StockSharp.Algo;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/2a908a19-0272-48e1-b143-df7ff9e2607c.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.Transactions | TaskCategories.RealTime |
		TaskCategories.Candles | TaskCategories.Level1 | TaskCategories.MarketDepth |
		TaskCategories.Stock | TaskCategories.Free | TaskCategories.Ticks | TaskCategories.News)]
	class AlfaTask : ConnectorHydraTask<AlfaDirectMessageAdapter>
	{
		private const string _sourceName = "AlfaDirect";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class AlfaSettings : ConnectorHydraTaskSettings
		{
			public AlfaSettings(HydraTaskSettings settings)
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

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public AlfaTask()
		{
			SupportedDataTypes = base.SupportedDataTypes.Concat(
				AlfaTimeFrames
					.AllTimeFrames
					.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf)))
				.ToArray();
		}

		private AlfaSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new AlfaSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsDownloadNews = true;
		}

		protected override AlfaDirectMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new AlfaDirectMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password
			};
		}
	}
}