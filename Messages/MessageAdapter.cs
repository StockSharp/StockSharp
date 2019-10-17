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
			private readonly CachedSynchronizedDictionary<long, TimeSpan> _registeredIds = new CachedSynchronizedDictionary<long, TimeSpan>();

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

			public void StartTimeOut(long transactionId)
			{
				if (transactionId == 0)
				{
					//throw new ArgumentNullException(nameof(transactionId));
					return;
				}

				_registeredIds.SafeAdd(transactionId, s => TimeOut);
			}

			public void UpdateTimeOut(long transactionId)
			{
				if (transactionId == 0)
					return;

				_registeredIds[transactionId] = TimeOut;
			}

			public void RemoveTimeOut(long transactionId)
			{
				if (transactionId == 0)
					return;

				_registeredIds.Remove(transactionId);
			}

			public IEnumerable<long> ProcessTime(TimeSpan diff)
			{
				if (_registeredIds.Count == 0)
					return Enumerable.Empty<long>();

				return _registeredIds.SyncGet(d =>
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

			public void Clear()
			{
				_registeredIds.Clear();
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

		private IEnumerable<MessageTypes> _supportedMessages = Enumerable.Empty<MessageTypes>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IEnumerable<MessageTypes> SupportedMessages
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

				OnPropertyChanged(nameof(SupportedMessages));
			}
		}

		private IEnumerable<MessageTypeInfo> _possibleSupportedMessages = Enumerable.Empty<MessageTypeInfo>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IEnumerable<MessageTypeInfo> PossibleSupportedMessages
		{
			get => _possibleSupportedMessages;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				var duplicate = value.GroupBy(m => m.Type).FirstOrDefault(g => g.Count() > 1);
				if (duplicate != null)
					throw new ArgumentException(LocalizedStrings.Str415Params.Put(duplicate.Key), nameof(value));

				_possibleSupportedMessages = value;
				OnPropertyChanged(nameof(PossibleSupportedMessages));

				SupportedMessages = value.Select(t => t.Type).ToArray();
			}
		}

		private IEnumerable<MarketDataTypes> _supportedMarketDataTypes = Enumerable.Empty<MarketDataTypes>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IEnumerable<MarketDataTypes> SupportedMarketDataTypes
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
		public virtual IEnumerable<Level1Fields> CandlesBuildFrom => Enumerable.Empty<Level1Fields>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool CheckTimeFrameByRequest { get; set; }

		private TimeSpan _heartbeatInterval = TimeSpan.Zero;

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str192Key,
			Description = LocalizedStrings.Str193Key,
			GroupName = LocalizedStrings.Str186Key,
			Order = 300)]
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

		/// <inheritdoc />
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
		/// Gets a value indicating whether the adapter translates <see cref="SecurityLookupResultMessage"/>.
		/// </summary>
		protected virtual bool IsSupportSecurityLookupResult => false;

		/// <summary>
		/// Gets a value indicating whether the adapter translates <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		protected virtual bool IsSupportPortfolioLookupResult => false;

		/// <summary>
		/// Bit process, which can run the adapter.
		/// </summary>
		[Browsable(false)]
		public Platforms Platform { get; protected set; }

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IEnumerable<Tuple<string, Type>> SecurityExtendedFields { get; } = Enumerable.Empty<Tuple<string, Type>>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportSecuritiesLookupAll => true;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual IEnumerable<int> SupportedOrderBookDepths => Enumerable.Empty<int>();

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportOrderBookIncrements => false;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSupportExecutionsPnL => false;

		/// <inheritdoc />
		[Browsable(false)]
		public virtual bool IsSecurityNewsOnly => false;

		/// <inheritdoc />
		public virtual Type OrderConditionType => GetType()
			.GetAttribute<OrderConditionAttribute>()?
			.ConditionType;

		/// <inheritdoc />
		public virtual OrderCondition CreateOrderCondition() => OrderConditionType?.CreateInstance<OrderCondition>();

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

		/// <inheritdoc />
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AssociatedSecurityBoardKey,
			Description = LocalizedStrings.Str199Key,
			GroupName = LocalizedStrings.Str186Key,
			Order = 301)]
		public string AssociatedBoardCode { get; set; } = DefaultAssociatedBoardCode;

		/// <inheritdoc />
		public event Action<Message> NewOutMessage;

		bool IMessageChannel.IsOpened => true;

		void IMessageChannel.Open()
		{
		}

		void IMessageChannel.Close()
		{
		}

		event Action IMessageChannel.StateChanged
		{
			add { }
			remove { }
		}

		/// <inheritdoc />
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
					_prevTime = default;
					_secLookupTimeOut.Clear();
					_pfLookupTimeOut.Clear();
					break;

				case MessageTypes.PortfolioLookup:
				{
					if (!IsSupportPortfolioLookupResult)
						_pfLookupTimeOut.StartTimeOut(((PortfolioLookupMessage)message).TransactionId);

					break;
				}
				case MessageTypes.SecurityLookup:
				{
					if (!IsSupportSecurityLookupResult)
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
					case MessageTypes.SecurityLookup:
					{
						var lookupMsg = (SecurityLookupMessage)message;
					
						if (!IsSupportSecurityLookupResult)
							_secLookupTimeOut.RemoveTimeOut(lookupMsg.TransactionId);

						break;
					}

					case MessageTypes.PortfolioLookup:
					{
						var lookupMsg = (PortfolioLookupMessage)message;

						if (!IsSupportPortfolioLookupResult)
							_pfLookupTimeOut.RemoveTimeOut(lookupMsg.TransactionId);

						break;
					}
				}

				message.HandleErrorResponse(ex, CurrentTime, SendOutMessage);

				SendOutError(ex);
			}
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
			//	else if (message is BasePositionChangeMessage posMsg && posMsg.Changes.Count == 0)
			//		return;
			//}

			InitMessageLocalTime(message);

			if (/*message.IsBack && */message.Adapter == null)
				message.Adapter = this;

			switch (message.Type)
			{
				case MessageTypes.Security:
					if (!IsSupportSecurityLookupResult)
						_secLookupTimeOut.UpdateTimeOut(((SecurityMessage)message).OriginalTransactionId);

					break;

				case MessageTypes.Portfolio:
					if (!IsSupportPortfolioLookupResult)
						_pfLookupTimeOut.UpdateTimeOut(((PortfolioMessage)message).OriginalTransactionId);

					break;

				case MessageTypes.SecurityLookupResult:
					if (!IsSupportSecurityLookupResult)
						_secLookupTimeOut.RemoveTimeOut(((SecurityLookupResultMessage)message).OriginalTransactionId);

					break;

				case MessageTypes.PortfolioLookupResult:
					if (!IsSupportPortfolioLookupResult)
						_pfLookupTimeOut.RemoveTimeOut(((PortfolioLookupResultMessage)message).OriginalTransactionId);

					break;

				case MessageTypes.TimeFrameLookupResult:
					_timeFrames = ((TimeFrameLookupResultMessage)message).TimeFrames;

					break;
			}

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
				case BasePositionChangeMessage posMsg when posMsg.ServerTime.IsDefault():
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
			SendOutDisconnectMessage(expected ? null : new InvalidOperationException(LocalizedStrings.Str2551));
		}

		/// <summary>
		/// Send to <see cref="SendOutMessage"/> disconnect message.
		/// </summary>
		/// <param name="error">Error info. Can be <see langword="null"/>.</param>
		protected void SendOutDisconnectMessage(Exception error)
		{
			SendOutMessage(error == null ? (BaseConnectionMessage)new DisconnectMessage() : new ConnectMessage
			{
				Error = error
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
		/// Initialize a new message <see cref="MarketDataMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
		protected void SendOutMarketDataReply(long originalTransactionId)
		{
			SendOutMessage(new MarketDataMessage { OriginalTransactionId = originalTransactionId });
		}

		/// <summary>
		/// Initialize a new message <see cref="MarketDataMessage"/> and pass it to the method <see cref="SendOutMessage"/>.
		/// </summary>
		/// <param name="originalTransactionId">ID of the original message for which this message is a response.</param>
		protected void SendOutMarketDataNotSupported(long originalTransactionId)
		{
			SendOutMessage(new MarketDataMessage { OriginalTransactionId = originalTransactionId, IsNotSupported = true });
		}

		/// <inheritdoc />
		public virtual bool IsConnectionAlive()
			=> true;

		/// <inheritdoc />
		public virtual IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(SecurityId securityId)
			=> new OrderLogMarketDepthBuilder(securityId);

		private IEnumerable<TimeSpan> _timeFrames = Enumerable.Empty<TimeSpan>();

		/// <summary>
		/// Get possible time-frames for the specified instrument.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Possible time-frames.</returns>
		protected virtual IEnumerable<TimeSpan> GetTimeFrames(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
			=> _timeFrames;

		/// <inheritdoc />
		public virtual IEnumerable<object> GetCandleArgs(Type candleType, SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
		{
			return candleType == typeof(TimeFrameCandleMessage)
				? GetTimeFrames(securityId, from, to).Cast<object>()
				: Enumerable.Empty<object>();
		}

		/// <inheritdoc />
		public virtual TimeSpan GetHistoryStepSize(MarketDataMessage request, out TimeSpan iterationInterval)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			iterationInterval = TimeSpan.FromSeconds(2);

			switch (request.DataType)
			{
				case MarketDataTypes.Level1:
				case MarketDataTypes.MarketDepth:
				case MarketDataTypes.Trades:
				case MarketDataTypes.OrderLog:
					return TimeSpan.FromDays(1);
				case MarketDataTypes.CandleTimeFrame:
				{
					var tf = request.GetTimeFrame();

					if (tf.TotalDays <= 1)
						return TimeSpan.FromDays(30);

					return TimeSpan.MaxValue;
				}
				case MarketDataTypes.CandleTick:
				case MarketDataTypes.CandleVolume:
				case MarketDataTypes.CandleRange:
				case MarketDataTypes.CandlePnF:
				case MarketDataTypes.CandleRenko:
					return TimeSpan.FromDays(30);
				default:
					return TimeSpan.MaxValue;
			}
		}

		/// <inheritdoc />
		public virtual bool IsAllDownloadingSupported(MarketDataTypes dataType) => false;

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			Id = storage.GetValue(nameof(Id), Id);
			HeartbeatInterval = storage.GetValue<TimeSpan>(nameof(HeartbeatInterval));

			if (storage.ContainsKey(nameof(SupportedMessages)))
				SupportedMessages = storage.GetValue<string[]>(nameof(SupportedMessages)).Select(i => i.To<MessageTypes>()).ToArray();
			
			AssociatedBoardCode = storage.GetValue(nameof(AssociatedBoardCode), AssociatedBoardCode);
			CheckTimeFrameByRequest = storage.GetValue(nameof(CheckTimeFrameByRequest), CheckTimeFrameByRequest);

			if (storage.ContainsKey(nameof(ReConnectionSettings)))
				ReConnectionSettings.Load(storage.GetValue<SettingsStorage>(nameof(ReConnectionSettings)));

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
			storage.SetValue(nameof(ReConnectionSettings), ReConnectionSettings.Save());

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