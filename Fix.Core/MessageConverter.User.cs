namespace StockSharp.Fix;

/// <summary>
/// User message converters: UserRequest, UserRequestEx.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual UserRequestMessage ToMessage(FixUserRequest fix)
	{
		var msg = new UserRequestMessage
		{
			TransactionId = fix.UserRequestId.ToLong(),
			Login = fix.UserName,
		};

		if (fix.UserId != null)
			msg.Id = fix.UserId;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixUserRequest ToFixUserRequest(UserRequestMessage message)
	{
		return new FixUserRequest(
			UserRequestId: FixId.FromLong(message.TransactionId),
			UserRequestType: null,
			UserName: message.Login,
			Password: null,
			NewPassword: null,
			Own: null,
			UserId: message.Id);
	}

	/// <inheritdoc />
	public virtual UserLookupMessage ToMessage(FixUserRequestEx fix)
	{
		var msg = new UserLookupMessage
		{
			TransactionId = fix.UserRequestId.ToLong(),
			IsSubscribe = GetIsSubscribe(fix.SubscriptionRequestType),
		};

		if (!fix.UserName.IsEmpty())
			msg.Like = fix.UserName;

		// A lookup by primary key must keep the key; dropping it turned a targeted
		// user request into an unfiltered/wrong lookup after one conversion.
		if (fix.Id != null)
			msg.UserId = fix.Id;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixUserRequestEx ToFixUserRequestEx(UserLookupMessage message)
	{
		return new FixUserRequestEx(
			UserRequestId: FixId.FromLong(message.TransactionId),
			Id: message.UserId,
			UserName: message.Like,
			SubscriptionRequestType: ToFixSubscriptionType(message.IsSubscribe));
	}
}
