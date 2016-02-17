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
	public abstract class MessageAdapter : BaseLogReceiver, IMessageAdapter
	{
		private class CodeTimeOut
			//where T : class
		{
			private readonly CachedSynchronizedDictionary<long, TimeSpan> _registeredKeys = new CachedSynchronizedDictionary<long, TimeSpan>();

			private TimeSpan _timeOut = TimeSpan.FromSeconds(10);

			public TimeSpan TimeOut
			{
				get { return _timeOut; }
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
				//	throw new ArgumentNullException("key");

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
			if (transactionIdGenerator == null)
				throw new ArgumentNullException(nameof(transactionIdGenerator));

			Platform = Platforms.AnyCPU;

			TransactionIdGenerator = transactionIdGenerator;
			SecurityClassInfo = new Dictionary<string, RefPair<SecurityTypes, string>>();
		}

		private MessageTypes[] _supportedMessages = ArrayHelper.Empty<MessageTypes>();

		/// <summary>
		/// Supported by adapter message types.
		/// </summary>
		[Browsable(false)]
		public virtual MessageTypes[] SupportedMessages
		{
			get { return _supportedMessages; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				var dulicate = value.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1);
				if (dulicate != null)
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(dulicate.Key), nameof(value));

				_supportedMessages = value;
			}
		}

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		[Browsable(false)]
		public virtual bool IsValid => true;

		/// <summary>
		/// Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.
		/// </summary>
		[Browsable(false)]
		public IDictionary<string, RefPair<SecurityTypes, string>> SecurityClassInfo { get; }

		private TimeSpan _heartbeatInterval = TimeSpan.Zero;

		/// <summary>
		/// Server check interval for track the connection alive. The value is <see cref="TimeSpan.Zero"/> turned off tracking.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.Str192Key)]
		[DescriptionLoc(LocalizedStrings.Str193Key)]
		public TimeSpan HeartbeatInterval
		{
			get { return _heartbeatInterval; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException();

				_heartbeatInterval = value;
			}
		}

		/// <summary>
		/// <see cref="SecurityLookupMessage"/> required to get securities.
		/// </summary>
		[Browsable(false)]
		public virtual bool SecurityLookupRequired => this.IsMessageSupported(MessageTypes.SecurityLookup);

		/// <summary>
		/// <see cref="PortfolioLookupMessage"/> required to get portfolios and positions.
		/// </summary>
		[Browsable(false)]
		public virtual bool PortfolioLookupRequired => this.IsMessageSupported(MessageTypes.PortfolioLookup);

		/// <summary>
		/// <see cref="OrderStatusMessage"/> required to get orders and ow trades.
		/// </summary>
		[Browsable(false)]
		public virtual bool OrderStatusRequired => this.IsMessageSupported(MessageTypes.OrderStatus);

		/// <summary>
		/// <see cref="OrderCancelMessage.Volume"/> required to cancel orders.
		/// </summary>
		public virtual bool OrderCancelVolumeRequired { get; } = false;

		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected virtual bool IsSupportNativeSecurityLookup => false;

		/// <summary>
		/// Gets a value indicating whether the connector supports position lookup.
		/// </summary>
		protected virtual bool IsSupportNativePortfolioLookup => false;

		/// <summary>
		/// Bit process, which can run the adapter. By default is <see cref="Platforms.AnyCPU"/>.
		/// </summary>
		[Browsable(false)]
		public Platforms Platform { get; protected set; }

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <returns>Order condition. If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.</returns>
		public virtual OrderCondition CreateOrderCondition()
		{
			return null;
		}

		/// <summary>
		/// Connection tracking settings <see cref="IMessageAdapter"/> with a server.
		/// </summary>
		public ReConnectionSettings ReConnectionSettings { get; } = new ReConnectionSettings();

		private IdGenerator _transactionIdGenerator;

		/// <summary>
		/// Transaction id generator.
		/// </summary>
		[Browsable(false)]
		public IdGenerator TransactionIdGenerator
		{
			get { return _transactionIdGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_transactionIdGenerator = value;
			}
		}

		/// <summary>
		/// Securities and portfolios lookup timeout.
		/// </summary>
		/// <remarks>
		/// By defaut is 10 seconds.
		/// </remarks>
		[Browsable(false)]
		public TimeSpan LookupTimeOut
		{
			get { return _secLookupTimeOut.TimeOut; }
			set
			{
				_secLookupTimeOut.TimeOut = value;
				_pfLookupTimeOut.TimeOut = value;
			}
		}

		/// <summary>
		/// Associated board code. The default is ALL.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.AssociatedSecurityBoardKey)]
		[DescriptionLoc(LocalizedStrings.Str199Key)]
		public string AssociatedBoardCode { get; set; } = "ALL";

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
					{
						SendOutErrorExecution(((OrderRegisterMessage)message).ToExecutionMessage(), ex);
						return;
					}

					case MessageTypes.OrderReplace:
					{
						SendOutErrorExecution(((OrderReplaceMessage)message).ToExecutionMessage(), ex);
						return;
					}

					case MessageTypes.OrderPairReplace:
					{
						SendOutErrorExecution(((OrderPairReplaceMessage)message).ToExecutionMessage(), ex);
						return;
					}

					case MessageTypes.OrderCancel:
					{
						SendOutErrorExecution(((OrderCancelMessage)message).ToExecutionMessage(), ex);
						return;
					}

					case MessageTypes.OrderGroupCancel:
					{
						SendOutErrorExecution(((OrderGroupCancelMessage)message).ToExecutionMessage(), ex);
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

			if (message.IsBack)
			{
				message.IsBack = false;

				// time msg should be return back
				SendOutMessage(message);
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
		/// Send outgoing message and raise <see cref="MessageAdapter.NewOutMessage"/> event.
		/// </summary>
		/// <param name="message">Message.</param>
		public virtual void SendOutMessage(Message message)
		{
			InitMessageLocalTime(message);

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
			NewOutMessage.SafeInvoke(message);
		}

		/// <summary>
		/// Initialize local timestamp <see cref="Message"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		private void InitMessageLocalTime(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = CurrentTime;
		}

		/// <summary>
		/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="description">Error detais.</param>
		protected void SendOutError(string description)
		{
			SendOutError(new InvalidOperationException(description));
		}

		/// <summary>
		/// Initialize a new message <see cref="ErrorMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="error">Error detais.</param>
		protected void SendOutError(Exception error)
		{
			SendOutMessage(new ErrorMessage { Error = error });
		}

		/// <summary>
		/// Initialize a new message <see cref="SecurityMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		protected void SendOutSecurityMessage(SecurityId securityId)
		{
			SendOutMessage(new SecurityMessage { SecurityId = securityId });
		}

		/// <summary>
		/// Initialize a new message <see cref="SecurityMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
		protected void SendOutMarketDataNotSupported(long originalTransactionId)
		{
			SendOutMessage(new MarketDataMessage { OriginalTransactionId = originalTransactionId, IsNotSupported = true });
		}

		/// <summary>
		/// Check the connection is alive. Uses only for connected states.
		/// </summary>
		/// <returns><see langword="true" />, is the connection still alive, <see langword="false" />, if the connection was rejected.</returns>
		public virtual bool IsConnectionAlive()
		{
			return true;
		}

		/// <summary>
		/// Create market depth builder.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Order log to market depth builder.</returns>
		public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			HeartbeatInterval = storage.GetValue<TimeSpan>("HeartbeatInterval");
			SupportedMessages = storage.GetValue<string[]>("SupportedMessages").Select(i => i.To<MessageTypes>()).ToArray();
			AssociatedBoardCode = storage.GetValue("AssociatedBoardCode", AssociatedBoardCode);

			base.Load(storage);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("HeartbeatInterval", HeartbeatInterval);
			storage.SetValue("SupportedMessages", SupportedMessages.Select(t => t.To<string>()).ToArray());
			storage.SetValue("AssociatedBoardCode", AssociatedBoardCode);

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

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnSendInMessage(Message message)
		{
			SendOutMessage(message);
		}
	}
}