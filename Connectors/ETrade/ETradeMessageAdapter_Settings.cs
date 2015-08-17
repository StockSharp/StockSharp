namespace StockSharp.ETrade
{
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.ETrade.Native;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayName("E*TRADE")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "E*TRADE")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class ETradeMessageAdapter
	{
		#region properties

		/// <summary>
		/// Ключ.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3304Key)]
		[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
		[PropertyOrder(1)]
		public string ConsumerKey { get; set; }

		/// <summary>
		/// Секрет.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3306Key)]
		[DescriptionLoc(LocalizedStrings.Str3307Key)]
		[PropertyOrder(2)]
		public SecureString ConsumerSecret { get; set; }

		/// <summary>
		/// Демо режим.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.DemoKey)]
		[DescriptionLoc(LocalizedStrings.Str3369Key)]
		[PropertyOrder(3)]
		public bool Sandbox { get; set; }

		/// <summary>
		/// OAuth access token. Нужен для восстановления соединения по упрощенной процедуре.
		/// Сохраненный AccessToken может быть использован до полуночи по EST.
		/// </summary>
		[Browsable(false)]
		public OAuthToken AccessToken { get; set; }

		///// <summary>
		///// Для режима Sandbox. Список инструментов, которые будут переданы в событии <see cref="IConnector.NewSecurities"/>.
		///// </summary>
		//[Browsable(false)]
		//public Security[] SandboxSecurities
		//{
		//	get { return _client.SandboxSecurities; }
		//	set { _client.SandboxSecurities = value; }
		//}

		/// <summary>
		/// Код верификации, полученный пользователем в браузере после подтверждения разрешения на работу приложения.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3370Key)]
		[DescriptionLoc(LocalizedStrings.Str3371Key)]
		[PropertyOrder(4)]
		public string VerificationCode { get; set; }

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return !ConsumerKey.IsEmpty() && !ConsumerSecret.IsEmpty(); }
		}

		#endregion

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			ConsumerKey = storage.GetValue<string>("ConsumerKey");
			ConsumerSecret = storage.GetValue<SecureString>("ConsumerSecret");
			Sandbox = storage.GetValue<bool>("Sandbox");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("ConsumerKey", ConsumerKey);
			storage.SetValue("ConsumerSecret", ConsumerSecret);
			storage.SetValue("Sandbox", Sandbox);
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return ConsumerKey.IsEmpty() ? string.Empty : "ConsumerKey = {0}".Put(ConsumerKey);
		}
	}
}
