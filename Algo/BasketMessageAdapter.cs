namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий список адаптеров к торговым системам, с которыми оперирует агрегатор.
	/// </summary>
	public interface IInnerAdapterList : ISynchronizedCollection<IMessageAdapter>, INotifyList<IMessageAdapter>
	{
		/// <summary>
		/// Внутренние адаптеры, отсортированные по скорости работы.
		/// </summary>
		IEnumerable<IMessageAdapter> SortedAdapters { get; }

		/// <summary>
		/// Индексатор, через который задаются приоритеты скорости (чем меньше значение, те быстрее адаптер) на внутренние адаптеры.
		/// </summary>
		/// <param name="adapter">Внутренний адаптер.</param>
		/// <returns>Приоритет адаптера. Если задается значение -1, то адаптер считается выключенным.</returns>
		int this[IMessageAdapter adapter] { get; set; }
	}

	/// <summary>
	/// Адаптер-агрегатор, позволяющий оперировать одновременно несколькими адаптерами, подключенных к разным торговым системам.
	/// </summary>
	public class BasketMessageAdapter : MessageAdapter
	{
		private sealed class InnerAdapterList : CachedSynchronizedList<IMessageAdapter>, IInnerAdapterList
		{
			private readonly Dictionary<IMessageAdapter, int> _enables = new Dictionary<IMessageAdapter, int>();

			public IEnumerable<IMessageAdapter> SortedAdapters
			{
				get { return Cache.Where(t => this[t] != -1).OrderBy(t => this[t]); }
			}

			protected override bool OnAdding(IMessageAdapter item)
			{
				_enables.Add(item, 0);
				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, IMessageAdapter item)
			{
				_enables.Add(item, 0);
				return base.OnInserting(index, item);
			}

			protected override bool OnRemoving(IMessageAdapter item)
			{
				_enables.Remove(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				_enables.Clear();
				return base.OnClearing();
			}

			public int this[IMessageAdapter adapter]
			{
				get
				{
					lock (SyncRoot)
						return _enables.TryGetValue2(adapter) ?? -1;
				}
				set
				{
					if (value < -1)
						throw new ArgumentOutOfRangeException();

					lock (SyncRoot)
					{
						if (!Contains(adapter))
							Add(adapter);

						_enables[adapter] = value;
						//_portfolioTraders.Clear();
					}
				}
			}
		}

		private readonly SynchronizedPairSet<Tuple<SecurityId, MarketDataTypes>, IEnumerator<IMessageAdapter>> _subscriptionQueue = new SynchronizedPairSet<Tuple<SecurityId, MarketDataTypes>, IEnumerator<IMessageAdapter>>();
		private readonly SynchronizedDictionary<long, Tuple<SecurityId, MarketDataTypes>> _subscriptionKeys = new SynchronizedDictionary<long, Tuple<SecurityId, MarketDataTypes>>();
		private readonly SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes>, IMessageAdapter> _subscriptions = new SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes>, IMessageAdapter>();
		//private readonly SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>> _adapterStates = new SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>>();
		private readonly SynchronizedDictionary<IMessageAdapter, IMessageAdapter> _hearbeatAdapters = new SynchronizedDictionary<IMessageAdapter, IMessageAdapter>();
		private readonly CachedSynchronizedDictionary<MessageTypes, CachedSynchronizedList<IMessageAdapter>> _connectedAdapters = new CachedSynchronizedDictionary<MessageTypes, CachedSynchronizedList<IMessageAdapter>>();
		private bool _isFirstConnect;
		private readonly InnerAdapterList _innerAdapters;

		/// <summary>
		/// Адаптеры, с которыми оперирует агрегатор.
		/// </summary>
		public IInnerAdapterList InnerAdapters
		{
			get { return _innerAdapters; }
		}

		/// <summary>
		/// Портфели, которые используются для отправки транзакций.
		/// </summary>
		public IDictionary<string, IMessageAdapter> Portfolios { get; private set; }

		/// <summary>
		/// Создать <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public BasketMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			_innerAdapters = new InnerAdapterList();
			Portfolios = new SynchronizedDictionary<string, IMessageAdapter>(StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Поддерживаемые типы сообщений, который может обработать адаптер.
		/// </summary>
		public override MessageTypes[] SupportedMessages
		{
			get { return GetSortedAdapters().SelectMany(a => a.SupportedMessages).ToArray(); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return GetSortedAdapters().Any(a => a.PortfolioLookupRequired); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="OrderStatusMessage"/> для получения списка заявок и собственных сделок.
		/// </summary>
		public override bool OrderStatusRequired
		{
			get { return GetSortedAdapters().Any(a => a.OrderStatusRequired); }
		}

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="SecurityLookupMessage"/> для получения списка инструментов.
		/// </summary>
		public override bool SecurityLookupRequired
		{
			get { return GetSortedAdapters().Any(a => a.SecurityLookupRequired); }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск портфелей.
		/// </summary>
		protected override bool IsSupportNativePortfolioLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если было успешно установлено подключение.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, <see langword="false"/>, если торговая система разорвала подключение.</returns>
		public override bool IsConnectionAlive()
		{
			throw new NotSupportedException();
		}

		private void ProcessReset(Message message)
		{
			_hearbeatAdapters.Values.ForEach(a =>
			{
				a.SendInMessage(message);
				a.Dispose();
			});

			_connectedAdapters.Clear();
			_hearbeatAdapters.Clear();
			_subscriptionQueue.Clear();
			_subscriptions.Clear();
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					ProcessReset(message);
					break;

				case MessageTypes.Connect:
					if (_isFirstConnect)
						_isFirstConnect = false;
					else
						ProcessReset(new ResetMessage());

					_hearbeatAdapters.AddRange(GetSortedAdapters().ToDictionary(a => a, a =>
					{
						var hearbeatAdapter = (IMessageAdapter)new HeartbeatAdapter(a);
						hearbeatAdapter.Parent = this;
						hearbeatAdapter.NewOutMessage += m => OnInnerAdapterNewMessage(a, m);
						return hearbeatAdapter;
					}));

					if (_hearbeatAdapters.Count == 0)
						throw new InvalidOperationException(LocalizedStrings.Str3650);

					_hearbeatAdapters.Values.ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.Disconnect:
					_connectedAdapters
						.CachedValues
						.SelectMany(c => c.Cache)
						.Distinct()
						.ForEach(a => a.SendInMessage(message));
					break;

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					ProcessPortfolioMessage(pfMsg.PortfolioName, pfMsg);
					break;
				}

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var ordMsg = (OrderMessage)message;
					ProcessPortfolioMessage(ordMsg.PortfolioName, ordMsg);
					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var ordMsg = (OrderPairReplaceMessage)message;
					ProcessPortfolioMessage(ordMsg.Message1.PortfolioName, ordMsg);
					break;
				}

				case MessageTypes.MarketData:
				{
					var adapters = _connectedAdapters.TryGetValue(message.Type);

					if (adapters == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					var mdMsg = (MarketDataMessage)message;
					
					switch (mdMsg.DataType)
					{
						case MarketDataTypes.News:
							adapters.Cache.ForEach(a => a.SendInMessage(mdMsg));
							break;

						default:
						{
							var key = Tuple.Create(mdMsg.SecurityId, mdMsg.DataType);

							if (mdMsg.IsSubscribe)
							{
								if (_subscriptionQueue.ContainsKey(key))
									return;

								var enumerator = adapters.Cache.Cast<IMessageAdapter>().GetEnumerator();

								_subscriptionQueue.Add(key, enumerator);

								if (mdMsg.TransactionId != 0)
									_subscriptionKeys.Add(mdMsg.TransactionId, key);

								ProcessSubscriptionAction(enumerator, mdMsg, mdMsg.TransactionId);
							}
							else
							{
								var adapter = _subscriptions.TryGetValue(key);

								if (adapter != null)
								{
									_subscriptions.Remove(key);
									adapter.SendInMessage(message);
								}
							}

							break;
						}
					}
					
					break;
				}

				default:
				{
					var adapters = _connectedAdapters.TryGetValue(message.Type);

					if (adapters == null)
						throw new InvalidOperationException(LocalizedStrings.Str629Params.Put(message.Type));

					adapters.Cache.ForEach(a => a.SendInMessage(message));
					break;
				}
			}
		}

		private void ProcessPortfolioMessage(string portfolioName, Message message)
		{
			var adapter = Portfolios.TryGetValue(portfolioName);

			if (adapter == null)
			{
				var adapters = _connectedAdapters.TryGetValue(message.Type);

				if (adapters == null || adapters.Count != 1)
					throw new InvalidOperationException(LocalizedStrings.Str623Params.Put(portfolioName));

				adapter = adapters.Cache.First();
			}
			else
				adapter = _hearbeatAdapters[adapter];

			adapter.SendInMessage(message);
		}

		/// <summary>
		/// Обработчик события <see cref="IMessageChannel.NewOutMessage"/> вложенного адаптера.
		/// </summary>
		/// <param name="innerAdapter">Вложенный адаптер.</param>
		/// <param name="message">Сообщение.</param>
		protected virtual void OnInnerAdapterNewMessage(IMessageAdapter innerAdapter, Message message)
		{
			if (message.IsBack)
			{
				message.IsBack = false;
				innerAdapter.SendInMessage(message);
				return;
			}

			message.Adapter = innerAdapter;

			switch (message.Type)
			{
				case MessageTypes.Connect:
					ProcessConnectMessage(innerAdapter, (ConnectMessage)message);
					return;

				case MessageTypes.Disconnect:
					ProcessDisconnectMessage(innerAdapter, (DisconnectMessage)message);
					return;

				case MessageTypes.MarketData:
					ProcessMarketDataMessage(innerAdapter, (MarketDataMessage)message);
					return;
			}

			SendOutMessage(message);
		}

		private void ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message)
		{
			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str625Params, innerAdapter.GetType().Name, message.Error);
			else
			{
				var adapter = _hearbeatAdapters[innerAdapter];

				foreach (var supportedMessage in adapter.SupportedMessages)
				{
					_connectedAdapters.SafeAdd(supportedMessage).Add(adapter);
				}
			}

			SendOutMessage(message);
		}

		private void ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message)
		{
			if (message.Error != null)
				this.AddErrorLog(LocalizedStrings.Str627Params, innerAdapter.GetType().Name, message.Error);

			SendOutMessage(message);
		}

		private void ProcessSubscriptionAction(IEnumerator<IMessageAdapter> enumerator, MarketDataMessage message, long originalTransactionId)
		{
			if (enumerator.MoveNext())
				enumerator.Current.SendInMessage(message);
			else
			{
				_subscriptionQueue.RemoveByValue(enumerator);
				_subscriptionKeys.Remove(originalTransactionId);

				RaiseMarketDataMessage(null, originalTransactionId, new ArgumentException(LocalizedStrings.Str629Params.Put(message.SecurityId + " " + message.DataType), "message"));
			}
		}

		private void ProcessMarketDataMessage(IMessageAdapter adapter, MarketDataMessage message)
		{
			var key = _subscriptionKeys.TryGetValue(message.OriginalTransactionId)
				?? Tuple.Create(message.SecurityId, message.DataType);
			
			var enumerator = _subscriptionQueue.TryGetValue(key);

			if (message.Error == null)
			{
				if (message.IsNotSupported)
				{
					if (enumerator != null)
						ProcessSubscriptionAction(enumerator, message, message.OriginalTransactionId);
					else
						RaiseMarketDataMessage(adapter, message.OriginalTransactionId, new InvalidOperationException(LocalizedStrings.Str633Params.Put(message.SecurityId, message.DataType)));

					return;
				}
				else
				{
					this.AddDebugLog(LocalizedStrings.Str630Params, message.SecurityId, adapter);
				}
			}

			_subscriptionQueue.Remove(key);
			_subscriptionKeys.Remove(message.OriginalTransactionId);

			RaiseMarketDataMessage(adapter, message.OriginalTransactionId, message.Error);
		}

		private void RaiseMarketDataMessage(IMessageAdapter adapter, long originalTransactionId, Exception error)
		{
			SendOutMessage(new MarketDataMessage
			{
				OriginalTransactionId = originalTransactionId,
				Error = error,
				Adapter = adapter,
			});
		}

		/// <summary>
		/// Получить адаптеры <see cref="IInnerAdapterList.SortedAdapters"/>, отсортированные в зависимости от заданного приоритета. По-умолчанию сортировка отсутствует.
		/// </summary>
		/// <returns>Отсортированные адаптеры.</returns>
		protected IEnumerable<IMessageAdapter> GetSortedAdapters()
		{
			return _innerAdapters.SortedAdapters;
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("InnerAdapters", _innerAdapters.Cache.Select(a =>
			{
				var s = new SettingsStorage();

				s.SetValue("Priority", _innerAdapters[a]);
				s.SetValue("Adapter", a.Save());

				return s;
			}).ToArray());

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			var index = 0;

			foreach (var s in storage.GetValue<IEnumerable<SettingsStorage>>("InnerAdapters"))
			{
				var adapter = _innerAdapters.Cache[index++];

				_innerAdapters[adapter] = s.GetValue<int>("Priority");
				adapter.Load(s.GetValue<SettingsStorage>("Adapter"));
			}

			base.Load(storage);
		}
	}
}