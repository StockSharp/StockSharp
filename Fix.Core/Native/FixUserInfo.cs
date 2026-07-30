namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for UserInfo FIX message.
/// </summary>
/// <param name="UserRequestId">User request identifier.</param>
/// <param name="Message">User info message.</param>
public record struct FixUserInfo(
	FixId UserRequestId,
	UserInfoMessage Message);
