#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: IMessageAdapterWrapper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
				throw new ArgumentNullException(nameof(innerAdapter));

			InnerAdapter = innerAdapter;
			InnerAdapter.NewOutMessage += OnInnerAdapterNewOutMessage;
		}

		/// <summary>
		/// Underlying adapter.
		/// </summary>
		public IMessageAdapter InnerAdapter { get; }

		/// <summary>
		/// Control <see cref="InnerAdapter"/> lifetime.
		/// </summary>
		public bool OwnInnerAdaper { get; set; }

		/// <summary>
		/// Process <see cref="InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected virtual void OnInnerAdapterNewOutMessage(Message message)
		{
			RaiseNewOutMessage(message);
		}

		/// <summary>
		/// To call the event <see cref="NewOutMessage"/>.
		/// </summary>
		/// <param name="message">The message.</param>
		protected void RaiseNewOutMessage(Message message)
		{
			NewOutMessage.SafeInvoke(message);
		}

		bool IMessageChannel.IsOpened => InnerAdapter.IsOpened;

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
		public virtual void SendInMessage(Message message)
		{
			InnerAdapter.SendInMessage(message);
		}

		/// <summary>
		/// New message event.
		/// </summary>
		public virtual event Action<Message> NewOutMessage;

		void IPersistable.Load(SettingsStorage storage)
		{
			InnerAdapter.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			InnerAdapter.Save(storage);
		}

		Guid ILogSource.Id => InnerAdapter.Id;

		string ILogSource.Name => InnerAdapter.Name;

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

		DateTimeOffset ILogSource.CurrentTime => InnerAdapter.CurrentTime;

		bool ILogSource.IsRoot => InnerAdapter.IsRoot;

		event Action<LogMessage> ILogSource.Log
		{
			add { InnerAdapter.Log += value; }
			remove { InnerAdapter.Log -= value; }
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			InnerAdapter.AddLog(message);
		}

		ReConnectionSettings IMessageAdapter.ReConnectionSettings => InnerAdapter.ReConnectionSettings;

		IdGenerator IMessageAdapter.TransactionIdGenerator => InnerAdapter.TransactionIdGenerator;

		MessageTypes[] IMessageAdapter.SupportedMessages
		{
			get { return InnerAdapter.SupportedMessages; }
			set { InnerAdapter.SupportedMessages = value; }
		}

		bool IMessageAdapter.IsValid => InnerAdapter.IsValid;

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo => InnerAdapter.SecurityClassInfo;

		TimeSpan IMessageAdapter.HeartbeatInterval
		{
			get { return InnerAdapter.HeartbeatInterval; }
			set { InnerAdapter.HeartbeatInterval = value; }
		}

		bool IMessageAdapter.PortfolioLookupRequired => InnerAdapter.PortfolioLookupRequired;

		bool IMessageAdapter.SecurityLookupRequired => InnerAdapter.SecurityLookupRequired;

		bool IMessageAdapter.OrderStatusRequired => InnerAdapter.OrderStatusRequired;

		bool IMessageAdapter.OrderCancelVolumeRequired => InnerAdapter.OrderCancelVolumeRequired;

		string IMessageAdapter.AssociatedBoardCode => InnerAdapter.AssociatedBoardCode;

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
			InnerAdapter.NewOutMessage -= OnInnerAdapterNewOutMessage;

			if (OwnInnerAdaper)
				InnerAdapter.Dispose();
		}
	}
}