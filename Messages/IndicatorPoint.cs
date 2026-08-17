namespace StockSharp.Messages;

/// <summary>
/// One indicator value mapped to a candle: its timestamp and one or more decimal outputs.
/// </summary>
public sealed class IndicatorPoint
{
	/// <summary>Candle timestamp.</summary>
	public DateTime Time { get; set; }

	/// <summary>Per-output values. <see langword="null"/> means warm-up is not finished.</summary>
	public decimal?[] Values { get; set; }

	/// <summary>
	/// For sparse or shifted indicators, the bars-back offset from <see cref="Time"/> to the candle
	/// the value belongs to.
	/// </summary>
	public int? Shift { get; set; }

	internal IndicatorPoint Clone()
		=> new()
		{
			Time = Time,
			Values = Values?.ToArray(),
			Shift = Shift,
		};
}
