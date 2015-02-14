namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class Connector
	{
		private readonly CachedSynchronizedDictionary<IMessageProcessor, ISmartPointer> _processorPointers = new CachedSynchronizedDictionary<IMessageProcessor, ISmartPointer>();
		private readonly CachedSynchronizedDictionary<IMessageSessionHolder, ISmartPointer> _sessionHolderPointers = new CachedSynchronizedDictionary<IMessageSessionHolder, ISmartPointer>();

		private readonly Dictionary<Security, OrderLogMarketDepthBuilder> _olBuilders = new Dictionary<Security, OrderLogMarketDepthBuilder>();

		private bool _joinIn;
		private bool _joinOut;

		private bool _isDisposing;

		private bool IsDisposeAdapters(Message message)
		{
			if (!_isDisposing)
				return false;

			if (message.Type == MessageTypes.Disconnect)
				return true;
			else if (message.Type == MessageTypes.Connect && ((ConnectMessage)message).Error != null)
				return true;

			return false;
		}

		private void IncRefSession(IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			_sessionHolderPointers.SafeAdd(adapter.SessionHolder, key =>
			{
				if (key.Parent == null)
					key.Parent = this;

				return new SmartPointer<IMessageSessionHolder>(key, h =>
				{
					if (!_isDisposing)
						return;

					if (h.Parent == this)
						h.Parent = null;

					_sessionHolderPointers.Remove(h);

					h.Dispose();
				});
			});
		}

		private void DecRefSession(IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			var pointer = _sessionHolderPointers.TryGetValue(adapter.SessionHolder);
			if (pointer != null)
				pointer.DecRef();
		}

		private readonly SyncObject _transactionAdapterSync = new SyncObject();
		private IMessageAdapter _transactionAdapter;

		/// <summary>
		/// Адаптер для транзакций.
		/// </summary>
		public IMessageAdapter TransactionAdapter
		{
			get { return _transactionAdapter; }
			set
			{
				lock (_transactionAdapterSync)
				{
					if (_transactionAdapter == value)
						return;

					if (_transactionAdapter != null)
					{
						var managedAdapter = _transactionAdapter as ManagedMessageAdapter;
						if (managedAdapter != null && MarketDataAdapter != null)
							MarketDataAdapter.NewOutMessage -= managedAdapter.ProcessMessage;

						_transactionAdapter.NewOutMessage -= TransactionAdapterOnNewOutMessage;
						_transactionAdapter.Dispose();

						ReConnectionSettings.ConnectionSettings.AdapterSettings = new MessageAdapterReConnectionSettings();

						DecRefSession(_transactionAdapter);

						if (_transactionAdapter.InMessageProcessor != null)
						{
							DecRefProcessor(_transactionAdapter.InMessageProcessor);
							_transactionAdapter.InMessageProcessor = null;
						}

						if (_transactionAdapter.OutMessageProcessor != null)
						{
							DecRefProcessor(_transactionAdapter.OutMessageProcessor);
							_transactionAdapter.OutMessageProcessor = null;
						}
					}

					_transactionAdapter = value;
					TrySetMarketDataIndependent();

					if (_transactionAdapter == null)
						return;

					if (CalculateMessages)
					{
						var managedAdapter = new ManagedMessageAdapter(value);

						if (MarketDataAdapter != null)
							MarketDataAdapter.NewOutMessage += managedAdapter.ProcessMessage;

						_transactionAdapter = managedAdapter;
					}

					IncRefSession(_transactionAdapter);

					_transactionAdapter.NewOutMessage += TransactionAdapterOnNewOutMessage;

					if (_joinIn)
						TransactionAdapter.InMessageProcessor = MarketDataAdapter.InMessageProcessor;

					if (_joinOut)
						TransactionAdapter.OutMessageProcessor = MarketDataAdapter.OutMessageProcessor;

					ReConnectionSettings.ConnectionSettings.AdapterSettings = TransactionAdapter.SessionHolder.ReConnectionSettings;
				}
			}
		}

		private void TransactionAdapterOnNewOutMessage(Message message)
		{
			OnProcessMessage(message, MessageAdapterTypes.Transaction, MessageDirections.Out);

			if (IsDisposeAdapters(message))
				TransactionAdapter = null;
		}

		private readonly SyncObject _marketDataAdapterSync = new SyncObject();
		private IMessageAdapter _marketDataAdapter;

		/// <summary>
		/// Адаптер для маркет-данных.
		/// </summary>
		public IMessageAdapter MarketDataAdapter
		{
			get { return _marketDataAdapter; }
			set
			{
				lock (_marketDataAdapterSync)
				{
					if (_marketDataAdapter == value)
						return;

					var mangedAdapter = TransactionAdapter as ManagedMessageAdapter;

					if (_marketDataAdapter != null)
					{
						if (mangedAdapter != null)
							_marketDataAdapter.NewOutMessage -= mangedAdapter.ProcessMessage;

						_marketDataAdapter.NewOutMessage -= MarketDataAdapterOnNewOutMessage;
						_marketDataAdapter.Dispose();

						ReConnectionSettings.ExportSettings.AdapterSettings = new MessageAdapterReConnectionSettings();

						DecRefSession(_marketDataAdapter);

						if (_marketDataAdapter.InMessageProcessor != null)
						{
							DecRefProcessor(_marketDataAdapter.InMessageProcessor);
							_marketDataAdapter.InMessageProcessor = null;
						}

						if (_marketDataAdapter.OutMessageProcessor != null)
						{
							DecRefProcessor(_marketDataAdapter.OutMessageProcessor);
							_marketDataAdapter.OutMessageProcessor = null;
						}
					}

					_marketDataAdapter = value;
					TrySetMarketDataIndependent();

					if (_marketDataAdapter == null)
						return;

					if (mangedAdapter != null)
						_marketDataAdapter.NewOutMessage += mangedAdapter.ProcessMessage;

					IncRefSession(_marketDataAdapter);

					_marketDataAdapter.NewOutMessage += MarketDataAdapterOnNewOutMessage;

					if (_joinIn)
						MarketDataAdapter.InMessageProcessor = TransactionAdapter.InMessageProcessor;

					if (_joinOut)
						MarketDataAdapter.OutMessageProcessor = TransactionAdapter.OutMessageProcessor;

					ReConnectionSettings.ExportSettings.AdapterSettings = MarketDataAdapter.SessionHolder.ReConnectionSettings;
				}
			}
		}

		private void MarketDataAdapterOnNewOutMessage(Message message)
		{
			OnProcessMessage(message, MessageAdapterTypes.MarketData, MessageDirections.Out);

			if (IsDisposeAdapters(message))
				MarketDataAdapter = null;
		}

		private void DecRefProcessor(IMessageProcessor processor)
		{
			var ptr = _processorPointers.TryGetValue(processor);

			if (ptr != null)
				ptr.DecRef();
		}

		/// <summary>
		/// Проинициализировать обработчик сообщений для адаптеров <see cref="TransactionAdapter"/> и <see cref="MarketDataAdapter"/>.
		/// </summary>
		/// <param name="direction">Направление, определяющее тип обработчика.</param>
		/// <param name="isTransaction">Нужно ли проинициализировать <see cref="TransactionAdapter"/>.</param>
		/// <param name="isMarketData">Нужно ли проинициализировать <see cref="MarketDataAdapter"/>.</param>
		/// <param name="defaultProcessor">Обработчик сообщений по-умолчанию. Если не задан, то будет создан автоматически.</param>
		public void ApplyMessageProcessor(MessageDirections direction, bool isTransaction, bool isMarketData, IMessageProcessor defaultProcessor = null)
		{
			var processor = new MessageProcessorPool(defaultProcessor ?? new MessageProcessor("Processor '{0}' ({1})".Put(GetType().Name.Replace("Trader", string.Empty), direction), RaiseProcessDataError));
			ISmartPointer pointer = new SmartPointer<IMessageProcessor>(processor, p =>
			{
				if (!_isDisposing)
					return;

				p.Stop();
				_processorPointers.Remove(p);
			});

			_processorPointers[processor] = pointer;

			switch (direction)
			{
				case MessageDirections.In:
					_joinIn = true;

					if (isTransaction)
					{
						if (TransactionAdapter.InMessageProcessor != null)
						{
							DecRefProcessor(TransactionAdapter.InMessageProcessor);
							TransactionAdapter.InMessageProcessor = null;
						}

						pointer.IncRef();
						TransactionAdapter.InMessageProcessor = processor;
					}

					if (isMarketData)
					{
						if (MarketDataAdapter.InMessageProcessor != null)
						{
							DecRefProcessor(MarketDataAdapter.InMessageProcessor);
							MarketDataAdapter.InMessageProcessor = null;
						}

						pointer.IncRef();
						MarketDataAdapter.InMessageProcessor = processor;
					}

					break;
				case MessageDirections.Out:
					_joinOut = true;

					if (isTransaction)
					{
						if (TransactionAdapter.OutMessageProcessor != null)
						{
							DecRefProcessor(TransactionAdapter.OutMessageProcessor);
							TransactionAdapter.OutMessageProcessor = null;
						}

						pointer.IncRef();
						TransactionAdapter.OutMessageProcessor = processor;
					}

					if (isMarketData)
					{
						if (MarketDataAdapter.OutMessageProcessor != null)
						{
							DecRefProcessor(MarketDataAdapter.OutMessageProcessor);
							MarketDataAdapter.OutMessageProcessor = null;
						}

						pointer.IncRef();
						MarketDataAdapter.OutMessageProcessor = processor;
					}

					break;
				default:
					_processorPointers.Remove(processor);
					throw new ArgumentOutOfRangeException("direction");
			}
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="adapterType">Тип адаптера, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected virtual void OnProcessMessage(Message message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			if (!(message.Type == MessageTypes.Time && direction == MessageDirections.Out))
				this.AddDebugLog("BP:{0}", message);

			_currentTime = message.LocalTime;
			ProcessTimeInterval();

			RaiseNewMessage(message, direction);

			try
			{
				switch (message.Type)
				{
					case MessageTypes.QuoteChange:
						ProcessSecurityAction((QuoteChangeMessage)message, m => m.SecurityId, ProcessQuotesMessage, true);
						break;

					case MessageTypes.Board:
						ProcessBoardMessage((BoardMessage)message);
						break;

					case MessageTypes.Security:
						ProcessSecurityMessage((SecurityMessage)message);
						break;

					case MessageTypes.SecurityLookupResult:
						ProcessSecurityLookupResultMessage((SecurityLookupResultMessage)message);
						break;

					case MessageTypes.PortfolioLookupResult:
						ProcessPortfolioLookupResultMessage((PortfolioLookupResultMessage)message);
						break;

					case MessageTypes.Level1Change:
						ProcessSecurityAction((Level1ChangeMessage)message, m => m.SecurityId, ProcessLevel1ChangeMessage, true);
						break;

					case MessageTypes.News:
						ProcessNewsMessage(null, (NewsMessage)message);
						break;

					case MessageTypes.Execution:
						ProcessExecutionMessage((ExecutionMessage)message);
						break;

					case MessageTypes.Portfolio:
						ProcessPortfolioMessage((PortfolioMessage)message);
						break;

					case MessageTypes.PortfolioChange:
						ProcessPortfolioChangeMessage((PortfolioChangeMessage)message);
						break;

					case MessageTypes.Position:
						ProcessSecurityAction((PositionMessage)message, m => m.SecurityId, ProcessPositionMessage);
						break;

					case MessageTypes.PositionChange:
						ProcessSecurityAction((PositionChangeMessage)message, m => m.SecurityId, ProcessPositionChangeMessage);
						break;

					//case MessageTypes.Time:
					//	var timeMsg = (TimeMessage)message;

					//	if (timeMsg.Shift != null)
					//		TimeShift = timeMsg.Shift;

					//	// TimeMessage могут пропускаться при наличии других месседжей, поэтому событие
					//	// MarketTimeChanged необходимо вызывать при обработке времени из любых месседжей.
					//	break;

					case MessageTypes.MarketData:
					{
						var mdMsg = (MarketDataMessage)message;

						//инструмент может быть не указан
						//и нет необходимости вызывать события MarketDataSubscriptionSucceeded/Failed
						if (mdMsg.SecurityId.IsDefault())
						{
							if (mdMsg.Error != null)
								RaiseProcessDataError(mdMsg.Error);

							break;
						}

						ProcessSecurityAction(mdMsg, m => m.SecurityId, (s, m) =>
						{
							if (m.IsSubscribe)
								ProcessSubscribeMarketDataMessage(s, m, direction);
							else
								ProcessUnSubscribeMarketDataMessage(s, m, direction);
						});

						break;
					}

					case MessageTypes.Error:
						var mdErrorMsg = (ErrorMessage)message;
						RaiseProcessDataError(mdErrorMsg.Error);
						break;

					case MessageTypes.Connect:
						ProcessConnectMessage((ConnectMessage)message, adapterType, direction);
						break;

					case MessageTypes.Disconnect:
						ProcessConnectMessage((DisconnectMessage)message, adapterType, direction);
						break;

					case MessageTypes.SecurityLookup:
					{
						var lookupMsg = (SecurityLookupMessage)message;
						_securityLookups.Add(lookupMsg.TransactionId, (SecurityLookupMessage)lookupMsg.Clone());
						MarketDataAdapter.SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
						break;
					}

					case MessageTypes.Session:
						ProcessSessionMessage((SessionMessage)message);
						break;

					// если адаптеры передают созданиют специфичные сообщения
					//default:
					//	throw new ArgumentOutOfRangeException("Тип сообщения {0} не поддерживается.".Put(message.Type));
				}
			}
			catch (Exception ex)
			{
				RaiseProcessDataError(new InvalidOperationException(LocalizedStrings.Str681Params.Put(message), ex));
			}

			if (message.Type != MessageTypes.Time && direction == MessageDirections.Out && adapterType == MessageAdapterTypes.MarketData)
				RaiseNewDataExported();
		}

		private void ProcessSecurityAction<TMessage>(TMessage message, Func<TMessage, SecurityId> getId, Action<Security, TMessage> action, bool ignoreIfNotExist = false, string associatedBoard = null)
			where TMessage : Message
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (getId == null)
				throw new ArgumentNullException("getId");

			var securityId = getId(message);

			var nativeSecurityId = securityId.Native;
			var securityCode = securityId.SecurityCode;
			var boardCode = associatedBoard ?? securityId.BoardCode;

			var isSecurityIdEmpty = securityCode.IsEmpty() || boardCode.IsEmpty();
			var isNativeIdNull = nativeSecurityId == null;

			Security security = null;

			if (isSecurityIdEmpty && isNativeIdNull)
			{
				// если указан код и тип инструмента, то пытаемся найти инструмент по ним
				if (!securityCode.IsEmpty() && securityId.SecurityType != null)
				{
					var securities = _securities.CachedValues.Where(s => s.Code.CompareIgnoreCase(securityCode)).ToArray();

					security = securities.FirstOrDefault(s => s.Type == securityId.SecurityType)
					           ?? securities.FirstOrDefault(s => s.Type == null);
				}
				else
					throw new ArgumentNullException("message", LocalizedStrings.Str682Params.Put(securityCode, securityId.SecurityType));
			}

			string stockSharpId = null;

			if (!isSecurityIdEmpty)
				stockSharpId = CreateSecurityId(securityCode, boardCode);

			lock (_suspendSync)
			{
				if (!isSecurityIdEmpty)
					security = _securities.TryGetValue(stockSharpId);

				if (security == null && !isNativeIdNull)
					security = _nativeIdSecurities.TryGetValue(nativeSecurityId);

				if (security == null && !isSecurityIdEmpty)
				{
					var secProvider = EntityFactory as ISecurityProvider;

					if (secProvider != null)
						security = secProvider.LookupById(stockSharpId);
				}

				if (security == null)
				{
					if (!ignoreIfNotExist)
					{
						var clone = message.Clone();
						_suspendedSecurityMessages.SafeAdd(securityId).Add(clone);
						_messageStat.Add(clone);
					}

					return;
				}
			}

			action(security, message);
		}

		private void ProcessConnectMessage(BaseConnectionMessage message, ConnectionStates state, ConnectionStates prevState, Action connected, Action disconnected, Action<Exception> connectionError, ReConnectionSettings.Settings settings)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (connected == null)
				throw new ArgumentNullException("connected");

			if (disconnected == null)
				throw new ArgumentNullException("disconnected");

			if (connectionError == null)
				throw new ArgumentNullException("connectionError");

			var isConnect = message is ConnectMessage;

			switch (state)
			{
				case ConnectionStates.Connecting:
				{
					if (isConnect)
					{
						if (message.Error == null)
						{
							connected();

							if (prevState == ConnectionStates.Failed)
								settings.RaiseRestored();
						}
						else
						{
							connectionError(message.Error);

							if (message.Error is TimeoutException)
								settings.RaiseTimeOut();
						}
					}
					else
					{
						connectionError(new InvalidOperationException(LocalizedStrings.Str683, message.Error));
					}

					return;
				}
				case ConnectionStates.Disconnecting:
				{
					if (isConnect)
					{
						connectionError(new InvalidOperationException(LocalizedStrings.Str684, message.Error));
					}
					else
					{
						if (message.Error == null)
							disconnected();
						else
							connectionError(message.Error);
					}

					return;
				}
				case ConnectionStates.Connected:
				{
					if (isConnect && message.Error != null)
					{
						connectionError(new InvalidOperationException(LocalizedStrings.Str683, message.Error));
						return;
					}

					break;
				}
				case ConnectionStates.Disconnected:
				case ConnectionStates.Failed:
				{
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			// так как соединение установлено, то выдаем ошибку через ProcessDataError, чтобы не сбрасывать состояние
			RaiseProcessDataError(new InvalidOperationException(LocalizedStrings.Str685Params.Put(state, message.GetType().Name), message.Error));
		}

		private void ProcessConnectMessage(BaseConnectionMessage message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			if (direction != MessageDirections.Out)
				throw new ArgumentOutOfRangeException("direction");

			switch (adapterType)
			{
				case MessageAdapterTypes.Transaction:
				{
					ProcessConnectMessage(message, ConnectionState, _prevConnectionState, RaiseConnected, RaiseDisconnected, RaiseConnectionError, ReConnectionSettings.ConnectionSettings);
					break;
				}
				case MessageAdapterTypes.MarketData:
				{
					ProcessConnectMessage(message, ExportState, _prevExportState, RaiseExportStarted, RaiseExportStopped, RaiseExportError, ReConnectionSettings.ExportSettings);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException("adapterType");
			}
		}

		private void ProcessSessionMessage(SessionMessage message)
		{
			var board = ExchangeBoard.GetOrCreateBoard(message.BoardCode);
			_sessionStates[board] = message.State;
			SessionStateChanged.SafeInvoke(board, message.State);
		}

		private static void ProcessBoardMessage(BoardMessage message)
		{
			ExchangeBoard.GetOrCreateBoard(message.Code, code => new ExchangeBoard
			{
				Code = code,
				WorkingTime = message.WorkingTime,
				IsSupportAtomicReRegister = message.IsSupportAtomicReRegister,
				IsSupportMarketOrders = message.IsSupportMarketOrders,
				ExpiryTime = message.ExpiryTime,
				Exchange = new Exchange { Name = message.ExchangeCode, TimeZoneInfo = message.TimeZoneInfo },
			});
		}

		private void ProcessUnSubscribeMarketDataMessage(Security security, MarketDataMessage message, MessageDirections direction)
		{
			if (direction != MessageDirections.In)
			{
				if (message.DataType != _filteredMarketDepth)
					return;

				_filteredMarketDepths.Remove(security);

				return;
			}
			
			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
					OnUnRegisterSecurity(security);
					break;

				case MarketDataTypes.MarketDepth:
					OnUnRegisterMarketDepth(security);
					break;

				case MarketDataTypes.Trades:
					OnUnRegisterTrades(security);
					break;

				case MarketDataTypes.OrderLog:
					OnUnRegisterOrderLog(security);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ProcessSubscribeMarketDataMessage(Security security, MarketDataMessage message, MessageDirections direction)
		{
			bool registered;

			if (direction == MessageDirections.In)
			{
				switch (message.DataType)
				{
					case MarketDataTypes.Level1:
						registered = OnRegisterSecurity(security);
						break;

					case MarketDataTypes.MarketDepth:
						registered = OnRegisterMarketDepth(security);
						break;

					case MarketDataTypes.Trades:
						registered = OnRegisterTrades(security);
						break;

					case MarketDataTypes.OrderLog:
						registered = OnRegisterOrderLog(security);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				if (message.DataType == _filteredMarketDepth)
				{
					GetFilteredMarketDepthInfo(security).Init(GetMarketDepth(security), _entityCache.GetOrders(security, OrderStates.Active));
					return;
				}

				registered = message.Error == null;
			}

			if (registered)
				RaiseMarketDataSubscriptionSucceeded(security, message);
			else
				RaiseMarketDataSubscriptionFailed(security, message.DataType, message.Error ?? new NotSupportedException(LocalizedStrings.ConnectionNotSupportSecurity.Put(security.Id)));
		}

		private void BindNativeSecurityId(SecurityId securityId)
		{
			var native = securityId.Native;
			var stocksharp = CreateSecurityId(securityId.SecurityCode, securityId.BoardCode);

			lock (_suspendSync)
			{
				var sec = _nativeIdSecurities.TryGetValue(native);

				if (sec == null)
					_nativeIdSecurities.Add(native, _securities[stocksharp]);
				else
				{
					if (!sec.Id.CompareIgnoreCase(stocksharp))
						throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(stocksharp, sec.Id, native));
				}
			}
		}

		private void ProcessSecurityMessage(SecurityMessage message, string boardCode = null)
		{
			var secId = CreateSecurityId(message.SecurityId.SecurityCode, boardCode ?? message.SecurityId.BoardCode);

			var security = GetSecurity(secId, s =>
			{
				if (!message.SecurityId.SecurityCode.IsEmpty())
					s.Code = message.SecurityId.SecurityCode;

				if (message.Currency != null)
					s.Currency = message.Currency;

				s.Board = ExchangeBoard.GetOrCreateBoard(boardCode ?? message.SecurityId.BoardCode);

				if (message.ExpiryDate != null)
					s.ExpiryDate = message.ExpiryDate;

				if (message.VolumeStep != 0)
					s.VolumeStep = message.VolumeStep;

				if (message.Multiplier != 0)
					s.Multiplier = message.Multiplier;

				if (message.PriceStep != 0)
					s.PriceStep = message.PriceStep;

				if (!message.Name.IsEmpty())
					s.Name = message.Name;

				if (!message.Class.IsEmpty())
					s.Class = message.Class;

				if (message.OptionType != null)
					s.OptionType = message.OptionType;

				if (message.Strike != 0)
					s.Strike = message.Strike;

				if (!message.BinaryOptionType.IsEmpty())
					s.BinaryOptionType = message.BinaryOptionType;

				if (message.SettlementDate != null)
					s.SettlementDate = message.SettlementDate;

				if (!message.ShortName.IsEmpty())
					s.ShortName = message.ShortName;

				if (message.SecurityType != null)
					s.Type = message.SecurityType.Value;

				if (!message.UnderlyingSecurityCode.IsEmpty())
					s.UnderlyingSecurityId = SecurityIdGenerator.GenerateId(message.UnderlyingSecurityCode, message.SecurityId.BoardCode);

				if (message.SecurityId.HasExternalId())
					s.ExternalId = message.SecurityId.ToExternalId();

				message.CopyExtensionInfo(s);

				//если для инструмента есть NativeId, то его надо  связать с инструментом до вызова NewSecurities,
				//т.к. в обработчике может выполняться подписка на маркет-данные где нужен nativeId.
				if (message.SecurityId.Native != null)
					BindNativeSecurityId(message.SecurityId);

				return true;
			});

			if (message.OriginalTransactionId != 0)
				_lookupResult.Add(security);

			if (message.SecurityId.Native != null)
				ProcessSuspendedSecurityMessages(message.SecurityId);

			//необходимо обработать отложенные сообщения не только по NativeId, но и по обычным идентификаторам S#
			var stocksharpId = message.SecurityId.Native == null
				? message.SecurityId
				: new SecurityId { SecurityCode = message.SecurityId.SecurityCode, BoardCode = boardCode ?? message.SecurityId.BoardCode };

			ProcessSuspendedSecurityMessages(stocksharpId);
		}

		private void ProcessSecurityLookupResultMessage(SecurityLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseProcessDataError(message.Error);

			var result = _lookupResult.CopyAndClear();

			if (result.Length == 0)
			{
				var criteria = _securityLookups.TryGetValue(message.OriginalTransactionId);

				if (criteria != null)
				{
					_securityLookups.Remove(message.OriginalTransactionId);
					result = this.FilterSecurities(criteria).ToArray();
				}
			}

			RaiseLookupSecuritiesResult(result);

			lock (_lookupQueue.SyncRoot)
			{
				if (_lookupQueue.Count == 0)
					return;

				//удаляем текущий запрос лукапа из очереди
				_lookupQueue.Dequeue();

				if (_lookupQueue.Count == 0)
					return;

				var nextCriteria = _lookupQueue.Peek();

				//если есть еще запросы, для которых нет инструментов, то отправляем следующий
				if (NeedLookupSecurities(nextCriteria.SecurityId))
					MarketDataAdapter.SendInMessage(nextCriteria);
				else
				{
					_securityLookups.Add(nextCriteria.TransactionId, (SecurityLookupMessage)nextCriteria.Clone());
					MarketDataAdapter.SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = nextCriteria.TransactionId });
				}
			}
		}

		private void ProcessPortfolioLookupResultMessage(PortfolioLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseProcessDataError(message.Error);

			var criteria = _portfolioLookups.TryGetValue(message.OriginalTransactionId);

			if (criteria == null)
				return;

			RaiseLookupPortfoliosResult(Portfolios.Where(pf => pf.Name.CompareIgnoreCase(criteria.PortfolioName)));
		}

		private void ProcessLevel1ChangeMessage(Security security, Level1ChangeMessage message)
		{
			if (UpdateSecurityByLevel1)
			{
				security.ApplyChanges(message);
				RaiseSecurityChanged(security);
			}

			var values = GetSecurityValues(security);

			lock (values.SyncRoot)
			{
				foreach (var change in message.Changes)
					values[(int)change.Key] = change.Value;	
			}

			RaiseValuesChanged(security, message.Changes, message.ServerTime, message.LocalTime);

			if (CalculateMessages)
				SlippageManager.ProcessMessage(message);
		}

		private void ProcessPortfolioMessage(PortfolioMessage message)
		{
			GetPortfolio(message.PortfolioName, p =>
			{
				p.Board = message.BoardCode.IsEmpty() ? null : ExchangeBoard.GetOrCreateBoard(message.BoardCode);

				if (message.Currency != null)
					p.Currency = message.Currency;

				if (message.State != null)
					p.State = message.State;

				message.CopyExtensionInfo(p);

				return true;
			});
		}

		private void ProcessPortfolioChangeMessage(PortfolioChangeMessage message)
		{
			GetPortfolio(message.PortfolioName, portfolio =>
			{
				if (!message.BoardCode.IsEmpty())
					portfolio.Board = ExchangeBoard.GetOrCreateBoard(message.BoardCode);

				portfolio.ApplyChanges(message);
				return true;
			});
		}

		private void ProcessPositionMessage(Security security, PositionMessage message)
		{
			var portfolio = GetPortfolio(message.PortfolioName);
			var position = GetPosition(portfolio, security, message.DepoName, message.LimitType, message.Description);

			message.CopyExtensionInfo(position);
		}

		private void ProcessPositionChangeMessage(Security security, PositionChangeMessage message)
		{
			var portfolio = GetPortfolio(message.PortfolioName);

			var valueInLots = message.Changes.TryGetValue(PositionChangeTypes.CurrentValueInLots);
			if (valueInLots != null)
			{
				if (!message.Changes.ContainsKey(PositionChangeTypes.CurrentValue))
				{
					var currValue = (decimal)valueInLots / security.VolumeStep;
					message.Add(PositionChangeTypes.CurrentValue, currValue);
				}

				message.Changes.Remove(PositionChangeTypes.CurrentValueInLots);
			}

			var position = GetPosition(portfolio, security, message.DepoName, message.LimitType, message.Description);
			position.ApplyChanges(message);

			RaisePositionsChanged(new[] { position });
		}

		private void ProcessNewsMessage(Security security, NewsMessage message)
		{
			if (security != null || message.SecurityId.SecurityCode.IsEmpty())
			{
				var news = _entityCache.ProcessNewsMessage(security, message);

				if (news.Item2)
					RaiseNewNews(news.Item1);
				else
					RaiseNewsChanged(news.Item1);
			}
			else
				ProcessSecurityAction(message, m => m.SecurityId, ProcessNewsMessage);
		}

		private void ProcessQuotesMessage(Security security, QuoteChangeMessage message)
		{
			if (MarketDepthsChanged != null)
			{
				var marketDepth = GetMarketDepth(security);

				message.ToMarketDepth(marketDepth, GetSecurity);

				if (_subscriptionManager.IsFilteredMarketDepthRegistered(security))
					GetFilteredMarketDepthInfo(security).Process(message);

				RaiseMarketDepthsChanged(new[] { marketDepth });
			}
			else
			{
				lock (_marketDepths.SyncRoot)
				{
					var info = _marketDepths.SafeAdd(security, key => new MarketDepthInfo(EntityFactory.CreateMarketDepth(security)));

					info.First.LocalTime = message.LocalTime;
					info.First.LastChangeTime = message.ServerTime;

					info.Second = message.Bids;
					info.Third = message.Asks;
				}
			}

			var bestBid = message.GetBestBid();
			var bestAsk = message.GetBestAsk();

			if (bestBid != null || bestAsk != null)
			{
				var values = GetSecurityValues(security);
				var changes = new List<KeyValuePair<Level1Fields, object>>(4);

				lock (values.SyncRoot)
				{
					if (bestBid != null)
					{
						values[(int)Level1Fields.BestBidPrice] = bestBid.Price;
						changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestBidPrice, bestBid.Price));

						if (bestBid.Volume != 0)
						{
							values[(int)Level1Fields.BestBidVolume] = bestBid.Volume;
							changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestBidVolume, bestBid.Volume));
						}
					}

					if (bestAsk != null)
					{
						values[(int)Level1Fields.BestAskPrice] = bestAsk.Price;
						changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestAskPrice, bestAsk.Price));

						if (bestAsk.Volume != 0)
						{
							values[(int)Level1Fields.BestAskVolume] = bestAsk.Volume;
							changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.BestAskVolume, bestAsk.Volume));
						}
					}
				}

				RaiseValuesChanged(security, changes, message.ServerTime, message.LocalTime);
			}

			if (UpdateSecurityLastQuotes)
			{
				security.BestBid = bestBid == null ? null : new Quote(security, bestBid.Price, bestBid.Volume, Sides.Buy);
				security.BestAsk = bestAsk == null ? null : new Quote(security, bestAsk.Price, bestAsk.Volume, Sides.Sell);
				security.LocalTime = message.LocalTime;
				security.LastChangeTime = message.ServerTime;

				RaiseSecurityChanged(security);

				// стаканы по ALL обновляют BestXXX по конкретным инструментам
				if (security.Board.Code == MarketDataAdapter.SessionHolder.AssociatedBoardCode)
				{
					var changedSecurities = new Dictionary<Security, RefPair<bool, bool>>();

					foreach (var bid in message.Bids)
					{
						if (bid.BoardCode.IsEmpty())
							continue;

						var innerSecurity = GetSecurity(new SecurityId
						{
							SecurityCode = security.Code,
							BoardCode = bid.BoardCode
						});

						var info = changedSecurities.SafeAdd(innerSecurity);

						if (info.First)
							continue;

						info.First = true;

						innerSecurity.BestBid = new Quote(innerSecurity, bid.Price, bid.Volume, Sides.Buy);
						innerSecurity.LocalTime = message.LocalTime;
						innerSecurity.LastChangeTime = message.ServerTime;
					}

					foreach (var ask in message.Asks)
					{
						if (ask.BoardCode.IsEmpty())
							continue;

						var innerSecurity = GetSecurity(new SecurityId
						{
							SecurityCode = security.Code,
							BoardCode = ask.BoardCode
						});

						var info = changedSecurities.SafeAdd(innerSecurity);

						if (info.Second)
							continue;

						info.Second = true;

						innerSecurity.BestAsk = new Quote(innerSecurity, ask.Price, ask.Volume, Sides.Sell);
						innerSecurity.LocalTime = message.LocalTime;
						innerSecurity.LastChangeTime = message.ServerTime;
					}

					RaiseSecuritiesChanged(changedSecurities.Keys);
				}
			}

			if (CalculateMessages)
				SlippageManager.ProcessMessage(message);
		}

		private void ProcessOrderLogMessage(Security security, ExecutionMessage message)
		{
			var trade = (message.TradeId != 0 || !message.TradeStringId.IsEmpty())
				? EntityFactory.CreateTrade(security, message.TradeId, message.TradeStringId)
				: null;

			var logItem = message.ToOrderLog(EntityFactory.CreateOrderLogItem(new Order { Security = security }, trade));
			//logItem.LocalTime = message.LocalTime;

			RaiseNewOrderLogItems(new[] { logItem });

			if (CreateDepthFromOrdersLog)
			{
				try
				{
					var builder = _olBuilders.SafeAdd(security, key => new OrderLogMarketDepthBuilder(new QuoteChangeMessage { SecurityId = message.SecurityId, IsSorted = true }));
					var updated = builder.Update(message);
					
					if (updated)
						ProcessQuotesMessage(security, builder.Depth);
				}
				catch (Exception ex)
				{
					// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
					// а только выводим сообщение в лог
					RaiseProcessDataError(ex);
				}
			}

			if (trade != null && CreateTradesFromOrdersLog)
			{
				var tuple = _entityCache.GetTrade(security, message.TradeId, message.TradeStringId, (id, stringId) =>
				{
					var t = trade.Clone();
					t.OrderDirection = message.Side.Invert();
					return t;
				});

				if (tuple.Item2)
					RaiseNewTrades(new[] { tuple.Item1 });
			}
		}

		private void ProcessTradeMessage(Security security, ExecutionMessage message)
		{
			var tuple = _entityCache.ProcessTradeMessage(security, message);

			if (tuple.Item2)
				RaiseNewTrades(new[] { tuple.Item1 });

			var values = GetSecurityValues(security);

			var changes = new List<KeyValuePair<Level1Fields, object>>(4)
			{
				new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeTime, message.ServerTime),
				new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradePrice, message.TradePrice)
			};

			lock (values.SyncRoot)
			{
				values[(int)Level1Fields.LastTradeTime] = message.ServerTime;
				values[(int)Level1Fields.LastTradePrice] = message.TradePrice;

				if (!message.IsSystem)
					values[(int)Level1Fields.IsSystem] = message.IsSystem;

				if (message.TradeId != 0)
				{
					values[(int)Level1Fields.LastTradeId] = message.TradeId;
					changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeId, message.TradeId));
				}

				if (message.Volume != 0)
				{
					values[(int)Level1Fields.Volume] = message.Volume;
					changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeVolume, message.Volume));
				}
			}

			RaiseValuesChanged(security, changes, message.ServerTime, message.LocalTime);

			if (!UpdateSecurityLastQuotes)
				return;

			security.LastTrade = tuple.Item1;
			security.LastChangeTime = tuple.Item1.Time;

			RaiseSecurityChanged(security);
		}

		private void ProcessOrderMessage(Security security, ExecutionMessage message)
		{
			if (message.OrderState != OrderStates.Failed)
			{
				var tuple = _entityCache.ProcessOrderMessage(security, message);

				var order = tuple.Item1;
				var isNew = tuple.Item2;
				var isChanged = tuple.Item3;

				if (message.OrderType == OrderTypes.Conditional && (message.DerivedOrderId != null || !message.DerivedOrderStringId.IsEmpty()))
				{
					var derivedOrder = _entityCache.GetOrder(security, 0L, message.DerivedOrderId ?? 0, message.DerivedOrderStringId);

					if (derivedOrder == null)
						_orderStopOrderAssociations.Add(Tuple.Create(message.DerivedOrderId ?? 0, message.DerivedOrderStringId), new RefPair<Order, Action<Order, Order>>(order, (s, o) => s.DerivedOrder = o));
					else
						order.DerivedOrder = derivedOrder;
				}

				TryProcessFilteredMarketDepth(security, message);

				if (isNew)
				{
					this.AddOrderInfoLog(order, "New order");

					if (order.Type == OrderTypes.Conditional)
						RaiseNewStopOrders(new[] { order });
					else
						RaiseNewOrders(new[] { order });
				}
				else if (isChanged)
				{
					this.AddOrderInfoLog(order, "Order changed");

					if (order.Type == OrderTypes.Conditional)
						RaiseStopOrdersChanged(new[] { order });
					else
						RaiseOrdersChanged(new[] { order });
				}

				if (order.Id != 0)
					ProcessMyTrades(order, order.Id, _nonOrderedByIdMyTrades);

				ProcessMyTrades(order, order.TransactionId, _nonOrderedByTransactionIdMyTrades);

				if (!order.StringId.IsEmpty())
					ProcessMyTrades(order, order.StringId, _nonOrderedByStringIdMyTrades);

				ProcessConditionOrders(order);
			}
			else
			{
				var tuple = _entityCache.ProcessOrderFailMessage(security, message);

				if (tuple == null)
					return;

				var fail = tuple.Item1;

				TryProcessFilteredMarketDepth(security, message);

				var isRegisterFail = (fail.Order.Id == 0 && fail.Order.StringId.IsEmpty()) || fail.Order.Status == OrderStatus.RejectedBySystem;

				this.AddErrorLog(() => (isRegisterFail ? "OrderFailed" : "OrderCancelFailed")
					+ Environment.NewLine + fail.Order + Environment.NewLine + fail.Error);

				var fails = new[] { fail };
				var isStop = fail.Order.Type == OrderTypes.Conditional;

				if (isRegisterFail)
				{
					_orderRegisterFails.Add(fail);

					if (isStop)
						RaiseStopOrdersRegisterFailed(fails);
					else
						RaiseOrdersRegisterFailed(fails);
				}
				else
				{
					_orderCancelFails.Add(fail);

					if (isStop)
						RaiseStopOrdersCancelFailed(fails);
					else
						RaiseOrdersCancelFailed(fails);
				}
			}
		}

		private void TryProcessFilteredMarketDepth(Security security, ExecutionMessage order)
		{
			var info = _filteredMarketDepths.TryGetValue(security);

			if (info != null)
				info.Process(order);
		}

		private void ProcessConditionOrders(Order order)
		{
			var changedStopOrders = new List<Order>();

			var key = Tuple.Create(order.Id, order.StringId);

			var collection = _orderStopOrderAssociations.TryGetValue(key);

			if (collection == null)
				return;

			foreach (var pair in collection)
			{
				pair.Second(pair.First, order);
				changedStopOrders.TryAdd(pair.First);
			}

			_orderStopOrderAssociations.Remove(key);

			if (changedStopOrders.Count > 0)
				RaiseStopOrdersChanged(changedStopOrders);
		}

		private void ProcessMyTradeMessage(Security security, ExecutionMessage message)
		{
			var tuple = _entityCache.ProcessMyTradeMessage(security, message);

			if (CalculateMessages && message.Slippage == null)
				message.Slippage = SlippageManager.ProcessMessage(message);

			if (tuple == null)
			{
				List<ExecutionMessage> nonOrderedMyTrades;

				if (message.OrderId != 0)
					nonOrderedMyTrades = _nonOrderedByIdMyTrades.SafeAdd(message.OrderId);
				else if (message.OriginalTransactionId != 0)
					nonOrderedMyTrades = _nonOrderedByTransactionIdMyTrades.SafeAdd(message.OriginalTransactionId);
				else
					nonOrderedMyTrades = _nonOrderedByStringIdMyTrades.SafeAdd(message.OrderStringId);

				nonOrderedMyTrades.Add((ExecutionMessage)message.Clone());

				return;
			}

			if (!tuple.Item2)
				return;

			RaiseNewMyTrades(new[] { tuple.Item1 });
		}

		private void ProcessExecutionMessage(ExecutionMessage message)
		{
			if (message.ExecutionType == null)
				throw new ArgumentException(LocalizedStrings.Str688Params.Put(message));

			Action<Security> handler = security =>
			{
				switch (message.ExecutionType)
				{
					case ExecutionTypes.Tick:
						ProcessTradeMessage(security, message);
						break;
					case ExecutionTypes.Order:
						ProcessOrderMessage(security, message);
						break;
					case ExecutionTypes.Trade:
						ProcessMyTradeMessage(security, message);
						break;
					case ExecutionTypes.OrderLog:
						ProcessOrderLogMessage(security, message);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			};

			switch (message.ExecutionType)
			{
				case ExecutionTypes.Order:
				case ExecutionTypes.Trade:
				{
					if (message.SecurityId.SecurityCode.IsEmpty() && message.SecurityId.Native == null)
					{
						var order = (
							_entityCache.GetOrderByTransactionId(message.OriginalTransactionId, false)
							??
							_entityCache.GetOrderByTransactionId(message.OriginalTransactionId, true)
						            ?? _entityCache.GetOrderById(message.OrderId));

						if (order == null)
							throw new InvalidOperationException(LocalizedStrings.Str689Params.Put(message.OrderId, message.OriginalTransactionId));

						handler(order.Security);
					}
					else
						ProcessSecurityAction(message, m => m.SecurityId, (s, m) => handler(s));

					break;
				}

				case ExecutionTypes.Tick:
				case ExecutionTypes.OrderLog:
				case null:
					ProcessSecurityAction(message, m => m.SecurityId, (s, m) => handler(s));
					break;
				
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ProcessSuspendedSecurityMessages(SecurityId securityId)
		{
			List<Message> msgs;

			lock (_suspendSync)
			{
				msgs = _suspendedSecurityMessages.TryGetValue(securityId);

				if (msgs != null)
					_suspendedSecurityMessages.Remove(securityId);

				// одновременно могут быть сообщения по полному идентификатору и по код + тип
				var pair = _suspendedSecurityMessages
					.FirstOrDefault(p => p.Key.SecurityCode.CompareIgnoreCase(securityId.SecurityCode) && (securityId.SecurityType == null || p.Key.SecurityType == securityId.SecurityType));

				if (pair.Value != null)
					_suspendedSecurityMessages.Remove(pair.Key);

				if (msgs != null)
				{
					if (pair.Value != null)
						msgs.AddRange(pair.Value);
				}
				else
					msgs = pair.Value;

				if (msgs == null)
					return;
			}

			foreach (var msg in msgs)
			{
				try
				{
					OnProcessMessage(msg, MessageAdapterTypes.MarketData, MessageDirections.Out);
					_messageStat.Remove(msg);
				}
				catch (Exception error)
				{
					RaiseProcessDataError(error);
				}
			}
		}
	}
}