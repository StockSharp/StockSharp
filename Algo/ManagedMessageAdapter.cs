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
	public class ManagedMessageAdapter : IMessageAdapter
	{
		private readonly IMessageAdapter _innerAdapter;

		/// <summary>
		/// Вложенный адаптер.
		/// </summary>
		public IMessageAdapter InnerAdapter { get { return _innerAdapter; } }

		/// <summary>
		/// Создать <see cref="ManagedMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Адаптер, в который будут перенаправляться сообщения.</param>
		public ManagedMessageAdapter(IMessageAdapter innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			_innerAdapter = innerAdapter;
			_innerAdapter.NewOutMessage += ProcessOutMessage;

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

		void IDisposable.Dispose()
		{
			_innerAdapter.Dispose();
		}

		MessageAdapterTypes IMessageAdapter.Type
		{
			get { return _innerAdapter.Type; }
		}

		IMessageSessionHolder IMessageAdapter.SessionHolder
		{
			get { return _innerAdapter.SessionHolder; }
		}

		private Action<Message> _newOutMessage;

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add { _newOutMessage += value; }
			remove { _newOutMessage -= value; }
		}

		void IMessageChannel.SendInMessage(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = _innerAdapter.SessionHolder.CurrentTime.LocalDateTime;

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

			_innerAdapter.SendInMessage(message);
		}

		void IMessageAdapter.SendOutMessage(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = _innerAdapter.SessionHolder.CurrentTime.LocalDateTime;

			_innerAdapter.SendOutMessage(message);
		}

		IMessageProcessor IMessageAdapter.InMessageProcessor
		{
			get { return _innerAdapter.InMessageProcessor; }
			set { _innerAdapter.InMessageProcessor = value; }
		}

		IMessageProcessor IMessageAdapter.OutMessageProcessor
		{
			get { return _innerAdapter.OutMessageProcessor; }
			set { _innerAdapter.OutMessageProcessor = value; }
		}

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
				_innerAdapter.SessionHolder.AddWarningLog(LocalizedStrings.Str855Params,
					rule.GetType().GetDisplayName(), rule.Title, rule.Action);
				
				switch (rule.Action)
				{
					case RiskActions.ClosePositions:
					{
						break;
					}
					case RiskActions.StopTrading:
						_innerAdapter.SendInMessage(new DisconnectMessage());
						break;
					case RiskActions.CancelOrders:
						_innerAdapter.SendInMessage(new OrderGroupCancelMessage { TransactionId = _innerAdapter.SessionHolder.TransactionIdGenerator.GetNextId() });
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
	}
}