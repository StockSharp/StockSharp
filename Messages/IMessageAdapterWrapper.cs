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
		IMessageAdapter InnerAdapter { get; set; }
	}

	/// <summary>
	/// Base implementation of <see cref="IMessageAdapterWrapper"/>.
	/// </summary>
	public abstract class MessageAdapterWrapper : Cloneable<IMessageChannel>, IMessageAdapterWrapper
	{
		private IMessageAdapter _innerAdapter;

		/// <summary>
		/// Initialize <see cref="MessageAdapterWrapper"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		protected MessageAdapterWrapper(IMessageAdapter innerAdapter)
		{
			if (innerAdapter == null)
				throw new ArgumentNullException(nameof(innerAdapter));

			InnerAdapter = innerAdapter;
		}

		/// <summary>
		/// Underlying adapter.
		/// </summary>
		public IMessageAdapter InnerAdapter
		{
			get => _innerAdapter;
			set
			{
				if (_innerAdapter == value)
					return;

				if(_innerAdapter != null)
					_innerAdapter.NewOutMessage -= OnInnerAdapterNewOutMessage;

				_innerAdapter = value;

				if (_innerAdapter == null)
					throw new ArgumentException();

				_innerAdapter.NewOutMessage += OnInnerAdapterNewOutMessage;
			}
		}

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
			NewOutMessage?.Invoke(message);
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

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			InnerAdapter.Load(storage);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			InnerAdapter.Save(storage);
		}

		Guid ILogSource.Id => InnerAdapter.Id;

		string ILogSource.Name => InnerAdapter.Name;

		ILogSource ILogSource.Parent
		{
			get => InnerAdapter.Parent;
			set => InnerAdapter.Parent = value;
		}

		LogLevels ILogSource.LogLevel
		{
			get => InnerAdapter.LogLevel;
			set => InnerAdapter.LogLevel = value;
		}

		/// <inheritdoc />
		public DateTimeOffset CurrentTime => InnerAdapter.CurrentTime;

		bool ILogSource.IsRoot => InnerAdapter.IsRoot;

		event Action<LogMessage> ILogSource.Log
		{
			add => InnerAdapter.Log += value;
			remove => InnerAdapter.Log -= value;
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			InnerAdapter.AddLog(message);
		}

		/// <inheritdoc />
		public ReConnectionSettings ReConnectionSettings => InnerAdapter.ReConnectionSettings;

		/// <inheritdoc />
		public IdGenerator TransactionIdGenerator => InnerAdapter.TransactionIdGenerator;

		MessageTypes[] IMessageAdapter.SupportedMessages
		{
			get => InnerAdapter.SupportedMessages;
			set => InnerAdapter.SupportedMessages = value;
		}

		MarketDataTypes[] IMessageAdapter.SupportedMarketDataTypes
		{
			get => InnerAdapter.SupportedMarketDataTypes;
			set => InnerAdapter.SupportedMarketDataTypes = value;
		}

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo => InnerAdapter.SecurityClassInfo;

		/// <inheritdoc />
		public TimeSpan HeartbeatInterval
		{
			get => InnerAdapter.HeartbeatInterval;
			set => InnerAdapter.HeartbeatInterval = value;
		}

		bool IMessageAdapter.PortfolioLookupRequired => InnerAdapter.PortfolioLookupRequired;

		bool IMessageAdapter.SecurityLookupRequired => InnerAdapter.SecurityLookupRequired;

		bool IMessageAdapter.OrderStatusRequired => InnerAdapter.OrderStatusRequired;

		IEnumerable<TimeSpan> IMessageAdapter.TimeFrames => InnerAdapter.TimeFrames;

		/// <inheritdoc />
		public string StorageName => InnerAdapter.StorageName;

		/// <inheritdoc />
		public bool IsNativeIdentifiersPersistable => InnerAdapter.IsNativeIdentifiersPersistable;

		/// <inheritdoc />
		public bool IsNativeIdentifiers => InnerAdapter.IsNativeIdentifiers;

		/// <inheritdoc />
		public bool IsFullCandlesOnly => InnerAdapter.IsFullCandlesOnly;

		/// <inheritdoc />
		public bool IsSupportSubscriptions => InnerAdapter.IsSupportSubscriptions;

		/// <inheritdoc />
		public bool IsSupportSubscriptionBySecurity => InnerAdapter.IsSupportSubscriptionBySecurity;

		/// <inheritdoc />
		public bool IsSupportSubscriptionByPortfolio => InnerAdapter.IsSupportSubscriptionByPortfolio;

		OrderCancelVolumeRequireTypes? IMessageAdapter.OrderCancelVolumeRequired => InnerAdapter.OrderCancelVolumeRequired;

		/// <inheritdoc />
		public string AssociatedBoardCode => InnerAdapter.AssociatedBoardCode;

		Tuple<string, Type>[] IMessageAdapter.SecurityExtendedFields => InnerAdapter.SecurityExtendedFields;

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