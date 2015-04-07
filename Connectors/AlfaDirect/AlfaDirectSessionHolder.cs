namespace StockSharp.AlfaDirect
{
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("AlfaDirect")]
	[CategoryLoc(LocalizedStrings.Str1769Key)]
	[DescriptionLoc(LocalizedStrings.Str2260Key)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	[TargetPlatform(Languages.Russian, Platforms.x86)]
	public class AlfaDirectSessionHolder : MessageSessionHolder
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
		/// Создать <see cref="AlfaDirectSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public AlfaDirectSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			SecurityClassInfo.Add("FORTS", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("INDEX", RefTuple.Create(SecurityTypes.Index, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("INDEX2", RefTuple.Create(SecurityTypes.Index, "INDEX"));
			SecurityClassInfo.Add("MICEX_SHR_T", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("RTS_STANDARD", RefTuple.Create(SecurityTypes.Stock, ExchangeBoard.Forts.Code));

			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new AlfaOrderCondition();
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