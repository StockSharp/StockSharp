#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: MessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// The base adapter converts messages <see cref="Message"/> to the command of the trading system and back.
	/// </summary>
	public abstract class MessageAdapter : BaseLogReceiver, IMessageAdapter, INotifyPropertyChanged
	{
		private class CodeTimeOut
			//where T : class
		{
			private readonly CachedSynchronizedDictionary<long, TimeSpan> _registeredKeys = new CachedSynchronizedDictionary<long, TimeSpan>();

			private TimeSpan _timeOut = TimeSpan.FromSeconds(10);

			public TimeSpan TimeOut
			{
				get => _timeOut;
				set
				{
					if (value <= TimeSpan.Zero)
						throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.IntervalMustBePositive);

					_timeOut = value;
				}
			}

			public void StartTimeOut(long key)
			{
				//if (key == 0)
				//	throw new ArgumentNullException(nameof(key));

				_registeredKeys.SafeAdd(key, s => TimeOut);
			}

			public IEnumerable<long> ProcessTime(TimeSpan diff)
			{
				if (_registeredKeys.Count == 0)
					return Enumerable.Empty<long>();

				return _registeredKeys.SyncGet(d =>
				{
					var timeOutCodes = new List<long>();

					foreach (var pair in d.CachedPairs)
					{
						d[pair.Key] -= diff;

						if (d[pair.Key] > TimeSpan.Zero)
							continue;

						timeOutCodes.Add(pair.Key);
						d.Remove(pair.Key);
					}

					return timeOutCodes;
				});
			}
		}

		private DateTimeOffset _prevTime;

		private readonly CodeTimeOut _secLookupTimeOut = new CodeTimeOut();
		private readonly CodeTimeOut _pfLookupTimeOut = new CodeTimeOut();

		/// <summary>
		/// Initialize <see cref="MessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		protected MessageAdapter(IdGenerator transactionIdGenerator)
		{
			Platform = Platforms.AnyCPU;

			TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
			SecurityClassInfo = new Dictionary<string, RefPair<SecurityTypes, string>>();

			StorageName = GetType().Namespace.Remove(nameof(StockSharp)).Remove(".");

			Platform = GetType().GetPlatform();

			var attr = GetType().GetAttribute<MessageAdapterCategoryAttribute>();
			if (attr != null)
				Categories = attr.Categories;
		}

		private MessageTypes[] _supportedMessages = ArrayHelper.Empty<MessageTypes>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual MessageTypes[] SupportedMessages
		{
			get => _supportedMessages;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				var duplicate = value.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1);
				if (duplicate != null)
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(duplicate.Key), nameof(value));

				_supportedMessages = value;
			}
		}

		private MarketDataTypes[] _supportedMarketDataTypes = ArrayHelper.Empty<MarketDataTypes>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual MarketDataTypes[] SupportedMarketDataTypes
		{
			get => _supportedMarketDataTypes;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				var duplicate = value.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1);
				if (duplicate != null)
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(duplicate.Key), nameof(value));

				_supportedMarketDataTypes = value;
			}
		}

		/// <inheritdoc />
		[Browsable(false)]
		public IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IEnumerable<TimeSpan> TimeFrames => Enumerable.Empty<TimeSpan>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool CheckTimeFrameByRequest { get; set; }

		private TimeSpan _heartbeatInterval = TimeSpan.Zero;

		/// <summary>
		/// Server check interval for track the connection alive. The value is <see cref="TimeSpan.Zero"/> turned off tracking.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str192Key,
			Description = LocalizedStrings.Str193Key,
			GroupName = LocalizedStrings.Str186Key)]
		public TimeSpan HeartbeatInterval
		{
			get => _heartbeatInterval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_heartbeatInterval = value;
			}
		}

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool SecurityLookupRequired => this.IsMessageSupported(MessageTypes.SecurityLookup);

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool PortfolioLookupRequired => this.IsMessageSupported(MessageTypes.PortfolioLookup);

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool OrderStatusRequired => this.IsMessageSupported(MessageTypes.OrderStatus);

		/// <summary>
		/// Native identifier can be stored.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsNativeIdentifiersPersistable => true;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsNativeIdentifiers => false;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsFullCandlesOnly => true;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportSubscriptions => true;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportSubscriptionBySecurity => true;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportSubscriptionByPortfolio => this.IsMessageSupported(MessageTypes.Portfolio);

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportCandlesUpdates => false;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual MessageAdapterCategories Categories { get; }

		/// <inheritdoc />
		[Browsable(false)]
		public virtual string StorageName { get; }

		/// <inheritdoc />
		[Browsable(false)]
		public virtual OrderCancelVolumeRequireTypes? OrderCancelVolumeRequired { get; } = null;

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected virtual bool IsSupportNativeSecurityLookup => false;

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected virtual bool IsSupportNativePortfolioLookup => false;

		/// <summary>
		/// Bit process, which can run the adapter.
		/// </summary>
		[Browsable(false)]
		public Platforms Platform { get; protected set; }

		/// <inheritdoc />
		[Browsable(false)]
		public virtual Tuple<string, Type>[] SecurityExtendedFields { get; } = ArrayHelper.Empty<Tuple<string, Type>>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportSecuritiesLookupAll => true;

		/// <inheritdoc />
		public virtual OrderCondition CreateOrderCondition() => null;

		/// <inheritdoc />
		[CategoryLoc(LocalizedStrings.Str174Key)]
		public ReConnectionSettings ReConnectionSettings { get; } = new ReConnectionSettings();

		private IdGenerator _transactionIdGenerator;

		/// <inheritdoc />
		[Browsable(false)]
		public IdGenerator TransactionIdGenerator
		{
			get => _transactionIdGenerator;
			set => _transactionIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Securities and portfolios lookup timeout.
		/// </summary>
		/// <remarks>
		/// By default is 10 seconds.
		/// </remarks>
		[Browsable(false)]
		public TimeSpan LookupTimeOut
		{
			get => _secLookupTimeOut.TimeOut;
			set
			{
				_secLookupTimeOut.TimeOut = value;
				_pfLookupTimeOut.TimeOut = value;
			}
		}

		/// <summary>
		/// Default value for <see cref="AssociatedBoardCode"/>.
		/// </summary>
		public const string DefaultAssociatedBoardCode = "ALL";

		/// <summary>
		/// Associated board code. The default is ALL.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.AssociatedSecurityBoardKey)]
		[DescriptionLoc(LocalizedStrings.Str199Key)]
		public string AssociatedBoardCode { get; set; } = DefaultAssociatedBoardCode;

		/// <summary>
		/// Outgoing message event.
		/// </summary>
		public event Action<Message> NewOutMessage;

		bool IMessageChannel.IsOpened => true;

		void IMessageChannel.Open()
		{
		}

		void IMessageChannel.Close()
		{
		}

		/// <summary>
		/// Send incoming message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendInMessage(Message message)
		{
			if (message.Type == MessageTypes.Connect)
			{
				if (!Platform.IsCompatible())
				{
					SendOutMessage(new ConnectMessage
					{
						Error = new InvalidOperationException(LocalizedStrings.Str169Params.Put(GetType().Name, Platform))
					});

					return;
				}
			}

			InitMessageLocalTime(message);

			switch (message.Type)
			{
				case MessageTypes.Reset:
					_prevTime = default(DateTimeOffset);
					break;

				case MessageTypes.PortfolioLookup:
				{
					if (!IsSupportNativePortfolioLookup)
						_pfLookupTimeOut.StartTimeOut(((PortfolioLookupMessage)message).TransactionId);

					break;
				}
				case MessageTypes.SecurityLookup:
				{
					if (!IsSupportNativeSecurityLookup)
						_secLookupTimeOut.StartTimeOut(((SecurityLookupMessage)message).TransactionId);

					break;
				}
			}

			try
			{
				OnSendInMessage(message);
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);

				switch (message.Type)
				{
					case MessageTypes.Connect:
						SendOutMessage(new ConnectMessage { Error = ex });
						return;

					case MessageTypes.Disconnect:
						SendOutMessage(new DisconnectMessage { Error = ex });
						return;

					case MessageTypes.OrderRegister:
					case MessageTypes.OrderReplace:
					case MessageTypes.OrderCancel:
					case MessageTypes.OrderGroupCancel:
					{
						var replyMsg = ((OrderMessage)message).CreateReply();
						SendOutErrorExecution(replyMsg, ex);
						return;
					}
					case MessageTypes.OrderPairReplace:
					{
						var replyMsg = ((OrderPairReplaceMessage)message).Message1.CreateReply();
						SendOutErrorExecution(replyMsg, ex);
						return;
					}

					case MessageTypes.MarketData:
					{
						var reply = (MarketDataMessage)message.Clone();
						reply.OriginalTransactionId = reply.TransactionId;
						reply.Error = ex;
						SendOutMessage(reply);
						return;
					}

					case MessageTypes.SecurityLookup:
					{
						var lookupMsg = (SecurityLookupMessage)message;
						SendOutMessage(new SecurityLookupResultMessage
						{
							OriginalTransactionId = lookupMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.BoardLookup:
					{
						var lookupMsg = (BoardLookupMessage)message;
						SendOutMessage(new BoardLookupResultMessage
						{
							OriginalTransactionId = lookupMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.BoardRequest:
					{
						var requestMsg = (BoardRequestMessage)message;
						SendOutMessage(new BoardRequestMessage
						{
							OriginalTransactionId = requestMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.PortfolioLookup:
					{
						var lookupMsg = (PortfolioLookupMessage)message;
						SendOutMessage(new PortfolioLookupResultMessage
						{
							OriginalTransactionId = lookupMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.UserLookup:
					{
						var lookupMsg = (UserLookupMessage)message;
						SendOutMessage(new UserLookupResultMessage
						{
							OriginalTransactionId = lookupMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.UserRequest:
					{
						var requestMsg = (UserRequestMessage)message;
						SendOutMessage(new UserRequestMessage
						{
							OriginalTransactionId = requestMsg.TransactionId,
							Error = ex
						});
						return;
					}

					case MessageTypes.ChangePassword:
					{
						var pwdMsg = (ChangePasswordMessage)message;
						SendOutMessage(new ChangePasswordMessage
						{
							OriginalTransactionId = pwdMsg.TransactionId,
							Error = ex
						});
						return;
					}
				}

				SendOutError(ex);
			}
		}

		private void SendOutErrorExecution(ExecutionMessage execMsg, Exception ex)
		{
			execMsg.ServerTime = CurrentTime;
			execMsg.Error = ex;
			execMsg.OrderState = OrderStates.Failed;
			SendOutMessage(execMsg);
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected abstract void OnSendInMessage(Message message);

		/// <summary>
		/// Send outgoing message and raise <see cref="NewOutMessage"/> event.
		/// </summary>
		/// <param name="message">Message.</param>
		public virtual void SendOutMessage(Message message)
		{
			//// do not process empty change msgs
			//if (!message.IsBack)
			//{
			//	if (message is Level1ChangeMessage l1Msg && l1Msg.Changes.Count == 0)
			//		return;
			//	else if (message is BaseChangeMessage<PositionChangeTypes> posMsg && posMsg.Changes.Count == 0)
			//		return;
			//}

			InitMessageLocalTime(message);

			if (/*message.IsBack && */message.Adapter == null)
				message.Adapter = this;

			if (_prevTime != DateTimeOffset.MinValue)
			{
				var diff = message.LocalTime - _prevTime;

				_secLookupTimeOut
					.ProcessTime(diff)
					.ForEach(id => SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = id }));

				_pfLookupTimeOut
					.ProcessTime(diff)
					.ForEach(id => SendOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = id }));
			}

			_prevTime = message.LocalTime;
			NewOutMessage?.Invoke(message);
		}

		/// <summary>
		/// Initialize local timestamp <see cref="Message"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		private void InitMessageLocalTime(Message message)
		{
			message.TryInitLocalTime(this);

			switch (message)
			{
				case BaseChangeMessage<PositionChangeTypes> posMsg when posMsg.ServerTime.IsDefault():
					posMsg.ServerTime = CurrentTime;
					break;
				case ExecutionMessage execMsg when execMsg.ExecutionType == ExecutionTypes.Transaction && execMsg.ServerTime.IsDefault():
					execMsg.ServerTime = CurrentTime;
					break;
			}
		}

		/// <summary>
		/// Send to <see cref="SendOutMessage"/> disconnect message.
		/// </summary>
		/// <param name="expected">Is disconnect expected.</param>
		protected void SendOutDisconnectMessage(bool expected)
		{
			SendOutMessage(expected ? (BaseConnectionMessage)new DisconnectMessage() : new ConnectMessage
			{
				Error = new InvalidOperationException(LocalizedStrings.Str2551)
			});
		}

		/// <summary>
		/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="description">Error details.</param>
		protected void SendOutError(string description)
		{
			SendOutError(new InvalidOperationException(description));
		}

		/// <summary>
		/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="error">Error details.</param>
		protected void SendOutError(Exception error)
		{
			SendOutMessage(error.ToErrorMessage());
		}

		/// <summary>
		/// Initialize a new message <see cref="SecurityMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
		protected void SendOutMarketDataNotSupported(long originalTransactionId)
		{
			SendOutMessage(new MarketDataMessage { OriginalTransactionId = originalTransactionId, IsNotSupported = true });
		}

		/// <inheritdoc />
		public virtual bool IsConnectionAlive()
		{
			return true;
		}

		/// <inheritdoc />
		public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			return new OrderLogMarketDepthBuilder(securityId);
		}

		/// <inheritdoc />
		public virtual IEnumerable<TimeSpan> GetTimeFrames(SecurityId securityId)
		{
			return TimeFrames;
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			Id = storage.GetValue(nameof(Id), Id);
			HeartbeatInterval = storage.GetValue<TimeSpan>(nameof(HeartbeatInterval));
			SupportedMessages = storage.GetValue<string[]>(nameof(SupportedMessages)).Select(i => i.To<MessageTypes>()).ToArray();
			AssociatedBoardCode = storage.GetValue(nameof(AssociatedBoardCode), AssociatedBoardCode);
			CheckTimeFrameByRequest = storage.GetValue(nameof(CheckTimeFrameByRequest), CheckTimeFrameByRequest);

			base.Load(storage);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Id), Id);
			storage.SetValue(nameof(HeartbeatInterval), HeartbeatInterval);
			storage.SetValue(nameof(SupportedMessages), SupportedMessages.Select(t => t.To<string>()).ToArray());
			storage.SetValue(nameof(AssociatedBoardCode), AssociatedBoardCode);
			storage.SetValue(nameof(CheckTimeFrameByRequest), CheckTimeFrameByRequest);

			base.Save(storage);
		}

		/// <summary>
		/// Create a copy of <see cref="MessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public virtual IMessageChannel Clone()
		{
			var clone = GetType().CreateInstance<MessageAdapter>(TransactionIdGenerator);
			clone.Load(this.Save());
			return clone;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		/// <summary>
		/// Raise <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
		/// </summary>
		/// <param name="propertyName">The name of the property that changed.</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportStopLoss => false;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportTakeProfit => false;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportWithdraw => false;
	}

	/// <summary>
	/// Special adapter, which transmits directly to the output of all incoming messages.
	/// </summary>
	public class PassThroughMessageAdapter : MessageAdapter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PassThroughMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public PassThroughMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			SendOutMessage(message);
		}
	}
}