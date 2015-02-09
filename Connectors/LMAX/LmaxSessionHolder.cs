namespace StockSharp.LMAX
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Security;

	using Com.Lmax.Api;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("LMAX")]
	[Category("Forex")]
	[DescriptionLoc(LocalizedStrings.Str3387Key)]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	public class LmaxSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Логин.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(1)]
		public string Login { get; set; }

		/// <summary>
		/// Пароль.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(2)]
		public SecureString Password { get; set; }

		/// <summary>
		/// Подключаться ли к демо торгам вместо сервера с реальной торговлей.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.DemoKey)]
		[DescriptionLoc(LocalizedStrings.Str3388Key)]
		[PropertyOrder(1)]
		public bool IsDemo { get; set; }

		/// <summary>
		/// Загружать ли инструменты из архива с сайта LMAX. По-умолчанию выключено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2135Key)]
		[DescriptionLoc(LocalizedStrings.Str3389Key)]
		[PropertyOrder(2)]
		public bool IsDownloadSecurityFromSite { get; set; }

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new LmaxMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new LmaxMessageAdapter(MessageAdapterTypes.MarketData, this);
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return !Login.IsEmpty() && !Password.IsEmpty(); }
		}

		/// <summary>
		/// Создать <see cref="LmaxSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public LmaxSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		private ISession _session;

		[Browsable(false)]
		internal ISession Session
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

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new LmaxOrderCondition();
		}

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromTicks(1),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromDays(1)
		});

		/// <summary>
		/// Доступные тайм-фреймы.
		/// </summary>
		[Browsable(false)]
		public static IEnumerable<TimeSpan> TimeFrames
		{
			get { return _timeFrames; }
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
			storage.SetValue("IsDemo", IsDemo);
			storage.SetValue("IsDownloadSecurityFromSite", IsDownloadSecurityFromSite);
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
			IsDemo = storage.GetValue<bool>("IsDemo");
			IsDownloadSecurityFromSite = storage.GetValue<bool>("IsDownloadSecurityFromSite");
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str3390Params.Put(Login, IsDemo);
		}
	}
}