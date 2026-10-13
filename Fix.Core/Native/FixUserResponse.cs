namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for UserResponse FIX message.
/// </summary>
/// <param name="UserRequestId">User request identifier.</param>
/// <param name="UserName">User name.</param>
/// <param name="Status">User status.</param>
/// <param name="Text">Status text.</param>
public record struct FixUserResponse(
	FixId UserRequestId,
	string UserName,
	UserStatus Status,
	string Text);
