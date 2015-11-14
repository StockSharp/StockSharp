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

		private ApiLogLevels _apiLogLevel = ApiLogLevels.Standard;

		/// <summary>
		/// Уровень логирования коннектора. По умолчанию <see cref="ApiLogLevels.Standard"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str3541Key)]
		[PropertyOrder(2)]
		public ApiLogLevels ApiLogLevel
		{
			get { return _apiLogLevel; }
			set
			{
				_apiLogLevel = value;
			}
		}

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

		private bool _micexRegisters = true;

		/// <summary>
		/// Передавать ли данные для фондового рынка.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3543Key)]
		[DescriptionLoc(LocalizedStrings.Str3544Key)]
		[PropertyOrder(3)]
		public bool MicexRegisters
		{
			get { return _micexRegisters; }
			set { _micexRegisters = value; }
		}

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

		private string _apiLogsPath = Path.Combine(Directory.GetCurrentDirectory(), "StockSharp.Transaq", "Logs");

		/// <summary>
		/// Путь к логам коннектора.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str3332Key)]
		[DescriptionLoc(LocalizedStrings.Str3548Key)]
		[PropertyOrder(7)]
		[Editor(typeof(FolderBrowserEditor), typeof(FolderBrowserEditor))]
		public string ApiLogsPath
		{
			get { return _apiLogsPath; }
			set { _apiLogsPath = value; }
		}

		private bool _overrideDll = true;

		/// <summary>
		/// Перезаписать файл библиотеки из ресурсов. По-умолчанию файл будет перезаписан.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.OverrideKey)]
		[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
		[PropertyOrder(8)]
		public bool OverrideDll
		{
			get { return _overrideDll; }
			set { _overrideDll = value; }
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return !Login.IsEmpty() && !Password.IsEmpty(); }
		}

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
			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("DllPath", DllPath);
			storage.SetValue("ApiLogsPath", ApiLogsPath);
			storage.SetValue("ApiLogLevel", ApiLogLevel.To<string>());
			storage.SetValue("IsHFT", IsHFT);
			storage.SetValue("MarketDataInterval", MarketDataInterval);

			storage.SetValue("HasProxy", Proxy != null);

			if (Proxy != null)
			{
				storage.SetValue("ProxyAddress", Proxy.Address.To<string>());
				storage.SetValue("ProxyLogin", Proxy.Login);
				storage.SetValue("ProxyPassword", Proxy.Password);
				storage.SetValue("ProxyType", Proxy.Type.To<string>());
			}

			// не нужно сохранять это свойство, так как иначе окно смены пароля будет показывать каждый раз
			//storage.SetValue("ChangePasswordOnConnect", ShowChangePasswordWindowOnConnect);

			storage.SetValue("MicexRegisters", MicexRegisters);
			storage.SetValue("OverrideDll", OverrideDll);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
			Address = storage.GetValue<EndPoint>("Address");
			DllPath = storage.GetValue<string>("DllPath");
			ApiLogsPath = storage.GetValue<string>("ApiLogsPath");
			ApiLogLevel = storage.GetValue<ApiLogLevels>("ApiLogLevel");
			IsHFT = storage.GetValue<bool>("IsHFT");
			MarketDataInterval = storage.GetValue<TimeSpan?>("MarketDataInterval");

			if (storage.GetValue<bool>("HasProxy"))
			{
				Proxy = new Proxy
				{
					Address = storage.GetValue<EndPoint>("ProxyAddress"),
					Login = storage.GetValue<string>("ProxyLogin"),
					Password = storage.GetValue<string>("ProxyPassword"),
					Type = storage.GetValue<ProxyTypes>("ProxyType"),
				};
			}

			//ShowChangePasswordWindowOnConnect = storage.GetValue<bool>("ChangePasswordOnConnect");

			MicexRegisters = storage.GetValue("MicexRegisters", true);
			OverrideDll = storage.GetValue<bool>("OverrideDll");

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