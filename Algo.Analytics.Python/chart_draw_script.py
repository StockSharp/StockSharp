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

        for idx, security in enumerate(securities):
            # Stop calculation if user cancels script execution
            if cancellation_token.IsCancellationRequested:
                break

            logs.LogInfo("Processing {0} of {1}: {2}...", idx + 1, len(securities), security)

            candles_series = {}
            vols_series = {}

            # Get candle storage for the current security
            candle_storage = get_candle_storage(storage, security, time_frame, drive, format)
            prev_date = None

            for candle in iter_candles(candle_storage, from_date, to_date, cancellation_token):
                # Log date change
                curr_date = candle.OpenTime.Date
                if curr_date != prev_date:
                    prev_date = curr_date
                    logs.LogInfo("  {0}...", curr_date.ToString("yyyy-MM-dd"))

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
