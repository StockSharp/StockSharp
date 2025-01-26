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

# The analytic script, shows chart drawing possibilities.
class chart_draw_script(IAnalyticsScript):
    def Run(
        self,
        logs,
        panel,
        securities,
        from_date,
        to_date,
        storage,
        drive,
        format,
        time_frame,
        cancellation_token
    ):
        # Check if there are no instruments
        if not securities:
            logs.LogWarning("No instruments.")
            return Task.CompletedTask

        # Create charts using the panel
        line_chart = create_chart(panel, datetime, float)
        histogram_chart = create_chart(panel, datetime, float)

        for security in securities:
            # Stop calculation if user cancels script execution
            if cancellation_token.IsCancellationRequested:
                break

            candles_series = {}
            vols_series = {}

            # Get candle storage for the current security
            candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)

            for candle in load_tf_candles(candle_storage, from_date, to_date):
                # Fill series with closing prices and volumes
                candles_series[candle.OpenTime] = candle.ClosePrice
                vols_series[candle.OpenTime] = candle.TotalVolume

            # Draw series on line chart with dashed line style
            line_chart.Append(
                f"{security} (close)",
                list(candles_series.keys()),
                list(candles_series.values()),
                DrawStyles.DashedLine
            )

            # Draw series on histogram chart with histogram style
            histogram_chart.Append(
                f"{security} (vol)",
                list(vols_series.keys()),
                list(vols_series.values()),
                DrawStyles.Histogram
            )

        return Task.CompletedTask
