namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий список контейнеров для сессий, с которыми оперирует агрегирующий контейнер для сессии.
	/// </summary>
	public interface IInnerSessionHolderList : INotifyList<IMessageSessionHolder>, ISynchronizedCollection<IMessageSessionHolder>
	{
		/// <summary>
		/// Внутренние контейнеры для сессии, отсортированные по скорости работы.
		/// </summary>
		IEnumerable<IMessageSessionHolder> SortedSessionHolders { get; }

		/// <summary>
		/// Добавить контейнер для сессии.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		/// <param name="priority">Приоритет</param>
		void Add(IMessageSessionHolder sessionHolder, int priority);

		/// <summary>
		/// Индексатор, через который задаются приоритеты скорости на внутренние контейнер для сессии.
		/// </summary>
		/// <param name="sessionHolder">Внутренний контейнер для сессии.</param>
		/// <returns>Приоритет контейнер для сессии. Если задается значение -1, то контейнер для сессии считается выключенным.</returns>
		int this[IMessageSessionHolder sessionHolder] { get; set; }
	}

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	public class BasketSessionHolder : MessageSessionHolder
	{
		private sealed class SessionHolderList : CachedSynchronizedList<IMessageSessionHolder>, IInnerSessionHolderList
		{
			private readonly CachedSynchronizedDictionary<IMessageSessionHolder, int> _enables = new CachedSynchronizedDictionary<IMessageSessionHolder, int>();

			public IEnumerable<IMessageSessionHolder> SortedSessionHolders
			{
				get { return Cache.Where(t => this[t] != -1).OrderBy(t => this[t]); }
			}

			public void Add(IMessageSessionHolder sessionHolder, int priority)
			{
				Add(sessionHolder);
				this[sessionHolder] = priority;
			}

			public int this[IMessageSessionHolder sessionHolder]
			{
				get { return _enables.TryGetValue2(sessionHolder) ?? -1; }
				set
				{
					if (value < -1)
						throw new ArgumentOutOfRangeException();

					if (!Contains(sessionHolder))
						Add(sessionHolder);

					_enables[sessionHolder] = value;
				}
			}
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		public override bool IsValid
		{
			get { return InnerSessions.All(s => s.IsValid); }
		}

		private readonly SessionHolderList _innerSession = new SessionHolderList();

		/// <summary>
		/// Контейнеры для сессий, которыми оперирует агрегатор.
		/// </summary>
		public IInnerSessionHolderList InnerSessions
		{
			get { return _innerSession; }
		}

		/// <summary>
		/// Являются ли подключения адаптеров независимыми друг от друга.
		/// </summary>
		public override bool IsAdaptersIndependent
		{
			get { return true; }
		}

		/// <summary>
		/// Портфели, которые используются для отправки транзакций.
		/// </summary>
		public IDictionary<string, IMessageSessionHolder> Portfolios { get; private set; }

		//TODO временно для корректной инициализации MessageProcessor-ов
		internal Dictionary<IMessageSessionHolder, AdaptersHolder> Adapters { get; private set; }

		/// <summary>
		/// Создать <see cref="BasketSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public BasketSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Adapters = new Dictionary<IMessageSessionHolder, AdaptersHolder>();
			Portfolios = new SynchronizedDictionary<string, IMessageSessionHolder>(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			InnerSessions.Clear();
			Portfolios.Clear();

			storage
				.GetValue<IEnumerable<SettingsStorage>>("InnerSessions")
				.ForEach(s =>
				{
					var sessionHolder = s.GetValue<Type>("type").CreateInstanceArgs<IMessageSessionHolder>(new object[] { TransactionIdGenerator });
					sessionHolder.Load(s.GetValue<SettingsStorage>("settings"));

					InnerSessions.Add(sessionHolder, s.GetValue<int>("Priority"));

					var portfolios = s.GetValue<IEnumerable<string>>("Portfolios");
					if (portfolios != null)
					{
						foreach (var portfolio in portfolios)
							Portfolios[portfolio] = sessionHolder;
					}
				});

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("InnerSessions", InnerSessions.Select(s =>
			{
				var settings = s.SaveEntire(true);
				settings.SetValue("Priority", InnerSessions[s]);
				settings.SetValue("Portfolios", Portfolios.Where(p => p.Value == s).Select(p => p.Key).ToArray());
				return settings;
			}).ToArray());

			base.Save(storage);
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new BasketMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new BasketMessageAdapter(MessageAdapterTypes.MarketData, this);
		}
	}
}