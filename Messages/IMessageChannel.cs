namespace StockSharp.Messages;

/// <summary>
/// Message channel base interface.
/// </summary>
public interface IMessageChannel : IDisposable, ICloneable<IMessageChannel>
{
	/// <summary>
	/// State.
	/// </summary>
	ChannelStates State { get; }

	/// <summary>
	/// <see cref="State"/> change event.
	/// </summary>
	event Action StateChanged;

	/// <summary>
	/// Open channel.
	/// </summary>
	void Open();

	/// <summary>
	/// Close channel.
	/// </summary>
	void Close();

	/// <summary>
	/// Suspend.
	/// </summary>
	void Suspend();

	/// <summary>
	/// Resume.
	/// </summary>
	void Resume();

	/// <summary>
	/// Clear.
	/// </summary>
	void Clear();

	/// <summary>
	/// Processes a generic message asynchronously.
	/// </summary>
	/// <param name="message">The message to process.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// New message event.
	/// </summary>
	event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;
}

/// <summary>
/// Message channel, which passes directly to the output all incoming messages.
/// </summary>
public class PassThroughMessageChannel : Cloneable<IMessageChannel>, IMessageChannel
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PassThroughMessageChannel"/>.
	/// </summary>
	public PassThroughMessageChannel()
	{
	}

	void IDisposable.Dispose()
	{
		GC.SuppressFinalize(this);
	}

	ChannelStates IMessageChannel.State => ChannelStates.Started;

	event Action IMessageChannel.StateChanged
	{
		add { }
		remove { }
	}

	void IMessageChannel.Open()
	{
	}

	void IMessageChannel.Close()
	{
	}

	void IMessageChannel.Suspend()
	{
	}

	void IMessageChannel.Resume()
	{
	}

	void IMessageChannel.Clear()
	{
	}

	ValueTask IMessageChannel.SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return _newOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
	}

	private Func<Message, CancellationToken, ValueTask> _newOutMessageAsync;

	event Func<Message, CancellationToken, ValueTask> IMessageChannel.NewOutMessageAsync
	{
		add => _newOutMessageAsync += value;
		remove => _newOutMessageAsync -= value;
	}

	/// <summary>
	/// Create a copy of <see cref="PassThroughMessageChannel"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new PassThroughMessageChannel();
	}
}