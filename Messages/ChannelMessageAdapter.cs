namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Message adapter, forward messages through a transport channel <see cref="IMessageChannel"/>.
	/// </summary>
	public class ChannelMessageAdapter : IMessageAdapter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChannelMessageAdapter"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="inputChannel">Incomming messages channgel.</param>
		/// <param name="outputChannel">Outgoing message channel.</param>
		public ChannelMessageAdapter(IMessageAdapter adapter, IMessageChannel inputChannel, IMessageChannel outputChannel)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			if (inputChannel == null)
				throw new ArgumentNullException("inputChannel");

			Adapter = adapter;

			InputChannel = inputChannel;
			OutputChannel = outputChannel;

			InputChannel.NewOutMessage += InputChannelOnNewOutMessage;
			OutputChannel.NewOutMessage += OutputChannelOnNewOutMessage;

			Adapter.NewOutMessage += AdapterOnNewOutMessage;
		}

		/// <summary>
		/// Adapter.
		/// </summary>
		public IMessageAdapter Adapter { get; private set; }

		/// <summary>
		/// Adapter.
		/// </summary>
		public IMessageChannel InputChannel { get; private set; }

		/// <summary>
		/// Adapter.
		/// </summary>
		public IMessageChannel OutputChannel { get; private set; }

		/// <summary>
		/// Control the lifetime of the incoming messages channel.
		/// </summary>
		public bool OwnInputChannel { get; set; }

		/// <summary>
		/// Control the lifetime of the outgoing messages channel.
		/// </summary>
		public bool OwnOutputChannel { get; set; }

		private void OutputChannelOnNewOutMessage(Message message)
		{
			_newMessage.SafeInvoke(message);
		}

		private void AdapterOnNewOutMessage(Message message)
		{
			if (!OutputChannel.IsOpened)
				OutputChannel.Open();

			OutputChannel.SendInMessage(message);
		}

		private void InputChannelOnNewOutMessage(Message message)
		{
			Adapter.SendInMessage(message);
		}

		void IDisposable.Dispose()
		{
			InputChannel.NewOutMessage -= InputChannelOnNewOutMessage;
			OutputChannel.NewOutMessage -= OutputChannelOnNewOutMessage;

			if (OwnInputChannel)
				InputChannel.Dispose();

			if (OwnOutputChannel)
				OutputChannel.Dispose();

			Adapter.NewOutMessage -= AdapterOnNewOutMessage;
			//Adapter.Dispose();
		}

		bool IMessageChannel.IsOpened
		{
			get { return Adapter.IsOpened; }
		}

		void IMessageChannel.Open()
		{
			Adapter.Open();
		}

		void IMessageChannel.Close()
		{
			Adapter.Close();
		}

		void IMessageChannel.SendInMessage(Message message)
		{
			if (!InputChannel.IsOpened)
				InputChannel.Open();

			InputChannel.SendInMessage(message);
		}

		private Action<Message> _newMessage;

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add { _newMessage += value; }
			remove { _newMessage -= value; }
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Adapter.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			Adapter.Save(storage);
		}

		Guid ILogSource.Id
		{
			get { return Adapter.Id; }
		}

		string ILogSource.Name
		{
			get { return Adapter.Name; }
		}

		ILogSource ILogSource.Parent
		{
			get { return Adapter.Parent; }
			set { Adapter.Parent = value; }
		}

		LogLevels ILogSource.LogLevel
		{
			get { return Adapter.LogLevel; }
			set { Adapter.LogLevel = value; }
		}

		DateTimeOffset ILogSource.CurrentTime
		{
			get { return Adapter.CurrentTime; }
		}

		bool ILogSource.IsRoot
		{
			get { return Adapter.IsRoot; }
		}

		event Action<LogMessage> ILogSource.Log
		{
			add { Adapter.Log += value; }
			remove { Adapter.Log -= value; }
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			Adapter.AddLog(message);
		}

		ReConnectionSettings IMessageAdapter.ReConnectionSettings
		{
			get { return Adapter.ReConnectionSettings; }
		}

		IdGenerator IMessageAdapter.TransactionIdGenerator
		{
			get { return Adapter.TransactionIdGenerator; }
		}

		MessageTypes[] IMessageAdapter.SupportedMessages
		{
			get { return Adapter.SupportedMessages; }
			set { Adapter.SupportedMessages = value; }
		}

		bool IMessageAdapter.IsValid
		{
			get { return Adapter.IsValid; }
		}

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo
		{
			get { return Adapter.SecurityClassInfo; }
		}

		TimeSpan IMessageAdapter.HeartbeatInterval
		{
			get { return Adapter.HeartbeatInterval; }
			set { Adapter.HeartbeatInterval = value; }
		}

		bool IMessageAdapter.PortfolioLookupRequired
		{
			get { return Adapter.PortfolioLookupRequired; }
		}

		bool IMessageAdapter.SecurityLookupRequired
		{
			get { return Adapter.SecurityLookupRequired; }
		}

		bool IMessageAdapter.OrderStatusRequired
		{
			get { return Adapter.OrderStatusRequired; }
		}

		string IMessageAdapter.AssociatedBoardCode
		{
			get { return Adapter.AssociatedBoardCode; }
		}

		OrderCondition IMessageAdapter.CreateOrderCondition()
		{
			return Adapter.CreateOrderCondition();
		}

		bool IMessageAdapter.IsConnectionAlive()
		{
			return Adapter.IsConnectionAlive();
		}

		IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			return Adapter.CreateOrderLogMarketDepthBuilder(securityId);
		}
	}
}