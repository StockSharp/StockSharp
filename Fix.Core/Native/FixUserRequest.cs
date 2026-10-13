namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data from UserRequest FIX message.
/// </summary>
/// <param name="UserRequestId">User request identifier.</param>
/// <param name="UserRequestType">Type of user request.</param>
/// <param name="UserName">User name.</param>
/// <param name="Password">User password.</param>
/// <param name="NewPassword">New password (for password change).</param>
/// <param name="Own">Filter for own records only.</param>
/// <param name="UserId">User identifier.</param>
public record struct FixUserRequest(
	FixId UserRequestId,
	UserRequestType? UserRequestType,
	string UserName,
	string Password,
	string NewPassword,
	bool? Own,
	long? UserId);
