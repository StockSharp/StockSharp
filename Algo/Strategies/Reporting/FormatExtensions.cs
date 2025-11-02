namespace StockSharp.Algo.Strategies.Reporting;

/// <summary>
/// Format extensions.
/// </summary>
public static class FormatExtensions
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
		=> time.ToString(@"hh\:mm\:ss");

	/// <summary>
	/// To format the <see cref="DateTime"/> in <see cref="string"/>.
	/// </summary>
	/// <param name="time"><see cref="DateTime"/> value.</param>
	/// <returns><see cref="string"/>.</returns>
	public static string Format(this DateTime time)
		=> time.To<string>();
}