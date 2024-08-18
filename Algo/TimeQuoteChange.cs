namespace StockSharp.Algo;

/// <summary>
/// The quote with the time mark. It used for CSV files.
/// </summary>
public class TimeQuoteChange : IServerTimeMessage, ISecurityIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TimeQuoteChange"/>.
	/// </summary>
	public TimeQuoteChange()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TimeQuoteChange"/>.
	/// </summary>
	/// <param name="side">Direction (buy or sell).</param>
	/// <param name="quote">The quote, from which changes will be copied.</param>
	/// <param name="message">The message with quotes.</param>
	public TimeQuoteChange(Sides side, QuoteChange quote, QuoteChangeMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		SecurityId = message.SecurityId;
		ServerTime = message.ServerTime;
		LocalTime = message.LocalTime;
		Quote = quote;
		Side = side;
	}

	/// <summary>
	/// Market depth quote representing bid or ask.
	/// </summary>
	public QuoteChange Quote { get; set; }

	/// <summary>
	/// Direction (buy or sell).
	/// </summary>
	public Sides Side { get; set; }

	/// <inheritdoc />
	public SecurityId SecurityId { get; set; }

	/// <inheritdoc />
	public DateTimeOffset ServerTime { get; set; }

	/// <summary>
	/// The local time mark.
	/// </summary>
	public DateTimeOffset LocalTime { get; set; }
}