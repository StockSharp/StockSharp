import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")
clr.AddReference("StockSharp.Messages")
clr.AddReference("Ecng.Drawing")

from Ecng.Drawing import DrawStyles
from System import TimeSpan
from System.Threading.Tasks import Task
from StockSharp.Algo.Analytics import IAnalyticsScript
from storage_extensions import *
from candle_extensions import *
from chart_extensions import *
from indicator_extensions import *

# The analytic script, calculating distribution of the biggest volume by hours.
class time_volume_script(IAnalyticsScript):
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

        # Grouping candles by opening time (hourly truncation) and summing their volumes
        candles = load_tf_candles(candle_storage, from_date, to_date)
        rows = {}
        for candle in candles:
            # Truncate TimeOfDay to the nearest hour
            time_of_day = candle.OpenTime.TimeOfDay
            truncated = TimeSpan.FromHours(int(time_of_day.TotalHours))
            # Sum volumes for each truncated hour
            rows[truncated] = rows.get(truncated, 0) + candle.TotalVolume

        # Put our calculations into grid
        grid = panel.CreateGrid("Time", "Volume")

        for key, value in rows.items():
            grid.SetRow(key, value)

        # Sorting by Volume column in descending order
        grid.SetSort("Volume", False)

        return Task.CompletedTask
