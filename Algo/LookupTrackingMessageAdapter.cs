namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Message adapter that tracks multiple lookups requests and put them into single queue.
	/// </summary>
	public class LookupTrackingMessageAdapter : MessageAdapterWrapper
	{
		private class LookupTimeOutTimer
		{
			private readonly ILogReceiver _logReceiver;
			private readonly CachedSynchronizedDictionary<long, TimeSpan> _registeredIds = new CachedSynchronizedDictionary<long, TimeSpan>();

			public LookupTimeOutTimer(ILogReceiver logReceiver)
			{
				_logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
			}

			public void StartTimeOut(ITransactionIdMessage message, TimeSpan timeOut)
			{
				var transactionId = message.TransactionId;

				if (transactionId == 0)
				{
					//throw new ArgumentNullException(nameof(transactionId));
					return;
				}

				if (timeOut == default)
					return;

				_logReceiver.AddInfoLog("Lookup timeout {0} started for {1}.", timeOut, transactionId);
				_registeredIds.SafeAdd(transactionId, s => timeOut);
			}

			public void UpdateTimeOut<TMessage>(TMessage message)
				where TMessage : IOriginalTransactionIdMessage
			{
				var transactionId = message.OriginalTransactionId;

				if (transactionId == 0)
					return;

				lock (_registeredIds.SyncRoot)
				{
					if (!_registeredIds.TryGetValue(transactionId, out var timeOut))
						return;

					_registeredIds[transactionId] = timeOut;
				}
			}

			public void RemoveTimeOut<TMessage>(TMessage message)
				where TMessage : IOriginalTransactionIdMessage
			{
				var transactionId = message.OriginalTransactionId;

				if (transactionId == 0)
					return;

				_logReceiver.AddInfoLog("Lookup finished {0}.", transactionId);
				_registeredIds.Remove(transactionId);
			}

			public IEnumerable<long> ProcessTime(TimeSpan diff)
			{
				if (_registeredIds.Count == 0)
					return Enumerable.Empty<long>();

				List<long> timeOutCodes = null;

				lock (_registeredIds.SyncRoot)
				{
					foreach (var pair in _registeredIds.CachedPairs)
					{
						var left = pair.Value - diff;

						if (left > TimeSpan.Zero)
						{
							_registeredIds[pair.Key] = left;
							continue;
						}

						if (timeOutCodes == null)
							timeOutCodes = new List<long>();

						timeOutCodes.Add(pair.Key);
						_registeredIds.Remove(pair.Key);
					}
				}
				
				return timeOutCodes ?? Enumerable.Empty<long>();
			}
		}

		private class LookupInfo
		{
			private readonly Func<long, Message> _createResult;

			public readonly Queue<ITransactionIdMessage> LookupQueue = new Queue<ITransactionIdMessage>();
			public readonly LookupTimeOutTimer LookupTimeOut;

			public LookupInfo(ILogReceiver logReceiver, MessageTypes resultType)
			{
				ResultType = resultType;
				LookupTimeOut = new LookupTimeOutTimer(logReceiver);
				_createResult = id => resultType.CreateLookupResult(id);
			}

			public MessageTypes ResultType { get; }

			public Message CreateResultMessage(long id) => _createResult(id);
		}

		private readonly CachedSynchronizedDictionary<MessageTypes, LookupInfo> _lookups = new CachedSynchronizedDictionary<MessageTypes, LookupInfo>();
		private DateTimeOffset _prevTime;

		/// <summary>
		/// Initializes a new instance of the <see cref="LookupTrackingMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public LookupTrackingMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private TimeSpan _timeOut = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Securities and portfolios lookup timeout.
		/// </summary>
		/// <remarks>
		/// By default is 10 seconds.
		/// </remarks>
		public TimeSpan TimeOut
		{
			get => _timeOut;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.IntervalMustBePositive);

				_timeOut = value;
			}
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_lookups.SyncRoot)
					{
						_prevTime = default;
						_lookups.Clear();
					}

					break;
				}

				default:
					if (message.Type.IsLookup() && !ProcessLookupMessage(message))
						return;

					break;
			}

			base.OnSendInMessage(message);
		}

		private bool ProcessLookupMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.Type == MessageTypes.OrderStatus)
				return true;

			var transIdMsg = (ITransactionIdMessage)message;
			var transId = transIdMsg.TransactionId;

			LookupInfo info;

			lock (_lookups.SyncRoot)
			{
				info = _lookups.SafeAdd(message.Type, key => new LookupInfo(this, message.Type.ToResultType()));

				// not prev queued lookup
				if (info.LookupQueue.All(msg => msg.TransactionId != transId))
				{
					info.LookupQueue.Enqueue((ITransactionIdMessage)message.Clone());

					if (info.LookupQueue.Count > 1)
						return false;
				}
			}

			if (!this.IsOutMessageSupported(info.ResultType))
				info.LookupTimeOut.StartTimeOut(transIdMsg, TimeOut);

			return true;
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Security:
					_lookups.TryGetValue(MessageTypes.SecurityLookup)?.LookupTimeOut.UpdateTimeOut((SecurityMessage)message);
					break;

				case MessageTypes.Board:
					_lookups.TryGetValue(MessageTypes.BoardLookup)?.LookupTimeOut.UpdateTimeOut((BoardMessage)message);
					break;

				case MessageTypes.Portfolio:
					_lookups.TryGetValue(MessageTypes.PortfolioLookup)?.LookupTimeOut.UpdateTimeOut((PortfolioMessage)message);
					break;

				case MessageTypes.UserInfo:
					_lookups.TryGetValue(MessageTypes.UserLookup)?.LookupTimeOut.UpdateTimeOut((UserInfoMessage)message);
					break;
			}

			base.OnInnerAdapterNewOutMessage(message);

			Message nextLookup = null;

			if (message.Type.IsLookupResult())
			{
				var lookupType = message.Type.ToLookupType();

				lock (_lookups.SyncRoot)
				{
					var info = _lookups.TryGetValue(lookupType);

					if (info != null)
					{
						info.LookupTimeOut.RemoveTimeOut((IOriginalTransactionIdMessage)message);

						if (info.LookupQueue.Count > 0)
						{
							//удаляем текущий запрос лукапа из очереди
							info.LookupQueue.Dequeue();

							nextLookup = (Message)info.LookupQueue.TryPeek();

							if (nextLookup == null)
								_lookups.Remove(lookupType);
						}
					}
				}
			}

			if (nextLookup != null)
			{
				nextLookup.IsBack = true;
				nextLookup.Adapter = this;

				base.OnInnerAdapterNewOutMessage(nextLookup);
			}

			if (_prevTime != DateTimeOffset.MinValue)
			{
				var diff = message.LocalTime - _prevTime;

				foreach (var pair in _lookups.CachedPairs)
				{
					var info = pair.Value;

					foreach (var id in info.LookupTimeOut.ProcessTime(diff))
					{
						this.AddInfoLog("Lookup timeout {0}.", id);

						base.OnInnerAdapterNewOutMessage(info.CreateResultMessage(id));
					}
				}
			}

			_prevTime = message.LocalTime;
		}

		/// <summary>
		/// Create a copy of <see cref="LookupTrackingMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new LookupTrackingMessageAdapter((IMessageAdapter)InnerAdapter.Clone()) { TimeOut = TimeOut };
		}
	}
}