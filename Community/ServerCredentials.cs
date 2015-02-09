namespace StockSharp.Community
{
	using System.Security;

	using Ecng.Serialization;

	/// <summary>
	/// Класс, хранящих в себе логин и пароль для доступа к сервисам http://stocksharp.com
	/// </summary>
	public sealed class ServerCredentials : IPersistable
	{
		/// <summary>
		/// Создать <see cref="ServerCredentials"/>.
		/// </summary>
		public ServerCredentials()
		{
			AutoLogon = true;
		}

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login { get; set; }

		/// <summary>
		/// Пароль.
		/// </summary>
		public SecureString Password { get; set; }

		/// <summary>
		/// Входить автоматически.
		/// </summary>
		public bool AutoLogon { get; set; }

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
			AutoLogon = storage.GetValue<bool>("AutoLogon");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
			storage.SetValue("AutoLogon", AutoLogon);
		}
	}
}