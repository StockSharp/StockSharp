#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Connector_ProcessMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Latency;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Slippage;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class Connector
	{
		private sealed class QuoteChangeDepthBuilder
		{
			private readonly Dictionary<SecurityId, QuoteChangeMessage> _feeds = new Dictionary<SecurityId, QuoteChangeMessage>();

			private readonly string _securityCode;
			private readonly string _boardCode;

			public QuoteChangeDepthBuilder(string securityCode, string boardCode)
			{
				_securityCode = securityCode;
				_boardCode = boardCode;
			}

			public QuoteChangeMessage Process(QuoteChangeMessage message)
			{
				_feeds[message.SecurityId] = message;

				var bids = _feeds.SelectMany(f => f.Value.Bids).ToArray();
				var asks = _feeds.SelectMany(f => f.Value.Asks).ToArray();

				return new QuoteChangeMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = _securityCode,
						BoardCode = _boardCode
					},
					ServerTime = message.ServerTime,
					LocalTime = message.LocalTime,
					Bids = bids,
					Asks = asks
				};
			}
		}

		private sealed class Level1DepthBuilder
		{
			private readonly SecurityId _securityId;

			public bool HasDepth { get; set; }

			public Level1DepthBuilder(SecurityId securityId)
			{
				_securityId = securityId;
			}

			public QuoteChangeMessage Process(Level1ChangeMessage message)
			{
				if (HasDepth)
					return null;

				var bidPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice);
				var askPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice);

				if (bidPrice == null && askPrice == null)
					return null;

				var bidVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidVolume);
				var askVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskVolume);

				return new QuoteChangeMessage
				{
					SecurityId = _securityId,
					ServerTime = message.ServerTime,
					LocalTime = message.LocalTime,
					Bids = bidPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Buy, bidPrice.Value, bidVolume ?? 0) },
					Asks = askPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Sell, askPrice.Value, askVolume ?? 0) },
				};
			}
		}

		private readonly Dictionary<Security, IOrderLogMarketDepthBuilder> _olBuilders = new Dictionary<Security, IOrderLogMarketDepthBuilder>();
		private readonly CachedSynchronizedDictionary<IMessageAdapter, ConnectionStates> _adapterStates = new CachedSynchronizedDictionary<IMessageAdapter, ConnectionStates>();
		private readonly SynchronizedDictionary<SecurityId, Level1DepthBuilder> _level1DepthBuilders = new SynchronizedDictionary<SecurityId, Level1DepthBuilder>();
		private readonly SynchronizedDictionary<string, QuoteChangeDepthBuilder> _quoteChangeDepthBuilders = new SynchronizedDictionary<string, QuoteChangeDepthBuilder>(StringComparer.InvariantCultureIgnoreCase);

		private string AssociatedBoardCode => Adapter.AssociatedBoardCode;

		private bool IsAssociated(string boardCode)
		{
			return /*boardCode.IsEmpty() || */boardCode.CompareIgnoreCase(AssociatedBoardCode);
		}

		private void CreateAssociatedSecurityQuotes(QuoteChangeMessage quoteMsg)
		{
			if (!CreateAssociatedSecurity)
				return;

			if (quoteMsg.SecurityId.IsDefault())
				return;

			if (IsAssociated(quoteMsg.SecurityId.BoardCode))
				return;

			var builder = _quoteChangeDepthBuilders
				.SafeAdd(quoteMsg.SecurityId.SecurityCode, c => new QuoteChangeDepthBuilder(c, AssociatedBoardCode));

			ProcessSecurityAction(builder.Process(quoteMsg), m => m.SecurityId, (s, m) => ProcessQuotesMessage(s, m, false), true);
		}

		private SecurityId CreateAssociatedId(SecurityId securityId)
		{
			return new SecurityId
			{
				SecurityCode = securityId.SecurityCode,
				BoardCode = AssociatedBoardCode,
				SecurityType = securityId.SecurityType,
				Bloomberg = securityId.Bloomberg,
				Cusip = securityId.Cusip,
				IQFeed = securityId.IQFeed,
				InteractiveBrokers = securityId.InteractiveBrokers,
				Isin = securityId.Isin,
				Native = securityId.Native,
				Plaza = securityId.Plaza,
				Ric = securityId.Ric,
				Sedol = securityId.Sedol,
			};
		}

		private void AdapterOnNewOutMessage(Message message)
		{
			OnProcessMessage(message);
		}

		/// <summary>
		/// To call the <see cref="Connector.Connected"/> event when the first adapter connects to <see cref="Connector.Adapter"/>.
		/// </summary>
		protected virtual bool RaiseConnectedOnFirstAdapter => true;

		private IMessageChannel _outMessageChannel;

		/// <summary>
		/// Outgoing message channel.
		/// </summary>
		public IMessageChannel OutMessageChannel
		{
			get { return _outMessageChannel; }
			protected set
			{
				if (value == _outMessageChannel)
					return;

				_outMessageChannel?.Dispose();
				_outMessageChannel = value;
			}
		}

		private IMessageChannel _inMessageChannel;

		/// <summary>
		/// Input message channel.
		/// </summary>
		public IMessageChannel InMessageChannel
		{
			get { return _inMessageChannel; }
			protected set
			{
				if (value == _inMessageChannel)
					return;

				_inMessageChannel?.Dispose();
				_inMessageChannel = value;
			}
		}

		private IMessageAdapter _inAdapter;
		private BasketMessageAdapter _adapter;

		/// <summary>
		/// Message adapter.
		/// </summary>
		public BasketMessageAdapter Adapter
		{
			get { return _adapter; }
			protected set
			{
				if (!_isDisposing && value == null)
					throw new ArgumentNullException(nameof(value));

				if (_adapter == value)
					return;

				if (_adapter != null)
				{
					_adapter.InnerAdapters.Added -= InnerAdaptersOnAdded;
					_adapter.InnerAdapters.Removed -= InnerAdaptersOnRemoved;
					_adapter.InnerAdapters.Cleared -= InnerAdaptersOnCleared;

					SendInMessage(new ResetMessage());

					_inAdapter.NewOutMessage -= AdapterOnNewOutMessage;
					_inAdapter.Dispose();

					//if (_inAdapter != _adapter)
					//	_adapter.Dispose();
				}

				_adapter = value;
				_inAdapter = _adapter;

				if (_adapter != null)
				{
					_adapter.InnerAdapters.Added += InnerAdaptersOnAdded;
					_adapter.InnerAdapters.Removed += InnerAdaptersOnRemoved;
					_adapter.InnerAdapters.Cleared += InnerAdaptersOnCleared;

					_adapter.Parent = this;

					_inAdapter = new ChannelMessageAdapter(_inAdapter, InMessageChannel, OutMessageChannel)
					{
						//OwnOutputChannel = true,
						OwnInnerAdaper = true
					};

					if (LatencyManager != null)
						_inAdapter = new LatencyMessageAdapter(_inAdapter) { LatencyManager = LatencyManager, OwnInnerAdaper = true };

					if (SlippageManager != null)
						_inAdapter = new SlippageMessageAdapter(_inAdapter) { SlippageManager = SlippageManager, OwnInnerAdaper = true };

					if (PnLManager != null)
						_inAdapter = new PnLMessageAdapter(_inAdapter) { PnLManager = PnLManager, OwnInnerAdaper = true };

					if (CommissionManager != null)
						_inAdapter = new CommissionMessageAdapter(_inAdapter) { CommissionManager = CommissionManager, OwnInnerAdaper = true };

					if (RiskManager != null)
						_inAdapter = new RiskMessageAdapter(_inAdapter) { RiskManager = RiskManager, OwnInnerAdaper = true };

					if (_entityRegistry != null && _storageRegistry != null)
						_inAdapter = StorageAdapter = new StorageMessageAdapter(_inAdapter, _entityRegistry, _storageRegistry) { OwnInnerAdaper = true };

					_inAdapter.NewOutMessage += AdapterOnNewOutMessage;
				}
			}
		}

		/// <summary>
		/// Send outgoing message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendOutMessage(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = CurrentTime;

			AdapterOnNewOutMessage(message);
		}

		/// <summary>
		/// Send error message.
		/// </summary>
		/// <param name="error">Error detais.</param>
		public void SendOutError(Exception error)
		{
			SendOutMessage(new ErrorMessage { Error = error });
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendInMessage(Message message)
		{
			_inAdapter.SendInMessage(message);
		}

		private void InnerAdaptersOnAdded(IMessageAdapter adapter)
		{
			if (adapter.IsMessageSupported(MessageTypes.OrderRegister))
				TransactionAdapter = adapter;

			if (adapter.IsMessageSupported(MessageTypes.MarketData))
				MarketDataAdapter = adapter;
		}

		private void InnerAdaptersOnRemoved(IMessageAdapter adapter)
		{
			if (TransactionAdapter == adapter)
				TransactionAdapter = null;

			if (MarketDataAdapter == adapter)
				MarketDataAdapter = null;

			_adapterStates.Remove(adapter);
		}

		private void InnerAdaptersOnCleared()
		{
			TransactionAdapter = null;
			MarketDataAdapter = null;
		}

		/// <summary>
		/// Transactional adapter.
		/// </summary>
		public IMessageAdapter TransactionAdapter { get; private set; }

		/// <summary>
		/// Market-data adapter.
		/// </summary>
		public IMessageAdapter MarketDataAdapter { get; private set; }

		/// <summary>
		/// Storage adapter.
		/// </summary>
		public StorageMessageAdapter StorageAdapter { get; private set; }

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected virtual void OnProcessMessage(Message message)
		{
			if (message.Type != MessageTypes.Time && message.Type != MessageTypes.QuoteChange)
				this.AddDebugLog("BP:{0}", message);

			ProcessTimeInterval(message);

			RaiseNewMessage(message);

			try
			{
				switch (message.Type)
				{
					case MessageTypes.QuoteChange:
						ProcessSecurityAction((QuoteChangeMessage)message, m => m.SecurityId, (s, m) => ProcessQuotesMessage(s, m, false), true);
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
								RaiseError(mdMsg.Error);

							break;
						}

						ProcessSecurityAction(mdMsg, m => m.SecurityId, (s, m) =>
						{
							if (m.IsSubscribe)
							{
								if (m.DataType == _filteredMarketDepth)
								{
									GetFilteredMarketDepthInfo(s).Init(GetMarketDepth(s), _entityCache.GetOrders(s, OrderStates.Active));
									return;
								}

								if (m.Error == null)
									RaiseMarketDataSubscriptionSucceeded(s, m);
								else
									RaiseMarketDataSubscriptionFailed(s, m.DataType, m.Error ?? new NotSupportedException(LocalizedStrings.ConnectionNotSupportSecurity.Put(s.Id)));
							}
							else
							{
								if (m.DataType == _filteredMarketDepth)
									_filteredMarketDepths.Remove(s);
							}
						});

						break;
					}

					case MessageTypes.Error:
						var mdErrorMsg = (ErrorMessage)message;
						RaiseError(mdErrorMsg.Error);
						break;

					case MessageTypes.Connect:
						ProcessConnectMessage((ConnectMessage)message);
						break;

					case MessageTypes.Disconnect:
						ProcessConnectMessage((DisconnectMessage)message);
						break;

					case MessageTypes.SecurityLookup:
					{
						var lookupMsg = (SecurityLookupMessage)message;
						_securityLookups.Add(lookupMsg.TransactionId, (SecurityLookupMessage)lookupMsg.Clone());
						SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
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
				RaiseError(new InvalidOperationException(LocalizedStrings.Str681Params.Put(message), ex));
			}

			//if (message.Type != MessageTypes.Time && direction == MessageDirections.Out && adapter == MarketDataAdapter)
			//	RaiseNewDataExported();
		}

		private void ProcessSecurityAction<TMessage>(TMessage message, Func<TMessage, SecurityId> getId, Action<Security, TMessage> action, bool ignoreIfNotExist = false)
			where TMessage : Message
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (getId == null)
				throw new ArgumentNullException(nameof(getId));

			var securityId = getId(message);

			var nativeSecurityId = securityId.Native;
			var securityCode = securityId.SecurityCode;
			var boardCode = securityId.BoardCode;

			//if (boardCode.IsEmpty())
			//	boardCode = AssociatedBoardCode;

			var isSecurityIdEmpty = securityCode.IsEmpty() || boardCode.IsEmpty();
			var isNativeIdNull = nativeSecurityId == null;

			Security security = null;

			if (isSecurityIdEmpty && isNativeIdNull)
			{
				// если указан код и тип инструмента, то пытаемся найти инструмент по ним
				if (!securityCode.IsEmpty() && securityId.SecurityType != null)
				{
					var securities = _entityCache.GetSecuritiesByCode(securityCode).ToArray();

					security = securities.FirstOrDefault(s => s.Type == securityId.SecurityType)
					           ?? securities.FirstOrDefault(s => s.Type == null);
				}
				else
					throw new ArgumentNullException(nameof(message), LocalizedStrings.Str682Params.Put(securityCode, securityId.SecurityType));
			}

			string stockSharpId = null;

			if (!isSecurityIdEmpty)
				stockSharpId = CreateSecurityId(securityCode, boardCode);

			lock (_suspendSync)
			{
				if (!isSecurityIdEmpty)
					security = _entityCache.GetSecurityById(stockSharpId);

				if (security == null && !isNativeIdNull)
					security = _entityCache.GetSecurityByNativeId(nativeSecurityId);

				if (security == null && !isSecurityIdEmpty)
				{
					var secProvider = EntityFactory as ISecurityProvider;

					if (secProvider != null)
						security = secProvider.LookupById(stockSharpId);
				}

				if (security == null)
				{
					if (!isSecurityIdEmpty)
						security = GetSecurity(securityId);

					if (security == null)
					{
						if (!ignoreIfNotExist)
						{
							var clone = message.Clone();
							_suspendedSecurityMessages.SafeAdd(securityId).Add(clone);

							this.AddInfoLog("Msg delayed (no sec info): {0}", message);
							_messageStat.Add(clone);
						}

						return;
					}
				}
			}

			action(security, message);
		}

		private void ProcessConnectMessage(BaseConnectionMessage message)
		{
			var isConnect = message is ConnectMessage;
			var adapter = message.Adapter;

			if (adapter == null)
			{
				if (message.Error != null)
					RaiseConnectionError(message.Error);

				return;
			}

			var state = _adapterStates[adapter];

			switch (state)
			{
				case ConnectionStates.Connecting:
				{
					if (isConnect)
					{
						if (message.Error == null)
						{
							_adapterStates[adapter] = ConnectionStates.Connected;

							if (ConnectionState == ConnectionStates.Connecting)
							{
								if (RaiseConnectedOnFirstAdapter)
								{
									// raise Connected event only one time for the first adapter
									RaiseConnected();
								}
								else
								{
									var isAllConnected = _adapterStates.CachedValues.All(v => v == ConnectionStates.Connected);

									// raise Connected event only one time when the last adapter connection successfully
									if (isAllConnected)
										RaiseConnected();
								}
							}

							if (adapter.PortfolioLookupRequired)
								SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

							if (adapter.OrderStatusRequired)
							{
								var transactionId = TransactionIdGenerator.GetNextId();
								_entityCache.AddOrderStatusTransactionId(transactionId);
								SendInMessage(new OrderStatusMessage { TransactionId = transactionId });
							}

							if (adapter.SecurityLookupRequired)
								SendInMessage(new SecurityLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

							if (message is RestoredConnectMessage)
								RaiseRestored();
						}
						else
						{
							_adapterStates[adapter] = ConnectionStates.Failed;

							// raise ConnectionError only one time
							if (ConnectionState == ConnectionStates.Connecting)
							{
								RaiseConnectionError(message.Error);

								if (message.Error is TimeoutException)
									RaiseTimeOut();
							}
							else
								RaiseError(message.Error);
						}
					}
					else
					{
						_adapterStates[adapter] = ConnectionStates.Failed;

						// raise ConnectionError only one time
						if (ConnectionState == ConnectionStates.Connecting)
							RaiseConnectionError(new InvalidOperationException(LocalizedStrings.Str683, message.Error));
						else
							RaiseError(message.Error);
					}

					return;
				}
				case ConnectionStates.Disconnecting:
				{
					if (isConnect)
					{
						_adapterStates[adapter] = ConnectionStates.Failed;

						var error = new InvalidOperationException(LocalizedStrings.Str684, message.Error);

						// raise ConnectionError only one time
						if (ConnectionState == ConnectionStates.Disconnecting)
							RaiseConnectionError(error);
						else
							RaiseError(error);
					}
					else
					{
						if (message.Error == null)
						{
							_adapterStates[adapter] = ConnectionStates.Disconnected;

							var isLast = _adapterStates.CachedValues.All(v => v != ConnectionStates.Disconnecting);

							// raise Disconnected only one time for the last adapter
							if (isLast)
								RaiseDisconnected();
						}
						else
						{
							_adapterStates[adapter] = ConnectionStates.Failed;

							// raise ConnectionError only one time
							if (ConnectionState == ConnectionStates.Disconnecting)
								RaiseConnectionError(message.Error);
							else
								RaiseError(message.Error);
						}
					}

					return;
				}
				case ConnectionStates.Connected:
				{
					if (isConnect && message.Error != null)
					{
						_adapterStates[adapter] = ConnectionStates.Failed;
						RaiseConnectionError(new InvalidOperationException(LocalizedStrings.Str683, message.Error));
						return;
					}

					break;
				}
				case ConnectionStates.Disconnected:
				case ConnectionStates.Failed:
				{
					StopMarketTimer();
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			// так как соединение установлено, то выдаем ошибку через Error, чтобы не сбрасывать состояние
			RaiseError(new InvalidOperationException(LocalizedStrings.Str685Params.Put(state, message.GetType().Name), message.Error));
		}

		private void ProcessSessionMessage(SessionMessage message)
		{
			var board = ExchangeBoard.GetOrCreateBoard(message.BoardCode);
			_sessionStates[board] = message.State;
			SessionStateChanged.SafeInvoke(board, message.State);
		}

		private void ProcessBoardMessage(BoardMessage message)
		{
			ExchangeBoard.GetOrCreateBoard(message.Code, code =>
			{
				var exchange = message.ToExchange(EntityFactory.CreateExchange(message.ExchangeCode));
				return message.ToBoard(EntityFactory.CreateBoard(code, exchange));
			});
		}

		private void BindNativeSecurityId(SecurityId securityId)
		{
			var native = securityId.Native;
			var stocksharp = CreateSecurityId(securityId.SecurityCode, securityId.BoardCode);

			lock (_suspendSync)
			{
				var sec = _entityCache.GetSecurityByNativeId(native);

				if (sec == null)
					_entityCache.AddSecurityByNativeId(native, stocksharp);
				else
				{
					if (!sec.Id.CompareIgnoreCase(stocksharp))
						throw new InvalidOperationException(LocalizedStrings.Str687Params.Put(stocksharp, sec.Id, native));
				}
			}
		}

		private void ProcessSecurityMessage(SecurityMessage message/*, string boardCode = null*/)
		{
			var secId = CreateSecurityId(message.SecurityId.SecurityCode, message.SecurityId.BoardCode);

			var security = GetSecurity(secId, s =>
			{
				s.ApplyChanges(message);

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
				: new SecurityId { SecurityCode = message.SecurityId.SecurityCode, BoardCode = message.SecurityId.BoardCode };

			ProcessSuspendedSecurityMessages(stocksharpId);

			if (CreateAssociatedSecurity && !IsAssociated(message.SecurityId.BoardCode))
			{
				var clone = (SecurityMessage)message.Clone();
				clone.SecurityId = CreateAssociatedId(clone.SecurityId);
				ProcessSecurityMessage(clone);
			}
		}

		private void ProcessSecurityLookupResultMessage(SecurityLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseError(message.Error);

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
					SendInMessage(nextCriteria);
				else
				{
					_securityLookups.Add(nextCriteria.TransactionId, (SecurityLookupMessage)nextCriteria.Clone());
					SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = nextCriteria.TransactionId });
				}
			}
		}

		private void ProcessPortfolioLookupResultMessage(PortfolioLookupResultMessage message)
		{
			if (message.Error != null)
				RaiseError(message.Error);

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

			if (CreateDepthFromLevel1)
			{
				// генерация стакана из Level1
				var quoteMsg = GetBuilder(message.SecurityId).Process(message);

				if (quoteMsg != null)
				{
					ProcessQuotesMessage(security, quoteMsg, true);
					CreateAssociatedSecurityQuotes(quoteMsg);
				}
			}

			if (CreateAssociatedSecurity && !IsAssociated(message.SecurityId.BoardCode))
			{
				// обновление BestXXX для ALL из конкретных тикеров
				var clone = (Level1ChangeMessage)message.Clone();
				clone.SecurityId = CreateAssociatedId(clone.SecurityId);
				ProcessSecurityAction(clone, m => m.SecurityId, ProcessLevel1ChangeMessage, true);
			}
		}

		private Level1DepthBuilder GetBuilder(SecurityId securityId)
		{
			return _level1DepthBuilders.SafeAdd(securityId, c => new Level1DepthBuilder(c));
		}

		private void ProcessPortfolioMessage(PortfolioMessage message)
		{
			GetPortfolio(message.PortfolioName, p =>
			{
				message.ToPortfolio(p);
				return true;
			});
		}

		private void ProcessPortfolioChangeMessage(PortfolioChangeMessage message)
		{
			GetPortfolio(message.PortfolioName, portfolio =>
			{
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
					var currValue = (decimal)valueInLots / (security.VolumeStep ?? 1);
					message.Add(PositionChangeTypes.CurrentValue, currValue);
				}

				message.Changes.Remove(PositionChangeTypes.CurrentValueInLots);
			}

			var position = GetPosition(portfolio, security, message.DepoName, message.LimitType, message.Description);
			position.ApplyChanges(message);

			RaisePositionChanged(position);
		}

		private void ProcessNewsMessage(Security security, NewsMessage message)
		{
			var secId = message.SecurityId;

			if (security != null || secId == null)
			{
				var news = _entityCache.ProcessNewsMessage(security, message);

				if (news.Item2)
					RaiseNewNews(news.Item1);
				else
					RaiseNewsChanged(news.Item1);
			}
			else
				ProcessSecurityAction(message, m => secId.Value, ProcessNewsMessage);
		}

		private void ProcessQuotesMessage(Security security, QuoteChangeMessage message, bool fromLevel1)
		{
			if (MarketDepthChanged != null || MarketDepthsChanged != null)
			{
				var marketDepth = GetMarketDepth(security);

				message.ToMarketDepth(marketDepth, GetSecurity);

				if (_subscriptionManager.IsFilteredMarketDepthRegistered(security))
					GetFilteredMarketDepthInfo(security).Process(message);

				RaiseMarketDepthChanged(marketDepth);
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

			if (!fromLevel1 && (bestBid != null || bestAsk != null))
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
				var updated = false;

				if (!fromLevel1 || bestBid != null)
				{
					updated = true;
					security.BestBid = bestBid == null ? null : new Quote(security, bestBid.Price, bestBid.Volume, Sides.Buy);
				}

				if (!fromLevel1 || bestAsk != null)
				{
					updated = true;
					security.BestAsk = bestAsk == null ? null : new Quote(security, bestAsk.Price, bestAsk.Volume, Sides.Sell);
				}

				if (updated)
				{
					security.LocalTime = message.LocalTime;
					security.LastChangeTime = message.ServerTime;

					RaiseSecurityChanged(security);

					// стаканы по ALL обновляют BestXXX по конкретным инструментам
					if (security.Board.Code == AssociatedBoardCode)
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

						RaiseSecuritiesChanged(changedSecurities.Keys.ToArray());
					}
				}
			}

			if (CreateDepthFromLevel1)
				GetBuilder(message.SecurityId).HasDepth = true;

			CreateAssociatedSecurityQuotes(message);
		}

		private void ProcessOrderLogMessage(Security security, ExecutionMessage message)
		{
			var trade = (message.TradeId != null || !message.TradeStringId.IsEmpty())
				? EntityFactory.CreateTrade(security, message.TradeId, message.TradeStringId)
				: null;

			var logItem = message.ToOrderLog(EntityFactory.CreateOrderLogItem(new Order { Security = security }, trade));
			//logItem.LocalTime = message.LocalTime;

			RaiseNewOrderLogItem(logItem);

			if (message.IsSystem == false)
				return;

			if (CreateDepthFromOrdersLog)
			{
				try
				{
					var builder = _olBuilders.SafeAdd(security, key => MarketDataAdapter.CreateOrderLogMarketDepthBuilder(message.SecurityId));

					if (builder == null)
						throw new InvalidOperationException();

					var updated = builder.Update(message);

					if (updated)
					{
						RaiseNewMessage(builder.Depth.Clone());
						ProcessQuotesMessage(security, builder.Depth, false);
					}
				}
				catch (Exception ex)
				{
					// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
					// а только выводим сообщение в лог
					RaiseError(ex);
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
					RaiseNewTrade(tuple.Item1);
			}
		}

		private void ProcessTradeMessage(Security security, ExecutionMessage message)
		{
			var tuple = _entityCache.ProcessTradeMessage(security, message);

			if (tuple.Item2)
				RaiseNewTrade(tuple.Item1);

			var values = GetSecurityValues(security);

			var changes = new List<KeyValuePair<Level1Fields, object>>(4)
			{
				new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeTime, message.ServerTime),
				new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradePrice, message.TradePrice)
			};

			lock (values.SyncRoot)
			{
				values[(int)Level1Fields.LastTradeTime] = message.ServerTime;

				if (message.TradePrice != null)
					values[(int)Level1Fields.LastTradePrice] = message.TradePrice.Value;

				if (message.IsSystem != null)
					values[(int)Level1Fields.IsSystem] = message.IsSystem.Value;

				if (message.TradeId != null)
				{
					values[(int)Level1Fields.LastTradeId] = message.TradeId.Value;
					changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeId, message.TradeId.Value));
				}

				if (message.TradeVolume != null)
				{
					values[(int)Level1Fields.Volume] = message.TradeVolume.Value;
					changes.Add(new KeyValuePair<Level1Fields, object>(Level1Fields.LastTradeVolume, message.TradeVolume.Value));
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

				if (tuple == null)
				{
					this.AddWarningLog(LocalizedStrings.Str1156Params, message.OrderId.To<string>() ?? message.OrderStringId);
					return;
				}

				var order = tuple.Item1;
				var isNew = tuple.Item2;
				var isChanged = tuple.Item3;

				if (message.OrderType == OrderTypes.Conditional && (message.DerivedOrderId != null || !message.DerivedOrderStringId.IsEmpty()))
				{
					var derivedOrder = _entityCache.GetOrder(security, 0L, message.DerivedOrderId ?? 0, message.DerivedOrderStringId);

					if (derivedOrder == null)
						_orderStopOrderAssociations.Add(Tuple.Create(message.DerivedOrderId, message.DerivedOrderStringId), new RefPair<Order, Action<Order, Order>>(order, (s, o) => s.DerivedOrder = o));
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
						RaiseNewOrder(order);
				}
				else if (isChanged)
				{
					this.AddOrderInfoLog(order, "Order changed");

					if (order.Type == OrderTypes.Conditional)
						RaiseStopOrdersChanged(new[] { order });
					else
						RaiseOrderChanged(order);
				}

				if (order.Id != null)
					ProcessMyTrades(order, order.Id.Value, _nonOrderedByIdMyTrades);

				ProcessMyTrades(order, order.TransactionId, _nonOrderedByTransactionIdMyTrades);

				if (!order.StringId.IsEmpty())
					ProcessMyTrades(order, order.StringId, _nonOrderedByStringIdMyTrades);

				ProcessConditionOrders(order);
			}
			else
			{
				foreach (var tuple in _entityCache.ProcessOrderFailMessage(security, message))
				{
					var fail = tuple.Item1;

					TryProcessFilteredMarketDepth(security, message);

					//var isRegisterFail = (fail.Order.Id == null && fail.Order.StringId.IsEmpty()) || fail.Order.Status == OrderStatus.RejectedBySystem;
					var isRegisterFail = tuple.Item2;

					this.AddErrorLog(() => (isRegisterFail ? "OrderFailed" : "OrderCancelFailed")
						+ Environment.NewLine + fail.Order + Environment.NewLine + fail.Error);

					var isStop = fail.Order.Type == OrderTypes.Conditional;

					if (isRegisterFail)
					{
						_entityCache.AddRegisterFail(fail);

						if (isStop)
							RaiseStopOrdersRegisterFailed(new[] { fail });
						else
							RaiseOrderRegisterFailed(fail);
					}
					else
					{
						_entityCache.AddCancelFail(fail);

						if (isStop)
							RaiseStopOrdersCancelFailed(new[] { fail });
						else
							RaiseOrderCancelFailed(fail);
					}
				}
			}
		}

		private void TryProcessFilteredMarketDepth(Security security, ExecutionMessage order)
		{
			var info = _filteredMarketDepths.TryGetValue(security);
			info?.Process(order);
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

			if (tuple == null)
			{
				List<ExecutionMessage> nonOrderedMyTrades;

				if (message.OrderId != null)
					nonOrderedMyTrades = _nonOrderedByIdMyTrades.SafeAdd(message.OrderId.Value);
				else if (message.OriginalTransactionId != 0)
					nonOrderedMyTrades = _nonOrderedByTransactionIdMyTrades.SafeAdd(message.OriginalTransactionId);
				else
					nonOrderedMyTrades = _nonOrderedByStringIdMyTrades.SafeAdd(message.OrderStringId);

				this.AddInfoLog("My trade delayed: {0}", message);

				nonOrderedMyTrades.Add((ExecutionMessage)message.Clone());

				return;
			}

			if (!tuple.Item2)
				return;

			RaiseNewMyTrade(tuple.Item1);
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
					case ExecutionTypes.OrderLog:
						ProcessOrderLogMessage(security, message);
						break;
					default:
					{
						var processed = false;

						if (message.HasOrderInfo())
						{
							processed = true;
							ProcessOrderMessage(security, message);
						}

						if (message.HasTradeInfo())
						{
							processed = true;
							ProcessMyTradeMessage(security, message);
						}

						if (!processed)
							throw new ArgumentOutOfRangeException(LocalizedStrings.Str1695Params.Put(message.ExecutionType));

						break;
					}
				}
			};

			switch (message.ExecutionType)
			{
				case ExecutionTypes.Transaction:
				{
					if (message.SecurityId.SecurityCode.IsEmpty() && message.SecurityId.Native == null)
					{
						var order = (
							_entityCache.GetOrderByTransactionId(message.OriginalTransactionId, false)
							??
							_entityCache.GetOrderByTransactionId(message.OriginalTransactionId, true)
						            ?? _entityCache.GetOrderById(message.OrderId ?? 0));

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
				//case null:
				{
					ProcessSecurityAction(message, m => m.SecurityId, (s, m) => handler(s));

					if (CreateAssociatedSecurity && !IsAssociated(message.SecurityId.BoardCode))
					{
						var clone = (ExecutionMessage)message.Clone();
						clone.SecurityId = CreateAssociatedId(clone.SecurityId);
						ProcessExecutionMessage(clone);
					}

					break;
				}
				
				default:
				throw new ArgumentOutOfRangeException(LocalizedStrings.Str1695Params.Put(message.ExecutionType));
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

				// find association by code and code + type
				var pair = _suspendedSecurityMessages
					.FirstOrDefault(p =>
						p.Key.SecurityCode.CompareIgnoreCase(securityId.SecurityCode) &&
						p.Key.BoardCode.IsEmpty() &&
						(securityId.SecurityType == null || p.Key.SecurityType == securityId.SecurityType));

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
					OnProcessMessage(msg);
					_messageStat.Remove(msg);
				}
				catch (Exception error)
				{
					RaiseError(error);
				}
			}
		}
	}
}