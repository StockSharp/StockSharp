#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: ServerCredentials.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System.Security;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	/// <summary>
	/// The class that contains a login and password to access the services https://stocksharp.com .
	/// </summary>
	public class ServerCredentials : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServerCredentials"/>.
		/// </summary>
		public ServerCredentials()
		{
		}

		private string _email;

		/// <summary>
		/// Email.
		/// </summary>
		public string Email
		{
			get => _email;
			set
			{
				_email = value;
				NotifyChanged(nameof(Email));
			}
		}

		private SecureString _password;

		/// <summary>
		/// Password.
		/// </summary>
		public SecureString Password
		{
			get => _password;
			set
			{
				_password = value;
				NotifyChanged(nameof(Password));
			}
		}

		private bool _autoLogon = true;

		/// <summary>
		/// Auto login.
		/// </summary>
		public bool AutoLogon
		{
			get => _autoLogon;
			set
			{
				_autoLogon = value;
				NotifyChanged(nameof(AutoLogon));
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Email = storage.GetValue<string>(nameof(Email));
			Password = storage.GetValue<SecureString>(nameof(Password));
			AutoLogon = storage.GetValue<bool>(nameof(AutoLogon));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Email), Email);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(AutoLogon), AutoLogon);
		}
	}
}