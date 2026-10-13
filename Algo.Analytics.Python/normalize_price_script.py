import clr

# Add .NET references
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("Ecng.Drawing")

from Ecng.Drawing import DrawStyles
from System import TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from storage_extensions import *
from candle_extensions import *
from chart_extensions import *
from indicator_extensions import *

# The analytic script, normalize securities close prices and shows on same chart.
class normalize_price_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        cancellation_token.ThrowIfCancellationRequested()

        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        chart = create_chart(panel, datetime, float)

        for idx, security in enumerate(securities):
            cancellation_token.ThrowIfCancellationRequested()

            logs.LogInfo("Processing {0} of {1}: {2}...", idx + 1, len(securities), security)
            cancellation_token.ThrowIfCancellationRequested()

            series = {}

            # get candle storage
            candle_storage = get_candle_storage(storage, security, time_frame, drive, format)

            first_close = None
            prev_date = None

            candles = iter_candles(candle_storage, from_date, to_date, cancellation_token)
            try:
                for candle in candles:
                    cancellation_token.ThrowIfCancellationRequested()

                    # Log date change
                    curr_date = candle.OpenTime.Date
                    if curr_date != prev_date:
                        prev_date = curr_date
                        logs.LogInfo("  {0}...", curr_date.ToString("yyyy-MM-dd"))
                        cancellation_token.ThrowIfCancellationRequested()

                    if first_close is None:
                        first_close = candle.ClosePrice

                    # normalize close prices by dividing on first close
                    if first_close != 0:
                        series[candle.OpenTime] = candle.ClosePrice / first_close
            finally:
                candles.close()

            # draw series on chart
            times = sorted(series.keys())
            chart.Append(to_string_id(security), times, [series[time] for time in times])

        return Task.CompletedTask
