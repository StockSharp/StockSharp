namespace StockSharp.Messages;

/// <summary>
/// One live indicator value tied to a finalized candle and its originating subscription.
/// </summary>
public class IndicatorMessage : Message, IOriginalTransactionIdMessage, ISecurityIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorMessage"/> class.
	/// </summary>
	public IndicatorMessage()
		: base(MessageTypes.Indicator)
	{
	}

	/// <inheritdoc />
	public long OriginalTransactionId { get; set; }

	/// <inheritdoc />
	public SecurityId SecurityId { get; set; }

	/// <summary>Indicator point carried by this message.</summary>
	public IndicatorPoint Point { get; set; }

	/// <inheritdoc />
	public override Message Clone()
	{
		var clone = new IndicatorMessage
		{
			OriginalTransactionId = OriginalTransactionId,
			SecurityId = SecurityId,
			Point = Point?.Clone(),
		};

		CopyTo(clone);
		return clone;
	}
}
