namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Wrapping based adapter.
	/// </summary>
	public interface IMessageAdapterWrapper : IMessageAdapter
	{
		/// <summary>
		/// Underlying adapter.
		/// </summary>
		IMessageAdapter InnerAdapter { get; }
	}

	/// <summary>
	/// Base implementation of <see cref="IMessageAdapterWrapper"/>.
	/// </summary>
	public abstract class MessageAdapterWrapper : Cloneable<IMessageChannel>, IMessageAdapterWrapper
	{
		/// <summary>
		/// Initialize <see cref="MessageAdapterWrapper"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		protected MessageAdapterWrapper(IMessageAdapter innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException("innerAdapter");

			InnerAdapter = innerAdapter;
		}

		/// <summary>
		/// Underlying adapter.
		/// </summary>
		public IMessageAdapter InnerAdapter { get; private set; }

		bool IMessageChannel.IsOpened
		{
			get { return InnerAdapter.IsOpened; }
		}

		void IMessageChannel.Open()
		{
			InnerAdapter.Open();
		}

		void IMessageChannel.Close()
		{
			InnerAdapter.Close();
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public abstract void SendInMessage(Message message);

		/// <summary>
		/// New message event.
		/// </summary>
		public abstract event Action<Message> NewOutMessage;

		void IPersistable.Load(SettingsStorage storage)
		{
			InnerAdapter.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			InnerAdapter.Save(storage);
		}

		Guid ILogSource.Id
		{
			get { return InnerAdapter.Id; }
		}

		string ILogSource.Name
		{
			get { return InnerAdapter.Name; }
		}

		ILogSource ILogSource.Parent
		{
			get { return InnerAdapter.Parent; }
			set { InnerAdapter.Parent = value; }
		}

		LogLevels ILogSource.LogLevel
		{
			get { return InnerAdapter.LogLevel; }
			set { InnerAdapter.LogLevel = value; }
		}

		DateTimeOffset ILogSource.CurrentTime
		{
			get { return InnerAdapter.CurrentTime; }
		}

		bool ILogSource.IsRoot
		{
			get { return InnerAdapter.IsRoot; }
		}

		event Action<LogMessage> ILogSource.Log
		{
			add { InnerAdapter.Log += value; }
			remove { InnerAdapter.Log -= value; }
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			InnerAdapter.AddLog(message);
		}

		ReConnectionSettings IMessageAdapter.ReConnectionSettings
		{
			get { return InnerAdapter.ReConnectionSettings; }
		}

		IdGenerator IMessageAdapter.TransactionIdGenerator
		{
			get { return InnerAdapter.TransactionIdGenerator; }
		}

		MessageTypes[] IMessageAdapter.SupportedMessages
		{
			get { return InnerAdapter.SupportedMessages; }
			set { InnerAdapter.SupportedMessages = value; }
		}

		bool IMessageAdapter.IsValid
		{
			get { return InnerAdapter.IsValid; }
		}

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo
		{
			get { return InnerAdapter.SecurityClassInfo; }
		}

		TimeSpan IMessageAdapter.HeartbeatInterval
		{
			get { return InnerAdapter.HeartbeatInterval; }
			set { InnerAdapter.HeartbeatInterval = value; }
		}

		bool IMessageAdapter.PortfolioLookupRequired
		{
			get { return InnerAdapter.PortfolioLookupRequired; }
		}

		bool IMessageAdapter.SecurityLookupRequired
		{
			get { return InnerAdapter.SecurityLookupRequired; }
		}

		bool IMessageAdapter.OrderStatusRequired
		{
			get { return InnerAdapter.OrderStatusRequired; }
		}

		string IMessageAdapter.AssociatedBoardCode
		{
			get { return InnerAdapter.AssociatedBoardCode; }
		}

		OrderCondition IMessageAdapter.CreateOrderCondition()
		{
			return InnerAdapter.CreateOrderCondition();
		}

		bool IMessageAdapter.IsConnectionAlive()
		{
			return InnerAdapter.IsConnectionAlive();
		}

		IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			return InnerAdapter.CreateOrderLogMarketDepthBuilder(securityId);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public virtual void Dispose()
		{
			//InnerAdapter.Dispose();
		}
	}
}