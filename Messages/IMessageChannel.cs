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
	/// Send message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
	bool SendInMessage(Message message);

	/// <summary>
	/// New message event.
	/// </summary>
	event Action<Message> NewOutMessage;
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

	bool IMessageChannel.SendInMessage(Message message)
	{
		_newMessage?.Invoke(message);
		return true;
	}

	private Action<Message> _newMessage;

	event Action<Message> IMessageChannel.NewOutMessage
	{
		add => _newMessage += value;
		remove => _newMessage -= value;
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