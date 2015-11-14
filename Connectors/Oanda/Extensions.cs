namespace StockSharp.Oanda
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using RestSharp;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	static class Extensions
	{
		public static string ToOanda(this SecurityId securityId)
		{
			return securityId.SecurityCode.Replace('/', '_');
		}

		public static SecurityId ToSecurityId(this string instrument)
		{
			return new SecurityId
			{
				SecurityCode = instrument.Replace('_', '/'),
				BoardCode = ExchangeBoard.Ond.Code,
			};
		}

		private static readonly Dictionary<TimeSpan, string> _timeFrames = new Dictionary<TimeSpan, string>
		{
			{ TimeSpan.FromSeconds(5), "S5" },
			{ TimeSpan.FromSeconds(10), "S10" },
			{ TimeSpan.FromSeconds(15), "S15" },
			{ TimeSpan.FromSeconds(30), "S30" },
			{ TimeSpan.FromMinutes(1), "M1" },
			{ TimeSpan.FromMinutes(2), "M2" },
			{ TimeSpan.FromMinutes(3), "M3" },
			{ TimeSpan.FromMinutes(5), "M5" },
			{ TimeSpan.FromMinutes(10), "M10" },
			{ TimeSpan.FromMinutes(15), "M15" },
			{ TimeSpan.FromMinutes(30), "M30" },
			{ TimeSpan.FromHours(1), "H1" },
			{ TimeSpan.FromHours(2), "H2" },
			{ TimeSpan.FromHours(3), "H3" },
			{ TimeSpan.FromHours(4), "H4" },
			{ TimeSpan.FromHours(6), "H6" },
			{ TimeSpan.FromHours(8), "H8" },
			{ TimeSpan.FromHours(12), "H12" },
			{ TimeSpan.FromDays(1), "D" },
			{ TimeSpan.FromDays(7), "W" },
			{ TimeSpan.FromTicks(TimeHelper.TicksPerMonth), "M" },
		};

		public static string ToOanda(this TimeSpan timeFrame)
		{
			var name = _timeFrames.TryGetValue(timeFrame);

			if (name == null)
				throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, LocalizedStrings.Str3439);

			return name;
		}

		public static long ToOanda(this DateTimeOffset time)
		{
			if (time.IsDefault())
				return 0;

			return (time.UtcDateTime - TimeHelper.GregorianStart).TotalSeconds.To<long>();
		}

		public static DateTimeOffset FromOanda(this long time)
		{
			return (TimeHelper.GregorianStart + (time * TimeHelper.TicksPerMicrosecond).To<TimeSpan>()).ApplyTimeZone(TimeZoneInfo.Utc);
		}

		public static IRestRequest AddParameterIfNotNull<T>(this IRestRequest request, string paramName, T? value)
			where T : struct
		{
			return value == null ? request : request.AddParameter(paramName, value.Value);
		}
	}
}