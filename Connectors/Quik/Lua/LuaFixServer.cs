namespace StockSharp.Quik.Lua
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Reflection;
	using System.Security;

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

	using SystemProcess = System.Diagnostics.Process;

	/// <summary>
	/// FIX сервер, запускающийся LUA.
	/// </summary>
	public class LuaFixServer
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
				if (securityId.BoardCode.IsEmpty())
					return;

				var info = this.GetSecurityClassInfo(securityId.BoardCode);

				if (info == null)
					return;

				setSecurityId(new SecurityId { SecurityCode = securityId.SecurityCode, BoardCode = info.Item2 });
			}

			public void ReplaceBoardCode(string classCode, Action<string> setboardCode)
			{
				if (classCode.IsEmpty())
					return;

				var info = this.GetSecurityClassInfo(classCode);

				if (info == null)
					return;

				setboardCode(info.Item2);
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

						SessionHolder.AddDebugLog("In. {0}", secMsg);
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
						SessionHolder.AddDebugLog("In. {0}", mdMsg);
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

						SessionHolder.AddDebugLog("Out. {0},Type={1}. ClassInfo={2}", secMsg, secMsg.SecurityType, classInfo);

						// из квика не транслируется поле тип инструмента, если тип инструмента не найден по классу, то берем по умолчанию.
						secMsg.SecurityType = secMsg.Multiplier == 0 ? SecurityTypes.Index : (classInfo.Item1 ?? SecurityTypes.Stock);

						SessionHolder.ReplaceSecurityId(secMsg.SecurityId, id => secMsg.SecurityId = id);
						break;
					}

					case MessageTypes.Level1Change:
					{
						var l1Msg = (Level1ChangeMessage)message;
						SessionHolder.AddDebugLog("Out. {0}", l1Msg);
						SessionHolder.ReplaceSecurityId(l1Msg.SecurityId, id => l1Msg.SecurityId = id);
						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)message;
						SessionHolder.AddDebugLog("Out. {0}", quoteMsg);
						SessionHolder.ReplaceSecurityId(quoteMsg.SecurityId, id => quoteMsg.SecurityId = id);
						quoteMsg.ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow);
						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;
						SessionHolder.AddDebugLog("Out. {0},OPrice={1},Volume={2}", execMsg, execMsg.Price, execMsg.Volume);
						SessionHolder.ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);
						break;
					}
				}

				base.SendOutMessage(message);
			}

			private void ProcessMarketDataMessage(MarketDataMessage message)
			{
				if (message.IsSubscribe)
				{
					var securityId = new SecurityId
					{
						SecurityCode = message.SecurityId.SecurityCode,
						BoardCode = SessionHolder.GetSecurityClass(message.SecurityId)
					};

					switch (message.DataType)
					{
						case MarketDataTypes.Level1:
							SessionHolder.Requests.Enqueue(new LuaRequest
							{
								MessageType = MessageTypes.Level1Change,
								SecurityId = securityId
							});
							break;

						case MarketDataTypes.Trades:
							SessionHolder.Requests.Enqueue(new LuaRequest
							{
								MessageType = MessageTypes.Execution,
								SecurityId = securityId
							});
							break;

						case MarketDataTypes.MarketDepth:
							SessionHolder.Requests.Enqueue(new LuaRequest
							{
								MessageType = MessageTypes.QuoteChange,
								SecurityId = securityId,
								TransactionId = message.IsSubscribe ? 1 : 0
							});
							break;

						default:
							throw new ArgumentOutOfRangeException("message", message.DataType, LocalizedStrings.Str1618);
					}
				}

				var result = (MarketDataMessage)message.Clone();
				result.OriginalTransactionId = message.TransactionId;
				SendOutMessage(result);
			}
		}

		private sealed class LuaTransactionAdapter : MessageAdapter<LuaSession>
		{
			private readonly Dictionary<long, long> _transactionsByLocalId = new Dictionary<long, long>();
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
				switch (message.Type)
				{
					case MessageTypes.PortfolioLookup:
						var pfMsg = (PortfolioLookupMessage)message;
						SessionHolder.AddDebugLog("In. {0}", pfMsg);
						SessionHolder.Requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.PortfolioLookup,
							TransactionId = pfMsg.TransactionId
						});
						break;

					case MessageTypes.OrderStatus:
						var statusMsg = (OrderStatusMessage)message;
						SessionHolder.AddDebugLog("In. {0}", statusMsg);
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
						SessionHolder.AddDebugLog("In. {0}", orderMsg);
						RegisterTransaction(orderMsg);
						break;
				}
			}

			private void RegisterTransaction(OrderMessage message)
			{
				var transactionId = message.OriginalTransactionId;

				if (transactionId <= 0 || transactionId > uint.MaxValue)
					throw new InvalidOperationException(LocalizedStrings.Str1700Params.Put(transactionId));

				switch (message.Type)
				{
					case MessageTypes.OrderRegister:
						var regMsg = (OrderRegisterMessage)message;
						_transactionsByLocalId.Add(transactionId, regMsg.TransactionId);
						RegisterTransaction(SessionHolder.CreateRegisterTransaction(regMsg, _depoNames.TryGetValue(regMsg.PortfolioName)), transactionId, regMsg.OrderType);
						break;

					case MessageTypes.OrderReplace:
						var replMsg = (OrderReplaceMessage)message;
						_transactionsByLocalId.Add(transactionId, replMsg.TransactionId);
						RegisterTransaction(SessionHolder.CreateMoveTransaction(replMsg), transactionId, replMsg.OrderType);
						break;

					case MessageTypes.OrderCancel:
						var cancelMsg = (OrderCancelMessage)message;
						_transactionsByLocalId.Add(transactionId, cancelMsg.TransactionId);
						RegisterTransaction(SessionHolder.CreateCancelTransaction(cancelMsg), transactionId, cancelMsg.OrderType);
						break;

					case MessageTypes.OrderGroupCancel:
						var cancelGroupMsg = (OrderGroupCancelMessage)message;
						_transactionsByLocalId.Add(transactionId, cancelGroupMsg.TransactionId);
						RegisterTransaction(SessionHolder.CreateCancelFuturesTransaction(cancelGroupMsg), transactionId, cancelGroupMsg.OrderType);
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			private void RegisterTransaction(Transaction transaction, long transactionId, OrderTypes type, bool addTransaction = true)
			{
				if (addTransaction)
					_transactions.Add(transactionId, transaction);

				SessionHolder.Requests.Enqueue(new LuaRequest
				{
					MessageType = MessageTypes.OrderRegister,
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
						SessionHolder.AddDebugLog("Out. {0}", pfMsg);
						SessionHolder.ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);
						break;
					}

					case MessageTypes.PortfolioChange:
					{
						var pfMsg = (PortfolioChangeMessage)message;

						SessionHolder.AddDebugLog("Out. {0}", pfMsg);
						SessionHolder.ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);

						var depoName = (string)pfMsg.Changes.TryGetValue(PositionChangeTypes.DepoName);
						if (!depoName.IsEmpty())
							_depoNames[pfMsg.PortfolioName] = depoName;

						break;
					}

					case MessageTypes.Position:
					{
						var pfMsg = (PositionMessage)message;
						SessionHolder.AddDebugLog("Out. {0}", pfMsg);
						SessionHolder.ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
						break;
					}

					case MessageTypes.PositionChange:
					{
						var pfMsg = (PositionChangeMessage)message;
						SessionHolder.AddDebugLog("Out. {0}", pfMsg);
						SessionHolder.ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						SessionHolder.AddDebugLog("Out. {0},Price={1},Volume={2}", execMsg, execMsg.Price, execMsg.Volume);

						switch (execMsg.ExecutionType)
						{
							case ExecutionTypes.Order:
							{
								if (execMsg.OrderId != 0 && execMsg.OriginalTransactionId != 0)
								{
									_transactionsByOrderId.SafeAdd(execMsg.OrderId, i => execMsg.OriginalTransactionId);

									var trades = _tradesByOrderId.TryGetValue(execMsg.OrderId);

									if (trades != null)
									{
										trades.ForEach(SendOutMessage);
										_tradesByOrderId.Remove(execMsg.OrderId);
									}
								}

								break;
							}

							case ExecutionTypes.Trade:
							{
								if (execMsg.OriginalTransactionId != 0)
									break;

								var origTransactionId = _transactionsByOrderId.TryGetValue2(execMsg.OrderId);

								if (origTransactionId == null)
								{
									_tradesByOrderId.SafeAdd(execMsg.OrderId).Add(execMsg);
									return;
								}
								
								execMsg.OriginalTransactionId = origTransactionId.Value;

								break;
							} 
						}

						SessionHolder.ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);

						// запоминаем номер исходной транзакции, который будет исопльзоваться в FixServer
						execMsg.UserOrderId = execMsg.OriginalTransactionId.To<string>();

						var transaction = _transactions.TryGetValue(execMsg.OriginalTransactionId);

						var transactionId = _transactionsByLocalId.TryGetValue2(execMsg.OriginalTransactionId);
						if (transactionId != null)
							execMsg.OriginalTransactionId = transactionId.Value;

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

			protected override void OnProcess(FixSession session, string client, string msgStr, bool isMarketData)
			{
				var msgType = QuickFix.Message.GetMsgType(msgStr);

				switch (msgType)
				{
					case NewStopOrderSingle.MsgType:
					{
						this.AddInfoLog("From client {0}: NewStopOrderSingle", client);

						var fixMsg = session.ToMessage<NewStopOrderSingle>(msgStr);
						var regMsg = fixMsg.ToRegisterMessage();
						
						regMsg.TransactionId = AddTransactionsMapping(client, regMsg.OriginalTransactionId);

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
					var fixMsg = order.ToExecutionReport<StopOrderExecutionReport>();

					var condition = (QuikOrderCondition)order.Condition;

					fixMsg.Type = new StopOrderExecutionReport.TypeField((int)condition.Type);
					fixMsg.StopPriceCondition = new StopOrderExecutionReport.StopPriceConditionField((int)condition.StopPriceCondition);
					fixMsg.ConditionOrderSide = new StopOrderExecutionReport.ConditionOrderSideField((int)condition.ConditionOrderSide);
					fixMsg.LinkedOrderCancel = new StopOrderExecutionReport.LinkedOrderCancelField(condition.LinkedOrderCancel);

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

		private static readonly LuaFixServer _luaServer = new LuaFixServer();

		/// <summary>
		/// Объект <see cref="LuaFixServer"/>.
		/// </summary>
		public static LuaFixServer Instance
		{
			get { return _luaServer; }
		}

		private readonly LogManager _logManager = new LogManager();

		private readonly FixServerEx _fixServer;
		private readonly LuaMarketDataAdapter _marketDataAdapter;
		private readonly LuaTransactionAdapter _transactionAdapter;
		private readonly LuaSession _sessionHolder;
		private string _login;
		private SecureString _password;

		private sealed class QuikNativeApp : BaseLogReceiver
		{
			public QuikNativeApp()
			{
				Name = "LuaServer";
				LogLevel = LogLevels.Info;
			}
		}

		private LuaFixServer()
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

			_fixServer = new FixServerEx((login, password) =>
			{
				if (_login.IsEmpty() || (login.CompareIgnoreCase(_login) && password.CompareIgnoreCase(_password.To<string>())))
					return Tuple.Create(TimeSpan.FromMilliseconds(100), FixClientRoles.Admin);
				
				return null;
			}, _transactionAdapter, _marketDataAdapter);

			_logManager.Application = new QuikNativeApp();

			_logManager.Sources.Add(_sessionHolder);
			_logManager.Sources.Add(_fixServer);
		}

		/// <summary>
		/// Запустить сервер.
		/// </summary>
		/// <param name="port">Серверный порт, на котором будет работать FIX сервер.</param>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <param name="logLevel">Уровень логирования для Lua.</param>
		/// <param name="logFile">Название текстового файла (без расширения), в который будут писаться логи.</param>
		/// <param name="incrementalDepthUpdates">Отправлять изменения по стакану. Если выключено, отправляется стакан целиком.</param>
		public void Start(int port, string login, string password, LogLevels logLevel, string logFile, bool incrementalDepthUpdates)
		{
			if (!login.IsEmpty())
			{
				if (password.IsEmpty())
					throw new ArgumentNullException("password");
			}

			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var logFileName = Path.Combine(path, logFile + ".log");

			_logManager.Listeners.Add(new FileLogListener(logFileName));

			_login = login;
			_password = password.To<SecureString>();

			_sessionHolder.Requests.Open();

			_fixServer.Port = port;
			_fixServer.IncrementalDepthUpdates = incrementalDepthUpdates;
			_fixServer.Start();

			_logManager.Application.LogLevel = logLevel;
		}

		/// <summary>
		/// Выключить сервер.
		/// </summary>
		public void Stop()
		{
			_sessionHolder.Requests.Close();

			_fixServer.Stop();

			_logManager.Listeners.ForEach(l => l.DoDispose());
			_logManager.Listeners.Clear();
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			switch (message.Type)
			{
				case MessageTypes.Portfolio:
				case MessageTypes.PortfolioChange:
				case MessageTypes.PortfolioLookupResult:
				case MessageTypes.Position:
				case MessageTypes.PositionChange:
					_transactionAdapter.SendOutMessage(message);
					return;

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
		/// Вывести в лог ошибку.
		/// </summary>
		/// <param name="message">Текст ошибки.</param>
		public void AddErrorLog(string message)
		{
			_logManager.Application.AddErrorLog(message);
		}

		/// <summary>
		/// Вывести в лог сообщение.
		/// </summary>
		/// <param name="message">Текст сообщения.</param>
		public void AddInfoLog(string message)
		{
			_logManager.Application.AddInfoLog(message);
		}

		/// <summary>
		/// Вывести в лог сообщение.
		/// </summary>
		/// <param name="message">Текст сообщения.</param>
		public void AddDebugLog(string message)
		{
			_logManager.Application.AddDebugLog(message);
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