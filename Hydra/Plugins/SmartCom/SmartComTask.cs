#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.SmartCom.SmartComPublic
File: SmartComTask.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.SmartCom
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.SmartCom;
	using StockSharp.SmartCom.Native;
	using StockSharp.SmartCom.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	[TargetPlatform(Languages.Russian)]
	[Doc("http://stocksharp.com/doc/html/1cca5a33-e5ab-434e-bfed-287389fea2eb.htm")]
	[TaskCategory(TaskCategories.Russia | TaskCategories.RealTime | TaskCategories.Stock |
		TaskCategories.Candles | TaskCategories.Level1 | TaskCategories.MarketDepth |
		TaskCategories.Transactions | TaskCategories.Free | TaskCategories.Ticks)]
	class SmartComTask : ConnectorHydraTask<SmartComMessageAdapter>
	{
		private const string _sourceName = "SmartCOM";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class SmartComSettings : ConnectorHydraTaskSettings
		{
			public SmartComSettings(HydraTaskSettings settings)
				: base(settings)
			{
				if (!ExtensionInfo.ContainsKey("IsVersion3"))
					ExtensionInfo.Add("IsVersion3", false);
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.AddressKey)]
			[DescriptionLoc(LocalizedStrings.AddressKey, true)]
			[Editor(typeof(SmartComEndPointEditor), typeof(SmartComEndPointEditor))]
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
			[DisplayName("SmartCOM 3")]
			[DescriptionLoc(LocalizedStrings.Str2829Key)]
			[PropertyOrder(3)]
			public bool IsVersion3
			{
				get { return (bool)ExtensionInfo[nameof(IsVersion3)]; }
				set { ExtensionInfo[nameof(IsVersion3)] = value; }
			}
		}

		public SmartComTask()
		{
			SupportedDataTypes = SmartComTimeFrames
				.AllTimeFrames
				.Select(tf => DataType.Create(typeof(TimeFrameCandleMessage), tf))
				.Concat(base.SupportedDataTypes)
				.ToArray();
		}

		private SmartComSettings _settings;

		public override HydraTaskSettings Settings => _settings;

		public override IEnumerable<DataType> SupportedDataTypes { get; }

		protected override void ApplySettings(HydraTaskSettings settings)
		{
			_settings = new SmartComSettings(settings);

			if (!settings.IsDefault)
				return;

			_settings.Address = SmartComAddresses.Matrix;
			_settings.Login = string.Empty;
			_settings.Password = new SecureString();
			_settings.IsVersion3 = true;
		}

		protected override SmartComMessageAdapter GetAdapter(IdGenerator generator)
		{
			return new SmartComMessageAdapter(generator)
			{
				Login = _settings.Login,
				Password = _settings.Password,
				Address = _settings.Address,
				Version = _settings.IsVersion3 ? SmartComVersions.V3 : SmartComVersions.V2
			};
		}
	}
}