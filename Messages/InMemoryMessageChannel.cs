namespace StockSharp.Messages;

/// <summary>
/// Message channel, based on the queue and operate within a single process.
/// </summary>
public class InMemoryMessageChannel : Disposable, IMessageChannel
{
	private readonly IMessageQueue _queue;
	private readonly Action<Exception> _errorHandler;

	private CancellationTokenSource _cancellationTokenSource;
	private Task _processingTask;

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

			if (!_state.ValidateChannelState(value))
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

		if (State == ChannelStates.Started || State == ChannelStates.Starting)
			return;

		// Close previous task if exists
		if (_processingTask != null)
		{
			_cancellationTokenSource?.Cancel();

			try
			{
				_processingTask.Wait(TimeSpan.FromSeconds(1));
			}
			catch (AggregateException)
			{
			}

			_cancellationTokenSource?.Dispose();
		}

		State = ChannelStates.Starting;
		_queue.Open();

		_cancellationTokenSource = new();

		_processingTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
		State = ChannelStates.Started;
	}

	private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
	{
		try
		{
			await foreach (var message in _queue.ReadAllAsync(cancellationToken).NoWait())
			{
				var state = State;

				if (state != ChannelStates.Started)
				{
					// Wait while suspended
					while (state == ChannelStates.Suspended || state == ChannelStates.Suspending)
					{
						if (cancellationToken.IsCancellationRequested)
							return;

						await Task.Delay(10, cancellationToken);
						state = State;
					}

					if (state == ChannelStates.Stopping || state == ChannelStates.Stopped)
						break;
				}

				try
				{
					await (NewOutMessageAsync?.Invoke(message, cancellationToken) ?? default);
				}
				catch (Exception ex)
				{
					_errorHandler(ex);
				}
			}
		}
		catch (Exception ex)
		{
			if (!cancellationToken.IsCancellationRequested)
				_errorHandler(ex);
		}
		finally
		{
			State = ChannelStates.Stopped;
		}
	}

	/// <inheritdoc />
	public void Close()
	{
		if (State == ChannelStates.Stopped || State == ChannelStates.Stopping)
			return;

		State = ChannelStates.Stopping;

		_cancellationTokenSource?.Cancel();
		_queue.Close();

		try
		{
			_processingTask?.Wait(TimeSpan.FromSeconds(5));
		}
		catch (AggregateException)
		{
			// Ignore cancellation exceptions
		}

		// Clear queue only after processing task has stopped
		_queue.Clear();

		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;
		_processingTask = null;
	}

	void IMessageChannel.Suspend()
	{
		State = ChannelStates.Suspending;
		State = ChannelStates.Suspended;
	}

	void IMessageChannel.Resume()
	{
		State = ChannelStates.Starting;
		State = ChannelStates.Started;
	}

	void IMessageChannel.Clear()
	{
		_queue.Clear();
	}

	ValueTask IMessageTransport.SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (!this.IsOpened())
		{
			//throw new InvalidOperationException();
			return default;
		}

		return _queue.Enqueue(message, cancellationToken);
	}

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	/// <inheritdoc />
	public virtual IMessageChannel Clone()
		=> new InMemoryMessageChannel(_queue.Clone(), Name, _errorHandler);

	object ICloneable.Clone() => Clone();

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Close();
		base.DisposeManaged();
	}
}