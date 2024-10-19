namespace StockSharp.Messages;

/// <summary>
/// User request message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
/// </summary>
[DataContract]
[Serializable]
public class UserRequestMessage : BaseRequestMessage
{
	/// <summary>
	/// Login.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LoginKey,
		Description = LocalizedStrings.LoginKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public string Login { get; set; }

	/// <summary>
	/// Identifier.
	/// </summary>
	[DataMember]
	public long? Id { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.Users;

	/// <summary>
	/// Initializes a new instance of the <see cref="UserRequestMessage"/>.
	/// </summary>
	public UserRequestMessage()
		: base(MessageTypes.UserRequest)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="UserRequestMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return CopyTo(new UserRequestMessage());
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	/// <returns>The object, to which copied information.</returns>
	protected UserRequestMessage CopyTo(UserRequestMessage destination)
	{
		base.CopyTo(destination);

		destination.Login = Login;
		destination.Id = Id;

		return destination;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",User={Login}/{Id}";
	}
}