namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

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
			_innerAdapter.NewOutMessage -= ProcessOutMessage;
			//_innerAdapter.Dispose();
		}

		ReConnectionSettings IMessageAdapter.ReConnectionSettings
		{
			get { return _innerAdapter.ReConnectionSettings; }
		}

		IdGenerator IMessageAdapter.TransactionIdGenerator
		{
			get { return _innerAdapter.TransactionIdGenerator; }
		}

		bool IMessageAdapter.PortfolioLookupRequired
		{
			get { return _innerAdapter.PortfolioLookupRequired; }
		}

		bool IMessageAdapter.OrderStatusRequired
		{
			get { return _innerAdapter.OrderStatusRequired; }
		}

		bool IMessageAdapter.SecurityLookupRequired
		{
			get { return _innerAdapter.SecurityLookupRequired; }
		}

		OrderCondition IMessageAdapter.CreateOrderCondition()
		{
			return _innerAdapter.CreateOrderCondition();
		}

		bool IMessageAdapter.IsConnectionAlive()
		{
			return _innerAdapter.IsConnectionAlive();
		}

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo
		{
			get { return _innerAdapter.SecurityClassInfo; }
		}

		TimeSpan IMessageAdapter.HeartbeatInterval
		{
			get { return _innerAdapter.HeartbeatInterval; }
			set { _innerAdapter.HeartbeatInterval = value; }
		}

		MessageTypes[] IMessageAdapter.SupportedMessages
		{
			get { return _innerAdapter.SupportedMessages; }
			set { _innerAdapter.SupportedMessages = value; }
		}

		bool IMessageAdapter.IsValid
		{
			get { return _innerAdapter.IsValid; }
		}

		Guid ILogSource.Id
		{
			get { return _innerAdapter.Id; }
		}

		string ILogSource.Name
		{
			get { return _innerAdapter.Name; }
		}

		ILogSource ILogSource.Parent
		{
			get { return _innerAdapter.Parent; }
			set { _innerAdapter.Parent = value; }
		}

		LogLevels ILogSource.LogLevel
		{
			get { return _innerAdapter.LogLevel; }
			set { _innerAdapter.LogLevel = value; }
		}

		DateTimeOffset ILogSource.CurrentTime
		{
			get { return _innerAdapter.CurrentTime; }
		}

		bool ILogSource.IsRoot
		{
			get { return _innerAdapter.IsRoot; }
		}

		event Action<LogMessage> ILogSource.Log
		{
			add { _innerAdapter.Log += value; }
			remove { _innerAdapter.Log -= value; }
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			_innerAdapter.AddLog(message);
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			_innerAdapter.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			_innerAdapter.Save(storage);
		}

		bool IMessageChannel.IsOpened
		{
			get { return _innerAdapter.IsOpened; }
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
				message.LocalTime = _innerAdapter.CurrentTime.LocalDateTime;

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

		void IMessageChannel.Open()
		{
			_innerAdapter.Open();
		}

		void IMessageChannel.Close()
		{
			_innerAdapter.Close();
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
				_innerAdapter.AddWarningLog(LocalizedStrings.Str855Params,
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
						_innerAdapter.SendInMessage(new OrderGroupCancelMessage { TransactionId = _innerAdapter.TransactionIdGenerator.GetNextId() });
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