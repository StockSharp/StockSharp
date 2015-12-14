#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Transaq
File: Proxy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq
{
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	/// <summary>
	/// Прокси.
	/// </summary>
	public class Proxy
	{
		/// <summary>
		/// Тип протокола, который использует прокси.
		/// </summary>
		public ProxyTypes Type { get; set; }

		/// <summary>
		/// Адрес прокси.
		/// </summary>
		public EndPoint Address { get; set; }

		/// <summary>
		/// Логин (если прокси требует авторизацию).
		/// </summary>
		public string Login { get; set; }

		private SecureString _password;

		/// <summary>
		/// Пароль (если прокси требует авторизацию).
		/// </summary>
		public string Password
		{
			get { return _password.To<string>(); }
			set { _password = value.To<SecureString>(); }
		}
	}

	/// <summary>
	/// Типы протоколов прокси.
	/// </summary>
	public enum ProxyTypes
	{
		/// <summary>
		/// SOCKS 4.
		/// </summary>
		[EnumDisplayName("SOCKS 4")]
		Socks4,

		/// <summary>
		/// SOCKS 5.
		/// </summary>
		[EnumDisplayName("SOCKS 5")]
		Socks5,

		/// <summary>
		/// HHTP Proxy.
		/// </summary>
		[EnumDisplayName("HTTP")]
		Http
	}
}