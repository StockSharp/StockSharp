namespace StockSharp.Algo.Latency
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Менеджер расчета задержки регистрации заявок.
	/// </summary>
	public class LatencyManager : ILatencyManager
	{
		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, DateTime> _register = new Dictionary<long, DateTime>();
		private readonly Dictionary<long, DateTime> _cancel = new Dictionary<long, DateTime>();

		/// <summary>
		/// Создать <see cref="LatencyManager"/>
		/// </summary>
		public LatencyManager()
		{
		}

		/// <summary>
		/// Суммарное значение задержки регистрации по всем заявкам.
		/// </summary>
		public virtual TimeSpan LatencyRegistration { get; private set; }

		/// <summary>
		/// Суммарное значение задержки отмены по всем заявкам.
		/// </summary>
		public virtual TimeSpan LatencyCancellation { get; private set; }

		/// <summary>
		/// Обработать сообщение для вычисления задержки транзакции. Принимаются сообщения типа
		/// <see cref="OrderRegisterMessage"/>, <see cref="OrderReplaceMessage"/>, <see cref="OrderPairReplaceMessage"/>,
		/// <see cref="OrderCancelMessage"/> и <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Задержка транзакции.</returns>
		public TimeSpan? ProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					lock (_syncObject)
					{
						AddRegister(regMsg.TransactionId, regMsg.LocalTime);
					}

					break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					lock (_syncObject)
					{
						AddCancel(replaceMsg.TransactionId, replaceMsg.LocalTime);
						AddRegister(replaceMsg.TransactionId, replaceMsg.LocalTime);
					}

					break;
				}
				case MessageTypes.OrderPairReplace:
				{
					var replaceMsg = (OrderPairReplaceMessage)message;

					lock (_syncObject)
					{
						AddCancel(replaceMsg.Message1.TransactionId, replaceMsg.LocalTime);
						AddRegister(replaceMsg.Message1.TransactionId, replaceMsg.LocalTime);

						AddCancel(replaceMsg.Message2.TransactionId, replaceMsg.LocalTime);
						AddRegister(replaceMsg.Message2.TransactionId, replaceMsg.LocalTime);
					}

					break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					lock (_syncObject)
						AddCancel(cancelMsg.TransactionId, cancelMsg.LocalTime);

					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType == ExecutionTypes.Order)
					{
						if (execMsg.OrderState == OrderStates.Pending)
							return null;

						lock (_syncObject)
						{
							var time = _register.TryGetValue2(execMsg.OriginalTransactionId);

							if (time == null)
							{
								time = _cancel.TryGetValue2(execMsg.OriginalTransactionId);

								if (time != null)
								{
									_cancel.Remove(execMsg.OriginalTransactionId);

									if (execMsg.OrderState == OrderStates.Failed)
										return null;

									return execMsg.LocalTime - time;
								}
							}
							else
							{
								_register.Remove(execMsg.OriginalTransactionId);

								if (execMsg.OrderState == OrderStates.Failed)
									return null;

								return execMsg.LocalTime - time;
							}
						}
					}

					break;
				}
			}

			return null;
		}

		private void AddRegister(long transactionId, DateTime localTime)
		{
			if (transactionId == 0)
				throw new ArgumentNullException("transactionId");

			if (localTime == default(DateTime))
				throw new ArgumentNullException("localTime");

			if (_register.ContainsKey(transactionId))
				throw new ArgumentException(LocalizedStrings.Str1106Params.Put(transactionId), "transactionId");

			_register.Add(transactionId, localTime);
		}

		private void AddCancel(long transactionId, DateTime localTime)
		{
			if (transactionId == 0)
				throw new ArgumentNullException("transactionId");

			if (localTime == default(DateTime))
				throw new ArgumentNullException("localTime");

			if (_cancel.ContainsKey(transactionId))
				throw new ArgumentException(LocalizedStrings.Str1107Params.Put(transactionId), "transactionId");

			_cancel.Add(transactionId, localTime);
		}

		/// <summary>
		/// Обнулить расчеты.
		/// </summary>
		public virtual void Reset()
		{
			LatencyRegistration = LatencyCancellation = TimeSpan.Zero;
		}
	}
}