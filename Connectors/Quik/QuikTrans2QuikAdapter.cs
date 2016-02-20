#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: QuikTrans2QuikAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Text.RegularExpressions;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Quik.Native;
	using StockSharp.Quik.Properties;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Транзакционный адаптер сообщений для Quik, работающий через библиотеку Trans2Quik.dll.
	/// </summary>
	[DisplayName("Quik. Trans DLL")]
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

				if (OverrideDll || !File.Exists(DllName))
				{
					var version = GetTerminal().Version;

					var trans2Quik = version >= "6.3.0.0".To<Version>()
						? Resources.TRANS2QUIK_12
						: (version >= "5.15.0.0".To<Version>() ? Resources.TRANS2QUIK_11 : Resources.TRANS2QUIK_10);

					DllName.CreateDirIfNotExists();

					trans2Quik.Save(DllName);
				}

				_api = new ApiWrapper(DllName);
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
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если было успешно установлено подключение.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, <see langword="false"/>, если торговая система разорвала подключение.</returns>
		public override bool IsConnectionAlive()
		{
			return Api.IsConnected;
		}

		/// <summary>
		/// Отформатировать транзакцию (добавить, удалить или заменить параметры) перед тем, как она будет отправлена в Quik.
		/// </summary>
		public event Action<Transaction> FormatTransaction;

		/// <summary>
		/// Создать <see cref="QuikTrans2QuikAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public QuikTrans2QuikAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Platform = Platforms.x86;
			//SessionHolder.TerminalChanged += ResetApi;
			this.AddTransactionalSupport();
			this.RemoveSupportedMessage(MessageTypes.OrderStatus);
			this.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
		}

		private const string _category = "TRANS2QUIK";

		private string _dllName = "TRANS2QUIK.DLL";

		/// <summary>
		/// Имя dll-файла, содержащее Quik API. По-умолчанию равно TRANS2QUIK.DLL.
		/// </summary>
		[Category(_category)]
		[DisplayNameLoc(LocalizedStrings.Str1777Key)]
		[DescriptionLoc(LocalizedStrings.Str1778Key)]
		[PropertyOrder(0)]
		[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
		public string DllName
		{
			get { return _dllName; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				if (value == _dllName)
					return;

				_dllName = value;
				ResetApi();
			}
		}

		/// <summary>
		/// Асинхронный режим. Если <see langword="true"/>, то все транзакции, такие как <see cref="OrderRegisterMessage"/>
		/// или <see cref="OrderCancelMessage"/> будут отправляться в асинхронном режиме.
		/// </summary>
		/// <remarks>
		/// По-умолчанию используется асинхронный режим.
		/// </remarks>
		[Category(_category)]
		[DisplayNameLoc(LocalizedStrings.Str1781Key)]
		[DescriptionLoc(LocalizedStrings.Str1782Key)]
		[PropertyOrder(2)]
		public bool IsAsyncMode { get; set; } = true;

		/// <summary>
		/// Перезаписать файл библиотеки из ресурсов. По-умолчанию файл будет перезаписан.
		/// </summary>
		[Category(_category)]
		[DisplayNameLoc(LocalizedStrings.OverrideKey)]
		[DescriptionLoc(LocalizedStrings.OverrideDllKey)]
		[PropertyOrder(3)]
		public bool OverrideDll { get; set; } = true;

		/// <summary>
		/// https://forum.quik.ru/forum10/topic1218/
		/// </summary>
		[Category(_category)]
		public bool SingleSlash { get; set; } = true;

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid => !DllName.IsEmpty();

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new QuikOrderCondition();
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
				{
					if (_api != null)
					{
						try
						{
							if (_api.IsConnected)
								_api.Disconnect();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						try
						{
							_api.Dispose();
						}
						catch (Exception ex)
						{
							SendOutError(ex);
						}

						_api = null;
					}

					SendOutMessage(new ResetMessage());

					break;
				}

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
					RegisterTransaction(regMsg.CreateRegisterTransaction(regMsg.GetValue<string>(PositionChangeTypes.DepoName), SecurityClassInfo, SingleSlash));
					break;

				case MessageTypes.OrderReplace:
					RegisterTransaction(((OrderReplaceMessage)message).CreateMoveTransaction(SecurityClassInfo));
					break;

				case MessageTypes.OrderCancel:
					RegisterTransaction(((OrderCancelMessage)message).CreateCancelTransaction(SecurityClassInfo));
					break;

				case MessageTypes.OrderGroupCancel:
					RegisterTransaction(((OrderGroupCancelMessage)message).CreateCancelFuturesTransaction(SecurityClassInfo));
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
				throw new ArgumentNullException(nameof(transaction));

			if (transaction.GetInstruction<long>(Transaction.TransactionId) != 0)
				throw new ArgumentException();

			var transactionId = transaction.TryGetTransactionId();

			if (transactionId == 0)
				transactionId = TransactionIdGenerator.GetNextId();

			if (transactionId <= 0 || transactionId > uint.MaxValue)
				throw new InvalidOperationException(LocalizedStrings.Str1700Params.Put(transactionId));

			FormatTransaction.SafeInvoke(transaction.SetTransactionId(transactionId));

			_transactions.Add(transactionId, transaction);

			if (IsAsyncMode)
				Api.SendAsyncTransaction(transaction.ToString());
			else
			{
				Exception error = null;

				// http://stocksharp.com/forum/yaf_postst2247_Oshibka-pri-kotirovanii--sinkhronnyie-tranzaktsii.aspx

				var execution = transaction.Message.ToExecutionMessage();

				if (execution == null)
					throw new ArgumentException(LocalizedStrings.Str1835, nameof(transaction));

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
						ProcessTransactionReply(execution, transaction, 0, apiMessage, apiEx?.Code ?? Codes.Failed, ex);
					else
					{
						execution.OrderState = OrderStates.Failed;
						execution.Error = apiEx ?? new ApiException(Codes.Failed, apiMessage);
						SendOutMessage(execution);
					}

					error = ex;
				}

				if (error != null)
					error.Throw();
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
								bool isAlive;

								try
								{
									isAlive = IsConnectionAlive();
								}
								catch
								{
									isAlive = false;
								}

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
			this.AddDebugLog("Order: Mode {0} transId {1} orderId {2} classCode {3} secCode {4} price {5} balance {6} volume {7} side {8} state {9}", mode, transId, orderId, classCode, secCode, price, balance, volume, side, state);
		}

		private void OnTradeReply(Modes mode, long tradeId, long orderId, string classCode, string secCode, double price, int balance, int volume, Sides side)
		{
			this.AddDebugLog("Trade: Mode {0} tradeId {1} orderId {2} classCode {3} secCode {4} price {5} balance {6} volume {7} side {8}", mode, tradeId, orderId, classCode, secCode, price, balance, volume, side);
		}

		private void OnTransactionReply(uint transactionId, Codes replyCode, Codes extendedCode, OrderStatus status, long orderId, string message)
		{
			this.AddDebugLog("Order: transId {0} replyCode {1} extendedCode {2} status {3} orderId {4} message {5}", transactionId, replyCode, extendedCode, status, orderId, message);

			if (!IsAsyncMode)
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
				OriginalTransactionId = replaceMessage.TransactionId,
				//OrderId = replaceMessage.OldOrderId,
				ExecutionType = ExecutionTypes.Transaction,
				OrderState = OrderStates.Failed,
				//IsCancelled = true,
				Error = error,
				HasOrderInfo = true,
				ServerTime = CurrentTime,
			});

			//SendOutMessage(new ExecutionMessage
			//{
			//	SecurityId = replaceMessage.SecurityId,
			//	OriginalTransactionId = replaceMessage.TransactionId,
			//	ExecutionType = ExecutionTypes.Transaction,
			//	OrderState = OrderStates.Failed,
			//	Error = error,
			//	HasOrderInfo = true,
			//	ServerTime = CurrentTime,
			//});
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(DllName), DllName);
			storage.SetValue(nameof(IsAsyncMode), IsAsyncMode);
			storage.SetValue(nameof(OverrideDll), OverrideDll);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			DllName = storage.GetValue<string>(nameof(DllName));
			IsAsyncMode = storage.GetValue<bool>(nameof(IsAsyncMode));
			OverrideDll = storage.GetValue<bool>(nameof(OverrideDll));

			base.Load(storage);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			//SessionHolder.TerminalChanged -= ResetApi;

			ResetApi();

			base.DisposeManaged();
		}
	}
}