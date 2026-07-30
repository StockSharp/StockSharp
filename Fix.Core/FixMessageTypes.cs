namespace StockSharp.Fix;

/// <summary>
/// FIX server extended messages.
/// </summary>
public static class FixMessageTypes
{
	/// <summary>
	/// <see cref="FixSeqResetMessage"/>.
	/// </summary>
	public const MessageTypes SeqReset = (MessageTypes)(-5000);

	/// <summary>
	/// <see cref="FixResendRequestMessage"/>.
	/// </summary>
	public const MessageTypes ResendRequest = (MessageTypes)(-5001);

	/// <summary>
	/// <see cref="FixUserRequestMessage"/>.
	/// </summary>
	public const MessageTypes UserRequest = (MessageTypes)(-5002);

	/// <summary>
	/// <see cref="FixUserResponseMessage"/>.
	/// </summary>
	public const MessageTypes UserResponse = (MessageTypes)(-5003);

	/// <summary>
	/// <see cref="FixUserForceLogoffMessage"/>.
	/// </summary>
	public const MessageTypes UserForceLogoff = (MessageTypes)(-5004);

	/// <summary>
	/// <see cref="FixConnectAttemptMessage"/>.
	/// </summary>
	public const MessageTypes ConnectAttempt = (MessageTypes)(-5006);
}

/// <summary>
/// The resend request is sent by the receiving application to initiate the retransmission of messages.
/// This function is utilized if a sequence number gap is detected, if the receiving application lost a message, or as a function of the initialization process.
/// </summary>
public class FixResendRequestMessage : Message
{
	/// <summary>
	/// Initialize <see cref="FixResendRequestMessage"/>.
	/// </summary>
	public FixResendRequestMessage()
		: base(FixMessageTypes.ResendRequest)
	{
	}

	/// <summary>
	/// Message sequence number of first message in range to be resent.
	/// </summary>
	public long BeginSeqNo { get; set; }

	/// <summary>
	/// Message sequence number of last message in range to be resent. If request is for a single message <see cref="BeginSeqNo"/> = <see cref="EndSeqNo"/>. If request is for all messages subsequent to a particular message, <see cref="EndSeqNo"/> = "0" (representing infinity).
	/// </summary>
	public long EndSeqNo { get; set; }

	/// <summary>
	/// Create a copy of <see cref="FixResendRequestMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return new FixResendRequestMessage
		{
			BeginSeqNo = BeginSeqNo,
			EndSeqNo = EndSeqNo
		};
	}
}

/// <summary>
/// Sequence reset message.
/// </summary>
public class FixSeqResetMessage : Message
{
	/// <summary>
	/// Initialize <see cref="FixSeqResetMessage"/>.
	/// </summary>
	public FixSeqResetMessage()
		: base(FixMessageTypes.SeqReset)
	{
	}

	/// <summary>
	/// Gap fill.
	/// </summary>
	public bool? GapFill { get; set; }

	/// <summary>
	/// New sequence number.
	/// </summary>
	public long NewSeqNo { get; set; }

	/// <summary>
	/// Sequence number.
	/// </summary>
	public long SeqNum { get; set; }

	/// <summary>
	/// Indicates that the range of messages retransmitted after a <see cref="FixResendRequestMessage"/> may not include all the application messages contained in the original range requested.
	/// </summary>
	public bool? PossMissingApplMsg { get; set; }

	/// <summary>
	/// Create a copy of <see cref="FixSeqResetMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return new FixSeqResetMessage
		{
			GapFill = GapFill,
			NewSeqNo = NewSeqNo,
			SeqNum = SeqNum,
			PossMissingApplMsg = PossMissingApplMsg,
		};
	}
}

/// <summary>
/// Request types.
/// </summary>
public enum FixUserRequestTypes
{
	/// <summary>
	/// Force log off.
	/// </summary>
	LogoffForce = 2,

	/// <summary>
	/// Change password.
	/// </summary>
	ChangePassword = 3,
}

/// <summary>
/// FIX server extended message for user based action.
/// </summary>
public class FixUserRequestMessage : ChangePasswordMessage
{
	/// <summary>
	/// Initialize <see cref="FixUserRequestMessage"/>.
	/// </summary>
	public FixUserRequestMessage()
		: base(FixMessageTypes.UserRequest)
	{
	}

	/// <summary>
	/// Request type.
	/// </summary>
	public FixUserRequestTypes RequestType { get; set; }

	/// <summary>
	/// Create a copy of <see cref="FixUserRequestMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new FixUserRequestMessage
		{
			RequestType = RequestType
		};

		CopyTo(clone);

		return clone;
	}
}

/// <summary>
/// Response types.
/// </summary>
public enum FixUserResponseTypes
{
	/// <summary>
	/// Password changed.
	/// </summary>
	PasswordChanged,

	/// <summary>
	/// User found.
	/// </summary>
	UserFound,

	/// <summary>
	/// User logged off.
	/// </summary>
	UserLoggedOff,

	/// <summary>
	/// User not found.
	/// </summary>
	UserNotFound,

	/// <summary>
	/// User blocked.
	/// </summary>
	UserBlocked,

	/// <summary>
	/// Old password is incorrect.
	/// </summary>
	OldPasswordIncorrect,

	/// <summary>
	/// New password is incorrect.
	/// </summary>
	NewPasswordIncorrect,

	/// <summary>
	/// Not supported.
	/// </summary>
	NotSupported,

	/// <summary>
	/// Unknown server error.
	/// </summary>
	UnknownError
}

/// <summary>
/// FIX server extended message for user based action result.
/// </summary>
public class FixUserResponseMessage : BaseResultMessage<FixUserResponseMessage>
{
	/// <summary>
	/// Initialize <see cref="FixUserResponseMessage"/>.
	/// </summary>
	public FixUserResponseMessage()
		: base(FixMessageTypes.UserResponse)
	{
	}

	/// <summary>
	/// User name.
	/// </summary>
	public string UserName { get; set; }

	/// <summary>
	/// Response type.
	/// </summary>
	public FixUserResponseTypes ResponseType { get; set; }

	/// <inheritdoc />
	protected override void CopyTo(FixUserResponseMessage destination)
	{
		base.CopyTo(destination);

		destination.UserName = UserName;
		destination.ResponseType = ResponseType;
	}
}

/// <summary>
/// FIX server extended message for force logoff message.
/// </summary>
public class FixUserForceLogoffMessage : BaseResultMessage<FixUserForceLogoffMessage>
{
	/// <summary>
	/// Initialize <see cref="FixUserForceLogoffMessage"/>.
	/// </summary>
	public FixUserForceLogoffMessage()
		: base(FixMessageTypes.UserForceLogoff)
	{
	}
}

/// <summary>
/// 
/// </summary>
public class FixConnectAttemptMessage : Message
{
	/// <summary>
	/// Initialize <see cref="FixConnectAttemptMessage"/>.
	/// </summary>
	public FixConnectAttemptMessage()
		: base(FixMessageTypes.ConnectAttempt)
	{
	}

	/// <summary>
	/// 
	/// </summary>
	public long ExpectedSeqNum { get; set; }

	/// <summary>
	/// Create a copy of <see cref="FixConnectAttemptMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return new FixConnectAttemptMessage { ExpectedSeqNum = ExpectedSeqNum };
	}
}