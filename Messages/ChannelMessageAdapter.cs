namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Адаптер сообщений, пересылающий сообщения через транспортный канал <see cref="IMessageChannel"/>.
	/// </summary>
	public class ChannelMessageAdapter : IMessageAdapter
	{
		private readonly IMessageAdapter _adapter;
		private readonly IMessageChannel _inputChannel;
		private readonly IMessageChannel _outputChannel;

		/// <summary>
		/// Создать <see cref="ChannelMessageAdapter"/>.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		/// <param name="inputChannel">Транспортный канал входящих сообщений.</param>
		/// <param name="outputChannel">Транспортный канал исходящих сообщений.</param>
		public ChannelMessageAdapter(IMessageAdapter adapter, IMessageChannel inputChannel, IMessageChannel outputChannel)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			if (inputChannel == null)
				throw new ArgumentNullException("inputChannel");

			_adapter = adapter;
			_inputChannel = inputChannel;
			_outputChannel = outputChannel;

			_inputChannel.NewOutMessage += InputChannelOnNewOutMessage;
			_outputChannel.NewOutMessage += OutputChannelOnNewOutMessage;

			_adapter.NewOutMessage += AdapterOnNewOutMessage;
		}

		/// <summary>
		/// Контролировать время жизни входящего канала входящих сообщений.
		/// </summary>
		public bool OwnInputChannel { get; set; }

		/// <summary>
		/// Контролировать время жизни входящего канала исходящих сообщений.
		/// </summary>
		public bool OwnOutputChannel { get; set; }

		private void OutputChannelOnNewOutMessage(Message message)
		{
			_newMessage.SafeInvoke(message);
		}

		private void AdapterOnNewOutMessage(Message message)
		{
			if (!_outputChannel.IsOpened)
				_outputChannel.Open();

			_outputChannel.SendInMessage(message);
		}

		private void InputChannelOnNewOutMessage(Message message)
		{
			_adapter.SendInMessage(message);
		}

		void IDisposable.Dispose()
		{
			_inputChannel.NewOutMessage -= InputChannelOnNewOutMessage;
			_outputChannel.NewOutMessage -= OutputChannelOnNewOutMessage;

			if (OwnInputChannel)
				_inputChannel.Dispose();

			if (OwnOutputChannel)
				_outputChannel.Dispose();

			_adapter.NewOutMessage -= AdapterOnNewOutMessage;
			//_adapter.Dispose();
		}

		bool IMessageChannel.IsOpened
		{
			get { return _adapter.IsOpened; }
		}

		void IMessageChannel.Open()
		{
			_adapter.Open();
		}

		void IMessageChannel.Close()
		{
			_adapter.Close();
		}

		void IMessageChannel.SendInMessage(Message message)
		{
			if (!_inputChannel.IsOpened)
				_inputChannel.Open();

			_inputChannel.SendInMessage(message);
		}

		private Action<Message> _newMessage;

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add { _newMessage += value; }
			remove { _newMessage -= value; }
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			_adapter.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			_adapter.Save(storage);
		}

		Guid ILogSource.Id
		{
			get { return _adapter.Id; }
		}

		string ILogSource.Name
		{
			get { return _adapter.Name; }
		}

		ILogSource ILogSource.Parent
		{
			get { return _adapter.Parent; }
			set { _adapter.Parent = value; }
		}

		LogLevels ILogSource.LogLevel
		{
			get { return _adapter.LogLevel; }
			set { _adapter.LogLevel = value; }
		}

		DateTimeOffset ILogSource.CurrentTime
		{
			get { return _adapter.CurrentTime; }
		}

		bool ILogSource.IsRoot
		{
			get { return _adapter.IsRoot; }
		}

		event Action<LogMessage> ILogSource.Log
		{
			add { _adapter.Log += value; }
			remove { _adapter.Log -= value; }
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			_adapter.AddLog(message);
		}

		ReConnectionSettings IMessageAdapter.ReConnectionSettings
		{
			get { return _adapter.ReConnectionSettings; }
		}

		IdGenerator IMessageAdapter.TransactionIdGenerator
		{
			get { return _adapter.TransactionIdGenerator; }
		}

		MessageTypes[] IMessageAdapter.SupportedMessages
		{
			get { return _adapter.SupportedMessages; }
			set { _adapter.SupportedMessages = value; }
		}

		bool IMessageAdapter.IsValid
		{
			get { return _adapter.IsValid; }
		}

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo
		{
			get { return _adapter.SecurityClassInfo; }
		}

		TimeSpan IMessageAdapter.HeartbeatInterval
		{
			get { return _adapter.HeartbeatInterval; }
			set { _adapter.HeartbeatInterval = value; }
		}

		bool IMessageAdapter.CreateAssociatedSecurity
		{
			get { return _adapter.CreateAssociatedSecurity; }
			set { _adapter.CreateAssociatedSecurity = value; }
		}

		bool IMessageAdapter.CreateDepthFromLevel1
		{
			get { return _adapter.CreateDepthFromLevel1; }
			set { _adapter.CreateDepthFromLevel1 = value; }
		}

		string IMessageAdapter.AssociatedBoardCode
		{
			get { return _adapter.AssociatedBoardCode; }
			set { _adapter.AssociatedBoardCode = value; }
		}

		bool IMessageAdapter.PortfolioLookupRequired
		{
			get { return _adapter.PortfolioLookupRequired; }
		}

		bool IMessageAdapter.SecurityLookupRequired
		{
			get { return _adapter.SecurityLookupRequired; }
		}

		bool IMessageAdapter.OrderStatusRequired
		{
			get { return _adapter.OrderStatusRequired; }
		}

		OrderCondition IMessageAdapter.CreateOrderCondition()
		{
			return _adapter.CreateOrderCondition();
		}

		bool IMessageAdapter.IsConnectionAlive()
		{
			return _adapter.IsConnectionAlive();
		}
	}
}