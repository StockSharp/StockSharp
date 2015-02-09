namespace StockSharp.AlfaDirect
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.AlfaDirect.Native;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

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
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new AlfaDirectMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new AlfaDirectMessageAdapter(MessageAdapterTypes.MarketData, this);
		}

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
			SecurityClassInfo.Add("FORTS", new RefPair<SecurityTypes, string>(SecurityTypes.Stock, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("INDEX", new RefPair<SecurityTypes, string>(SecurityTypes.Index, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("INDEX2", new RefPair<SecurityTypes, string>(SecurityTypes.Index, "INDEX"));
			SecurityClassInfo.Add("MICEX_SHR_T", new RefPair<SecurityTypes, string>(SecurityTypes.Stock, ExchangeBoard.Micex.Code));
			SecurityClassInfo.Add("RTS_STANDARD", new RefPair<SecurityTypes, string>(SecurityTypes.Stock, ExchangeBoard.Forts.Code));

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

		internal string GetSecurityClass(SecurityTypes? secType, string boardName)
		{
			if (secType == null)
				return null;

			if(boardName == ExchangeBoard.Forts.Code)
				return secType == SecurityTypes.Stock ? "RTS_STANDARD" : "FORTS";

			var kv = SecurityClassInfo.FirstOrDefault(kv2 => kv2.Value.First == secType && kv2.Value.Second == boardName);

			if (!kv.IsDefault())
				return kv.Key;

			return null;
		}

		internal IAlfaSession GetSession(ILogReceiver logReceiver)
		{
			return new AlfaSession(this, logReceiver);
		}

		private int _userCounter;
		private AlfaWrapper _wrapper;

		[Browsable(false)]
		internal AlfaWrapper Wrapper
		{
			get { return _wrapper; }
			private set
			{
				if (_wrapper != null)
					UnInitialize.SafeInvoke();

				_wrapper = value;

				if (_wrapper != null)
					Initialize.SafeInvoke();
			}
		}

		internal event Action Initialize;
		internal event Action UnInitialize;

		internal interface IAlfaSession : IDisposable {}

		class AlfaSession : Disposable, IAlfaSession
		{
			readonly AlfaDirectSessionHolder _holder;

			public AlfaSession(AlfaDirectSessionHolder holder, ILogReceiver receiver)
			{
				_holder = holder;
				lock (_holder)
				{
					if (++_holder._userCounter == 1)
						_holder.Wrapper = new AlfaWrapper(_holder, receiver);
				}
			}

			protected override void DisposeManaged()
			{
				lock (_holder)
				{
					if (--_holder._userCounter > 0)
						return;

					var wrapper = _holder.Wrapper;

					_holder.Wrapper = null;
					wrapper.Dispose();
				}

				base.DisposeManaged();
			}
		}
	}
}