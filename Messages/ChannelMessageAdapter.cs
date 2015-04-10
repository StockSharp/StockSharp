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
		private readonly IMessageChannel _channel;

		private bool _isChannelOpened;

		/// <summary>
		/// Создать <see cref="ChannelMessageAdapter"/>.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		/// <param name="channel">Транспортный канал сообщений.</param>
		public ChannelMessageAdapter(IMessageAdapter adapter, IMessageChannel channel)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			if (channel == null)
				throw new ArgumentNullException("channel");

			_adapter = adapter;
			_channel = channel;

			_channel.NewOutMessage += ChannelOnNewOutMessage;
		}

		private void ChannelOnNewOutMessage(Message message)
		{
			_adapter.SendInMessage(message);
		}

		void IDisposable.Dispose()
		{
			_channel.NewOutMessage -= ChannelOnNewOutMessage;
			_channel.Dispose();
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
			if (!_isChannelOpened)
			{
				_channel.Open();
				_isChannelOpened = true;
			}

			_channel.SendInMessage(message);
		}

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add { _adapter.NewOutMessage += value; }
			remove { _adapter.NewOutMessage -= value; }
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

		event Action<LogMessage> ILogSource.Log
		{
			add { _adapter.Log += value; }
			remove { _adapter.Log -= value; }
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			_adapter.AddLog(message);
		}

		IdGenerator IMessageAdapter.TransactionIdGenerator
		{
			get { return _adapter.TransactionIdGenerator; }
		}

		bool IMessageAdapter.IsMarketDataEnabled
		{
			get { return _adapter.IsMarketDataEnabled; }
		}

		bool IMessageAdapter.IsTransactionEnabled
		{
			get { return _adapter.IsTransactionEnabled; }
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

		TimeSpan IMessageAdapter.MarketTimeChangedInterval
		{
			get { return _adapter.MarketTimeChangedInterval; }
			set { _adapter.MarketTimeChangedInterval = value; }
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
	}
}