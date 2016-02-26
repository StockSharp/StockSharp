#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.OpenECry.OpenECryPublic
File: OECTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.OpenECry
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.OpenECry;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[Doc("http://stocksharp.com/doc/html/4d84a1e0-fe23-4b14-8323-c5f68f117cc7.htm")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Free | TaskCategories.Ticks | TaskCategories.MarketDepth | TaskCategories.Forex |
		TaskCategories.Level1 | TaskCategories.Candles | TaskCategories.Transactions)]
	class OECTask : ConnectorHydraTask<OpenECryMessageAdapter>
	{
		private const string _sourceName = "OpenECry";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class OECSettings : ConnectorHydraTaskSettings
		{
			public OECSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[PropertyOrder(0)]
			public EndPoint Address
			{
				get { return ExtensionInfo[nameof(Address)].To<EndPoint>(); }
				set { ExtensionInfo[nameof(Address)] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(1)]
			public string Login
			{
				get { return (string)ExtensionInfo[nameof(Login)]; }
				set { ExtensionInfo[nameof(Login)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(2)]
			public SecureString Password
			{
				get { return ExtensionInfo[nameof(Password)].To<SecureString>(); }
				set { ExtensionInfo[nameof(Password)] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayName("UUID")]
			[DescriptionLoc(LocalizedStrings.Str2565Key)]
			[PropertyOrder(3)]
			public SecureString Uuid
			{
				get { return ExtensionInfo.TryGetValue(nameof(Uuid)).To<SecureString>(); }
				set { ExtensionInfo[nameof(Uuid)] = value; }
			}

			[Browsable(true)]
			public override bool IsDownloadNews
			{
				get { return base.IsDownloadNews; }
				set { base.IsDownloadNews = value; }
			}
		}

		public OECTask()
		{
			SupportedDataTypes = OpenECryMessageAdapter
				.TimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(base.SupportedDataTypes)
				.ToArray();
		}

		private OECSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new OECSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Address = OpenECryAddresses.Api;
			_settings.Uuid = OpenECryMessageAdapter.DefaultUuid;
			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsDownloadNews = true;
			_settings.SupportedLevel1Fields = Enumerator.GetValues<Level1Fields>();
		}

		protected override OpenECryMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new OpenECryMessageAdapter(generator)
			{
				Uuid = _settings.Uuid,
				Login = _settings.Login,
				Password = _settings.Password,
				Address = _settings.Address
			};
		}
	}
}