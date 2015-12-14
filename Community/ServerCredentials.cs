#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: ServerCredentials.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System.Security;

	using Ecng.Serialization;

	/// <summary>
	/// The class that contains a login and password to access the services http://stocksharp.com.
	/// </summary>
	public sealed class ServerCredentials : IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServerCredentials"/>.
		/// </summary>
		public ServerCredentials()
		{
			AutoLogon = true;
		}

		/// <summary>
		/// Login.
		/// </summary>
		public string Login { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		public SecureString Password { get; set; }

		/// <summary>
		/// Auto login.
		/// </summary>
		public bool AutoLogon { get; set; }

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
			AutoLogon = storage.GetValue<bool>("AutoLogon");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
			storage.SetValue("AutoLogon", AutoLogon);
		}
	}
}