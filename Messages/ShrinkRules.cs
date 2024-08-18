namespace StockSharp.Messages;

/// <summary>
/// Price rounding rules.
/// </summary>
public enum ShrinkRules
{
	/// <summary>
	/// Automatically to determine rounding to lesser or to bigger value.
	/// </summary>
	Auto,

	/// <summary>
	/// To round to lesser value.
	/// </summary>
	Less,

	/// <summary>
	/// To round to bigger value.
	/// </summary>
	More,
}