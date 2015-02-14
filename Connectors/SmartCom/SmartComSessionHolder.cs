namespace StockSharp.SmartCom
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.SmartCom.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("SmartCOM")]
	[CategoryLoc(LocalizedStrings.Str1769Key)]
	[DescriptionLoc(LocalizedStrings.Str1857Key)]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	[TargetPlatform(Languages.Russian)]
	public class SmartComSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Версия SmartCOM API. По-умолчанию равна <see cref="SmartComVersions.V3"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str1859Key)]
		[DescriptionLoc(LocalizedStrings.Str1860Key)]
		[PropertyOrder(2)]
		public SmartComVersions Version
		{
			get { return _version; }
			set
			{
				_version = value;
				VersionChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Логин.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(2)]
		public string Login { get; set; }

		/// <summary>
		/// Пароль.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(3)]
		public SecureString Password { get; set; }

		private EndPoint _address = SmartComAddresses.Matrix;

		/// <summary>
		/// Адрес сервера. Значение по-умолчанию равно <see cref="SmartComAddresses.Matrix"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.ServerAddressKey)]
		[DescriptionLoc(LocalizedStrings.ServerAddressKey, true)]
		[PropertyOrder(1)]
		[Editor(typeof(SmartComEndPointEditor), typeof(SmartComEndPointEditor))]
		public EndPoint Address
		{
			get { return _address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_address = value;
			}
		}

		/// <summary>
		/// Настройки конфигурации клиентской части SmartCOM 3.x.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str1862Key)]
		[DescriptionLoc(LocalizedStrings.Str1863Key)]
		[PropertyOrder(3)]
		public string ClientSettings { get; set; }

		/// <summary>
		/// Настройки конфигурации серверной части SmartCOM 3.x.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str1864Key)]
		[DescriptionLoc(LocalizedStrings.Str1865Key)]
		[PropertyOrder(4)]
		public string ServerSettings { get; set; }

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new SmartComMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new SmartComMessageAdapter(MessageAdapterTypes.MarketData, this);
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return !Login.IsEmpty() && !Password.IsEmpty() && Address != null; }
		}

		/// <summary>
		/// Создать <see cref="SmartComSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public SmartComSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Version = SmartComVersions.V3;

			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;

			SecurityClassInfo.Add("OPT", new RefPair<SecurityTypes, string>(SecurityTypes.Option, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("OPTM", new RefPair<SecurityTypes, string>(SecurityTypes.Option, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("FUT", new RefPair<SecurityTypes, string>(SecurityTypes.Future, ExchangeBoard.Forts.Code));
		}

		private ISmartComWrapper _session;
		private SmartComVersions _version;

		[Browsable(false)]
		internal ISmartComWrapper Session
		{
			get { return _session; }
			set
			{
				if (_session != null)
					UnInitialize.SafeInvoke();

				_session = value;

				if (_session != null)
					Initialize.SafeInvoke();
			}
		}

		internal event Action Initialize;
		internal event Action UnInitialize;
		internal event Action VersionChanged;

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new SmartComOrderCondition();
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
			storage.SetValue("Address", Address.To<string>());
			storage.SetValue("Version", (int)Version);
			storage.SetValue("ClientSettings", ClientSettings);
			storage.SetValue("ServerSettings", ClientSettings);
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
			Address = storage.GetValue<EndPoint>("Address");
			Version = (SmartComVersions)storage.GetValue("Version", 2);
			ClientSettings = storage.GetValue<string>("ClientSettings");
			ServerSettings = storage.GetValue<string>("ServerSettings");
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str1866Params.Put(Login, Address);
		}
	}
}