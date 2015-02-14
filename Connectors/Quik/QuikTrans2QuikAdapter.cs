namespace StockSharp.Quik
{
	using System;
	using System.IO;
	using System.Text.RegularExpressions;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Quik.Native;
	using StockSharp.Quik.Properties;
	using StockSharp.Localization;

	/// <summary>
	/// Транзакционный адаптер сообщений для Quik, работающий через библиотеку Trans2Quik.dll.
	/// </summary>
	public class QuikTrans2QuikAdapter : QuikMessageAdapter
	{
		private readonly Regex _changeOrdersRegex = new Regex(@"ID:(?<order>\s\d+)", RegexOptions.Compiled);
		private readonly SynchronizedDictionary<long, Transaction> _transactions = new SynchronizedDictionary<long, Transaction>();

		private ApiWrapper _api;

		private ApiWrapper Api
		{
			get
			{
				if (_api != null)
					return _api;

				if (!File.Exists(SessionHolder.DllName))
				{
					var version = GetTerminal().Version;

					var trans2Quik = version >= "6.3.0.0".To<Version>()
						? Resources.TRANS2QUIK_12
						: (version >= "5.15.0.0".To<Version>() ? Resources.TRANS2QUIK_11 : Resources.TRANS2QUIK_10);

					SessionHolder.DllName.CreateDirIfNotExists();

					trans2Quik.Save(SessionHolder.DllName);
				}

				_api = new ApiWrapper(SessionHolder.DllName);
				_api.ConnectionChanged += OnConnectionChanged;
				_api.OrderReply += OnOrderReply;
				_api.TradeReply += OnTradeReply;
				_api.TransactionReply += OnTransactionReply;

				return _api;
			}
			set
			{
				if (_api == value)
					return;

				if (_api != null)
				{
					_api.ConnectionChanged -= OnConnectionChanged;
					_api.OrderReply -= OnOrderReply;
					_api.TradeReply -= OnTradeReply;
					_api.TransactionReply -= OnTransactionReply;
					_api.Dispose();
				}

				_api = value;
			}
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение.
		/// </summary>
		public bool IsConnectionAlive { get { return Api.IsConnected; } }

		/// <summary>
		/// Отформатировать транзакцию (добавить, удалить или заменить параметры) перед тем, как она будет отправлена в Quik.
		/// </summary>
		public event Action<Transaction> FormatTransaction;

		/// <summary>
		/// Создать <see cref="QuikTrans2QuikAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public QuikTrans2QuikAdapter(QuikSessionHolder sessionHolder)
			: base(MessageAdapterTypes.Transaction, sessionHolder)
		{
			Platform = Platforms.x86;
			SessionHolder.DllNameChanged += ResetApi;
			SessionHolder.TerminalChanged += ResetApi;
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
				{
					var terminal = GetTerminal();

					if (!terminal.IsLaunched)
						terminal.AssignProcess();

					Api.Connect(terminal.DirectoryName);

					break;
				}

				case MessageTypes.Disconnect:
					Api.Disconnect();
					break;

				case MessageTypes.OrderRegister:
					var regMsg = (OrderRegisterMessage)message;
					RegisterTransaction(SessionHolder.CreateRegisterTransaction(regMsg, regMsg.GetValue<string>(PositionChangeTypes.DepoName)));
					break;

				case MessageTypes.OrderReplace:
					RegisterTransaction(SessionHolder.CreateMoveTransaction((OrderReplaceMessage)message));
					break;

				case MessageTypes.OrderCancel:
					RegisterTransaction(SessionHolder.CreateCancelTransaction((OrderCancelMessage)message));
					break;

				case MessageTypes.OrderGroupCancel:
					RegisterTransaction(SessionHolder.CreateCancelFuturesTransaction((OrderGroupCancelMessage)message));
					break;
			}
		}

		/// <summary>
		/// Зарегистрировать транзакцию.
		/// </summary>
		/// <param name="transaction">Транзакция.</param>
		public void RegisterTransaction(Transaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException("transaction");

			if (transaction.GetInstruction<long>(Transaction.TransactionId) != 0)
				throw new ArgumentException();

			var transactionId = transaction.TryGetTransactionId();

			if (transactionId == 0)
				transactionId = TransactionIdGenerator.GetNextId();

			if (transactionId <= 0 || transactionId > uint.MaxValue)
				throw new InvalidOperationException(LocalizedStrings.Str1700Params.Put(transactionId));

			FormatTransaction.SafeInvoke(transaction.SetTransactionId(transactionId));

			_transactions.Add(transactionId, transaction);

			if (SessionHolder.IsAsyncMode)
				Api.SendAsyncTransaction(transaction.ToString());
			else
			{
				Exception error = null;

				// http://stocksharp.com/forum/yaf_postst2247_Oshibka-pri-kotirovanii--sinkhronnyie-tranzaktsii.aspx

				var execution = transaction.Message.ToExecutionMessage();

				if (execution == null)
					throw new ArgumentException(LocalizedStrings.Str1835, "transaction");

				var isReRegistering = transaction.TransactionType == TransactionTypes.ReRegister;
				var isRegistering = transaction.TransactionType == TransactionTypes.Register || isReRegistering;

				var apiMessage = "";

				try
				{
					long orderId;
					uint transId;
					OrderStatus status;

					var transactionTxt = transaction.ToString();

					Api.SendSyncTransaction(transactionTxt, out status, out transId, out orderId, out apiMessage);

					var isMatchOrCancel = (transaction.Message.Type == MessageTypes.OrderRegister || transaction.Message.Type == MessageTypes.OrderReplace)
										  && ((OrderRegisterMessage)transaction.Message).TimeInForce == TimeInForce.MatchOrCancel;

					if ((!isMatchOrCancel && status != OrderStatus.Accepted) || (isMatchOrCancel && !TransactionHelper.IfFOKCancelMessage(apiMessage) && orderId == 0))
						throw new InvalidOperationException(LocalizedStrings.Str1836Params.Put(transactionTxt, apiMessage));

					execution.OrderStatus = status;
					execution.SystemComment = apiMessage;

					if (isRegistering)
						ProcessTransactionReply(execution, transaction, orderId, apiMessage, Codes.Success, null);
				}
				catch (Exception ex)
				{
					var apiEx = ex as ApiException;

					if (isRegistering)
						ProcessTransactionReply(execution, transaction, 0, apiMessage, apiEx != null ? apiEx.Code : Codes.Failed, ex);
					else
					{
						execution.OrderState = OrderStates.Failed;
						execution.Error = apiEx ?? new ApiException(Codes.Failed, apiMessage);
						SendOutMessage(execution);
					}

					error = ex;
				}

				if (error != null)
					throw error;
			}
		}

		/// <summary>
		/// Получить транзакцию по идентификатору.
		/// </summary>
		/// <param name="id">Идентификатор транзакции.</param>
		/// <returns>Транзакция.</returns>
		public Transaction GetTransaction(long id)
		{
			return _transactions.TryGetValue(id);
		}

		private void ResetApi()
		{
			Api = null;
		}

		private void OnConnectionChanged(Codes code, Exception error, string text)
		{
			try
			{
				Message message;

				if (error == null)
				{
					switch (code)
					{
						case Codes.DllConnected:
						case Codes.QuikConnected:
							try
							{
								var isAlive = IsConnectionAlive;

								GetTerminal().AssignProcess();

								message = new ConnectMessage
								{
									Error = isAlive ? null : new ApiException(code, LocalizedStrings.Str1837)
								};
							}
							catch (Exception ex)
							{
								message = new ConnectMessage { Error = ex };
							}
							break;
						case Codes.DllDisconnected:
							message = new DisconnectMessage();
							break;
						case Codes.QuikDisconnected:
							message = new ConnectMessage { Error = new ApiException(code, text) };
							break;
						default:
							message = new ConnectMessage
							{
								Error = new InvalidOperationException(LocalizedStrings.Str1838Params.Put(code))
							};
							break;
					}
				}
				else
				{
					message = new ConnectMessage { Error = error };
				}

				SendOutMessage(message);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private void OnOrderReply(Modes mode, uint transId, long orderId, string classCode, string secCode, double price, int balance, int volume, Sides side, OrderStates state)
		{
			SessionHolder.AddDebugLog("Order: Mode {0} transId {1} orderId {2} classCode {3} secCode {4} price {5} balance {6} volume {7} side {8} state {9}", mode, transId, orderId, classCode, secCode, price, balance, volume, side, state);
		}

		private void OnTradeReply(Modes mode, long tradeId, long orderId, string classCode, string secCode, double price, int balance, int volume, Sides side)
		{
			SessionHolder.AddDebugLog("Trade: Mode {0} tradeId {1} orderId {2} classCode {3} secCode {4} price {5} balance {6} volume {7} side {8}", mode, tradeId, orderId, classCode, secCode, price, balance, volume, side);
		}

		private void OnTransactionReply(uint transactionId, Codes replyCode, Codes extendedCode, OrderStatus status, long orderId, string message)
		{
			SessionHolder.AddDebugLog("Order: transId {0} replyCode {1} extendedCode {2} status {3} orderId {4} message {5}", transactionId, replyCode, extendedCode, status, orderId, message);

			if (!SessionHolder.IsAsyncMode)
				return;

			try
			{
				var builder = _transactions.TryGetValue(transactionId);

				if (builder == null)
					throw new InvalidOperationException(LocalizedStrings.Str1839Params.Put(transactionId));

				if (builder.TransactionType == TransactionTypes.CancelGroup)
				{
					if (replyCode != Codes.Success || status != OrderStatus.Accepted)
						SendOutError(new ApiException(replyCode, message));

					return;
				}

				if (builder.TransactionType == TransactionTypes.Register && extendedCode == Codes.Success && orderId == 0)
					extendedCode = Codes.Failed;

				var isCancelFailed = builder.TransactionType == TransactionTypes.Cancel && status != OrderStatus.Accepted;

				if (isCancelFailed)
					extendedCode = Codes.Failed;

				ApiException exception = null;

				if (extendedCode != Codes.Success)
					exception = new ApiException(extendedCode, message);

				var orderMessage = builder.Message.ToExecutionMessage();

				orderMessage.SystemComment = message;

				if (!isCancelFailed)
					orderMessage.OrderStatus = status;

				ProcessTransactionReply(orderMessage, builder, orderId, message, replyCode, exception);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}
		}

		private void ProcessTransactionReply(ExecutionMessage orderMessage, Transaction builder, long orderId, string message, Codes replyCode, Exception exception)
		{
			switch (builder.TransactionType)
			{
				case TransactionTypes.Register:
					if (exception != null || orderId == 0)
					{
						if (exception == null)
							exception = new ApiException(replyCode, message);

						SendOrderError(orderMessage, exception);
					}
					else
						TrySendFokOrder(orderMessage, orderId, message);

					break;

				case TransactionTypes.ReRegister:
					if (exception == null)
					{
						orderId = GetReplacedOrderId(orderMessage.OriginalTransactionId, message, out exception);

						if (orderId == 0)
						{
							if (exception == null)
								exception = new ApiException(replyCode, message);

							SendReplaceOrderError((OrderReplaceMessage)builder.Message, exception);
						}
						else
							TrySendFokOrder(orderMessage, orderId, message);
					}
					else
						SendReplaceOrderError((OrderReplaceMessage)builder.Message, exception);

					break;

				case TransactionTypes.Cancel:
					if (exception != null)
					{
						SendOrderError(orderMessage, exception);
					}
					break;

				default:
					throw new NotSupportedException(LocalizedStrings.Str1840Params.Put(builder.TransactionType));
			}
		}

		private long GetReplacedOrderId(long transactionId, string message, out Exception error)
		{
			var matches = _changeOrdersRegex.Matches(message);

			if (matches.Count == 2)
			{
				error = null;
				return matches[0].Groups["order"].Value.To<long>();
			}

			error = new InvalidOperationException(LocalizedStrings.Str1841Params.Put(_transactions.TryGetValue(transactionId), message));

			return 0;
		}

		private static bool IsFokTransaction(long orderId, string message)
		{
			return orderId != 0 && TransactionHelper.IfFOKCancelMessage(message);
		}

		private void SendOrderError(ExecutionMessage orderMessage, Exception error)
		{
			orderMessage.OrderState = OrderStates.Failed;
			orderMessage.Error = error;

			SendOutMessage(orderMessage);
		}

		private void TrySendFokOrder(ExecutionMessage orderMessage, long orderId, string message)
		{
			if (!IsFokTransaction(orderId, message))
				return;

			orderMessage.OrderState = OrderStates.Done;
			orderMessage.Balance = 0;

			SendOutMessage(orderMessage);
		}

		private void SendReplaceOrderError(OrderReplaceMessage replaceMessage, Exception error)
		{
			SendOutMessage(new ExecutionMessage
			{
				SecurityId = replaceMessage.SecurityId,
				OriginalTransactionId = replaceMessage.OldTransactionId,
				OrderId = replaceMessage.OldOrderId,
				ExecutionType = ExecutionTypes.Order,
				OrderState = OrderStates.Failed,
				IsCancelled = true,
				Error = error
			});

			SendOutMessage(new ExecutionMessage
			{
				SecurityId = replaceMessage.SecurityId,
				OriginalTransactionId = replaceMessage.TransactionId,
				ExecutionType = ExecutionTypes.Order,
				OrderState = OrderStates.Failed,
				Error = error
			});
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			SessionHolder.DllNameChanged -= ResetApi;
			SessionHolder.TerminalChanged -= ResetApi;

			ResetApi();

			base.DisposeManaged();
		}
	}
}