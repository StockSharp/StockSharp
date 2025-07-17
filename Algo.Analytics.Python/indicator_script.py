import clr

# Add .NET references
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("Ecng.Drawing")

from Ecng.Drawing import DrawStyles
from System import TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from StockSharp.Algo.Indicators import ROC
from storage_extensions import *
from candle_extensions import *
from chart_extensions import *
from indicator_extensions import *

# The analytic script, using indicator ROC.
class indicator_script(IAnalyticsScript):
    def Run(self, logs, panel, securities, from_date, to_date, storage, drive, format, time_frame, cancellation_token):
        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        # creating 2 panes for candles and indicator series
        candle_chart = create_chart(panel, datetime, float)
        indicator_chart = create_chart(panel, datetime, float)

        for security in securities:
            # stop calculation if user cancel script execution
            if cancellation_token.IsCancellationRequested:
                break

            candles_series = {}
            indicator_series = {}

            # creating ROC
            roc = ROC()

            # get candle storage
            candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)

            for candle in load_tf_candles(candle_storage, from_date, to_date):
                # fill series
                candles_series[candle.OpenTime] = candle.ClosePrice
                indicator_series[candle.OpenTime] = to_decimal(process_candle(roc, candle))

            # draw series on chart
            candle_chart.Append(
                f"{security} (close)",
                list(candles_series.keys()),
                list(candles_series.values())
            )
            indicator_chart.Append(
                f"{security} (ROC)",
                list(indicator_series.keys()),
                list(indicator_series.values())
            )

        return Task.CompletedTask
