namespace StockSharp.Algo
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Latency;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Risk;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Адаптер сообщения, вычисляющий автоматически проскальзывание, сетевые задержки и т.д.
	/// </summary>
	public class ManagedMessageAdapter : MessageAdapterWrapper
	{
		
		/// <summary>
		/// Создать <see cref="ManagedMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Адаптер, в который будут перенаправляться сообщения.</param>
		public ManagedMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			InnerAdapter.NewOutMessage += ProcessOutMessage;

			CommissionManager = new CommissionManager();
			LatencyManager = new LatencyManager();
			RiskManager = new RiskManager();
		}

		/// <summary>
		/// Менеджер расчета комиссии.
		/// </summary>
		public ICommissionManager CommissionManager { get; private set; }

		/// <summary>
		/// Менеджер расчета задержки регистрации заявок.
		/// </summary>
		public ILatencyManager LatencyManager { get; private set; }

		/// <summary>
		/// Менеджер расчета задержки регистрации заявок.
		/// </summary>
		public IRiskManager RiskManager { get; private set; }

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			InnerAdapter.NewOutMessage -= ProcessOutMessage;
			base.Dispose();
		}

		private Action<Message> _newOutMessage;

		/// <summary>
		/// New message event.
		/// </summary>
		public override event Action<Message> NewOutMessage
		{
			add { _newOutMessage += value; }
			remove { _newOutMessage -= value; }
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = InnerAdapter.CurrentTime.LocalDateTime;

			if (message.Type == MessageTypes.Connect)
			{
				LatencyManager.Reset();
				CommissionManager.Reset();
				RiskManager.Reset();
			}
			else
			{
				LatencyManager.ProcessMessage(message);
				ProcessRisk(message);
			}

			InnerAdapter.SendInMessage(message);
		}

		//void IMessageAdapter.SendOutMessage(Message message)
		//{
		//	if (message.LocalTime.IsDefault())
		//		message.LocalTime = _innerAdapter.CurrentTime.LocalDateTime;

		//	_innerAdapter.SendOutMessage(message);
		//}

		private void ProcessOutMessage(Message message)
		{
			ProcessExecution(message);
			ProcessRisk(message);

			_newOutMessage.SafeInvoke(message);
		}

		private void ProcessExecution(Message message)
		{
			if (message.Type != MessageTypes.Execution)
				return;

			var execMsg = (ExecutionMessage)message;

			var latency = LatencyManager.ProcessMessage(execMsg);

			if (latency != null)
				execMsg.Latency = latency;

			if (execMsg.Commission == null)
				execMsg.Commission = CommissionManager.ProcessExecution(execMsg);
		}

		private void ProcessRisk(Message message)
		{
			foreach (var rule in RiskManager.ProcessRules(message))
			{
				InnerAdapter.AddWarningLog(LocalizedStrings.Str855Params,
					rule.GetType().GetDisplayName(), rule.Title, rule.Action);
				
				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
					{
						break;
					}
					case RiskActions.StopTrading:
						InnerAdapter.SendInMessage(new DisconnectMessage());
						break;
					case RiskActions.CancelOrders:
						InnerAdapter.SendInMessage(new OrderGroupCancelMessage { TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId() });
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		public void ProcessMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			ProcessExecution(message);
		}

		/// <summary>
		/// Create a copy of <see cref="ManagedMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new ManagedMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}