namespace StockSharp.Quik.Lua
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Net;
	using System.Reflection;
	using System.Security;
	using SystemProcess = System.Diagnostics.Process;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Fix;
	using StockSharp.Fix.Native;
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

		[DisplayName("FixServer")]
		private sealed class FixServerEx : FixServer
		{
			private readonly SynchronizedSet<long> _transactionIds = new SynchronizedSet<long>(); 

			public FixServerEx(Func<string, string, Tuple<TimeSpan, FixClientRoles>> authorize)
				: base(authorize)
			{
			}

			public void AddTransactionId(long transactionId)
			{
				this.AddInfoLog("Added trans id {0} mapping.", transactionId);
				_transactionIds.Add(transactionId);
			}

			protected override long OnCreateTransactionId(FixSession session, string requestId)
			{
				return requestId.To<long>();
			}

			protected override string TryGetRequestId(long transactionId)
			{
				if (_transactionIds.Contains(transactionId))
					return transactionId.To<string>();

				return base.TryGetRequestId(transactionId);
			}

			protected override bool? OnProcess(FixSession session, string msgType, IFixReader reader)
			{
				switch (msgType)
				{
					case QuikFixMessages.NewStopOrderSingle:
					{
						var condition = new QuikOrderCondition();

						var dto = TimeHelper.Moscow.BaseUtcOffset;

						var regMsg = reader.ReadOrderRegisterMessage(dto,
							tag => reader.ReadOrderCondition(tag, dto, condition));

						if (regMsg == null)
							return null;

						regMsg.TransactionId = CreateTransactionId(session, regMsg.TransactionId.To<string>());
						regMsg.Condition = condition;

						RaiseNewOutMessage(regMsg);
						return true;
					}
				}

				return base.OnProcess(session, msgType, reader);
			}

			protected override void WriterFixOrderCondition(IFixWriter writer, ExecutionMessage message)
			{
				writer.WriteOrderCondition((QuikOrderCondition)message.Condition);
			}
		}

		private readonly LogManager _logManager = new LogManager();

		private readonly FixServerEx _fixServer;
		private readonly LuaSession _sessionHolder;
		private readonly SynchronizedDictionary<SecurityId, Level1ChangeMessage> _prevLevel1 = new CachedSynchronizedDictionary<SecurityId, Level1ChangeMessage>();
		
		private readonly Dictionary<long, long> _transactionsByOrderId = new Dictionary<long, long>();

		private readonly Dictionary<long, List<ExecutionMessage>> _tradesByOrderId = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, string> _depoNames = new Dictionary<string, string>();
		private readonly SynchronizedDictionary<long, Transaction> _transactions = new SynchronizedDictionary<long, Transaction>();

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

			_fixServer = new FixServerEx((l, p) =>
			{
				if (Login.IsEmpty() || (l.CompareIgnoreCase(Login) && p == Password))
				{
					_prevLevel1.Clear();
					return Tuple.Create(TimeSpan.FromMilliseconds(100), FixClientRoles.Admin);
				}

				return null;
			});

			_fixServer.NewOutMessage += message =>
			{
				_sessionHolder.AddDebugLog("In. {0}", message);

				switch (message.Type)
				{
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandleVolume:
						throw new NotSupportedException();
					case MessageTypes.MarketData:
					{
						var mdMsg = (MarketDataMessage)message;
						ProcessMarketDataMessage(mdMsg);
						break;
					}
					case MessageTypes.SecurityLookup:
					{
						var secMsg = (SecurityLookupMessage)message;

						var securityId = new SecurityId
						{
							SecurityCode = secMsg.SecurityId.SecurityCode,
							BoardCode = !secMsg.SecurityId.BoardCode.IsEmpty()
								? _sessionHolder.GetSecurityClass(secMsg.SecurityId)
								: null
						};

						_sessionHolder.Requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.SecurityLookup,
							TransactionId = secMsg.TransactionId,
							SecurityId = securityId,
							Value = secMsg.UnderlyingSecurityCode
						});
						break;
					}

					case MessageTypes.OrderPairReplace:
					case MessageTypes.Portfolio:
					case MessageTypes.Position:
						throw new NotSupportedException();

					case MessageTypes.PortfolioLookup:
						var pfMsg = (PortfolioLookupMessage)message;
						_sessionHolder.Requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.PortfolioLookup,
							TransactionId = pfMsg.TransactionId
						});
						break;

					case MessageTypes.OrderStatus:
						var statusMsg = (OrderStatusMessage)message;
						_sessionHolder.Requests.Enqueue(new LuaRequest
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

					default:
						throw new ArgumentOutOfRangeException();
				}
			};

			_logManager.Application = new QuikNativeApp();

			_logManager.Sources.Add(_sessionHolder);
			_logManager.Sources.Add(_fixServer);

			LogFile = "StockSharp.QuikLua.log";

			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var logFileName = Path.Combine(path, LogFile);

			_logManager.Listeners.Add(new FileLogListener(logFileName));
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			_sessionHolder.Requests.Enqueue(new LuaRequest
			{
				MessageType = message.Type,
				DataType = message.DataType,
				SecurityId = new SecurityId
				{
					SecurityCode = message.SecurityId.SecurityCode,
					BoardCode = _sessionHolder.GetSecurityClass(message.SecurityId)
				},
				IsSubscribe = message.IsSubscribe,
				TransactionId = message.TransactionId
			});

			var result = (MarketDataMessage)message.Clone();
			result.OriginalTransactionId = message.TransactionId;
			_fixServer.SendInMessage(result);
		}

		private void ProcessOrderMessage(OrderMessage message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
					var regMsg = (OrderRegisterMessage)message;
					RegisterTransaction(_sessionHolder.CreateRegisterTransaction(regMsg, _depoNames.TryGetValue(regMsg.PortfolioName)), message.Type, regMsg.TransactionId, regMsg.OrderType);
					break;

				case MessageTypes.OrderReplace:
					var replMsg = (OrderReplaceMessage)message;
					RegisterTransaction(_sessionHolder.CreateMoveTransaction(replMsg), message.Type, replMsg.TransactionId, replMsg.OrderType);
					break;

				case MessageTypes.OrderCancel:
					var cancelMsg = (OrderCancelMessage)message;
					RegisterTransaction(_sessionHolder.CreateCancelTransaction(cancelMsg), message.Type, cancelMsg.TransactionId, cancelMsg.OrderType);
					break;

				case MessageTypes.OrderGroupCancel:
					var cancelGroupMsg = (OrderGroupCancelMessage)message;
					RegisterTransaction(_sessionHolder.CreateCancelFuturesTransaction(cancelGroupMsg), message.Type, cancelGroupMsg.TransactionId, cancelGroupMsg.OrderType);
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

			_sessionHolder.Requests.Enqueue(new LuaRequest
			{
				MessageType = messageType,
				TransactionId = transactionId,
				OrderType = type,
				Value = transaction.SetTransactionId(transactionId).ToLuaString()
			});
		}

		/// <summary>
		/// Адрес, на котором FIX сервер будет обрабатывать транзакции.
		/// По-умолчанию равен 127.0.0.1:5001.
		/// </summary>
		public EndPoint TransactionAddress
		{
			get { return _fixServer.TransactionSession.Address; }
			set { _fixServer.TransactionSession.Address = value; }
		}

		/// <summary>
		/// Адрес, на котором FIX сервер будет рассылать маркет-данные.
		/// По-умолчанию равен 127.0.0.1:5001.
		/// </summary>
		public EndPoint MarketDataAddress
		{
			get { return _fixServer.MarketDataSession.Address; }
			set { _fixServer.MarketDataSession.Address = value; }
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
			return _fixServer.HasSubscriptions(dataType, new SecurityId
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
			_fixServer.AddTransactionId(transactionId);
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

					_sessionHolder.ReplaceSecurityId(l1Msg.SecurityId, id => l1Msg.SecurityId = id);

					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;

					var classCode = secMsg.SecurityId.BoardCode;
					var classInfo = _sessionHolder.GetSecurityClassInfo(classCode);

					// из квика не транслируется поле тип инструмента, если тип инструмента не найден по классу, то берем по умолчанию.
					secMsg.SecurityType = secMsg.Multiplier == 0 ? SecurityTypes.Index : (classInfo.Item1 ?? SecurityTypes.Stock);

					_sessionHolder.ReplaceSecurityId(secMsg.SecurityId, id => secMsg.SecurityId = id);
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;
					_sessionHolder.ReplaceSecurityId(quoteMsg.SecurityId, id => quoteMsg.SecurityId = id);
					quoteMsg.ServerTime = _sessionHolder.CurrentTime.Convert(TimeHelper.Moscow);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					var isTransaction = false;

					switch (execMsg.ExecutionType)
					{
						case ExecutionTypes.Order:
						{
							isTransaction = true;

							if (execMsg.OrderId != null && execMsg.OriginalTransactionId != 0)
							{
								_transactionsByOrderId.SafeAdd(execMsg.OrderId.Value, i => execMsg.OriginalTransactionId);

								var trades = _tradesByOrderId.TryGetValue(execMsg.OrderId.Value);

								if (trades != null)
								{
									trades.ForEach(_fixServer.SendInMessage);
									_tradesByOrderId.Remove(execMsg.OrderId.Value);
								}
							}

							break;
						}

						case ExecutionTypes.Trade:
						{
							isTransaction = true;

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

					if (isTransaction)
					{
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
					}

					_sessionHolder.ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					_sessionHolder.ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);
					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var pfMsg = (PortfolioChangeMessage)message;

					_sessionHolder.ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);

					var depoName = (string)pfMsg.Changes.TryGetValue(PositionChangeTypes.DepoName);
					if (!depoName.IsEmpty())
						_depoNames[pfMsg.PortfolioName] = depoName;

					break;
				}

				case MessageTypes.Position:
				{
					var pfMsg = (PositionMessage)message;
					_sessionHolder.ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
					break;
				}

				case MessageTypes.PositionChange:
				{
					var pfMsg = (PositionChangeMessage)message;
					_sessionHolder.ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
					break;
				}
			}

			_fixServer.SendInMessage(message);
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