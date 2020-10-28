#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: InMemoryMessageChannel.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Globalization;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Message channel, based on the queue and operate within a single process.
	/// </summary>
	public class InMemoryMessageChannel : IMessageChannel
	{
		private static readonly MemoryStatisticsValue<Message> _msgStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Messages);

		static InMemoryMessageChannel()
		{
			MemoryStatistics.Instance.Values.Add(_msgStat);
		}

		private readonly IMessageQueue _queue;
		private readonly Action<Exception> _errorHandler;

		private readonly SyncObject _suspendLock = new SyncObject();

		private int _version;

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <param name="queue">Message queue.</param>
		/// <param name="name">Channel name.</param>
		/// <param name="errorHandler">Error handler.</param>
		public InMemoryMessageChannel(IMessageQueue queue, string name, Action<Exception> errorHandler)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			Name = name;

			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

			_queue.Close();
		}

		/// <summary>
		/// Handler name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Message queue count.
		/// </summary>
		public int MessageCount => _queue.Count;

		/// <summary>
		/// Max message queue count.
		/// </summary>
		/// <remarks>
		/// The default value is -1, which corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get => _queue.MaxSize;
			set => _queue.MaxSize = value;
		}

		private int _suspendMaxCount = 10000;

		/// <summary>
		/// Suspend on <see cref="SuspendTimeout"/> if message queue is more than the specified count.
		/// </summary>
		public int SuspendMaxCount
		{
			get => _suspendMaxCount;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_suspendMaxCount = value;
			}
		}

		private TimeSpan _suspendTimeout = TimeSpan.FromSeconds(1);

		/// <summary>
		/// <see cref="SuspendMaxCount"/>.
		/// </summary>
		public TimeSpan SuspendTimeout
		{
			get => _suspendTimeout;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value));

				_suspendTimeout = value;
			}
		}

		/// <summary>
		/// The channel cannot be opened.
		/// </summary>
		public bool Disabled { get; set; }

		private ChannelStates _state = ChannelStates.Stopped;

		/// <inheritdoc />
		public ChannelStates State
		{
			get => _state;
			private set
			{
				if (_state == value)
					return;

				_state = value;
				StateChanged?.Invoke();
			}
		}

		/// <inheritdoc />
		public event Action StateChanged;

		/// <inheritdoc />
		public void Open()
		{
			if (Disabled)
				return;

			State = ChannelStates.Started;
			_queue.Open();

			var version = Interlocked.Increment(ref _version);

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					while (this.IsOpened())
					{
						try
						{
							if (!_queue.TryDequeue(out var message))
								break;

							if (State == ChannelStates.Suspended)
							{
								_suspendLock.Wait();

								if (!this.IsOpened())
									break;
							}

							if (_version != version)
								break;

							_msgStat.Remove(message);
							NewOutMessage?.Invoke(message);
						}
						catch (Exception ex)
						{
							_errorHandler(ex);
						}
					}

					State = ChannelStates.Stopped;
				}))
				.Name($"{Name} channel thread.")
				//.Culture(CultureInfo.InvariantCulture)
				.Launch();
		}

		/// <inheritdoc />
		public void Close()
		{
			State = ChannelStates.Stopping;

			_queue.Close();
			_queue.Clear();

			_suspendLock.Pulse();
		}

		void IMessageChannel.Suspend()
		{
			State = ChannelStates.Suspended;
		}

		void IMessageChannel.Resume()
		{
			State = ChannelStates.Started;
			_suspendLock.PulseAll();
		}

		void IMessageChannel.Clear()
		{
			_queue.Clear();
		}

		/// <inheritdoc />
		public bool SendInMessage(Message message)
		{
			if (!this.IsOpened())
			{
				//throw new InvalidOperationException();
				return false;
			}

			if (State == ChannelStates.Suspended)
			{
				_suspendLock.Wait();

				if (!this.IsOpened())
					return false;
			}

			_msgStat.Add(message);
			_queue.Enqueue(message);

			if (_queue.Count > SuspendMaxCount)
				SuspendTimeout.Sleep();

			return true;
		}

		/// <inheritdoc />
		public event Action<Message> NewOutMessage;

		/// <summary>
		/// Create a copy of <see cref="InMemoryMessageChannel"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public virtual IMessageChannel Clone()
		{
			return new InMemoryMessageChannel(_queue, Name, _errorHandler)
			{
				MaxMessageCount = MaxMessageCount,
				SuspendMaxCount = SuspendMaxCount,
				SuspendTimeout = SuspendTimeout,
			};
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		void IDisposable.Dispose()
		{
			Close();
		}
	}
}