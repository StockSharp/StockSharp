import clr

# Add .NET references
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo.Analytics")

from System import TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from storage_extensions import *

# The analytic script, calculating distribution of the volume by price levels.
class price_volume_script(IAnalyticsScript):
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

        # Script can process only 1 instrument
        security = securities[0]

        # Get candle storage
        candle_storage = get_tf_candle_storage(storage, security, time_frame, drive, format)

        # Get available dates for the specified period
        dates = get_dates(candle_storage, from_date, to_date)

        if len(dates) == 0:
            logs.LogWarning("no data")
            return Task.CompletedTask

        # Grouping candles by middle price and summing their volumes
        candles = load_tf_candles(candle_storage, from_date, to_date)
        rows_dict = {}
        for candle in candles:
            # Calculate middle price of the candle
            key = candle.LowPrice + candle.GetLength() / 2
            # Sum volumes for same price level
            rows_dict[key] = rows_dict.get(key, 0) + candle.TotalVolume

        # Draw on chart
        chart = panel.CreateChart[float, float]()
        chart.Append(security.ToStringId(), list(rows_dict.keys()), list(rows_dict.values()), DrawStyles.Histogram)

        return Task.CompletedTask
