#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: EmailLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Logging
{
	using System.Net;
	using System.Net.Mail;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The logger sending data to the email by Yandex SMTP Server.
	/// </summary>
	public class YandexEmailLogListener : EmailLogListener
	{
		/// <summary>
		/// SMTP server adress in format url/ip:port
		/// </summary>
		public EndPoint ServerAdress { get; set; }

		/// <summary>
		/// Username for SMTP server login. Email account login. If mailservice for domain name delegated to Yandex need to write entire Email box address. For example 'customer@customerdomain.com'
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Password for SMTP server login (Email account password).
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Create YandexEmailLogListener
		/// </summary>
		/// <param name="username"><see cref="Username"/></param>
		/// <param name="pass"><see cref="Password"/></param>
		/// <param name="from">From EMail Address</param>
		/// <param name="to">Destination EMail Address</param>
		public YandexEmailLogListener(string username, string pass, string from, string to)
		{
			ServerAdress = "smtp.yandex.ru:587".To<EndPoint>();
			From = from;
			To = to;
			Username = username;
			Password = pass;
		}

		/// <summary>
		/// To create the email client.
		/// </summary>
		/// <returns>The email client.</returns>
		protected override SmtpClient CreateClient()
		{
			var client = new SmtpClient
			{
				Host = ServerAdress.GetHost(),
				Port = ServerAdress.GetPort(),
				UseDefaultCredentials = false,
				Credentials = new NetworkCredential(Username, Password),
				EnableSsl = true,
				DeliveryMethod = SmtpDeliveryMethod.Network
			};

			return client;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			From = storage.GetValue<string>(nameof(From));
			To = storage.GetValue<string>(nameof(To));
			Username = storage.GetValue<string>(nameof(Username));
			Password = storage.GetValue<string>(nameof(Password));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(From), From);
			storage.SetValue(nameof(To), To);
			storage.SetValue(nameof(Username), Username);
			storage.SetValue(nameof(Password), Password);
		}
	}
}
