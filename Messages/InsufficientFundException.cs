namespace StockSharp.Messages;

using System;

/// <summary>
/// Insufficient fund exception.
/// </summary>
public class InsufficientFundException : InvalidOperationException
{
	/// <summary>
	/// Initialize <see cref="InsufficientFundException"/>.
	/// </summary>
	/// <param name="message"><see cref="Message"/></param>
	public InsufficientFundException(string message)
		: base(message)
	{
	}
}