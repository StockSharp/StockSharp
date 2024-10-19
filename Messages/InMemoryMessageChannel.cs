namespace StockSharp.Messages;

/// <summary>
/// Message channel, based on the queue and operate within a single process.
/// </summary>
public class InMemoryMessageChannel : IMessageChannel
{
	private readonly IMessageQueue _queue;
	private readonly Action<Exception> _errorHandler;

	private readonly SyncObject _suspendLock = new();

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
			.Thread(() => Do.Invariant(() =>
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

		_queue.Enqueue(message);

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
		};
	}

	object ICloneable.Clone()
	{
		return Clone();
	}

	void IDisposable.Dispose()
	{
		Close();

		GC.SuppressFinalize(this);
	}
}