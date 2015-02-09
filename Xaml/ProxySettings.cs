namespace StockSharp.Xaml
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Настройки прокси-сервера.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str1435Key)]
	[ExpandableObject]
	public class ProxySettings : IPersistable
	{
		/// <summary>
		/// Использовать прокси-сервера.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.Str1437Key)]
		[DescriptionLoc(LocalizedStrings.Str1438Key)]
		[PropertyOrder(1)]
		public bool UseProxy { get; set; }

		private EndPoint _address = "127.0.0.1:8080".To<EndPoint>();

		/// <summary>
		/// Адрес прокси-сервера.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.Str1440Key)]
		[PropertyOrder(2)]
		[Editor(typeof(EndPointEditor), typeof(EndPointEditor))]
		public EndPoint Address
		{
			get { return _address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_address = value;
			}
		}

		/// <summary>
		/// Использовать для локальных адресов.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.Str1441Key)]
		[DescriptionLoc(LocalizedStrings.Str1442Key)]
		[PropertyOrder(5)]
		public bool ByPassOnLocal { get; set; }

		/// <summary>
		/// Использовать авторизацию по логину и паролю.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.AuthorizationKey)]
		[DescriptionLoc(LocalizedStrings.Str1444Key)]
		[PropertyOrder(8)]
		public bool UseCredentials { get; set; }

		/// <summary>
		/// Логин.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(10)]
		public string Login { get; set; }

		/// <summary>
		/// Пароль.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(15)]
		public SecureString Password { get; set; }

		/// <summary>
		/// Установить настройки прокси-сервера для приложения.
		/// </summary>
		public void ApplyProxySettings()
		{
			WebRequest.DefaultWebProxy = !UseProxy
				? WebRequest.GetSystemWebProxy()
				: new WebProxy(Address.To<string>(), ByPassOnLocal)
				{
					UseDefaultCredentials = !UseCredentials,
					Credentials = UseCredentials ? new NetworkCredential(Login, Password) : null
				};
		}

		/// <summary>
		/// Получить настройки прокси-сервера.
		/// </summary>
		/// <returns>Настройки прокси-сервера.</returns>
		public static ProxySettings GetProxySettings()
		{
			var proxySettings = new ProxySettings();

			var proxy = (WebRequest.DefaultWebProxy ?? WebRequest.GetSystemWebProxy()) as WebProxy;

			if (proxy == null)
				return proxySettings;

			proxySettings.UseProxy = true;
			proxySettings.Address = "{0}:{1}".Put(proxy.Address.Host, proxy.Address.Port).To<EndPoint>();
			proxySettings.ByPassOnLocal = proxy.BypassProxyOnLocal;
			proxySettings.UseCredentials = !proxy.UseDefaultCredentials;

			if (proxy.UseDefaultCredentials)
				return proxySettings;

			var credentials = proxy.Credentials as NetworkCredential;

			if (credentials == null)
				return proxySettings;

			proxySettings.Login = credentials.UserName;
			proxySettings.Password = credentials.SecurePassword;

			return proxySettings;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("ByPassOnLocal", ByPassOnLocal);
			storage.SetValue("UseCredentails", UseCredentials);
			storage.SetValue("UseProxy", UseProxy);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
			Address = storage.GetValue("Address", Address);
			ByPassOnLocal = storage.GetValue<bool>("ByPassOnLocal");
			UseCredentials = storage.GetValue<bool>("UseCredentails");
			UseProxy = storage.GetValue<bool>("UseProxy");
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return UseProxy ? Address.ToString() : string.Empty;
		}
	}
}