namespace StockSharp.Rss
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Rss.Xaml;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("RSS")]
	[DescriptionLoc(LocalizedStrings.Str3504Key)]
	[CategoryOrder(_rss, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	public class RssSessionHolder : MessageSessionHolder
	{
		private const string _rss = "RSS";

		/// <summary>
		/// Создать <see cref="RssSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public RssSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			IsMarketDataEnabled = true;
		}

		private Uri _address;

		/// <summary>
		/// Адрес RSS фида.
		/// </summary>
		[Category(_rss)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.Str3505Key)]
		[PropertyOrder(0)]
		[Editor(typeof(RssAddressEditor), typeof(RssAddressEditor))]
		public Uri Address
		{
			get { return _address; }
			set
			{
				_address = value;

				if (value == RssAddresses.Nyse)
				{
					CustomDateFormat = "ddd, dd MMM yyyy HH:mm:ss zzzz GMT";
				}
			}
		}

		/// <summary>
		/// Формат дат. Необходимо заполнить, если формат RSS потока отличается от ddd, dd MMM yyyy HH:mm:ss zzzz.
		/// </summary>
		[Category(_rss)]
		[DisplayNameLoc(LocalizedStrings.Str3506Key)]
		[DescriptionLoc(LocalizedStrings.Str3507Key)]
		[PropertyOrder(1)]
		public string CustomDateFormat { get; set; }

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return null;
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new RssMarketDataMessageAdapter(this);
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return Address != null; }
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Address = storage.GetValue<Uri>("Address");
			CustomDateFormat = storage.GetValue<string>("CustomDateFormat");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("CustomDateFormat", CustomDateFormat);
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Address == null ? string.Empty : Address.To<string>();
		}
	}
}