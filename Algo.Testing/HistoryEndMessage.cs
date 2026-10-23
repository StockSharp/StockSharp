namespace StockSharp.Algo.Testing;

/// <summary>
/// The replay holds no more history of a security before the run's stop, so the emulator closes its market.
/// </summary>
public class HistoryEndMessage : Message, ISecurityIdMessage, IServerTimeMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HistoryEndMessage"/>.
	/// </summary>
	public HistoryEndMessage()
		: base(HistoryMessageTypes.HistoryEnd)
	{
	}

	/// <inheritdoc />
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// The moment of the replay at which the history ran out.
	/// </summary>
	public DateTime ServerTime { get; set; }

	/// <summary>
	/// Create a copy of <see cref="HistoryEndMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new HistoryEndMessage
		{
			SecurityId = SecurityId,
			ServerTime = ServerTime,
		};

		CopyTo(clone);

		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
		=> base.ToString() + $",Sec={SecurityId}";
}
