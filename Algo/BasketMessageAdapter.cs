namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс, описывающий список адаптеров к торговым системам, с которыми оперирует агрегатор.
	/// </summary>
	public interface IInnerAdapterList : ISynchronizedCollection<IMessageAdapter>
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
	public class BasketMessageAdapter : MessageAdapter<BasketSessionHolder>
	{
		private sealed class InnerAdapterList : CachedSynchronizedList<IMessageAdapter>, IInnerAdapterList
		{
			private readonly Dictionary<IMessageAdapter, int> _enables = new Dictionary<IMessageAdapter, int>();
			private readonly BasketMessageAdapter _parent;

			public InnerAdapterList(BasketMessageAdapter parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			public IEnumerable<IMessageAdapter> SortedAdapters
			{
				get { return Cache.Where(t => this[t] != -1).OrderBy(t => this[t]); }
			}

			protected override bool OnAdding(IMessageAdapter item)
			{
				Subscribe(item);
				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, IMessageAdapter item)
			{
				Subscribe(item);
				return base.OnInserting(index, item);
			}

			protected override bool OnRemoving(IMessageAdapter item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				lock (SyncRoot)
					ForEach(UnSubscribe);

				return base.OnClearing();
			}

			private void Subscribe(IMessageAdapter adapter, int priority = 0)
			{
				if (adapter == null)
					throw new ArgumentNullException("adapter");

				//adapter.DoIf<IMessageAdapter, BaseLogSource>(bt => bt.Parent = _parent);

				adapter.NewOutMessage += message => _parent.OnInnerAdapterNewMessage(adapter, message);

				_enables.Add(adapter, priority);
			}

			public void UnSubscribe(IMessageAdapter adapter)
			{
				if (adapter == null)
					throw new ArgumentNullException("adapter");

				_enables.Remove(adapter);

				//adapter.Parent = null;
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

		private readonly SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes>, RefPair<IEnumerator<IMessageAdapter>, bool>> _subscriptionQueue = new SynchronizedDictionary<Tuple<SecurityId, MarketDataTypes>, RefPair<IEnumerator<IMessageAdapter>, bool>>();
		private readonly SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>> _adapterStates = new SynchronizedDictionary<IMessageAdapter, RefPair<bool, Exception>>();

		private readonly InnerAdapterList _innerAdapters;

		/// <summary>
		/// Адаптеры, с которыми оперирует агрегатор.
		/// </summary>
		public IInnerAdapterList InnerAdapters
		{
			get { return _innerAdapters; }
		}

		/// <summary>
		/// Создать <see cref="BasketMessageAdapter"/>.
		/// </summary>
		/// <param name="type">Тип адаптера.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public BasketMessageAdapter(MessageAdapterTypes type, BasketSessionHolder sessionHolder)
			: base(type, sessionHolder)
		{
			_innerAdapters = new InnerAdapterList(this);
			//SessionHolder.SetChilds(_innerAdapters);
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
					CreateInnerAdapters();
					GetSortedAdapters().ForEach(a => a.SendInMessage(message.Clone()));
					break;

				case MessageTypes.Disconnect:
				case MessageTypes.SecurityLookup:
				case MessageTypes.PortfolioLookup:
					GetConnectedAdapters().ForEach(a => a.SendInMessage(message.Clone()));
					break;

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderPairReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var orderMessage = (OrderMessage)message;
					var sessionHolder = SessionHolder.Portfolios.TryGetValue(orderMessage.PortfolioName);

					var adapter = InnerAdapters.FirstOrDefault(a => a.SessionHolder == sessionHolder);

					if (adapter != null)
					{
						adapter.SendInMessage(orderMessage);
					}
					else
					{
						var orderError = orderMessage.ToExecutionMessage();
						orderError.Error = new InvalidOperationException(LocalizedStrings.Str623Params.Put(orderMessage.PortfolioName));
						orderError.OrderState = OrderStates.Failed;
						SendOutMessage(orderError);
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					
					switch (mdMsg.DataType)
					{
						case MarketDataTypes.News:
							GetSortedAdapters().ForEach(a => a.SendInMessage(mdMsg.Clone()));
							break;

						case MarketDataTypes.Level1:
						case MarketDataTypes.MarketDepth:
						case MarketDataTypes.Trades:
						case MarketDataTypes.OrderLog:
						case MarketDataTypes.CandleTimeFrame:
						case MarketDataTypes.CandleTick:
						case MarketDataTypes.CandleVolume:
						case MarketDataTypes.CandleRange:
						case MarketDataTypes.CandlePnF:
						case MarketDataTypes.CandleRenko:
						{
							if (mdMsg.IsSubscribe)
								ProcessSubscribeAction(mdMsg);
							else
								ProcessUnSubscribeAction(mdMsg);

							break;
						}

						default:
							RaiseMarketDataMessage(mdMsg, new InvalidOperationException(LocalizedStrings.Str624Params.Put(mdMsg.DataType)));
							break;
					}
					
					break;
				}
			}
		}

		/// <summary>
		/// Обработчик события <see cref="IMessageChannel.NewOutMessage"/> вложенного адаптера.
		/// </summary>
		/// <param name="innerAdapter">Вложенный адаптер.</param>
		/// <param name="message">Сообщение.</param>
		protected virtual void OnInnerAdapterNewMessage(IMessageAdapter innerAdapter, Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
					ProcessInnerAdapterConnectMessage(innerAdapter, (ConnectMessage)message);
					return;

				case MessageTypes.Disconnect:
					ProcessInnerAdapterDisconnectMessage(innerAdapter, (DisconnectMessage)message);
					return;

				case MessageTypes.MarketData:
					ProcessInnerAdapterMarketDataMessage(innerAdapter, (MarketDataMessage)message);
					return;

				case MessageTypes.Portfolio:
					SetPortfolioSessionHolder(((PortfolioMessage)message).PortfolioName, innerAdapter.SessionHolder);
					break;

				case MessageTypes.PortfolioChange:
					SetPortfolioSessionHolder(((PortfolioChangeMessage)message).PortfolioName, innerAdapter.SessionHolder);
					break;
			}

			SendOutMessage(message);
		}

		private void SetPortfolioSessionHolder(string portfolio, IMessageSessionHolder sessionHolder)
		{
			if (!SessionHolder.Portfolios.ContainsKey(portfolio))
				SessionHolder.Portfolios[portfolio] = sessionHolder;
		}

		#region Connect/disconnect

		private bool _canRaiseConnected = true;
		private bool _canRaiseDisconnected;
		
		private void ProcessInnerAdapterConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message)
		{
			if(message.Error != null)
				SessionHolder.AddErrorLog(LocalizedStrings.Str625Params, innerAdapter.GetType().Name, message.Error);

			Exception error = null;

			var canProcess = _innerAdapters.SyncGet(c =>
			{
				var connected = message.Error == null;

				_adapterStates[innerAdapter] = new RefPair<bool, Exception>(connected, message.Error);

				if (_canRaiseConnected && connected)
				{
					_canRaiseConnected = false;
					_canRaiseDisconnected = true;

					ResetConnectionErrors();

					return true;
				}

				if (_adapterStates.Count == c.Count)
				{
					// произошла ошибка и нет ни одного подключенного коннектора
					if (_adapterStates.Values.All(p => !p.First))
					{
						error = new AggregateException(LocalizedStrings.Str626, _adapterStates.Values.Select(p => p.Second));
						return true;
					}

					ResetConnectionErrors();
				}				

				return false;
			});

			if (canProcess)
				SendOutMessage(new ConnectMessage { Error = error });
		}

		private void ProcessInnerAdapterDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message)
		{
			if (message.Error != null)
				SessionHolder.AddErrorLog(LocalizedStrings.Str627Params, innerAdapter.GetType().Name, message.Error);

			Exception error = null;

			var canProcess = _innerAdapters.SyncGet(c =>
			{
				_adapterStates[innerAdapter] = new RefPair<bool, Exception>(false, message.Error);

				if (_canRaiseDisconnected && _adapterStates.Values.All(p => !p.First))
				{
					_canRaiseConnected = true;
					_canRaiseDisconnected = false;

					var errors = _adapterStates.Values.Where(p => p.Second != null).Select(p => p.Second).ToArray();

					if (errors.Length != 0)
					{
						error = new AggregateException(LocalizedStrings.Str628, errors);
						ResetConnectionErrors();
					}

					DisposeInnerAdapters();

					return true;
				}

				return false;
			});

			if (canProcess)
				SendOutMessage(new DisconnectMessage { Error = error });
		}

		private void ResetConnectionErrors()
		{
			// сбрасываем возможные ошибки отключения/отключения
			foreach (var state in _adapterStates.Values)
				state.Second = null;
		}

		#endregion

		#region Subscribe/UnSubscribe data

		private void ProcessSubscribeAction(MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var key = Tuple.Create(message.SecurityId, message.DataType);

			if (_subscriptionQueue.ContainsKey(key))
				return;

			var enumerator = GetConnectedAdapters().ToArray().Cast<IMessageAdapter>().GetEnumerator();

			_subscriptionQueue.Add(key, new RefPair<IEnumerator<IMessageAdapter>, bool>(enumerator, false));

			ProcessSubscriptionAction(enumerator, message);
		}

		private void ProcessUnSubscribeAction(MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			lock (_subscriptionQueue.SyncRoot)
			{
				var tuple = _subscriptionQueue.TryGetValue(Tuple.Create(message.SecurityId, message.DataType));
				if (tuple != null)
				{
					tuple.Second = true;
					return;
				}
			}

			GetSortedAdapters().ForEach(a => a.SendInMessage(message.Clone()));
		}

		private void ProcessSubscriptionAction(IEnumerator<IMessageAdapter> enumerator, MarketDataMessage message)
		{
			if (enumerator.MoveNext())
				enumerator.Current.SendInMessage(message.Clone());
			else
				RaiseSubscriptionFailed(message, new ArgumentException(LocalizedStrings.Str629Params.Put(message.SecurityId), "message"));
		}

		private void ProcessInnerAdapterMarketDataMessage(IMessageAdapter adapter, MarketDataMessage message)
		{
			var key = Tuple.Create(message.SecurityId, message.DataType);
			
			var tuple = _subscriptionQueue.TryGetValue(key);
			var cancel = tuple != null && tuple.Second;

			if (message.Error == null)
			{
				SessionHolder.AddDebugLog(LocalizedStrings.Str630Params, message.SecurityId, adapter);

				_subscriptionQueue.Remove(key);

				RaiseMarketDataMessage(message, null);

				if (!cancel)
					return;

				//в процессе подписки пользователь отменил ее - надо отписаться от получения данных
				var cancelMessage = (MarketDataMessage)message.Clone();
				cancelMessage.IsSubscribe = false;
				SendInMessage(cancelMessage);
			}
			else
			{
				SessionHolder.AddDebugLog(LocalizedStrings.Str631Params, adapter, message.SecurityId, message.DataType, message.Error);

				if (cancel)
					RaiseSubscriptionFailed(message, new InvalidOperationException(LocalizedStrings.SubscriptionProcessCancelled));
				else if (tuple != null)
					ProcessSubscriptionAction(tuple.First, message);
				else
					RaiseSubscriptionFailed(message, new InvalidOperationException(LocalizedStrings.Str633Params.Put(message.SecurityId, message.DataType)));
			}
		}

		private void RaiseMarketDataMessage(MarketDataMessage request, Exception error)
		{
			var reply = (MarketDataMessage)request.Clone();
			reply.OriginalTransactionId = request.TransactionId;
			reply.Error = error;
			SendOutMessage(reply);
		}

		private void RaiseSubscriptionFailed(MarketDataMessage message, Exception error)
		{
			_subscriptionQueue.Remove(Tuple.Create(message.SecurityId, message.DataType));

			SessionHolder.AddDebugLog(LocalizedStrings.Str634Params, message.SecurityId, message.DataType, error);

			RaiseMarketDataMessage(message, error);
		}

		#endregion

		/// <summary>
		/// Получить адаптеры <see cref="IInnerAdapterList.SortedAdapters"/>, отсортированные в зависимости от заданного приоритета. По-умолчанию сортировка отсутствует.
		/// </summary>
		/// <returns>Отсортированные адаптеры.</returns>
		protected virtual IEnumerable<IMessageAdapter> GetSortedAdapters()
		{
			return _innerAdapters.SortedAdapters;
		}

		private IEnumerable<IMessageAdapter> GetConnectedAdapters()
		{
			return _innerAdapters
				.SortedAdapters
				.Where(a =>
				{
					var pair = _adapterStates.TryGetValue(a);
					return pair != null && pair.First;
				});
		}

		/// <summary>
		/// Создать адаптеры для <see cref="MessageAdapter{TSessionHolder}.SessionHolder"/>.
		/// </summary>
		protected virtual void CreateInnerAdapters()
		{
			foreach (var session in SessionHolder.InnerSessions)
			{
				var adapterHolder = SessionHolder.Adapters.TryGetValue(session) 
					?? new AdaptersHolder(session, SessionHolder.AddErrorLog);

				if (session.IsMarketDataEnabled && Type == MessageAdapterTypes.MarketData)
					AddInnerAdapter(adapterHolder.MarketDataAdapter, SessionHolder.InnerSessions[session]);

				if (session.IsTransactionEnabled && Type == MessageAdapterTypes.Transaction)
					AddInnerAdapter(adapterHolder.TransactionAdapter, SessionHolder.InnerSessions[session]);

				SessionHolder.Adapters[session] = adapterHolder;
			}
		}

		/// <summary>
		/// Добавить адаптер.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		/// <param name="priority">Приоритет.</param>
		protected void AddInnerAdapter(IMessageAdapter adapter, int priority)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			InnerAdapters.Add(adapter);
			InnerAdapters[adapter] = priority;
		}

		/// <summary>
		/// Удалить адаптеры.
		/// </summary>
		protected virtual void DisposeInnerAdapters()
		{
			foreach (var adapter in _innerAdapters.Cache)
			{
				lock (_innerAdapters.SyncRoot)
					_innerAdapters.UnSubscribe(adapter);

				var holder = SessionHolder.Adapters.TryGetValue(adapter.SessionHolder);

				if (holder != null && holder.TryDispose(Type == MessageAdapterTypes.Transaction))
					SessionHolder.Adapters.Remove(adapter.SessionHolder);

				adapter.Dispose();
			}

			_innerAdapters.Clear();
			_adapterStates.Clear();
		}

		/// <summary>
		/// Освободить ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			DisposeInnerAdapters();

			base.DisposeManaged();
		}
	}
}