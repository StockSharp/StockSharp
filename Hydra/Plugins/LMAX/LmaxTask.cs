#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.LMAX.LMAXPublic
File: LmaxTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.LMAX
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.LMAX;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/0b962432-d81d-4646-a818-9fa7093cbe4f.htm")]
	[TaskCategory(TaskCategories.Forex | TaskCategories.RealTime |
		TaskCategories.Free | TaskCategories.History | TaskCategories.MarketDepth |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class LmaxTask : ConnectorHydraTask<LmaxMessageAdapter>
	{
		private const string _sourceName = "LMAX";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class LmaxSettings : ConnectorHydraTaskSettings
		{
			public LmaxSettings(HydraTaskSettings settings)
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
			[DisplayNameLoc(LocalizedStrings.DemoKey)]
			[DescriptionLoc(LocalizedStrings.Str3388Key)]
			[PropertyOrder(2)]
			public bool IsDemo
			{
				get { return ExtensionInfo[nameof(IsDemo)].To<bool>(); }
				set { ExtensionInfo[nameof(IsDemo)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.Str3710Key)]
			[DescriptionLoc(LocalizedStrings.Str3711Key)]
			[PropertyOrder(3)]
			public bool IsDownloadSecurityFromSite
			{
				get { return ExtensionInfo[nameof(IsDownloadSecurityFromSite)].To<bool>(); }
				set { ExtensionInfo[nameof(IsDownloadSecurityFromSite)] = value; }
			}
		}

		public LmaxTask()
		{
			SupportedDataTypes = LmaxMessageAdapter
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(new[]
				{
					DataType.Create(typeof(ExecutionMessage), ExecutionTypes.Transaction),
					DataType.Create(typeof(QuoteChangeMessage), null),
					DataType.Create(typeof(Level1ChangeMessage), null),
				})
				.ToArray();
		}

		private LmaxSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new LmaxSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsDemo = false;
			_settings.IsDownloadSecurityFromSite = false;
		}

		protected override LmaxMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new LmaxMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password,
				IsDemo = _settings.IsDemo,
				IsDownloadSecurityFromSite = _settings.IsDownloadSecurityFromSite
			};
		}
	}
}