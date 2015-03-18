namespace StockSharp.Quik.Lua
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Reflection;
	using System.Security;
	using SystemProcess = System.Diagnostics.Process;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using QuickFix.Fields;

	using StockSharp.Algo;
	using StockSharp.Fix;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// FIX сервер, запускающийся LUA.
	/// </summary>
	public class LuaFixServer : Disposable
	{
		[DisplayName("Quik")]
		private sealed class LuaSession : QuikSessionHolder
		{
			public BlockingQueue<LuaRequest> Requests { get; private set; }

			public LuaSession(IdGenerator transactionIdGenerator)
				: base(transactionIdGenerator)
			{
				Requests = new BlockingQueue<LuaRequest>();
				Requests.Close();
			}

			public void ReplaceSecurityId(SecurityId securityId, Action<SecurityId> setSecurityId)
			{
				if (setSecurityId == null)
					throw new ArgumentNullException("setSecurityId");

				ReplaceBoardCode(securityId.BoardCode, boardCode => setSecurityId(new SecurityId { SecurityCode = securityId.SecurityCode, BoardCode = boardCode }));
			}

			public void ReplaceBoardCode(string classCode, Action<string> setBoardCode)
			{
				if (setBoardCode == null)
					throw new ArgumentNullException("setBoardCode");

				if (classCode.IsEmpty())
					return;

				var info = this.GetSecurityClassInfo(classCode);

				if (info == null)
					return;

				setBoardCode(info.Item2);
			}

			public string GetBoardCode(string classCode)
			{
				if (classCode.IsEmpty())
					return classCode;

				var info = this.GetSecurityClassInfo(classCode);

				if (info == null)
					return classCode;

				return info.Item2;
			}
		}

		private sealed class LuaMarketDataAdapter : MessageAdapter<LuaSession>
		{
			protected override bool IsSupportNativeSecurityLookup
			{
				get { return true; }
			}

			public LuaMarketDataAdapter(LuaSession sessionHolder)
				: base(MessageAdapterTypes.MarketData, sessionHolder)
			{
			}

			protected override void OnSendInMessage(Message message)
			{
				SessionHolder.AddDebugLog("In. {0}", message);

				switch (message.Type)
				{
					case MessageTypes.SecurityLookup:
					{
						var secMsg = (SecurityLookupMessage)message;

						var securityId = new SecurityId
						{
							SecurityCode = secMsg.SecurityId.SecurityCode,
							BoardCode = !secMsg.SecurityId.BoardCode.IsEmpty()
								? SessionHolder.GetSecurityClass(secMsg.SecurityId)
								: null
						};

						SessionHolder.Requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.SecurityLookup,
							TransactionId = secMsg.TransactionId,
							SecurityId = securityId,
							Value = secMsg.UnderlyingSecurityCode
						});
						break;
					}

					case MessageTypes.MarketData:
					{
						var mdMsg = (MarketDataMessage)message;
						ProcessMarketDataMessage(mdMsg);
						break;
					}
				}
			}

			public override void SendOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Security:
					{
						var secMsg = (SecurityMessage)message;

						var classCode = secMsg.SecurityId.BoardCode;
						var classInfo = SessionHolder.GetSecurityClassInfo(classCode);

						// из квика не транслируется поле тип инструмента, если тип инструмента не найден по классу, то берем по умолчанию.
						secMsg.SecurityType = secMsg.Multiplier == 0 ? SecurityTypes.Index : (classInfo.Item1 ?? SecurityTypes.Stock);

						SessionHolder.ReplaceSecurityId(secMsg.SecurityId, id => secMsg.SecurityId = id);
						break;
					}

					case MessageTypes.Level1Change:
					{
						var l1Msg = (Level1ChangeMessage)message;
						SessionHolder.ReplaceSecurityId(l1Msg.SecurityId, id => l1Msg.SecurityId = id);
						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)message;
						SessionHolder.ReplaceSecurityId(quoteMsg.SecurityId, id => quoteMsg.SecurityId = id);
						quoteMsg.ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow);
						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;
						SessionHolder.ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);
						break;
					}
				}

				base.SendOutMessage(message);
			}

			private void ProcessMarketDataMessage(MarketDataMessage message)
			{
				SessionHolder.Requests.Enqueue(new LuaRequest
				{
					MessageType = message.Type,
					DataType = message.DataType,
					SecurityId = new SecurityId
					{
						SecurityCode = message.SecurityId.SecurityCode,
						BoardCode = SessionHolder.GetSecurityClass(message.SecurityId)
					},
					IsSubscribe = message.IsSubscribe,
					TransactionId = message.TransactionId
				});

				var result = (MarketDataMessage)message.Clone();
				result.OriginalTransactionId = message.TransactionId;
				SendOutMessage(result);
			}
		}

		private sealed class LuaTransactionAdapter : MessageAdapter<LuaSession>
		{
			//private readonly Dictionary<long, long> _transactionsByLocalId = new Dictionary<long, long>();
			private readonly Dictionary<long, long> _transactionsByOrderId = new Dictionary<long, long>();

			private readonly Dictionary<long, List<ExecutionMessage>> _tradesByOrderId = new Dictionary<long, List<ExecutionMessage>>();
			private readonly Dictionary<string, string> _depoNames = new Dictionary<string, string>();
			private readonly SynchronizedDictionary<long, Transaction> _transactions = new SynchronizedDictionary<long, Transaction>();

			protected override bool IsSupportNativePortfolioLookup
			{
				get { return true; }
			}

			public LuaTransactionAdapter(LuaSession sessionHolder)
				: base(MessageAdapterTypes.MarketData, sessionHolder)
			{
			}

			protected override void OnSendInMessage(Message message)
			{
				SessionHolder.AddDebugLog("In. {0}", message);

				switch (message.Type)
				{
					case MessageTypes.PortfolioLookup:
						var pfMsg = (PortfolioLookupMessage)message;
						SessionHolder.Requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.PortfolioLookup,
							TransactionId = pfMsg.TransactionId
						});
						break;

					case MessageTypes.OrderStatus:
						var statusMsg = (OrderStatusMessage)message;
						SessionHolder.Requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.OrderStatus,
							TransactionId = statusMsg.TransactionId
						});
						break;

					case MessageTypes.OrderRegister:
					case MessageTypes.OrderReplace:
					case MessageTypes.OrderCancel:
					case MessageTypes.OrderGroupCancel:
						var orderMsg = (OrderMessage)message;
						ProcessOrderMessage(orderMsg);
						break;
				}
			}

			private void ProcessOrderMessage(OrderMessage message)
			{
				switch (message.Type)
				{
					case MessageTypes.OrderRegister:
						var regMsg = (OrderRegisterMessage)message;
						RegisterTransaction(SessionHolder.CreateRegisterTransaction(regMsg, _depoNames.TryGetValue(regMsg.PortfolioName)), message.Type, regMsg.TransactionId, regMsg.OrderType);
						break;

					case MessageTypes.OrderReplace:
						var replMsg = (OrderReplaceMessage)message;
						RegisterTransaction(SessionHolder.CreateMoveTransaction(replMsg), message.Type, replMsg.TransactionId, replMsg.OrderType);
						break;

					case MessageTypes.OrderCancel:
						var cancelMsg = (OrderCancelMessage)message;
						RegisterTransaction(SessionHolder.CreateCancelTransaction(cancelMsg), message.Type, cancelMsg.TransactionId, cancelMsg.OrderType);
						break;

					case MessageTypes.OrderGroupCancel:
						var cancelGroupMsg = (OrderGroupCancelMessage)message;
						RegisterTransaction(SessionHolder.CreateCancelFuturesTransaction(cancelGroupMsg), message.Type, cancelGroupMsg.TransactionId, cancelGroupMsg.OrderType);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			private void RegisterTransaction(Transaction transaction, MessageTypes messageType, long transactionId, OrderTypes type)
			{
				if (transactionId <= 0 || transactionId > uint.MaxValue)
					throw new InvalidOperationException(LocalizedStrings.Str1700Params.Put(transactionId));

				_transactions.Add(transactionId, transaction);

				SessionHolder.Requests.Enqueue(new LuaRequest
				{
					MessageType = messageType,
					TransactionId = transactionId,
					OrderType = type,
					Value = transaction.SetTransactionId(transactionId).ToLuaString()
				});
			}

			public override void SendOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Portfolio:
					{
						var pfMsg = (PortfolioMessage)message;
						SessionHolder.ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);
						break;
					}

					case MessageTypes.PortfolioChange:
					{
						var pfMsg = (PortfolioChangeMessage)message;

						SessionHolder.ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);

						var depoName = (string)pfMsg.Changes.TryGetValue(PositionChangeTypes.DepoName);
						if (!depoName.IsEmpty())
							_depoNames[pfMsg.PortfolioName] = depoName;

						break;
					}

					case MessageTypes.Position:
					{
						var pfMsg = (PositionMessage)message;
						SessionHolder.ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
						break;
					}

					case MessageTypes.PositionChange:
					{
						var pfMsg = (PositionChangeMessage)message;
						SessionHolder.ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						switch (execMsg.ExecutionType)
						{
							case ExecutionTypes.Order:
							{
								if (execMsg.OrderId != null && execMsg.OriginalTransactionId != 0)
								{
									_transactionsByOrderId.SafeAdd(execMsg.OrderId.Value, i => execMsg.OriginalTransactionId);

									var trades = _tradesByOrderId.TryGetValue(execMsg.OrderId.Value);

									if (trades != null)
									{
										trades.ForEach(SendOutMessage);
										_tradesByOrderId.Remove(execMsg.OrderId.Value);
									}
								}

								break;
							}

							case ExecutionTypes.Trade:
							{
								if (execMsg.OriginalTransactionId != 0)
									break;

								var orderId = execMsg.OrderId;

								if (orderId != null)
								{
									var origTransactionId = _transactionsByOrderId.TryGetValue2(orderId.Value);

									if (origTransactionId == null)
									{
										_tradesByOrderId.SafeAdd(orderId.Value).Add(execMsg);
										return;
									}

									execMsg.OriginalTransactionId = origTransactionId.Value;	
								}

								break;
							} 
						}

						SessionHolder.ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);

						// запоминаем номер исходной транзакции, который будет исопльзоваться в FixServer
						execMsg.UserOrderId = execMsg.OriginalTransactionId.To<string>();

						var transaction = _transactions.TryGetValue(execMsg.OriginalTransactionId);

						if (transaction != null && execMsg.Error != null)
						{
							switch (transaction.TransactionType)
							{
								case TransactionTypes.ReRegister:
								{
									var replaceMsg = (OrderReplaceMessage)transaction.Message;
									
									// дополнительно отправляем сообщение ошибки снятия заявки
									var cancelErrMsg = (ExecutionMessage)execMsg.Clone();
									cancelErrMsg.OrderId = replaceMsg.OldOrderId;
									cancelErrMsg.IsCancelled = true;
									base.SendOutMessage(cancelErrMsg);
									
									break;
								}

								case TransactionTypes.Cancel:
								{
									var cancelMsg = (OrderCancelMessage)transaction.Message;
									
									// заполняем номер заявки
									execMsg.OrderId = cancelMsg.OrderId;
									execMsg.IsCancelled = true;
									break;
								}
							}
						}
						
						break;
					}
				}

				base.SendOutMessage(message);
			}
		}

		[DisplayName("FixServer")]
		private sealed class FixServerEx : FixServer
		{
			public FixServerEx(Func<string, string, Tuple<TimeSpan, FixClientRoles>> authorize, IMessageAdapter transactionAdapter, IMessageAdapter marketDataAdapter)
				: base(authorize, transactionAdapter, marketDataAdapter)
			{
			}

			protected override long OnCreateTransactionId(string client, long requestId)
			{
				return requestId;
			}

			protected override void OnProcess(FixSession session, string client, string msgStr, bool isMarketData)
			{
				var msgType = QuickFix.Message.GetMsgType(msgStr);

				switch (msgType)
				{
					case QuikFixMessages.NewStopOrderSingle:
					{
						this.AddInfoLog("From client {0}: NewStopOrderSingle", client);

						var fixMsg = session.ToMessage<NewStopOrderSingle>(msgStr);
						var regMsg = fixMsg.ToRegisterMessage();

						regMsg.TransactionId = CreateTransactionId(client, regMsg.TransactionId);

						var condition = new QuikOrderCondition
						{
							Type = (QuikOrderConditionTypes)fixMsg.Type.Obj,
							Result = fixMsg.IsSetResult() ? (QuikOrderConditionResults?)fixMsg.Result.Obj : null,
							StopPriceCondition = (QuikStopPriceConditions)fixMsg.StopPriceCondition.Obj,
							StopPrice = fixMsg.IsSetStopPx() ? fixMsg.StopPx.Obj : (decimal?)null,
							StopLimitPrice = fixMsg.IsSetStopLimitPrice() ? fixMsg.StopLimitPrice.Obj : (decimal?)null,
							IsMarketStopLimit = fixMsg.IsSetIsMarketStopLimit() ? fixMsg.IsMarketStopLimit.Obj : (bool?)null,
							ConditionOrderId = fixMsg.IsSetConditionOrderId() ? fixMsg.ConditionOrderId.Obj : (long?)null,
							ConditionOrderSide = (Sides)fixMsg.ConditionOrderSide.Obj,
							ConditionOrderPartiallyMatched = fixMsg.IsSetConditionOrderPartiallyMatched() ? fixMsg.ConditionOrderPartiallyMatched.Obj : (bool?)null,
							ConditionOrderUseMatchedBalance = fixMsg.IsSetConditionOrderUseMatchedBalance() ? fixMsg.ConditionOrderUseMatchedBalance.Obj : (bool?)null,
							LinkedOrderPrice = fixMsg.IsSetLinkedOrderPrice() ? fixMsg.LinkedOrderPrice.Obj : (decimal?)null,
							LinkedOrderCancel = fixMsg.LinkedOrderCancel.Obj,
							Offset = fixMsg.IsSetOffset() ? fixMsg.Offset.Obj.ToUnit() : null,
							Spread = fixMsg.IsSetStopSpread() ? fixMsg.StopSpread.Obj.ToUnit() : null,
							IsMarketTakeProfit = fixMsg.IsSetIsMarketTakeProfit() ? fixMsg.IsMarketTakeProfit.Obj : (bool?)null,
						};

						if (fixMsg.IsSetOtherSecurityCode())
							condition.OtherSecurityId = new SecurityId { SecurityCode = fixMsg.OtherSecurityCode.Obj };
						if (fixMsg.IsSetActiveTimeFrom() && fixMsg.IsSetActiveTimeTo())
							condition.ActiveTime = new Range<DateTimeOffset>(fixMsg.ActiveTimeFrom.Obj.ApplyTimeZone(TimeHelper.Moscow), fixMsg.ActiveTimeTo.Obj.ApplyTimeZone(TimeHelper.Moscow));

						regMsg.Condition = condition;

						TransactionAdapter.SendInMessage(regMsg);
						return;
					}
				}

				base.OnProcess(session, client, msgStr, isMarketData);
			}

			protected override void SendOrder(IEnumerable<string> receivers, ExecutionMessage order)
			{
				if (order.OrderType == OrderTypes.Conditional)
				{
					var fixMsg = order.ToExecutionReport<StopOrderExecutionReport>(order.TransactionId == 0 ? (long?)null : GetRequestId(order.TransactionId), GetRequestId(order.OriginalTransactionId));

					var condition = (QuikOrderCondition)order.Condition;

					if (condition.Type != null)
						fixMsg.Type = new StopOrderExecutionReport.TypeField((int)condition.Type);

					if (condition.StopPriceCondition != null)
						fixMsg.StopPriceCondition = new StopOrderExecutionReport.StopPriceConditionField((int)condition.StopPriceCondition);

					if (condition.ConditionOrderSide != null)
						fixMsg.ConditionOrderSide = new StopOrderExecutionReport.ConditionOrderSideField((int)condition.ConditionOrderSide);

					if (condition.LinkedOrderCancel != null)
						fixMsg.LinkedOrderCancel = new StopOrderExecutionReport.LinkedOrderCancelField(condition.LinkedOrderCancel.Value);

					if (condition.Result != null)
						fixMsg.Result = new StopOrderExecutionReport.ResultField((int)condition.Result);
					
					if (condition.OtherSecurityId != null)
						fixMsg.OtherSecurityCode = new StopOrderExecutionReport.OtherSecurityCodeField(condition.OtherSecurityId.Value.SecurityCode);
					
					if (condition.StopPrice != null)
						fixMsg.StopPx = new StopPx(condition.StopPrice.Value);
					
					if (condition.StopLimitPrice != null)
						fixMsg.StopLimitPrice = new StopOrderExecutionReport.StopLimitPriceField(condition.StopLimitPrice.Value);
					
					if (condition.IsMarketStopLimit != null)
						fixMsg.IsMarketStopLimit = new StopOrderExecutionReport.IsMarketStopLimitField(condition.IsMarketStopLimit.Value);
					
					if (condition.ActiveTime != null)
					{
						fixMsg.ActiveTimeFrom = new StopOrderExecutionReport.ActiveTimeFromField(condition.ActiveTime.Min.UtcDateTime);
						fixMsg.ActiveTimeTo = new StopOrderExecutionReport.ActiveTimeToField(condition.ActiveTime.Min.UtcDateTime);
					}
					
					if (condition.ConditionOrderId != null)
						fixMsg.ConditionOrderId = new StopOrderExecutionReport.ConditionOrderIdField((int)condition.ConditionOrderId);
					
					if (condition.ConditionOrderPartiallyMatched != null)
						fixMsg.ConditionOrderPartiallyMatched = new StopOrderExecutionReport.ConditionOrderPartiallyMatchedField(condition.ConditionOrderPartiallyMatched.Value);
					
					if (condition.ConditionOrderUseMatchedBalance != null)
						fixMsg.ConditionOrderUseMatchedBalance = new StopOrderExecutionReport.ConditionOrderUseMatchedBalanceField(condition.ConditionOrderUseMatchedBalance.Value);
					
					if (condition.LinkedOrderPrice != null)
						fixMsg.LinkedOrderPrice = new StopOrderExecutionReport.LinkedOrderPriceField(condition.LinkedOrderPrice.Value);
					
					if (condition.Offset != null)
						fixMsg.Offset = new StopOrderExecutionReport.OffsetField(condition.Offset.ToString());
					
					if (condition.Spread != null)
						fixMsg.StopSpread = new StopOrderExecutionReport.SpreadField(condition.Spread.ToString());
					
					if (condition.IsMarketTakeProfit != null)
						fixMsg.IsMarketTakeProfit = new StopOrderExecutionReport.IsMarketTakeProfitField(condition.IsMarketTakeProfit.Value);

					SendMessage(receivers, false, fixMsg);
				}
				else
					base.SendOrder(receivers, order);
			}
		}

		private readonly LogManager _logManager = new LogManager();

		private readonly FixServerEx _fixServer;
		private readonly LuaMarketDataAdapter _marketDataAdapter;
		private readonly LuaTransactionAdapter _transactionAdapter;
		private readonly LuaSession _sessionHolder;
		private readonly SynchronizedDictionary<SecurityId, Level1ChangeMessage> _prevLevel1 = new CachedSynchronizedDictionary<SecurityId, Level1ChangeMessage>();

		private sealed class QuikNativeApp : BaseLogReceiver
		{
			public QuikNativeApp()
			{
				Name = "LuaServer";
				LogLevel = LogLevels.Info;
			}
		}

		/// <summary>
		/// Создать <see cref="LuaFixServer"/>.
		/// </summary>
		public LuaFixServer()
		{
			_sessionHolder = new LuaSession(new MillisecondIncrementalIdGenerator())
			{
				Path = SystemProcess.GetCurrentProcess().MainModule.FileName
			};

			var inProcessor = new MessageProcessorPool(new MessageProcessor("Processor 'LuaServer' (In)", err => _logManager.Application.AddErrorLog(err)));
			var outProcessor = new MessageProcessorPool(new MessageProcessor("Processor 'LuaServer' (Out)", err => _logManager.Application.AddErrorLog(err)));

			_marketDataAdapter = new LuaMarketDataAdapter(_sessionHolder)
			{
				InMessageProcessor = inProcessor,
				OutMessageProcessor = outProcessor
			};
			_transactionAdapter = new LuaTransactionAdapter(_sessionHolder)
			{
				InMessageProcessor = inProcessor,
				OutMessageProcessor = outProcessor
			};

			_fixServer = new FixServerEx((l, p) =>
			{
				if (Login.IsEmpty() || (l.CompareIgnoreCase(Login) && p == Password))
				{
					_prevLevel1.Clear();
					return Tuple.Create(TimeSpan.FromMilliseconds(100), FixClientRoles.Admin);
				}

				return null;
			}, _transactionAdapter, _marketDataAdapter);

			_logManager.Application = new QuikNativeApp();

			_logManager.Sources.Add(_sessionHolder);
			_logManager.Sources.Add(_fixServer);

			LogFile = "StockSharp.QuikLua.log";

			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var logFileName = Path.Combine(path, LogFile);

			_logManager.Listeners.Add(new FileLogListener(logFileName));
		}

		/// <summary>
		/// Серверный порт, на котором будет работать FIX сервер.
		/// </summary>
		public int Port
		{
			get { return _fixServer.Port; }
			set { _fixServer.Port = value; }
		}

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login { get; set; }

		private SecureString _password;

		/// <summary>
		/// Пароль.
		/// </summary>
		public string Password
		{
			get { return _password.To<string>(); }
			set { _password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Отправлять изменения по стакану. Если выключено, отправляется стакан целиком.
		/// </summary>
		public bool IncrementalDepthUpdates
		{
			get { return _fixServer.IncrementalDepthUpdates; }
			set { _fixServer.IncrementalDepthUpdates = value; }
		}

		// TODO
		/// <summary>
		/// Название текстового файла, в который будут писаться логи.
		/// </summary>
		public string LogFile { get; set; }

		/// <summary>
		/// Уровень логирования для Lua.
		/// </summary>
		public LogLevels LogLevel
		{
			get { return _logManager.Application.LogLevel; }
			set { _logManager.Application.LogLevel = value; }
		}

		/// <summary>
		/// Получатель логов.
		/// </summary>
		public ILogReceiver LogReceiver
		{
			get { return _logManager.Application; }
		}

		/// <summary>
		/// Запустить сервер.
		/// </summary>
		public void Start()
		{
			_sessionHolder.Requests.Open();
			_fixServer.Start();
		}

		/// <summary>
		/// Выключить сервер.
		/// </summary>
		public void Stop()
		{
			_sessionHolder.Requests.Close();
			_fixServer.Stop();
			_prevLevel1.Clear();
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_logManager.Listeners.ForEach(l => l.DoDispose());
			_logManager.Listeners.Clear();

			base.DisposeManaged();
		}

		/// <summary>
		/// Нужно ли обрабатывать маркет-данные.
		/// </summary>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Нужно ли обрабатывать маркет-данные.</returns>
		public bool NeedProcess(MarketDataTypes dataType, SecurityId securityId)
		{
			return _fixServer.HasReceivers(dataType, new SecurityId
			{
				SecurityCode = securityId.SecurityCode,
				BoardCode = _sessionHolder.GetBoardCode(securityId.BoardCode)
			});
		}

		/// <summary>
		/// Добавить ассоциацию идентификатора запроса и транзакции.
		/// </summary>
		/// <param name="transactionId">Идентификатор транзакции.</param>
		public void AddTransactionId(long transactionId)
		{
			LogReceiver.AddInfoLog("Added trans id {0} mapping.", transactionId);
			_fixServer.AddTransactionId(Login, transactionId, transactionId);
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			LogReceiver.AddDebugLog("Out. {0}", message);

			switch (message.Type)
			{
				case MessageTypes.Portfolio:
				case MessageTypes.PortfolioChange:
				case MessageTypes.PortfolioLookupResult:
				case MessageTypes.Position:
				case MessageTypes.PositionChange:
					_transactionAdapter.SendOutMessage(message);
					return;

				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;

					lock (_prevLevel1.SyncRoot)
					{
						var prevLevel1 = _prevLevel1.TryGetValue(l1Msg.SecurityId);

						if (prevLevel1 == null)
						{
							_prevLevel1.Add(l1Msg.SecurityId, (Level1ChangeMessage)l1Msg.Clone());
						}
						else
						{
							l1Msg.Changes.RemoveWhere(p =>
							{
								var prevValue = prevLevel1.Changes.TryGetValue(p.Key);

								if (prevValue != null && prevValue.Equals(p.Value))
									return true;

								prevLevel1.Changes[p.Key] = p.Value;
								return false;
							});

							if (l1Msg.Changes.Count == 0)
								return;
						}	
					}

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					
					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Order:
						case ExecutionTypes.Trade:
							_transactionAdapter.SendOutMessage(message);
							return;
					}

					break;
				}
			}

			_marketDataAdapter.SendOutMessage(message);
		}

		/// <summary>
		/// Получить пользовательский запрос.
		/// </summary>
		/// <returns>Пользовательский запрос.</returns>
		public LuaRequest GetNextRequest()
		{
			LuaRequest request;
			_sessionHolder.Requests.TryDequeue(out request);
			return request;
		}
	}
}