import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsScript

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
        security = Enumerable.First(securities)

        # Get candle storage
        candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)

        # Get available dates for the specified period
        dates = list(candle_storage.GetDates(from_date, to_date))

        if len(dates) == 0:
            logs.LogWarning("no data")
            return Task.CompletedTask

        # Grouping candles by opening time (hourly truncation) and summing their volumes
        candles = list(candle_storage.Load(from_date, to_date))
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
