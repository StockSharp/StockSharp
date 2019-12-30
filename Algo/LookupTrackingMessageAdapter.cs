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
		private class LookupInfo
		{
			private readonly TimeSpan _initLeft;
			private TimeSpan _left;

			public LookupInfo(ISubscriptionMessage subscription, TimeSpan left)
			{
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
				_initLeft = left;
				_left = left;
			}

			public ISubscriptionMessage Subscription { get; }

			public bool ProcessTime(TimeSpan diff)
			{
				var left = _left - diff;

				if (left <= TimeSpan.Zero)
					return true;

				_left = left;
				return false;
			}

			public void UpdateTimeOut()
			{
				_left = _initLeft;
			}
		}

		private readonly CachedSynchronizedDictionary<long, LookupInfo> _lookups = new CachedSynchronizedDictionary<long, LookupInfo>();
		private readonly Dictionary<MessageTypes, Queue<ITransactionIdMessage>> _queue = new Dictionary<MessageTypes, Queue<ITransactionIdMessage>>();
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
						_queue.Clear();
					}

					break;
				}

				default:
					if (message.Type.IsLookup() && !ProcessLookupMessage((ISubscriptionMessage)message))
						return;

					break;
			}

			base.OnSendInMessage(message);
		}

		private bool ProcessLookupMessage(ISubscriptionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message is OrderStatusMessage orderMsg && orderMsg.HasOrderId())
				return true;

			var transId = message.TransactionId;

			lock (_lookups.SyncRoot)
			{
				var queue = _queue.SafeAdd(message.Type, key => new Queue<ITransactionIdMessage>());

				// not prev queued lookup
				if (queue.All(msg => msg.TransactionId != transId))
				{
					queue.Enqueue((ITransactionIdMessage)message.Clone());

					if (queue.Count > 1)
						return false;
				}

				if (!this.IsResultMessageSupported(message.Type) && TimeOut > TimeSpan.Zero)
				{
					_lookups.Add(transId, new LookupInfo((ISubscriptionMessage)message.Clone(), TimeOut));
					this.AddInfoLog("Lookup timeout {0} started for {1}.", TimeOut, transId);
				}
			}

			return true;
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			base.OnInnerAdapterNewOutMessage(message);

			Message nextLookup = null;

			void TryInitNextLookup(LookupInfo info)
			{
				if (_queue.TryGetValue(info.Subscription.Type, out var queue))
				{
					//удаляем текущий запрос лукапа из очереди
					queue.Dequeue();

					nextLookup = (Message)queue.TryPeek();

					if (nextLookup == null)
						_queue.Remove(info.Subscription.Type);
				}
			}

			if (message is IOriginalTransactionIdMessage originIdMsg)
			{
				lock (_lookups.SyncRoot)
				{
					if (_lookups.TryGetValue(originIdMsg.OriginalTransactionId, out var info))
					{
						if (originIdMsg is SubscriptionFinishedMessage ||
						    originIdMsg is SubscriptionResponseMessage resp && !resp.IsOk())
						{
							_lookups.Remove(originIdMsg.OriginalTransactionId);
							this.AddInfoLog("Lookup finished {0}.", originIdMsg.OriginalTransactionId);

							TryInitNextLookup(info);
						}
						else
							info.UpdateTimeOut();
					}
				}
			}

			if (nextLookup != null)
			{
				nextLookup.LoopBack(this);
				base.OnInnerAdapterNewOutMessage(nextLookup);
			}

			if (_prevTime != DateTimeOffset.MinValue)
			{
				var diff = message.LocalTime - _prevTime;

				foreach (var pair in _lookups.CachedPairs)
				{
					var info = pair.Value;

					if (!info.ProcessTime(diff))
						continue;

					var transId = info.Subscription.TransactionId;
					_lookups.Remove(transId);
					this.AddInfoLog("Lookup timeout {0}.", transId);

					base.OnInnerAdapterNewOutMessage(info.Subscription.CreateResult());

					if (nextLookup == null)
					{
						lock (_lookups.SyncRoot)
							TryInitNextLookup(info);
					}
				}
			}

			if (nextLookup != null)
			{
				nextLookup.LoopBack(this);
				base.OnInnerAdapterNewOutMessage(nextLookup);
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