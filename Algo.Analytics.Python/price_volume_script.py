import clr

# Add .NET references
clr.AddReference("StockSharp.Algo.Analytics")

from StockSharp.Algo.Analytics import IAnalyticsScript

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
        security = Enumerable.First(securities)

        # Get candle storage
        candle_storage = storage.GetTimeFrameCandleMessageStorage(security, time_frame, drive, format)

        # Get available dates for the specified period
        dates = list(candle_storage.GetDates(from_date, to_date))

        if len(dates) == 0:
            logs.LogWarning("no data")
            return Task.CompletedTask

        # Grouping candles by middle price and summing their volumes
        candles = list(candle_storage.Load(from_date, to_date))
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
