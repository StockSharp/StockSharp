namespace StockSharp.Fix.Native;

using Ecng.Common;

/// <summary>
/// Combined string + long identifier used in FIX protocol messages.
/// Holds both string representation (for FIX text protocol) and optional long value (for SBE binary protocol).
/// </summary>
/// <param name="Str">String representation of the identifier.</param>
public readonly record struct FixId(string Str)
{
	/// <summary>
	/// Optional numeric representation of the identifier (used for SBE optimization).
	/// </summary>
	public long? N { get; init; }

	/// <summary>
	/// Convert to long. Uses <see cref="N"/> if available, otherwise parses <see cref="Str"/>.
	/// </summary>
	public long ToLong() => N ?? Str.To<long>();

	/// <summary>
	/// Convert to nullable long. Returns null if both <see cref="N"/> and <see cref="Str"/> are empty.
	/// </summary>
	public long? ToLongN() => N ?? (Str.IsEmpty() ? null : Str.To<long?>());

	/// <summary>
	/// Create <see cref="FixId"/> from a long value (populates both string and numeric fields).
	/// </summary>
	public static FixId FromLong(long id) => new(id.To<string>()) { N = id };

	/// <summary>
	/// Create <see cref="FixId"/> from string and optional long value.
	/// </summary>
	public static FixId From(string str, long? n) => new(str) { N = n };

	/// <summary>
	/// Implicit conversion from <see cref="FixId"/> to string (returns <see cref="Str"/>).
	/// </summary>
	public static implicit operator string(FixId id) => id.Str;

	/// <summary>
	/// Implicit conversion from string to <see cref="FixId"/>.
	/// </summary>
	public static implicit operator FixId(string str) => new(str);

	/// <summary>
	/// Whether the identifier is empty (no numeric value and empty string).
	/// </summary>
	public bool IsEmpty() => !N.HasValue && Str.IsEmpty();

	/// <inheritdoc />
	public override string ToString() => Str;
}
