#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Transaq
File: TransaqMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[Icon("Transaq_logo.png")]
	[Doc("http://stocksharp.com/doc/html/a010f9bd-15bb-4858-a067-590101087dff.htm")]
	[DisplayName("Transaq")]
	[CategoryLoc(LocalizedStrings.RussiaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "Transaq")]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	[TargetPlatform(Languages.Russian)]
	partial class TransaqMessageAdapter
	{
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

		private EndPoint _address = TransaqAddresses.FinamDemo;

		/// <summary>
		/// Адрес сервера.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.ServerAddressKey)]
		[DescriptionLoc(LocalizedStrings.ServerAddressKey, true)]
		[PropertyOrder(1)]
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
		/// Прокси.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3539Key)]
		[DescriptionLoc(LocalizedStrings.Str3540Key)]
		[PropertyOrder(4)]
		public Proxy Proxy { get; set; }

		/// <summary>
		/// Уровень логирования коннектора. По умолчанию <see cref="ApiLogLevels.Standard"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str3541Key)]
		[PropertyOrder(2)]
		public ApiLogLevels ApiLogLevel { get; set; } = ApiLogLevels.Standard;

		private string _dllPath = "txmlconnector.dll";

		/// <summary>
		/// Полный путь к dll файлу, содержащее Transaq API. По-умолчанию равно txmlconnector.dll.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3410Key)]
		[DescriptionLoc(LocalizedStrings.Str3542Key)]
		[PropertyOrder(1)]
		[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
		public string DllPath
		{
			get { return _dllPath; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_dllPath = value;
			}
		}

		///// <summary>
		///// Показать окно смены пароля при соединении.
		///// </summary>
		//[Category("Общее")]
		//[DisplayName("Сменить пароль")]
		//[Description("Показать окно смены пароля при соединении.")]
		//[PropertyOrder(4)]
		//public bool ShowChangePasswordWindowOnConnect { get; set; }

		/// <summary>
		/// Передавать ли данные для фондового рынка.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3543Key)]
		[DescriptionLoc(LocalizedStrings.Str3544Key)]
		[PropertyOrder(3)]
		public bool MicexRegisters { get; set; } = true;

		/// <summary>
		/// Подключаться ли к HFT серверу Финам.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayName("HFT")]
		[DescriptionLoc(LocalizedStrings.Str3545Key)]
		[PropertyOrder(6)]
		public bool IsHFT { get; set; }

		/// <summary>
		/// Период агрегирования данных на сервере Transaq.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3546Key)]
		[DescriptionLoc(LocalizedStrings.Str3547Key)]
		[PropertyOrder(4)]
		public TimeSpan? MarketDataInterval { get; set; }

		/// <summary>
		/// Путь к логам коннектора.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3332Key)]
		[DescriptionLoc(LocalizedStrings.Str3548Key)]
		[PropertyOrder(7)]
		[Editor(typeof(FolderBrowserEditor), typeof(FolderBrowserEditor))]
		public string ApiLogsPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "StockSharp.Transaq", "Logs");

		/// <summary>
		/// Перезаписать файл библиотеки из ресурсов. По-умолчанию файл будет перезаписан.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.OverrideKey)]
		[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
		[PropertyOrder(8)]
		public bool OverrideDll { get; set; } = true;

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid => !Login.IsEmpty() && !Password.IsEmpty();

		/// <summary>
		/// Версия коннектора.
		/// </summary>
		[Browsable(false)]
		public string ConnectorVersion { get; internal set; }

		/// <summary>
		/// Текущий сервер.
		/// </summary>
		[Browsable(false)]
		public int CurrentServer { get; internal set; }

		/// <summary>
		/// Разница между локальным и серверным временем.
		/// </summary>
		[Browsable(false)]
		public TimeSpan? ServerTimeDiff { get; internal set; }

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(Address), Address.To<string>());
			storage.SetValue(nameof(DllPath), DllPath);
			storage.SetValue(nameof(ApiLogsPath), ApiLogsPath);
			storage.SetValue(nameof(ApiLogLevel), ApiLogLevel.To<string>());
			storage.SetValue(nameof(IsHFT), IsHFT);
			storage.SetValue(nameof(MarketDataInterval), MarketDataInterval);

			if (Proxy != null)
			{
				storage.SetValue(nameof(Proxy), Proxy.Save());
			}

			// не нужно сохранять это свойство, так как иначе окно смены пароля будет показывать каждый раз
			//storage.SetValue("ChangePasswordOnConnect", ShowChangePasswordWindowOnConnect);

			storage.SetValue(nameof(MicexRegisters), MicexRegisters);
			storage.SetValue(nameof(OverrideDll), OverrideDll);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
			Address = storage.GetValue<EndPoint>(nameof(Address));
			DllPath = storage.GetValue<string>(nameof(DllPath));
			ApiLogsPath = storage.GetValue<string>(nameof(ApiLogsPath));
			ApiLogLevel = storage.GetValue<ApiLogLevels>(nameof(ApiLogLevel));
			IsHFT = storage.GetValue<bool>(nameof(IsHFT));
			MarketDataInterval = storage.GetValue<TimeSpan?>(nameof(MarketDataInterval));

			var proxy = storage.GetValue<SettingsStorage>("Proxy");
			if (proxy != null)
			{
				Proxy = new Proxy();
				((IPersistable)Proxy).Load(proxy);
			}

			//ShowChangePasswordWindowOnConnect = storage.GetValue<bool>("ChangePasswordOnConnect");

			MicexRegisters = storage.GetValue(nameof(MicexRegisters), true);
			OverrideDll = storage.GetValue<bool>(nameof(OverrideDll));

			base.Load(storage);
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