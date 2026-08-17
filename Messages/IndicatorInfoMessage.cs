namespace StockSharp.Messages;

/// <summary>
/// What the values of an indicator subscription mean, sent once before any of them.
/// </summary>
/// <remarks>
/// An indicator can produce several series at once - a band has an upper, a middle and a lower one -
/// and <see cref="IndicatorPoint.Values"/> carries them positionally. Which position is which
/// follows from the indicator itself, which the client cannot know, so it is said once here rather
/// than repeated on every value.
/// </remarks>
public class IndicatorInfoMessage : Message, IOriginalTransactionIdMessage, ISecurityIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorInfoMessage"/> class.
	/// </summary>
	public IndicatorInfoMessage()
		: base(MessageTypes.IndicatorInfo)
	{
	}

	/// <inheritdoc />
	public long OriginalTransactionId { get; set; }

	/// <inheritdoc />
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Names of the value series, in the order <see cref="IndicatorPoint.Values"/> carries them.
	/// </summary>
	public string[] OutputNames { get; set; }

	/// <inheritdoc />
	public override Message Clone()
	{
		var clone = new IndicatorInfoMessage
		{
			OriginalTransactionId = OriginalTransactionId,
			SecurityId = SecurityId,
			OutputNames = OutputNames?.ToArray(),
		};

		CopyTo(clone);
		return clone;
	}
}
