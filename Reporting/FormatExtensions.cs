namespace StockSharp.Reporting;

using System.Globalization;

/// <summary>
/// Format extensions.
/// </summary>
static class FormatExtensions
{
	/// <summary>
	/// To format the <see cref="TimeSpan"/> in <see cref="string"/>.
	/// </summary>
	/// <param name="time"><see cref="TimeSpan"/> value.</param>
	/// <returns><see cref="string"/>.</returns>
	public static string Format(this TimeSpan? time)
		=> time?.Format() ?? string.Empty;

	/// <summary>
	/// To format the <see cref="TimeSpan"/> in <see cref="string"/>.
	/// </summary>
	/// <param name="time"><see cref="TimeSpan"/> value.</param>
	/// <returns><see cref="string"/>.</returns>
	public static string Format(this TimeSpan time)
		=> time.ToString("c", CultureInfo.InvariantCulture);

	/// <summary>
	/// To format the <see cref="DateTime"/> in <see cref="string"/>.
	/// </summary>
	/// <param name="time"><see cref="DateTime"/> value.</param>
	/// <returns><see cref="string"/>.</returns>
	public static string Format(this DateTime time)
		// Invariant ISO-8601. The leading '.' before the 'F' specifiers is emitted only when the
		// fractional part is non-zero, and trailing zero digits are trimmed, so whole-second values
		// render as 2024-01-15T10:00:00Z - matching the JSON report representation.
		=> time.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture);
}
