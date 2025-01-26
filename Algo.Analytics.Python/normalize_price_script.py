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

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            series = {}

            # get candle storage
            candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)

            first_close = None

            for candle in load_tf_candles(candle_storage, from_date, to_date):
                if first_close is None:
                    first_close = candle.ClosePrice

                # normalize close prices by dividing on first close
                series[candle.OpenTime] = candle.ClosePrice / first_close

            # draw series on chart
            chart.Append(to_string_id(security), list(series.keys()), list(series.values()))

        return Task.CompletedTask
