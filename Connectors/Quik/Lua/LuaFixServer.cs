#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Lua.QuikPublic
File: LuaFixServer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik.Lua
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Net;
	using System.Reflection;
	using System.Security;

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
				if (transactionId == 0)
					throw new ArgumentNullException(nameof(transactionId));

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

			protected override OrderCondition CreateCondition()
			{
				return new QuikOrderCondition();
			}

			protected override void PostInitCondition(char ordType, OrderCondition condition)
			{
			}

			protected override bool ReadOrderCondition(IFixReader reader, FixTags tag, Func<OrderCondition> getCondition)
			{
				return reader.ReadOrderCondition(tag, TimeHelper.Moscow, TransactionSession.DateTimeFormat, () => (QuikOrderCondition)getCondition());
			}

			protected override void WriterFixOrderCondition(IFixWriter writer, ExecutionMessage message)
			{
				writer.WriteOrderCondition((QuikOrderCondition)message.Condition, TransactionSession.DateTimeFormat);
			}
		}

		private readonly LogManager _logManager = new LogManager();

		private readonly FixServerEx _fixServer;
		private readonly SynchronizedDictionary<SecurityId, Level1ChangeMessage> _prevLevel1 = new CachedSynchronizedDictionary<SecurityId, Level1ChangeMessage>();
		
		private readonly Dictionary<string, string> _depoNames = new Dictionary<string, string>();
		private readonly SynchronizedDictionary<long, Transaction> _transactions = new SynchronizedDictionary<long, Transaction>();

		private readonly BlockingQueue<LuaRequest> _requests = new BlockingQueue<LuaRequest>();

		private readonly IDictionary<string, RefPair<SecurityTypes, string>> _securityClassInfo = new SynchronizedDictionary<string, RefPair<SecurityTypes, string>>();

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
			_requests.Close();
			_securityClassInfo.FillDefault();

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
				_fixServer.AddDebugLog("In. {0}", message);

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
								? _securityClassInfo.GetSecurityClass(secMsg.SecurityId)
								: null
						};

						_requests.Enqueue(new LuaRequest
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
						_requests.Enqueue(new LuaRequest
						{
							MessageType = MessageTypes.PortfolioLookup,
							TransactionId = pfMsg.TransactionId
						});
						break;

					case MessageTypes.OrderStatus:
						var statusMsg = (OrderStatusMessage)message;
						_requests.Enqueue(new LuaRequest
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

			_fixServer.TransactionSession.TimeZone = TimeHelper.Moscow;
			_fixServer.MarketDataSession.TimeZone = TimeHelper.Moscow;

			_logManager.Application = new QuikNativeApp();

			_logManager.Sources.Add(_fixServer);

			LogFile = "StockSharp.QuikLua.log";
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			_requests.Enqueue(new LuaRequest
			{
				MessageType = message.Type,
				DataType = message.DataType,
				SecurityId = new SecurityId
				{
					SecurityCode = message.SecurityId.SecurityCode,
					BoardCode = _securityClassInfo.GetSecurityClass(message.SecurityId)
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
					RegisterTransaction(regMsg.CreateRegisterTransaction(_depoNames.TryGetValue(regMsg.PortfolioName), _securityClassInfo, SingleSlash), message.Type, regMsg.TransactionId, regMsg.OrderType);
					break;

				case MessageTypes.OrderReplace:
					var replMsg = (OrderReplaceMessage)message;
					RegisterTransaction(replMsg.CreateMoveTransaction(_securityClassInfo), message.Type, replMsg.TransactionId, replMsg.OrderType);
					break;

				case MessageTypes.OrderCancel:
					var cancelMsg = (OrderCancelMessage)message;
					RegisterTransaction(cancelMsg.CreateCancelTransaction(_securityClassInfo), message.Type, cancelMsg.TransactionId, cancelMsg.OrderType);
					break;

				case MessageTypes.OrderGroupCancel:
					var cancelGroupMsg = (OrderGroupCancelMessage)message;
					RegisterTransaction(cancelGroupMsg.CreateCancelFuturesTransaction(_securityClassInfo), message.Type, cancelGroupMsg.TransactionId, cancelGroupMsg.OrderType);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void RegisterTransaction(Transaction transaction, MessageTypes messageType, long transactionId, OrderTypes? type)
		{
			if (transactionId <= 0 || transactionId > uint.MaxValue)
				throw new InvalidOperationException(LocalizedStrings.Str1700Params.Put(transactionId));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			_transactions.Add(transactionId, transaction);

			_requests.Enqueue(new LuaRequest
			{
				MessageType = messageType,
				TransactionId = transactionId,
				OrderType = type.Value,
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
			get { return _fixServer.MarketDataSession.IncrementalDepthUpdates; }
			set { _fixServer.MarketDataSession.IncrementalDepthUpdates = value; }
		}

		/// <summary>
		/// https://forum.quik.ru/forum10/topic1218/
		/// </summary>
		public bool SingleSlash { get; set; } = true;

		private FileLogListener _prevFileLogListener;
		private string _logFile;

		/// <summary>
		/// Название текстового файла, в который будут писаться логи.
		/// </summary>
		public string LogFile
		{
			get { return _logFile; }
			set
			{				
				if (_logFile.CompareIgnoreCase(value))
					return;

				if (_prevFileLogListener != null)
				{
					_logManager.Listeners.Remove(_prevFileLogListener);
					_prevFileLogListener.Dispose();
					_prevFileLogListener = null;
				}

				_logFile = value;

				if (_logFile.IsEmpty())
					return;

				var path = _logFile;

				if (!Path.IsPathRooted(_logFile))
					path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);

				_prevFileLogListener = new FileLogListener(path);
				_logManager.Listeners.Add(_prevFileLogListener);
			}
		}

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
		public ILogReceiver LogReceiver => _logManager.Application;

		/// <summary>
		/// Запустить сервер.
		/// </summary>
		public void Start()
		{
			_requests.Open();
			_fixServer.Start();
		}

		/// <summary>
		/// Выключить сервер.
		/// </summary>
		public void Stop()
		{
			_requests.Close();
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
				BoardCode = GetBoardCode(securityId.BoardCode)
			});
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void Process(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

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

					ReplaceSecurityId(l1Msg.SecurityId, id => l1Msg.SecurityId = id);

					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;

					var classCode = secMsg.SecurityId.BoardCode;
					var classInfo = _securityClassInfo.GetSecurityClassInfo(classCode);

					// из квика не транслируется поле тип инструмента, если тип инструмента не найден по классу, то берем по умолчанию.
					secMsg.SecurityType = secMsg.Multiplier == 0 ? SecurityTypes.Index : (classInfo.Item1 ?? SecurityTypes.Stock);

					ReplaceSecurityId(secMsg.SecurityId, id => secMsg.SecurityId = id);
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;
					ReplaceSecurityId(quoteMsg.SecurityId, id => quoteMsg.SecurityId = id);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.HasOrderInfo())
					{
						if (execMsg.OrderId != null)
						{
							if (execMsg.TransactionId == 0)
							{
								// TODO

								// автоинкремент проставляется для ручных заявок, но возникает баг,
								// что и для заявок робота может приходить нулевой идентификатор
								// https://forum.quik.ru/forum10/topic870/
								//
								//	execMsg.TransactionId = GetNextTransactionId();
							}
							else
								_fixServer.AddTransactionId(execMsg.TransactionId);

							_fixServer.AddTransactionId(execMsg.OriginalTransactionId);
						}

						var transaction = _transactions.TryGetValue(execMsg.OriginalTransactionId);

						if (transaction != null && execMsg.Error != null)
						{
							switch (transaction.TransactionType)
							{
								//case TransactionTypes.ReRegister:
								//{
								//	var replaceMsg = (OrderReplaceMessage)transaction.Message;

								//	// дополнительно отправляем сообщение ошибки снятия заявки
								//	var cancelErrMsg = (ExecutionMessage)execMsg.Clone();
								//	cancelErrMsg.OrderId = replaceMsg.OldOrderId;
								//	cancelErrMsg.IsCancelled = true;

								//	break;
								//}

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

					ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);
					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var pfMsg = (PortfolioChangeMessage)message;

					ReplaceBoardCode(pfMsg.BoardCode, board => pfMsg.BoardCode = board);

					var depoName = (string)pfMsg.Changes.TryGetValue(PositionChangeTypes.DepoName);
					if (!depoName.IsEmpty())
						_depoNames[pfMsg.PortfolioName] = depoName;

					break;
				}

				case MessageTypes.Position:
				{
					var pfMsg = (PositionMessage)message;
					ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
					break;
				}

				case MessageTypes.PositionChange:
				{
					var pfMsg = (PositionChangeMessage)message;
					ReplaceSecurityId(pfMsg.SecurityId, id => pfMsg.SecurityId = id);
					break;
				}
			}

			_fixServer.SendInMessage(message);
		}

		private void ReplaceSecurityId(SecurityId securityId, Action<SecurityId> setSecurityId)
		{
			if (setSecurityId == null)
				throw new ArgumentNullException(nameof(setSecurityId));

			ReplaceBoardCode(securityId.BoardCode, boardCode => setSecurityId(new SecurityId { SecurityCode = securityId.SecurityCode, BoardCode = boardCode }));
		}

		private void ReplaceBoardCode(string classCode, Action<string> setBoardCode)
		{
			if (setBoardCode == null)
				throw new ArgumentNullException(nameof(setBoardCode));

			if (classCode.IsEmpty())
				return;

			var info = _securityClassInfo.GetSecurityClassInfo(classCode);

			if (info == null)
				return;

			setBoardCode(info.Item2);
		}

		private string GetBoardCode(string classCode)
		{
			if (classCode.IsEmpty())
				return classCode;

			var info = _securityClassInfo.GetSecurityClassInfo(classCode);

			if (info == null)
				return classCode;

			return info.Item2;
		}

		/// <summary>
		/// Получить пользовательский запрос.
		/// </summary>
		/// <returns>Пользовательский запрос.</returns>
		public LuaRequest GetNextRequest()
		{
			LuaRequest request;
			_requests.TryDequeue(out request);
			return request;
		}

		private readonly IncrementalIdGenerator _idGenerator = new IncrementalIdGenerator();

		/// <summary>
		/// Получить номер транзакции для ручной заявки. Начинается с 1.
		/// </summary>
		/// <returns>Номер транзакции.</returns>
		public long GetNextTransactionId()
		{
			return _idGenerator.GetNextId();
		}
	}
}