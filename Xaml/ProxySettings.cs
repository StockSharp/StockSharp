#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: ProxySettings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Proxy-server settings.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str1435Key)]
	[ExpandableObject]
	public class ProxySettings : IPersistable
	{
		/// <summary>
		/// To use proxy server.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.Str1437Key)]
		[DescriptionLoc(LocalizedStrings.Str1438Key)]
		[PropertyOrder(1)]
		public bool UseProxy { get; set; }

		private EndPoint _address = "127.0.0.1:8080".To<EndPoint>();

		/// <summary>
		/// Proxy server address.
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
					throw new ArgumentNullException(nameof(value));

				_address = value;
			}
		}

		/// <summary>
		/// Use for local addresses.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.Str1441Key)]
		[DescriptionLoc(LocalizedStrings.Str1442Key)]
		[PropertyOrder(5)]
		public bool ByPassOnLocal { get; set; }

		/// <summary>
		/// Use login and password authorization.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.AuthorizationKey)]
		[DescriptionLoc(LocalizedStrings.Str1444Key)]
		[PropertyOrder(8)]
		public bool UseCredentials { get; set; }

		/// <summary>
		/// Login.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(10)]
		public string Login { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1436Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(15)]
		public SecureString Password { get; set; }

		/// <summary>
		/// To set proxy settings for the application.
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
		/// To get proxy settings.
		/// </summary>
		/// <returns>Proxy-server settings.</returns>
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
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(Address), Address.To<string>());
			storage.SetValue(nameof(ByPassOnLocal), ByPassOnLocal);
			storage.SetValue(nameof(UseCredentials), UseCredentials);
			storage.SetValue(nameof(UseProxy), UseProxy);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
			Address = storage.GetValue(nameof(Address), Address);
			ByPassOnLocal = storage.GetValue<bool>(nameof(ByPassOnLocal));
			UseCredentials = storage.GetValue<bool>(nameof(UseCredentials));
			UseProxy = storage.GetValue<bool>(nameof(UseProxy));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return UseProxy ? Address.ToString() : string.Empty;
		}
	}
}