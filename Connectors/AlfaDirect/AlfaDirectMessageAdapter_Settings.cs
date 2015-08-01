namespace StockSharp.AlfaDirect
{
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayName("AlfaDirect")]
	[CategoryLoc(LocalizedStrings.RussiaKey)]
	[DescriptionLoc(LocalizedStrings.Str2260Key)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	[TargetPlatform(Languages.Russian, Platforms.x86)]
	partial class AlfaDirectMessageAdapter
	{
		/// <summary>
		/// Имя пользователя в терминале Альфа-Директ.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.Str2261Key)]
		[PropertyOrder(1)]
		public string Login { set; get; }

		/// <summary>
		/// Пароль для входа в терминал.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.Str2262Key)]
		[PropertyOrder(2)]
		public SecureString Password { set; get; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get
			{
				if (Login.IsEmpty())
					return true;
				else
					return !Password.IsEmpty();
			}
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Login = storage.GetValue<string>("Login");
			Password = storage.GetValue<SecureString>("Password");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Login", Login);
			storage.SetValue("Password", Password);
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Login.IsEmpty() ? string.Empty : LocalizedStrings.Str2263Params.Put(Login);
		}
	}
}