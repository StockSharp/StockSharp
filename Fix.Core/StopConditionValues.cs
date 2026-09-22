namespace StockSharp.Fix;

using StockSharp.Messages;

/// <summary>
/// Reads and writes the values of a stop condition that is held as a plain
/// <see cref="OrderCondition"/>.
/// </summary>
/// <remarks>
/// A condition carries its values in a name-keyed bag, which is how it travels the wire. A caller
/// holding one as the base type can only reach those values by naming the keys itself, and a name
/// spelled differently in two places compiles and then silently loses the value - a take-profit
/// arriving at the venue as a stop-loss. The names live here, once, and callers ask for the value.
/// </remarks>
public static class StopConditionValues
{
	/// <summary>
	/// The flavour of the stop: which way it triggers, and whether it follows the price.
	/// </summary>
	/// <param name="condition">The condition, which may be <see langword="null"/>.</param>
	/// <returns>The flavour, or <see langword="null"/> when it states none.</returns>
	public static FixStopOrderTypes? GetStopFlavour(this OrderCondition condition)
		=> (FixStopOrderTypes?)condition?.Parameters.TryGetValue(nameof(FixOrderCondition.Type));

	/// <summary>
	/// States the flavour of the stop.
	/// </summary>
	/// <param name="condition">The condition.</param>
	/// <param name="flavour">Which way it triggers, and whether it follows the price.</param>
	/// <exception cref="ArgumentNullException"><paramref name="condition"/> is <see langword="null"/>.</exception>
	public static void SetStopFlavour(this OrderCondition condition, FixStopOrderTypes flavour)
	{
		if (condition is null)
			throw new ArgumentNullException(nameof(condition));

		condition.Parameters[nameof(FixOrderCondition.Type)] = flavour;
	}
}
