#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.SmartCom.SmartCom
File: SmartComMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.SmartCom
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.SmartCom.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Icon("SmartCom_logo.png")]
	[Doc("http://stocksharp.com/doc/html/7f488b0b-0f59-42b4-845b-fd766f5699dc.htm")]
	[DisplayName("SmartCOM")]
	[CategoryLoc(LocalizedStrings.RussiaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "IT Invest (SmartCOM)")]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	[TargetPlatform(Languages.Russian)]
	partial class SmartComMessageAdapter
	{
		private SmartComVersions _version;

		/// <summary>
		/// Версия SmartCOM API. По-умолчанию равна <see cref="SmartComVersions.V3"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str1859Key)]
		[DescriptionLoc(LocalizedStrings.Str1860Key)]
		[PropertyOrder(2)]
		public SmartComVersions Version
		{
			get { return _version; }
			set
			{
				_version = value;
				UpdatePlatform();
			}
		}

		/// <summary>
		/// Логин.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(2)]
		public string Login { get; set; }

		/// <summary>
		/// Пароль.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(3)]
		public SecureString Password { get; set; }

		private EndPoint _address = SmartComAddresses.Matrix;

		/// <summary>
		/// Адрес сервера. Значение по-умолчанию равно <see cref="SmartComAddresses.Matrix"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.ServerAddressKey)]
		[DescriptionLoc(LocalizedStrings.ServerAddressKey, true)]
		[PropertyOrder(1)]
		[Editor(typeof(SmartComEndPointEditor), typeof(SmartComEndPointEditor))]
		public EndPoint Address
		{
			get { return _address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_address = value;
			}
		}

		/// <summary>
		/// Настройки конфигурации клиентской части SmartCOM 3.x.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str1862Key)]
		[DescriptionLoc(LocalizedStrings.Str1863Key)]
		[PropertyOrder(3)]
		public string ClientSettings { get; set; }

		/// <summary>
		/// Настройки конфигурации серверной части SmartCOM 3.x.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str1864Key)]
		[DescriptionLoc(LocalizedStrings.Str1865Key)]
		[PropertyOrder(4)]
		public string ServerSettings { get; set; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid => !Login.IsEmpty() && !Password.IsEmpty() && Address != null;

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(Address), Address.To<string>());
			storage.SetValue(nameof(Version), (int)Version);
			storage.SetValue(nameof(ClientSettings), ClientSettings);
			storage.SetValue(nameof(ServerSettings), ClientSettings);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
			Address = storage.GetValue<EndPoint>(nameof(Address));
			Version = (SmartComVersions)storage.GetValue(nameof(Version), 2);
			ClientSettings = storage.GetValue<string>(nameof(ClientSettings));
			ServerSettings = storage.GetValue<string>(nameof(ServerSettings));
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str1866Params.Put(Login, Address.To<string>());
		}
	}
}