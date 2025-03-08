namespace StockSharp.Messages;

/// <summary>
/// A message containing changes.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
/// <typeparam name="TField">Changes type.</typeparam>
/// <remarks>
/// Initialize <see cref="BaseChangeMessage{TMessage,TField}"/>.
/// </remarks>
/// <param name="type">Message type.</param>
[DataContract]
[Serializable]
public abstract class BaseChangeMessage<TMessage, TField>(MessageTypes type) :	BaseSubscriptionIdMessage<TMessage>(type),
	IServerTimeMessage, IGeneratedMessage
	where TMessage : BaseChangeMessage<TMessage, TField>, new()
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ServerTimeKey,
		Description = LocalizedStrings.ChangeServerTimeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset ServerTime { get; set; }

	/// <inheritdoc />
	[DataMember]
	public DataType BuildFrom { get; set; }

	/// <summary>
	/// Changes.
	/// </summary>
	[Browsable(false)]
	//[DataMember]
	[XmlIgnore]
	public IDictionary<TField, object> Changes { get; } = new Dictionary<TField, object>();

	/// <inheritdoc />
	public override void CopyTo(TMessage destination)
	{
		base.CopyTo(destination);

		destination.ServerTime = ServerTime;
		destination.BuildFrom = BuildFrom;

		destination.Changes.AddRange(Changes);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",T(S)={ServerTime:yyyy/MM/dd HH:mm:ss.fff}";
	}
}