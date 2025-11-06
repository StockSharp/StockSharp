namespace StockSharp.Configuration;

/// <summary>
/// Application-wide time conversion utilities respecting configured timezone.
/// </summary>
public static class AppTime
{
	private static TimeZoneInfo _timeZone = TimeZoneInfo.Local;

	/// <summary>
	/// Global application time zone.
	/// </summary>
	public static TimeZoneInfo TimeZone
	{
		get => _timeZone;
		set => _timeZone = value ?? throw new ArgumentNullException(nameof(value));
	}

	private static TimeZoneInfo Tz => TimeZone;

	/// <summary>
	/// Convert a date-time (UTC/Local/Unspecified) to application time zone, mimicking TimeConverter logic.
	/// </summary>
	public static DateTime ToAppTime(this DateTime dt)
		=> TimeZoneInfo.ConvertTime(dt, Tz);

	/// <summary>
	/// Convert DateTimeOffset to application time zone, mimicking TimeConverter logic.
	/// </summary>
	public static DateTimeOffset ToApp(DateTimeOffset dto)
		=> TimeZoneInfo.ConvertTime(dto, Tz);

	/// <summary>
	/// Convert UTC <see cref="DateTime"/> to application time zone.
	/// </summary>
	public static DateTime FromUtc(DateTime utc)
	{
		if (utc.Kind != DateTimeKind.Utc)
			utc = utc.UtcKind();

		return TimeZoneInfo.ConvertTimeFromUtc(utc, Tz);
	}

	/// <summary>
	/// Convert UTC <see cref="DateTimeOffset"/> to application time zone.
	/// </summary>
	public static DateTimeOffset FromUtc(DateTimeOffset utc)
		=> TimeZoneInfo.ConvertTime(utc, Tz);

	/// <summary>
	/// Format UTC <see cref="DateTime"/> with application timezone applied.
	/// </summary>
	public static string FormatFromUtc(DateTime utc, string format)
		=> FromUtc(utc).ToString(format);
}