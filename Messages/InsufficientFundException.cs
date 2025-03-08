namespace StockSharp.Messages;

/// <summary>
/// Insufficient fund exception.
/// </summary>
/// <remarks>
/// Initialize <see cref="InsufficientFundException"/>.
/// </remarks>
/// <param name="message"><see cref="Message"/></param>
public class InsufficientFundException(string message) : InvalidOperationException(message)
{
}