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
        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        chart = create_chart(panel, datetime, float)

        for idx, security in enumerate(securities):
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            logs.LogInfo("Processing {0} of {1}: {2}...", idx + 1, len(securities), security)

            series = {}

            # get candle storage
            candle_storage = get_candle_storage(storage, security, time_frame, drive, format)

            first_close = None
            prev_date = None

            for candle in iter_candles(candle_storage, from_date, to_date, cancellation_token):
                # Log date change
                curr_date = candle.OpenTime.Date
                if curr_date != prev_date:
                    prev_date = curr_date
                    logs.LogInfo("  {0}...", curr_date.ToString("yyyy-MM-dd"))

                if first_close is None:
                    first_close = candle.ClosePrice

                # normalize close prices by dividing on first close
                series[candle.OpenTime] = candle.ClosePrice / first_close

            # draw series on chart
            chart.Append(to_string_id(security), list(series.keys()), list(series.values()))

        return Task.CompletedTask
