namespace StockSharp.Algo;

using StockSharp.Messages;

/// <summary>
/// Symmetric spread widener for <see cref="Level1ChangeMessage"/>. Shifts
/// <see cref="Level1Fields.BestBidPrice"/> down and
/// <see cref="Level1Fields.BestAskPrice"/> up by the configured percent.
/// Stateless.
/// </summary>
public sealed class Level1SpreadWidener
{
	private readonly decimal _bidFactor;
	private readonly decimal _askFactor;

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1SpreadWidener"/> class.
	/// </summary>
	/// <param name="percent">Half-spread widening, in percent. Non-positive disables widening.</param>
	public Level1SpreadWidener(decimal percent)
	{
		Percent = percent;

		if (percent > 0m)
		{
			var p = percent / 100m;
			_bidFactor = 1m - p;
			_askFactor = 1m + p;
		}
		else
		{
			_bidFactor = 1m;
			_askFactor = 1m;
		}
	}

	/// <summary>
	/// Half-spread widening, in percent.
	/// </summary>
	public decimal Percent { get; }

	/// <summary>
	/// <see langword="true"/> if <see cref="Percent"/> is positive and widening is applied.
	/// </summary>
	public bool IsEnabled => Percent > 0m;

	/// <summary>
	/// Returns a copy of <paramref name="msg"/> with the best bid shifted down and the
	/// best ask shifted up by <see cref="Percent"/>.
	/// </summary>
	/// <param name="msg">Source level1 change message.</param>
	/// <returns>
	/// Widened copy; the original message when widening is disabled; or
	/// <see langword="null"/> if <paramref name="msg"/> is <see langword="null"/>.
	/// </returns>
	public Level1ChangeMessage Apply(Level1ChangeMessage msg)
	{
		if (msg is null)
			return null;

		if (!IsEnabled)
			return msg;

		var copy = msg.TypedClone();

		if (copy.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid && bid > 0m)
			copy.Changes[Level1Fields.BestBidPrice] = bid * _bidFactor;

		if (copy.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask && ask > 0m)
			copy.Changes[Level1Fields.BestAskPrice] = ask * _askFactor;

		return copy;
	}
}
