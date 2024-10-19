namespace StockSharp.Algo.Testing;

/// <summary>
/// Market-data message with historical source.
/// </summary>
public class HistorySourceMessage : MarketDataMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HistorySourceMessage"/>.
	/// </summary>
	public HistorySourceMessage()
		: base(ExtendedMessageTypes.HistorySource)
	{
	}

	/// <summary>
	/// Callback to retrieve historical data for the specified date.
	/// </summary>
	public Func<DateTimeOffset, IEnumerable<Message>> GetMessages { get; set; }

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(HistorySourceMessage destination)
	{
		base.CopyTo(destination);

		destination.GetMessages = GetMessages;
	}

	/// <summary>
	/// Create a copy of <see cref="HistorySourceMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new HistorySourceMessage();
		CopyTo(clone);
		return clone;
	}
}