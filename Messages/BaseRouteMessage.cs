namespace StockSharp.Messages;

/// <summary>
/// Base route response message.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
/// <remarks>
/// Initialize <see cref="BaseRouteMessage{TMessage}"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[Serializable]
[DataContract]
public abstract class BaseRouteMessage<TMessage>(MessageTypes type) : BaseSubscriptionIdMessage<TMessage>(type)
	where TMessage : BaseRouteMessage<TMessage>, new()
{
	/// <summary>
	/// Adapter identifier.
	/// </summary>
	[DataMember]
	public Guid AdapterId { get; set; }

	/// <inheritdoc />
	public override void CopyTo(TMessage destination)
	{
		base.CopyTo(destination);

		destination.AdapterId = AdapterId;
	}
}