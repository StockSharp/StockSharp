namespace StockSharp.Btce
{
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("BTC-e")]
	[CategoryLoc(LocalizedStrings.Str3302Key)]
	[DescriptionLoc(LocalizedStrings.Str3342Key)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	public class BtceSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Создать <see cref="BtceSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public BtceSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		/// <summary>
		/// Ключ.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3304Key)]
		[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
		[PropertyOrder(1)]
		public SecureString Key { get; set; }

		/// <summary>
		/// Секрет.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3306Key)]
		[DescriptionLoc(LocalizedStrings.Str3307Key)]
		[PropertyOrder(2)]
		public SecureString Secret { get; set; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get
			{
				if (IsTransactionEnabled)
					return !Key.IsEmpty() && !Secret.IsEmpty();
				else
					return true;
			}
		}

		/// <summary>
		/// Объединять обработчики входящих сообщений для адаптеров.
		/// </summary>
		[Browsable(false)]
		public override bool JoinInProcessors
		{
			get { return false; }
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Key", Key);
			storage.SetValue("Secret", Secret);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Key = storage.GetValue<SecureString>("Key");
			Secret = storage.GetValue<SecureString>("Secret");
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str3304 + " = " + Key;
		}
	}
}